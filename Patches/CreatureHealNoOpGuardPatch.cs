using System.Reflection;
using CrashGuard.Core;
using HarmonyLib;
using JmcModLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace CrashGuard.Patches;

/// <summary>
/// 跳过空目标和 0/负数治疗，避免第三方治疗补丁链在无意义治疗上触发闪退。
/// </summary>
internal static class CreatureHealNoOpGuardPatch
{
    private const string HextechHarmonyId = "Natsuki.HextechRunes";
    private const string BaseLibHarmonyId = "BaseLib";
    private const string ExtraTftHealHarmonyId = "ExtraTFTCombat.ExtraCombatRuntimeAdapter";

    private static readonly Type[] HealParameterTypes =
    [
        typeof(Creature),
        typeof(decimal),
        typeof(bool)
    ];

    private static bool warnedSkippedHeal;
    private static bool warnedDisabled;
    private static bool warnedSuppressedHealException;

    public static void Apply(Harmony harmony)
    {
        MethodInfo? healMethod = AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.Heal), HealParameterTypes);
        if (healMethod == null)
        {
            ModLogger.Warn("未找到 CreatureCmd.Heal(Creature, decimal, bool)，跳过无效治疗指令防崩补丁。");
            return;
        }
        if (healMethod.ReturnType != typeof(Task))
        {
            ModLogger.Warn($"CreatureCmd.Heal 返回类型已变为 {healMethod.ReturnType.FullName}，跳过无效治疗指令防崩补丁。");
            return;
        }

        HarmonyMethod prefix = new(typeof(CreatureHealNoOpGuardPatch), nameof(SkipNoOpHeal))
        {
            priority = Priority.First,
            before = [HextechHarmonyId, BaseLibHarmonyId, ExtraTftHealHarmonyId]
        };
        HarmonyMethod finalizer = new(typeof(CreatureHealNoOpGuardPatch), nameof(SuppressNoOpHealException))
        {
            priority = Priority.Last
        };

        harmony.Patch(healMethod, prefix, finalizer: finalizer);
        ModLogger.Info("已安装无效治疗指令防崩补丁。");
    }

    private static bool SkipNoOpHeal(Creature? creature, decimal amount, ref Task __result)
    {
        if (!CrashGuardSettings.ShouldSkipNoOpCreatureHeal)
        {
            WarnDisabledOnce();
            return true;
        }

        if (creature != null && amount > 0m)
        {
            return true;
        }

        __result = Task.CompletedTask;
        WarnSkippedHealOnce(creature, amount);
        return false;
    }

    private static Exception? SuppressNoOpHealException(Creature? creature, decimal amount, ref Task __result, Exception? __exception)
    {
        if (__exception == null)
        {
            return null;
        }

        if (!ShouldGuardHeal(creature, amount))
        {
            return __exception;
        }

        __result = Task.CompletedTask;
        WarnSuppressedHealExceptionOnce(creature, amount, __exception);
        return null;
    }

    private static void WarnDisabledOnce()
    {
        if (warnedDisabled)
        {
            return;
        }

        warnedDisabled = true;
        ModLogger.Warn("无效治疗指令防崩补丁已在配置中关闭，CreatureCmd.Heal 将完全放行。");
    }

    private static void WarnSkippedHealOnce(Creature? creature, decimal amount)
    {
        if (warnedSkippedHeal)
        {
            return;
        }

        warnedSkippedHeal = true;
        string target = creature == null ? "空目标" : DescribeCreature(creature);
        ModLogger.Warn($"已跳过一次无效治疗指令以避免补丁链闪退：目标={target}，治疗量={amount}。");
    }

    private static void WarnSuppressedHealExceptionOnce(Creature? creature, decimal amount, Exception exception)
    {
        if (warnedSuppressedHealException)
        {
            return;
        }

        warnedSuppressedHealException = true;
        string target = creature == null ? "空目标" : DescribeCreature(creature);
        ModLogger.Warn($"已吞掉一次无效治疗指令补丁链异常：目标={target}，治疗量={amount}，异常={exception.GetType().Name}: {exception.Message}");
    }

    private static bool ShouldGuardHeal(Creature? creature, decimal amount)
    {
        return CrashGuardSettings.ShouldSkipNoOpCreatureHeal
            && (creature == null || amount <= 0m);
    }

    private static string DescribeCreature(Creature creature)
    {
        try
        {
            string id = creature.ModelId.ToString();
            return $"{id} HP={creature.CurrentHp}/{creature.MaxHp}";
        }
        catch (Exception ex)
        {
            return $"读取目标信息失败：{ex.Message}";
        }
    }
}
