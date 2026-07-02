using CrashGuard.Core;
using Godot;
using HarmonyLib;
using JmcModLib.Utils;
using MegaCrit.Sts2.Core.Modding;
using System.Reflection;

namespace CrashGuard;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public static void Initialize()
    {
        JmcModLib.Core.ModRegistry.Register<MainFile>();
        CrashGuardSettings.NormalizeStoredValues();

        ModLogger.Info("======================================");
        ModLogger.Info($" {VersionInfo.Name} Mod 正在启动...");
        ModLogger.Info("======================================");

        Harmony harmony = new(VersionInfo.Name);
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        try
        {
            CrashGuard.Patches.BaseLibLogWindowCrashGuardPatch.ApplyWhenAvailable(harmony);
        }
        catch (Exception ex)
        {
            ModLogger.Error($"安装 BaseLib 日志窗口防崩补丁失败，已跳过该补丁以避免 MOD 加载失败：{ex}");
        }
        try
        {
            CrashGuard.Patches.CreatureHealNoOpGuardPatch.Apply(harmony);
        }
        catch (Exception ex)
        {
            ModLogger.Error($"安装无效治疗指令防崩补丁失败，已跳过该补丁以避免 MOD 加载失败：{ex}");
        }

        ModLogger.Info("Harmony 补丁已应用。");
    }
}
