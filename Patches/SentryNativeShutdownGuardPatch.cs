using System.Reflection;
using System.Reflection.Emit;
using CrashGuard.Core;
using Godot;
using HarmonyLib;
using JmcModLib.Utils;

namespace CrashGuard.Patches;

/// <summary>
/// 跳过 Sentry GDExtension 的原生 shutdown，避免游戏启动阶段禁用上报时触发原生崩溃。
/// </summary>
internal static class SentryNativeShutdownGuardPatch
{
    private const string SentryServiceTypeName = "MegaCrit.Sts2.Core.Debug.SentryService";
    private const string ShutdownMethodName = "Shutdown";
    private const string ShutdownFieldName = "_shutdownMethod";

    private static readonly MethodInfo? GodotObjectCallMethod = FindGodotObjectCallMethod();
    private static readonly MethodInfo? GuardedShutdownMethod =
        AccessTools.Method(typeof(SentryNativeShutdownGuardPatch), nameof(GuardedNativeShutdown));

    private static bool warnedSkip;
    private static bool warnedDisabled;
    private static bool warnedInvalidInstance;
    private static bool warnedTranspilerMissed;

    public static void Apply(Harmony harmony)
    {
        Type? sentryServiceType = AccessTools.TypeByName(SentryServiceTypeName);
        if (sentryServiceType == null)
        {
            ModLogger.Warn($"未找到 {SentryServiceTypeName}，跳过 Sentry 原生关闭防崩补丁。");
            return;
        }

        MethodInfo? shutdownMethod = AccessTools.Method(sentryServiceType, ShutdownMethodName, Type.EmptyTypes);
        if (shutdownMethod == null)
        {
            ModLogger.Warn($"未找到 {SentryServiceTypeName}.{ShutdownMethodName}()，跳过 Sentry 原生关闭防崩补丁。");
            return;
        }

        if (GodotObjectCallMethod == null || GuardedShutdownMethod == null)
        {
            ModLogger.Warn("GodotObject.Call(StringName, Variant[]) 签名已变化，跳过 Sentry 原生关闭防崩补丁。");
            return;
        }

        HarmonyMethod transpiler = new(typeof(SentryNativeShutdownGuardPatch), nameof(ReplaceNativeShutdownCall))
        {
            priority = Priority.Last,
            after =
            [
                "com.ritsukage.sts2-RitsuLib",
                "STS2RitsuLib",
                "STS2-RitsuLib",
                "RitsuLib"
            ]
        };

        harmony.Patch(shutdownMethod, transpiler: transpiler);
        ModLogger.Info("已安装 Sentry 原生关闭防崩补丁。");
    }

    private static IEnumerable<CodeInstruction> ReplaceNativeShutdownCall(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = instructions.ToList();
        List<int> targets = [];

        for (int index = 0; index < codes.Count; index++)
        {
            if (IsGodotObjectCall(codes[index]) && HasLoadedShutdownMethodName(codes, index))
            {
                targets.Add(index);
            }
        }

        if (targets.Count != 1)
        {
            WarnTranspilerMissed(targets.Count);
            return codes;
        }

        CodeInstruction call = codes[targets[0]];
        call.opcode = OpCodes.Call;
        call.operand = GuardedShutdownMethod;
        return codes;
    }

    private static Variant GuardedNativeShutdown(GodotObject? instance, StringName method, Variant[]? args)
    {
        if (!CrashGuardSettings.ShouldGuardSentryNativeShutdown)
        {
            WarnDisabledOnce();
            return CallOriginalIfPossible(instance, method, args);
        }

        WarnSkipOnce(method);
        return default;
    }

    private static Variant CallOriginalIfPossible(GodotObject? instance, StringName method, Variant[]? args)
    {
        if (instance == null || !GodotObject.IsInstanceValid(instance))
        {
            WarnInvalidInstanceOnce(method);
            return default;
        }

        return instance.Call(method, args ?? []);
    }

    private static bool IsGodotObjectCall(CodeInstruction instruction)
    {
        return GodotObjectCallMethod != null && instruction.Calls(GodotObjectCallMethod);
    }

    private static bool HasLoadedShutdownMethodName(IReadOnlyList<CodeInstruction> codes, int callIndex)
    {
        int firstCandidate = Math.Max(0, callIndex - 6);
        for (int index = callIndex - 1; index >= firstCandidate; index--)
        {
            if (codes[index].opcode == OpCodes.Ldsfld
                && codes[index].operand is FieldInfo field
                && field.Name == ShutdownFieldName
                && field.DeclaringType?.FullName == SentryServiceTypeName)
            {
                return true;
            }
        }

        return false;
    }

    private static MethodInfo? FindGodotObjectCallMethod()
    {
        return typeof(GodotObject)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(static method =>
            {
                if (method.Name != nameof(GodotObject.Call) || method.ReturnType != typeof(Variant))
                {
                    return false;
                }

                ParameterInfo[] parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[0].ParameterType == typeof(StringName)
                    && parameters[1].ParameterType == typeof(Variant[]);
            });
    }

    private static void WarnTranspilerMissed(int matchCount)
    {
        if (warnedTranspilerMissed)
        {
            return;
        }

        warnedTranspilerMissed = true;
        if (matchCount == 0)
        {
            ModLogger.Warn("未在 SentryService.Shutdown 中找到原生 Sentry shutdown 调用，可能已由游戏或其他 MOD 修复/接管，跳过替换。");
            return;
        }

        ModLogger.Warn($"在 SentryService.Shutdown 中找到 {matchCount} 个疑似原生 Sentry shutdown 调用，为避免误改已跳过替换。");
    }

    private static void WarnSkipOnce(StringName method)
    {
        if (warnedSkip)
        {
            return;
        }

        warnedSkip = true;
        ModLogger.Warn($"已跳过 Sentry GDExtension 原生 {DescribeMethod(method)} 调用，避免禁用上报流程触发原生崩溃。");
    }

    private static void WarnDisabledOnce()
    {
        if (warnedDisabled)
        {
            return;
        }

        warnedDisabled = true;
        ModLogger.Warn("Sentry 原生关闭防崩补丁已在配置中关闭，原生 shutdown 调用将尽量按游戏原逻辑放行。");
    }

    private static void WarnInvalidInstanceOnce(StringName method)
    {
        if (warnedInvalidInstance)
        {
            return;
        }

        warnedInvalidInstance = true;
        ModLogger.Warn($"Sentry 原生 {DescribeMethod(method)} 调用目标已失效，已跳过该调用以避免空对象或原生对象失效崩溃。");
    }

    private static string DescribeMethod(StringName method)
    {
        try
        {
            return method.ToString();
        }
        catch (Exception ex)
        {
            return $"<读取方法名失败：{ex.Message}>";
        }
    }
}
