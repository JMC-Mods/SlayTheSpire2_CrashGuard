using System.Reflection;
using System.Reflection.Emit;
using CrashGuard.Core;
using Godot;
using HarmonyLib;
using JmcModLib.Utils;

namespace CrashGuard.Patches;

/// <summary>
/// 防护 BaseLib 内置日志窗口的动态文本刷新，避免 RichTextLabel 原生调用在特定环境中触发崩溃。
/// </summary>
internal static class BaseLibLogWindowCrashGuardPatch
{
    private const string BaseLibAssemblyName = "BaseLib";
    private const string LogWindowTypeName = "BaseLib.BaseLibScenes.NLogWindow";

    private static readonly string[] MethodsToSkip =
    [
        "OpenOnErr",
        "RegenText",
        "Refresh",
        "UpdateText"
    ];

    private static readonly object SyncRoot = new();
    private static readonly MethodInfo? RemoveParagraphMethod =
        AccessTools.Method(typeof(RichTextLabel), nameof(RichTextLabel.RemoveParagraph), [typeof(int)]);
    private static readonly MethodInfo SafeRemoveParagraphMethod =
        AccessTools.Method(typeof(BaseLibLogWindowCrashGuardPatch), nameof(SafeRemoveParagraph))!;

    private static bool applied;
    private static bool warnedRefreshSkipped;
    private static bool warnedParagraphTrimSkipped;
    private static bool warnedTranspilerMissed;

    public static void ApplyWhenAvailable(Harmony harmony)
    {
        if (TryApply(harmony))
        {
            return;
        }

        AppDomain.CurrentDomain.AssemblyLoad += (_, args) =>
        {
            if (args.LoadedAssembly.GetName().Name == BaseLibAssemblyName)
            {
                TryApply(harmony);
            }
        };
    }

    private static bool TryApply(Harmony harmony)
    {
        lock (SyncRoot)
        {
            if (applied)
            {
                return true;
            }

            Type? logWindowType = AccessTools.TypeByName(LogWindowTypeName);
            if (logWindowType == null)
            {
                return false;
            }

            HarmonyMethod prefix = new(typeof(BaseLibLogWindowCrashGuardPatch), nameof(SkipLogWindowRefresh));
            foreach (string methodName in MethodsToSkip)
            {
                MethodInfo? method = AccessTools.Method(logWindowType, methodName);
                if (method == null)
                {
                    ModLogger.Warn($"未找到 BaseLib 日志窗口方法 {LogWindowTypeName}.{methodName}，跳过该方法补丁。");
                    continue;
                }

                HarmonyMethod? transpiler = methodName == "UpdateText"
                    ? new HarmonyMethod(typeof(BaseLibLogWindowCrashGuardPatch), nameof(ReplaceDangerousParagraphRemoval))
                    : null;
                harmony.Patch(method, prefix, transpiler: transpiler);
            }

            applied = true;
            ModLogger.Info($"已安装 BaseLib 内置日志窗口防崩补丁。当前模式：{CrashGuardSettings.CurrentLogWindowProtectionMode}");
            return true;
        }
    }

    private static bool SkipLogWindowRefresh(MethodBase __originalMethod)
    {
        if (!CrashGuardSettings.ShouldDisableBaseLibLogWindowRefresh)
        {
            return true;
        }

        if (!warnedRefreshSkipped)
        {
            warnedRefreshSkipped = true;
            ModLogger.Warn($"已跳过 BaseLib 日志窗口方法 {__originalMethod.Name}，避免日志窗口刷新触发原生崩溃。");
        }

        return false;
    }

    private static IEnumerable<CodeInstruction> ReplaceDangerousParagraphRemoval(IEnumerable<CodeInstruction> instructions)
    {
        int replaced = 0;
        MethodInfo? removeParagraphMethod = RemoveParagraphMethod;

        foreach (CodeInstruction instruction in instructions)
        {
            if (removeParagraphMethod != null && instruction.Calls(removeParagraphMethod))
            {
                replaced++;
                instruction.opcode = OpCodes.Call;
                instruction.operand = SafeRemoveParagraphMethod;
            }

            yield return instruction;
        }

        if (replaced == 0 && !warnedTranspilerMissed)
        {
            warnedTranspilerMissed = true;
            ModLogger.Warn("未在 BaseLib 日志窗口 UpdateText 中找到 RichTextLabel.RemoveParagraph 调用，保留刷新模式可能无法规避该崩溃。");
        }
    }

    private static void SafeRemoveParagraph(RichTextLabel label, int paragraph)
    {
        if (!CrashGuardSettings.ShouldSkipBaseLibLogWindowParagraphTrim)
        {
            label.RemoveParagraph(paragraph);
            return;
        }

        if (!warnedParagraphTrimSkipped)
        {
            warnedParagraphTrimSkipped = true;
            ModLogger.Warn("已跳过 BaseLib 日志窗口旧日志裁剪，保留刷新但避免 RichTextLabel.RemoveParagraph 原生崩溃。");
        }
    }
}
