<p align="center">
  <a href="README.md"><img alt="中文" src=".github/badges/language-zh.svg"></a>
  <a href="README_en.md"><img alt="English" src=".github/badges/language-en.svg"></a>
  <a href="CHANGELOG.md"><img alt="更新日志" src=".github/badges/changelog-zh.svg"></a>
  <a href="https://github.com/JMC-Mods/SlayTheSpire2_CrashGuard/releases"><img alt="Releases" src=".github/badges/releases.svg"></a>
<!-- code-stats:start -->
  <a href="https://github.com/JMC-Mods/SlayTheSpire2_CrashGuard/actions/workflows/code-lines.yml"><img alt="C# 行数" src=".github/badges/code-lines-csharp.svg"></a>
  <a href="https://github.com/JMC-Mods/SlayTheSpire2_CrashGuard/actions/workflows/code-lines.yml"><img alt="JSON 行数" src=".github/badges/code-lines-json.svg"></a>
  <a href="https://github.com/JMC-Mods/SlayTheSpire2_CrashGuard/actions/workflows/code-lines.yml"><img alt="YAML 行数" src=".github/badges/code-lines-yaml.svg"></a>
  <a href="https://github.com/JMC-Mods/SlayTheSpire2_CrashGuard/actions/workflows/code-lines.yml"><img alt="MSBuild script 行数" src=".github/badges/code-lines-msbuild-script.svg"></a>
  <a href="https://github.com/JMC-Mods/SlayTheSpire2_CrashGuard/actions/workflows/code-lines.yml"><img alt="总代码行数" src=".github/badges/code-lines-total.svg"></a>
  <a href="https://github.com/JMC-Mods/SlayTheSpire2_CrashGuard/actions/workflows/code-lines.yml"><img alt="累计新增行数" src=".github/badges/code-lines-added.svg"></a>
  <a href="https://github.com/JMC-Mods/SlayTheSpire2_CrashGuard/actions/workflows/code-lines.yml"><img alt="累计删除行数" src=".github/badges/code-lines-deleted.svg"></a>
<!-- code-stats:end -->
</p>

# BaseLib 闪退的尝试性修复
---
## 🧠 1. 简介
---
最近有群友向我反馈游戏超高频繁闪退，初步调试定位发现和BaseLib的日志的裁剪部分有关，更上游的可能与Godot的控件有关，于是做了这么一个MOD尝试修复这个闪退问题，默认行为是完全干掉BaseLib的日志刷新（代价是它的日志窗口无法正常使用，作为代替，你可以下载我的另一个日志MOD），在设置中可以调节为尝试对刷新的最小化修复（代价是日志不会裁剪、会变得很卡），我个人是完全无法复现闪退的，只有这么一个样本，欢迎使用了这个依旧闪退的用户来进群或者Discord为我提供更多的样本。

---
最近有群友反馈先古之民处卡死，初步定位发现可能是由于海克斯符文MOD的某些不当实现被某些其它MOD诱发，对这一类的实现做了修复性patch后该群友成功运行，理论上可能规避一系列BUG，不过某些极为罕见的情况，某些MOD作者可能出于某些原因会刻意使用不当的函数实现功能（更确切地、使用回血函数而非伤害函数来实现伤害功能），可能会对这种产生影响

---

最近有群友反馈加载MOD百分之百闪退，初步定位发现可能是由于游戏在加载MOD时跳过sentry在某些环境下可能导致游戏原生崩溃，对此进行修复后该群友顺利进入游戏；同时，RitsuLib做了类似的修复，理论上本MOD不会与之有冲突

---

本MOD的所有功能理论上都是向后兼容、静默、尽量不影响其它行为的，并会持续更新，设计上、即便你没使用对应的被修复的MOD或者没遇到问题理论上不会受到任何影响，可以放心安装，即便历史上修复的某些问题被他人修复，也会尽量安全回退。同时也欢迎各位为我提供游戏不正常运行的样本，我来帮忙修复。

[演示视频（B站）](待定)

[Github仓库](https://github.com/JMC-Mods/SlayTheSpire2_CrashGuard)
## ⚙️ 2. 功能
- 如上所述
 
## 🔔 3. 提醒
- **本模组强依赖于模组[JmcModLib](https://github.com/JMC-Mods/SlayTheSpire2_JmcModLib/releases)**
- 在创意工坊中只启用BaseLib闪退是已探明的游戏本身的Bug，不属于本MOD修复范畴

## 🧩 4. 兼容性
- 由于游戏处于EA阶段，可能会随着游戏版本更新而失效
- 会导致BaseLib的日志窗口不可用

## 🧭 5. TODO

**如果你喜欢这个 Mod 的话，希望可以点一个star~**
