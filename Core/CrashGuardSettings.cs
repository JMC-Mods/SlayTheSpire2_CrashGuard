using System.Reflection;
using JmcModLib.Config;
using JmcModLib.Config.UI;

namespace CrashGuard.Core;

public static class CrashGuardSettings
{
    public const string DisableRefreshMode = "禁用 BaseLib 日志窗口刷新";
    public const string PreserveRefreshMode = "保留刷新，仅跳过旧日志裁剪（实验）";

    private const string LogWindowGroup = "BaseLib 日志窗口";
    private const string CommandGuardGroup = "指令防崩";
    private const string ModCompatibilityGroup = "MOD 兼容修复";
    private const string LogWindowProtectionModeKey = "baselib_log_window.mode";
    private const string LogWindowProtectionConfigKey = $"{LogWindowGroup}.{LogWindowProtectionModeKey}";

    [UIDropdown(DisableRefreshMode, PreserveRefreshMode)]
    [Config(
        "BaseLib 日志窗口保护模式",
        group: LogWindowGroup,
        Description = "禁用刷新最稳；保留刷新会让 BaseLib 日志窗口继续显示新日志，但跳过触发闪退的 RichTextLabel.RemoveParagraph 旧日志裁剪。",
        Key = LogWindowProtectionModeKey,
        Order = 10)]
    public static string LogWindowProtectionMode = DisableRefreshMode;

    [UIToggle]
    [Config(
        "跳过无效治疗指令",
        group: CommandGuardGroup,
        Description = "跳过 CreatureCmd.Heal 的空目标或 0/负数治疗，避免其他 MOD 的治疗补丁链处理无意义治疗时闪退。关闭后完全放行原治疗逻辑。",
        Key = "creature_heal.skip_noop",
        Order = 10)]
    public static bool SkipNoOpCreatureHeal = true;

    [UIToggle]
    [Config(
        "跳过 Sentry 原生关闭",
        group: ModCompatibilityGroup,
        Description = "在游戏因检测到 MOD 而关闭上报时，跳过 Sentry GDExtension 的原生 shutdown 调用，规避部分版本在启动阶段的原生闪退。关闭后尽量放行游戏原逻辑。",
        Key = "sentry.native_shutdown_guard",
        Order = 10)]
    public static bool GuardSentryNativeShutdown = true;

    public static void NormalizeStoredValues()
    {
        string normalizedMode = NormalizeLogWindowProtectionMode(LogWindowProtectionMode);
        if (LogWindowProtectionMode == normalizedMode)
        {
            return;
        }

        if (!ConfigManager.SetValue(LogWindowProtectionConfigKey, normalizedMode, Assembly.GetExecutingAssembly()))
        {
            LogWindowProtectionMode = normalizedMode;
        }
    }

    public static bool ShouldDisableBaseLibLogWindowRefresh =>
        NormalizeLogWindowProtectionMode(LogWindowProtectionMode) == DisableRefreshMode;

    public static bool ShouldSkipBaseLibLogWindowParagraphTrim =>
        NormalizeLogWindowProtectionMode(LogWindowProtectionMode) == PreserveRefreshMode;

    public static string CurrentLogWindowProtectionMode =>
        NormalizeLogWindowProtectionMode(LogWindowProtectionMode);

    public static bool ShouldSkipNoOpCreatureHeal => SkipNoOpCreatureHeal;

    public static bool ShouldGuardSentryNativeShutdown => GuardSentryNativeShutdown;

    private static string NormalizeLogWindowProtectionMode(string? mode)
    {
        return mode switch
        {
            PreserveRefreshMode => PreserveRefreshMode,
            DisableRefreshMode => DisableRefreshMode,
            _ => DisableRefreshMode
        };
    }
}
