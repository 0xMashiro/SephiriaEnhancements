<p align="center">
  <img src="./assets/readme-header.webp" alt="Sephiria Enhancements" />
</p>

<p align="center">
  <strong>English</strong> · <a href="./README.zh-CN.md">简体中文</a>
</p>

<p align="center">
  <a href="https://www.nexusmods.com/sephiria/mods/24">Nexus Mods</a> ·
  <a href="https://github.com/0xMashiro/SephiriaEnhancements/releases">GitHub Releases</a>
</p>

# Sephiria Enhancements

Combat information, keyboard controls, backpack arrangement and exploration tools for Sephiria.
Uses the game's built-in AddOns system; no BepInEx required.

## Features

- **Combat:** damage, DPS, battle reports, hit streaks, resource values and ally/enemy outlines.
- **Effect attributes:** view current values and bonuses for supported effects in the character menu.
- **Controls:** keyboard menu and reward navigation, automatic targeting, target lock and optional automatic magic casting.
- **Exploration:** map tools, NPC tracking, fast travel where available and adjustable camera distance.
- **Backpack:** arrange artifacts around your priorities and highlight rewards matching your preset's favorite combos.
- **Other options:** combat companion, retry after defeat, mid-run joining/reconnect support and **1–4 player** team rules.

**Beta:** backpack arrangement is experimental. Retry after defeat rolls back items and progress to the selected point.

## Install

1. Exit Sephiria.
2. Download the complete ZIP from Nexus Mods or GitHub Releases. [Verify the download](#verify-a-release) using the matching release checksum.
3. Extract it into the folder containing `Sephiria.exe`, merging `AddOns` if asked.
4. Launch the game, load a save, then open **Options → Gameplay → SEPHIRIA ENHANCEMENTS · by 0xMashiro**.

Files should sit directly in `AddOns\SephiriaEnhancements`, without an extra nested folder.

## Controls

These are defaults; in-game hints follow your current bindings. Rebind Mod shortcuts in the game's keyboard or gamepad settings under **SEPHIRIA ENHANCEMENTS SHORTCUTS**.

| Action | Default control |
| --- | --- |
| Lock or cycle targets / release lock | Tap / hold `L` or middle mouse button |
| Toggle automatic magic casting in the backpack | Middle-click the skill, or select it and press `L`; on gamepad, use Confirm |
| Pause / resume auto casting for selected magic | `F6` |
| Show / hide the current-floor map overlay | `M` |
| View statistics outside combat | Tap `F7`, or **View statistics** in the pause menu |
| Hide / restore the damage display | Hold `F7` for half a second; recording continues |
| Switch battle-report pages | `Q` / `E` |
| Close a battle report | `L`, middle mouse button, `Esc` or gamepad Start/Menu |
| Arrange an open backpack | `F8`, or the arrange button |
| Switch settings tabs / backpack areas | `Tab` / `Shift+Tab` |

Tapping `F7` closes an existing report first. After closing a report, press `Esc` or Start/Menu again to open the pause menu.
Gamepad target lock and auto-casting pause have no default binding; assign them in controls.

## In-game help

Hover over or select a Mod setting for its purpose, controls and limits. Backpack, map and battle-report screens show their own control hints.
Version information and update checks are in **About and Updates**.

## Feedback

Bugs, suggestions and sharing are welcome on [Nexus Mods](https://www.nexusmods.com/sephiria/mods/24?tab=posts) or [GitHub Issues](https://github.com/0xMashiro/SephiriaEnhancements/issues), in English or Chinese.
Use **About and Updates → Copy report details**, then add what happened and how to reproduce it.
**Open Mod log folder** provides the `support*.log` files to attach. Missing details or logs? You can still report the problem.

<details>
<summary>If the game or Mod cannot start</summary>

Copy logs before restarting, and remove private information before attaching them.

| Files | Location |
| --- | --- |
| `support*.log` | System **Documents** → `Saved Games\Sephiria\Mods\SephiriaEnhancements\Logs\Support` |
| `Player.log`; `Player-prev.log` after restarting | `%USERPROFILE%\AppData\LocalLow\TEAMHORAY\Sephiria` |

</details>

## Verify a release

<details>
<summary>Check the SHA-256 checksum</summary>

Download `SHA256SUMS.txt` from the matching GitHub Release. Put it beside the ZIP, open PowerShell there and run:

```powershell
$zip = Get-ChildItem -LiteralPath . -Filter 'SephiriaEnhancements-*.zip' |
    Select-Object -First 1
Get-FileHash -LiteralPath $zip.FullName -Algorithm SHA256
Get-Content -LiteralPath .\SHA256SUMS.txt
```

The filename and hash must match. The checksum verifies download integrity, not code safety.

</details>

## Build from source

<details>
<summary>For contributors</summary>

Requires PowerShell 7, a .NET 10 SDK and a legally installed copy of Sephiria.
Run from the repository root, replacing the example game directory:

```powershell
# Check
& .\scripts\test.ps1
# Build a development version
& .\scripts\build.ps1 -GameDir "C:\Games\Sephiria" -Configuration Debug -DeveloperTools
# Build and package a release
& .\scripts\package.ps1 -GameDir "C:\Games\Sephiria"
```

Outputs are under `artifacts/`. Keep contributions focused and include relevant checks.
Do not contribute game files, decompiled code, logs, personal paths or build output.

</details>

## License

Free to download and use under the [MIT License](./LICENSE).
Copyright (c) 2026 0xMashiro.
