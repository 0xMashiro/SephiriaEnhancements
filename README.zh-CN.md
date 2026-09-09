<p align="center">
  <img src="./assets/readme-header.webp" alt="Sephiria Enhancements" />
</p>

<p align="center">
  <a href="./README.md">English</a> · <strong>简体中文</strong>
</p>

<p align="center">
  <strong><a href="https://www.nexusmods.com/sephiria/mods/24">前往 Nexus Mods（N 网）下载</a></strong><br />
  本 MOD 免费获取与使用 · 基于 <a href="./LICENSE">MIT 许可证</a>开源
</p>

# Sephiria Enhancements / Sephiria 增强

为 Sephiria 添加战斗信息、键盘操作、背包整理和探索工具。
使用游戏自带的 AddOns 系统，无需 BepInEx。

> [!TIP]
> **你的反馈与分享很重要**
>
> 我的主要精力会放在这个 Mod 的开发与维护上，留给宣传的时间比较有限。
> 如果它让你玩得更舒服，欢迎推荐给朋友，或在社区分享使用心得、文章和视频，让更多玩家有机会发现它。
> **不需要专门制作内容，一句推荐、一张截图也很有帮助。**
>
> 我大部分时间都在单人游玩，尤其需要社区反馈来帮助发现联机 Bug，以及与其他 Mod 一起使用时的问题。
> 如果遇到异常，欢迎通过 [GitHub Issues](https://github.com/0xMashiro/SephiriaEnhancements/issues) 反馈。
> 不必准备完整的报告，先描述你遇到的情况就好；[问题反馈](#问题反馈)中列出了有助于排查的信息。
>
> **知道它帮到了你，以及哪些地方还需要改进，是我持续维护的重要动力。**
> 谢谢你的使用、分享，以及帮助它变得更好。

**当前为 Beta 测试版，背包整理仍是实验功能。**

整理也会主动提升已支持的位置相关神器效果，例如相邻法术修正和同行同伴效果。
你明确指定的优先级优先；默认保留已有加成，并限制升级带来的额外耗蓝。
在「特殊效果」中，可选择是否重新分配加成、是否允许升级后增加耗蓝。
搜索有预算限制，不保证找到最优布局或最高战斗伤害。

## 功能

- **战斗显示**：伤害、DPS、战后统计、连续命中、资源数值和敌友轮廓。
- **操作增强**：键盘浏览菜单、选择奖励，自动索敌与手动锁定。
- **地图探索**：本层地图、人物／地点名单、NPC 跟踪，以及 **75%–200%** 镜头距离。
- **背包整理**：按优先级安排神器，标出符合预设收藏连招的奖励。
- **单人和联机选项**：战斗伙伴、失败后重试、中途加入／重连，以及 **1–4 人**规则预设。

大部分显示和操作增强默认开启。失败后重试、显示隐藏房间、鼠标辅助瞄准和战斗伙伴默认**关闭**；
多人规则默认为**原版**，镜头距离为 **100%**。

## 安装

1. 退出 Sephiria。
2. 从最新的 [GitHub Release](https://github.com/0xMashiro/SephiriaEnhancements/releases) 下载 ZIP 和 `SHA256SUMS.txt`，包括标为 **Pre-release** 的测试版。
3. [验证下载文件](#验证发布包)，将完整安装包解压到包含 `Sephiria.exe` 的文件夹，遇到 `AddOns` 文件夹时选择合并。
4. 启动游戏，**先载入存档**，再打开**选项 → 游戏性 → SEPHIRIA ENHANCEMENTS · by 0xMashiro**。

受游戏当前 AddOns 实现影响，Mod 设置入口仅在载入存档、进入游戏后出现。
**主菜单中看不到此选项是正常现象，载入存档后即可找到。**

文件应直接放在 `AddOns\SephiriaEnhancements` 中，不要多套一层文件夹。

## 常用操作

在游戏键盘或手柄设置的 **SEPHIRIA ENHANCEMENTS 快捷键**中改键。
下表为默认操作，改键后以游戏内提示为准。

| 操作 | 默认方式 |
| --- | --- |
| 锁定／切换目标；解除锁定 | 短按／长按 `L` 或鼠标中键 |
| 为背包中的技能切换自动施法 | 中键点击技能，或选中后按 `L`；手柄使用确认键 |
| 显示／隐藏本层地图 | `M` |
| 脱战后查看统计 | 短按 `F7`，或暂停菜单中的**查看统计** |
| 隐藏／恢复伤害显示 | 长按 `F7` 半秒；隐藏时仍会记录 |
| 切换战报页面 | `Q` / `E` |
| 收起战报 | `L`、鼠标中键、`Esc` 或手柄 Start/Menu |
| 整理已打开的背包 | `F8`，或整理按钮 |
| 切换设置页签 | `Tab` / `Shift+Tab` |

已有自动战报时，短按 `F7` 会先收起它。收起战报后，再按 `Esc` 或 Start/Menu 打开暂停菜单。
手柄目标锁定默认未绑定，需要自行设置。

自动施法默认全部关闭。技能图标中的 **A** 表示已开启，键盘／手柄设置中的对应按键行也有开关。
正常消耗魔力与次数，沿用当前瞄准，没有敌人时也会释放；菜单中暂停。
更换快捷键时，选择跟随同一件神器；失去神器、更换角色或重新加载世界后清除。
仅支持已绑定快捷键的单次释放魔法，不包含武器攻击和主动连击效果技能。
背包技能区和「自定义武器输入」支持方向导航，连击效果条目和武器输入开关使用整行选中框。
`Tab` / `Shift+Tab` 在主背包、药水栏、子背包、连击效果、技能和自定义武器输入之间切换，跳过不可用区域。
各区域记住本次打开背包期间选中的格子；返回时，只要该格子仍可操作，就恢复原位置。关闭背包后清除记忆。

## 游戏内帮助

悬停或选中 Mod 设置，即可查看功能、操作方式和限制。背包、地图和战报界面也会显示操作提示，
按键跟随当前绑定；需要改键时，前往游戏的键盘或手柄设置。

Mod 的**基础功能**设置中，**地图增强**统一控制地图工具、本层地图叠层、NPC 跟踪和隐藏房间选项。
默认开启；关闭后使用游戏原有地图，并保留隐藏房间偏好。

**背包整理仍是实验功能；失败后重试会将道具和进度回退到所选时间点。** 使用前请阅读对应设置的说明。

## 问题反馈

欢迎在 [GitHub Issues](https://github.com/0xMashiro/SephiriaEnhancements/issues) 用中文或英文反馈。
如果方便，请附上游戏和 Mod 版本、复现步骤、预期与实际结果，以及单人／房主／客户端身份、其他 Mod 和相关设置。
信息不全也欢迎先描述你遇到的情况。

<details>
<summary>附上日志（Windows）</summary>

日志会自动生成。遇到问题后尽早复制，避免再次启动游戏覆盖旧记录。

| 文件 | 位置 |
| --- | --- |
| 所有 `support*.log` | 系统**文档** → `Saved Games\Sephiria\Mods\SephiriaEnhancements\Logs\Support` |
| 加载失败或崩溃时的 `Player.log`；重启后可附 `Player-prev.log` | `%USERPROFILE%\AppData\LocalLow\TEAMHORAY\Sephiria` |

按 **Win+R** 输入 `shell:Personal` 可打开系统文档，包括已移到 OneDrive 的情况。
检查并移除私人信息后，将日志压缩为 ZIP 附到 Issue。日志不会自动上传，无需提供存档文件夹或游戏文件。
**找不到日志也可以反馈。**

</details>

## 验证发布包

<details>
<summary>检查下载文件是否与发布版本一致</summary>

把 ZIP 和 `SHA256SUMS.txt` 放在同一个文件夹，在该文件夹打开 PowerShell，运行：

```powershell
$zip = Get-ChildItem -LiteralPath . -Filter 'SephiriaEnhancements-*.zip' |
    Select-Object -First 1
Get-FileHash -LiteralPath $zip.FullName -Algorithm SHA256
Get-Content -LiteralPath .\SHA256SUMS.txt
```

确认文件名和算出的校验值与校验文件一致。它能确认下载内容没有变化，不代表代码安全性的保证。

</details>

## 从源码构建

<details>
<summary>供代码贡献者使用，安装游玩无需这些步骤</summary>

需要 PowerShell 7、.NET 10 SDK，以及合法安装的 Sephiria。不要求特定的 SDK 补丁版本。
在仓库根目录运行以下命令，并把示例游戏目录换成你自己的目录。

运行检查：

```powershell
& .\scripts\test.ps1
```

构建开发版，输出到 `artifacts/build/Development/`：

```powershell
& .\scripts\build.ps1 -GameDir "C:\Games\Sephiria" -Configuration Debug -DeveloperTools
```

构建并打包正式版，在 `artifacts/` 生成 ZIP 和 `SHA256SUMS.txt`：

```powershell
& .\scripts\package.ps1 -GameDir "C:\Games\Sephiria"
```

贡献代码时，请保持改动聚焦，统一代码和玩家文案中的术语，并补充相关检查。
请勿提交游戏文件、反编译代码、日志、个人路径或构建产物。

</details>

## 许可证

本 Mod 免费获取与使用，基于 [MIT License](./LICENSE) 开源。
Copyright (c) 2026 0xMashiro。
