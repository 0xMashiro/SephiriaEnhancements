<p align="center">
  <img src="./assets/readme-header.webp" alt="Sephiria Enhancements" />
</p>

<p align="center">
  <a href="./README.md">English</a> · <strong>简体中文</strong>
</p>

<p align="center">
  <a href="https://www.nexusmods.com/sephiria/mods/24">Nexus Mods（N 网）</a> ·
  <a href="https://github.com/0xMashiro/SephiriaEnhancements/releases">GitHub Releases</a>
</p>

# Sephiria Enhancements / Sephiria 增强

为 Sephiria 添加战斗信息、键盘操作、背包整理和探索工具。
使用游戏自带的 AddOns 系统，无需 BepInEx。

## 功能

- **战斗显示**：伤害、DPS、战后统计、连续命中、资源数值和敌友轮廓。
- **效果属性**：在角色菜单查看已支持效果的当前数值与加成。
- **操作增强**：键盘浏览菜单、选择奖励、自动索敌与手动锁定，以及可选的魔法自动施法。
- **地图探索**：地图工具、NPC 跟踪、允许时快速移动，以及镜头距离调整。
- **背包整理**：按优先级安排神器，标出符合预设收藏连招的奖励。
- **其他选项**：战斗伙伴、失败后重试、中途加入／重连，以及 **1–4 人**队伍规则。

**当前为 Beta 测试版，背包整理仍是实验功能；失败后重试会将道具和进度回退到所选时间点。**

## 安装

1. 退出 Sephiria。
2. 从 Nexus Mods 或 GitHub Releases 下载完整 ZIP，使用对应版本的校验文件[验证下载](#验证发布包)。
3. 解压到包含 `Sephiria.exe` 的文件夹，遇到 `AddOns` 文件夹时选择合并。
4. 启动游戏并载入存档，打开**选项 → 游戏性 → SEPHIRIA ENHANCEMENTS · by 0xMashiro**。

文件应直接放在 `AddOns\SephiriaEnhancements` 中，不要多套一层文件夹。

## 常用操作

下表为默认操作，改键后以游戏内提示为准。在游戏键盘或手柄设置的 **SEPHIRIA ENHANCEMENTS 快捷键**中改键。

| 操作 | 默认方式 |
| --- | --- |
| 锁定／切换目标；解除锁定 | 短按／长按 `L` 或鼠标中键 |
| 开关魔法自动施法（背包内） | 中键点击技能，或选中后按 `L`；手柄使用确认键 |
| 暂停／恢复已选魔法的自动施法 | `F6` |
| 显示／隐藏本层地图 | `M` |
| 脱战后查看统计 | 短按 `F7`，或暂停菜单中的**查看统计** |
| 隐藏／恢复伤害显示 | 长按 `F7` 半秒；隐藏时仍会记录 |
| 切换战报页面 | `Q` / `E` |
| 收起战报 | `L`、鼠标中键、`Esc` 或手柄 Start/Menu |
| 整理已打开的背包 | `F8`，或整理按钮 |
| 切换设置页签／背包区域 | `Tab` / `Shift+Tab` |

已有战报时，短按 `F7` 会先收起它。收起后，再按 `Esc` 或 Start/Menu 打开暂停菜单。
手柄目标锁定与自动施法暂停默认未绑定，可在改键设置中自行绑定。

## 游戏内帮助

悬停或选中 Mod 设置，即可查看功能、操作方式和限制。背包、地图和战报界面会显示对应操作提示。
版本信息与更新检查位于**关于与更新**。

## 问题反馈

欢迎在 [Nexus Mods](https://www.nexusmods.com/sephiria/mods/24?tab=posts) 或 [GitHub Issues](https://github.com/0xMashiro/SephiriaEnhancements/issues) 用中文或英文反馈问题、提出建议、分享体验。
在**关于与更新**中选择**复制反馈信息**，再补充遇到的问题和复现步骤。
通过**打开 Mod 日志文件夹**找到 `support*.log` 并附上。信息不全或没有日志，也欢迎先反馈。

<details>
<summary>游戏或 Mod 无法启动时</summary>

重启游戏前先复制日志，提交附件前移除私人信息。

| 文件 | 位置 |
| --- | --- |
| `support*.log` | 系统**文档** → `Saved Games\Sephiria\Mods\SephiriaEnhancements\Logs\Support` |
| `Player.log`；重启后可附 `Player-prev.log` | `%USERPROFILE%\AppData\LocalLow\TEAMHORAY\Sephiria` |

</details>

## 验证发布包

<details>
<summary>检查 SHA-256 校验值</summary>

从对应版本的 GitHub Release 下载 `SHA256SUMS.txt`，与 ZIP 放在同一文件夹，在该文件夹打开 PowerShell：

```powershell
$zip = Get-ChildItem -LiteralPath . -Filter 'SephiriaEnhancements-*.zip' |
    Select-Object -First 1
Get-FileHash -LiteralPath $zip.FullName -Algorithm SHA256
Get-Content -LiteralPath .\SHA256SUMS.txt
```

确认文件名和算出的校验值一致。校验用于确认下载完整性，不代表代码安全。

</details>

## 从源码构建

<details>
<summary>供代码贡献者使用</summary>

需要 PowerShell 7、.NET 10 SDK，以及合法安装的 Sephiria。
在仓库根目录运行，把示例游戏目录换成自己的目录：

```powershell
# 检查
& .\scripts\test.ps1
# 构建开发版
& .\scripts\build.ps1 -GameDir "C:\Games\Sephiria" -Configuration Debug -DeveloperTools
# 构建并打包正式版
& .\scripts\package.ps1 -GameDir "C:\Games\Sephiria"
```

产物位于 `artifacts/`。贡献代码时，请保持改动聚焦并补充相关检查。
请勿提交游戏文件、反编译代码、日志、个人路径或构建产物。

</details>

## 许可证

本 Mod 免费获取与使用，基于 [MIT License](./LICENSE) 开源。
Copyright (c) 2026 0xMashiro。
