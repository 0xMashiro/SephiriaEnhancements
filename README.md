<p align="center">
  <img src="./assets/readme-header.webp" alt="Sephiria Enhancements" />
</p>

<p align="center">
  <strong>English</strong> · <a href="./README.zh-CN.md">简体中文</a>
</p>

<p align="center">
  <strong><a href="https://www.nexusmods.com/sephiria/mods/24">Download on Nexus Mods</a></strong><br />
  Free to download and use · Open source under the <a href="./LICENSE">MIT License</a>
</p>

# Sephiria Enhancements

Combat information, keyboard controls, backpack arrangement and exploration tools for Sephiria.
Uses the game's built-in AddOns system; no BepInEx required.

> [!TIP]
> **Your feedback and sharing make a difference**
>
> Most of my time goes into developing and maintaining this Mod, leaving little for promotion.
> If it makes your time with Sephiria more enjoyable, consider recommending it to friends or sharing your experience
> in community posts, articles or videos. **Even a quick recommendation or a screenshot helps.**
>
> I mostly play solo, so I especially rely on community feedback to discover multiplayer bugs and issues with other mods.
> If something goes wrong, please [report it on GitHub Issues](https://github.com/0xMashiro/SephiriaEnhancements/issues).
> You don't need a complete bug report to get started—just describe what happened. See [Feedback](#feedback) for useful details to include.
>
> **Knowing the Mod helps you—and hearing what needs work—is a big part of what keeps me motivated to maintain it.**
> Thank you for playing, sharing and helping make it better.

**Beta:** backpack arrangement is experimental. It also seeks better use of supported
position-dependent artifact effects, such as adjacent magic modifiers and same-row companion effects.
Your explicit priorities take precedence. By default, existing bonuses are preserved and additional MP costs are limited.
Under **Special effects**, choose whether to redistribute bonuses or allow upgrades that cost more MP.
Search is bounded and does not guarantee an optimal layout or maximum combat damage.

## Features

- **Combat:** damage, DPS, battle reports, hit streaks, resource values and ally/enemy outlines.
- **Controls:** keyboard menu and reward navigation, automatic targeting and manual target lock.
- **Exploration:** map overlay, People / Places lists, NPC tracking and camera distance from **75% to 200%**.
  Town maps include map destinations and preparation facilities. When fast travel is available, travel near a person or facility, or use a map destination's assigned landing point.
- **Backpack:** arrange artifacts around your priorities and highlight rewards matching your preset's favorite combos.
- **Solo and co-op:** optional combat companion, retry after defeat, mid-run joining/reconnect support and **1–4 player** rule presets.

Most display and control improvements start enabled. Retry after defeat, hidden-room display, mouse aim assist
and the combat companion start **off**. Multiplayer rules start at **Original**, camera distance at **100%**.

Set multiplayer rules before leaving town; only the host can change them. Starting an exploration locks its rules,
and continuing a saved exploration keeps its saved rules. The Mod's main switch is locked during exploration,
while connected to another host, and while a reconnect is pending. Display options remain individually adjustable.
**New players during exploration** and **Reconnect support** are separate settings; disabling new arrivals does
not disable reconnect support. Reconnection still depends on the game's room, character and save conditions.

With the same Mod version on the host and joining player, a new arrival receives the positive difference up to
the other players' average level and current gold, rounded down. Level gains use the game's growth rules;
starting at level 1 and catching up to level 30 provides 29 level reward choices. Use **Claim joining supplies**
in the pause menu outside combat to claim one reward at a time. Choice rewards use the game's own interfaces,
including miracle choices; choosing another miracle follows the game's replacement rules.

Joining supplies include recorded reward opportunities on one existing teammate's route. They do not copy
another player's equipment, reimburse purchases, grant permanent unlocks or reconstruct rewards from before
recording began. Keep the Mod installed to claim pending supplies. Reconnecting resumes the same allowance
and remaining choices; it does not award another joining allowance. Saved exploration and defeat retry restore
the corresponding supply progress.

Under combat/display settings, teammate HP/MP, companion HUD numbers and extra mana-reservation numbers start off.
**Disable all extra numbers** turns off the numeric display options together; your original HP/MP numbers remain.
Companion HUD numbers and ordinary creatures' world-space numbers have separate controls.

## Install

1. Exit Sephiria.
2. Download the ZIP and `SHA256SUMS.txt` from the newest [GitHub Release](https://github.com/0xMashiro/SephiriaEnhancements/releases), including **Pre-release** versions.
3. [Verify the download](#verify-a-release), then extract the whole package into the folder containing `Sephiria.exe`. Merge `AddOns` if asked.
4. Launch the game and **load a save file first**, then open **Options → Gameplay → SEPHIRIA ENHANCEMENTS · by 0xMashiro**.

The game's current AddOns implementation makes the settings entry available only after loading into a game session.
**It is expected to be missing from the main menu; load your save to find it.**

Files should sit directly in `AddOns\SephiriaEnhancements`, without an extra nested folder.

## Controls

Rebind shortcuts in the game's keyboard or gamepad settings under **SEPHIRIA ENHANCEMENTS SHORTCUTS**.
These are defaults; in-game hints follow your bindings.

| Action | Default |
| --- | --- |
| Lock or cycle targets / release lock | Tap / hold `L` or middle mouse button |
| Toggle auto casting for a skill in the backpack | Middle-click the skill, or select it and press `L`; on gamepad, use Confirm |
| Pause / resume auto casting for selected magic | `F6`; gamepad binding optional |
| Toggle the map overlay | `M` |
| View statistics outside combat | Tap `F7`, or **View statistics** in the pause menu |
| Hide / restore the damage display | Hold `F7` for half a second; recording continues |
| Switch battle report pages | `Q` / `E` |
| Close a report | `L`, middle mouse button, `Esc` or gamepad Start/Menu |
| Arrange an open backpack | `F8`, or the arrange button |
| Switch settings tabs | `Tab` / `Shift+Tab` |

Tapping `F7` closes an existing automatic report first. After closing a report, press `Esc` or Start/Menu again
to open the pause menu. Gamepad target lock has no default binding.

Auto casting starts off for every artifact. An **A** in the skill icon marks selected magic; **Ⅱ** means paused;
the corresponding keyboard/gamepad binding row also has an auto-casting switch.
While exploring, the shortcut above pauses / resumes all selected magic without clearing selections.
Pause survives floor changes and resets when the character changes or the world reloads.
It uses normal mana, charges and your current aim, including when there are no enemies, and pauses in menus.
Selections follow the same artifact when rebound and clear when it is lost, the character changes or the world reloads.
Only bound, single-cast magic is supported; weapon attacks and active combo abilities are excluded.
The backpack's skill area and custom weapon input toggle support directional navigation, with focus frames around combo rows and the toggle.
`Tab` / `Shift+Tab` cycles through the main backpack, potions, sub-bag, combo effects, skills and custom weapon input, skipping unavailable areas.
Each area remembers its selected slot until the backpack closes; returning restores that slot if it is still available.

## In-game help

Hover over or select a Mod setting to read its purpose, controls and limits. Backpack, map and battle-report
screens show their own controls; hints follow your current bindings. Use the keyboard or gamepad settings
to rebind Mod shortcuts.

In the Mod's **General** settings, **Map enhancements** controls the map tools, current-floor overlay,
NPC tracking and hidden-room option. Turn it off to use the original map. It defaults to on;
your hidden-room preference is retained while disabled.

**Backpack arrangement is experimental. Retry after defeat rolls back items and progress to the selected point.**
Read the corresponding setting's help before using these features.

## Feedback

Report bugs or suggestions through [GitHub Issues](https://github.com/0xMashiro/SephiriaEnhancements/issues).
English and Chinese are welcome. When possible, include game and Mod versions, steps, expected and actual results,
solo/host/client status, other mods and relevant settings.
Missing some details? You're still welcome to describe what happened.

<details>
<summary>Attach logs (Windows)</summary>

Logs are generated automatically. Copy them soon after the issue, before another launch replaces older logs.

| Files | Location |
| --- | --- |
| All `support*.log` files | System **Documents** → `Saved Games\Sephiria\Mods\SephiriaEnhancements\Logs\Support` |
| `Player.log` for loading failures or crashes; `Player-prev.log` if you restarted | `%USERPROFILE%\AppData\LocalLow\TEAMHORAY\Sephiria` |

Use **Win+R → `shell:Personal`** to open Documents, including redirected OneDrive folders.
Review logs for private information, ZIP them and attach them to the issue. Logs are not uploaded automatically.
No save folder or game files are needed. **You can still report a problem without logs.**

</details>

## Verify a release

<details>
<summary>Check that your download matches the release</summary>

Place the ZIP and `SHA256SUMS.txt` in the same folder. Open PowerShell there and run:

```powershell
$zip = Get-ChildItem -LiteralPath . -Filter 'SephiriaEnhancements-*.zip' |
    Select-Object -First 1
Get-FileHash -LiteralPath $zip.FullName -Algorithm SHA256
Get-Content -LiteralPath .\SHA256SUMS.txt
```

The filename and hash must match the checksum file. This confirms that the download has not changed;
it does not guarantee code safety.

</details>

## Build from source

<details>
<summary>For contributors — not needed to install or play</summary>

You need PowerShell 7, a .NET 10 SDK, and a legally installed copy of Sephiria. No specific SDK patch version is required.
Run these commands from the repository root, replacing the example game directory with your own.

Run the checks:

```powershell
& .\scripts\test.ps1
```

Build a development version into `artifacts/build/Development/`:

```powershell
& .\scripts\build.ps1 -GameDir "C:\Games\Sephiria" -Configuration Debug -DeveloperTools
```

Build and package a release ZIP with `SHA256SUMS.txt` in `artifacts/`:

```powershell
& .\scripts\package.ps1 -GameDir "C:\Games\Sephiria"
```

Keep changes focused, use consistent terms in code and player text, and include relevant checks.
Do not contribute game files, decompiled code, logs, personal paths or build output.

</details>

## License

Free to download and use. Released under the [MIT License](./LICENSE).
Copyright (c) 2026 0xMashiro.
