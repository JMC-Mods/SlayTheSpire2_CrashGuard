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
        CrashGuard.Patches.BaseLibLogWindowCrashGuardPatch.ApplyWhenAvailable(harmony);
        ModLogger.Info("Harmony 补丁已应用。");
    }
}
