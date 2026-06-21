using Godot;
using HarmonyLib;
using JmcModLib.Utils;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace CrashGuard.Patches;

/// <summary>
/// 防止 BaseLib 将普通 Node2D 商人素材转换为 NMerchantCharacter 后触发 SpineSprite 契约异常。
/// </summary>
[HarmonyPatch(typeof(NMerchantCharacter))]
internal static class MerchantCharacterSpineGuardPatch
{
    private const string SpineSpriteClassName = "SpineSprite";

    private static readonly HashSet<ulong> WarnedInvalidMerchants = [];

    [HarmonyPrefix]
    [HarmonyPatch(nameof(NMerchantCharacter._Ready))]
    private static bool ReadyPrefix(NMerchantCharacter __instance)
    {
        if (HasValidSpineSprite(__instance))
        {
            return true;
        }

        WarnOnce(__instance, "初始化");
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(NMerchantCharacter.PlayAnimation))]
    [HarmonyAfter("BaseLib")]
    private static bool PlayAnimationPrefix(NMerchantCharacter __instance)
    {
        if (HasValidSpineSprite(__instance))
        {
            return true;
        }

        WarnOnce(__instance, "播放动画");
        return false;
    }

    private static bool HasValidSpineSprite(NMerchantCharacter merchant)
    {
        try
        {
            return merchant.GetChildCount() > 0
                && merchant.GetChild(0) is { } firstChild
                && GodotObject.IsInstanceValid(firstChild)
                && firstChild.GetClass() == SpineSpriteClassName;
        }
        catch (Exception ex)
        {
            ModLogger.Warn($"商人 Spine 结构检查失败，已跳过原版商人动画逻辑以避免闪退：{ex.Message}");
            return false;
        }
    }

    private static void WarnOnce(NMerchantCharacter merchant, string action)
    {
        try
        {
            ulong instanceId = merchant.GetInstanceId();
            if (!WarnedInvalidMerchants.Add(instanceId))
            {
                return;
            }

            ModLogger.Warn($"检测到非 SpineSprite 的商人动画节点，已跳过商人{action}动画以避免 BaseLib 场景转换闪退。{DescribeMerchant(merchant)}");
        }
        catch (Exception ex)
        {
            ModLogger.Warn($"记录商人动画防崩日志失败：{ex.Message}");
        }
    }

    private static string DescribeMerchant(NMerchantCharacter merchant)
    {
        try
        {
            if (merchant.GetChildCount() <= 0)
            {
                return "商人节点没有子节点。";
            }

            Node firstChild = merchant.GetChild(0);
            return $"第一个子节点：Name={firstChild.Name}, Class={firstChild.GetClass()}, Path={firstChild.GetPath()}。";
        }
        catch (Exception ex)
        {
            return $"读取商人节点信息失败：{ex.Message}";
        }
    }
}
