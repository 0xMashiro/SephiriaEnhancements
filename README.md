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
- **Effect attributes:** the character menu's special-stat page shows nonzero bonuses for supported effects, normal Solar Blade and Storm Cloud damage before hit resolution, and Storm Cloud stock, discharge and recovery cycles. Equipped quick-slot magic shows MP cost, MP-budget casts, charges and new charge-cycle duration. Relevant Burn bonuses or supported application effects also show a per-stack damage reference. Tab switching follows the game's bindings; added rows support focus-following scrolling. This does not cover every item or effect.
- **Controls:** keyboard menu and reward navigation, automatic targeting and manual target lock.
- **Exploration:** map overlay, People / Places lists, NPC tracking and camera distance from **75% to 200%**.
  Town maps include map destinations and preparation facilities. When fast travel is available, travel near a person or facility, or use a map destination's assigned landing point.
- **Backpack:** arrange artifacts around your priorities and highlight rewards matching your preset's favorite combos.
- **Solo and co-op:** optional combat companion, retry after defeat, mid-run joining/reconnect support and **1–4 player** rule presets.

Most display and control improvements start enabled. Retry after defeat, hidden-room display, mouse aim assist
and the combat companion start **off**. Multiplayer rules start at **Original**, camera distance at **100%**.

Open **Team rules** from the multiplayer menu or Mod settings, or interact with its stone beside Roots’ Retreat in the multiplayer lobby.
Before departure, the host can select rules for **1–4 players** and adjust enemy health, damage, spawning, rewards and supplies.
Selecting a player count edits that count's rules without changing the team size. **Review changes** compares all unsaved edits;
**Save and close** saves them together for future explorations. **Reset this group** restores only the selected group's values for the selected count.
Numeric fields support direct input, step buttons and **Use game default**, with a number pad for gamepad controls.
If someone joins or leaves while editing, edits remain and saving pauses until the host reviews the new team size.

Departure locks the full rules table and the permission to combine it with another multiplayer extension for that exploration.
Later spawns and rewards use the applicable rules for the current team size. During exploration, the panel remains available to inspect the host's rules.
Saving, departure and team-size changes send short chat summaries; joining and reconnecting players receive the current rules.
The host can also share a summary manually, with a 10-second cooldown. Players using a compatible Mod version receive details and notifications in their own game language.
Players without a compatible Mod receive ordinary chat summaries in the host's language. The panel distinguishes unavailable host details and rules controlled by another multiplayer extension.
Continuing an exploration preserves its saved rules and extension setting. Start a new exploration when updating from the earlier rule-save format; old rule records are not migrated.
The Mod's main switch is locked during exploration,
while connected to another host, and while a reconnect is pending. Display options remain individually adjustable.
**New players during exploration** and **Reconnect support** are separate settings; disabling new arrivals does
not disable reconnect support. Reconnection still depends on the game's room, character and save conditions.

With the same Mod version on the host and joining player, a new arrival receives the positive difference up to
the other players' average level and current gold, rounded down. Level gains use the game's growth rules;
starting at level 1 and catching up to level 30 provides 29 level reward choices through the game's normal
level-up reward entry, continuing to the next choice after each claim. Once these choices are finished,
recorded route supplies arrive automatically outside combat and loading, while no other menu is open.
Choice rewards appear nearby one at a time for normal interaction; other supplies are applied directly.
Choosing another miracle follows the game's replacement rules. No pause-menu claiming is required.

Joining supplies include recorded reward opportunities on one existing teammate's route. They do not copy
another player's equipment, reimburse purchases, grant permanent unlocks or reconstruct rewards from before
recording began. Keep the Mod installed to claim pending supplies. Reconnecting resumes the same allowance
and remaining choices; it does not award another joining allowance. Saved exploration and defeat retry restore
the corresponding supply progress. Pending level-up rewards follow the game's own save behavior, which
stores their count and seeds; it does not preserve every previously displayed choice or reroll state.
When updating from the pause-menu level-reward system, start a new exploration; its old supply records are not migrated.

Under Bars and Numbers settings, teammate HP/MP, companion HUD numbers and extra mana-reservation numbers start off.
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

Standalone Mod notices use the `[Sephiria Enhancements]` prefix. Errors and important interrupted operations appear in the local chat log; routine feedback uses short native messages. These local notices are not broadcast as player chat. The chat log is temporary; use the Mod log when reporting an error.

Settings are grouped into **General**, **Combat and Display**, **Bars and Numbers**, **Controls and Camera**, **Multiplayer**, and **About and Updates**. Version information and update checks are in the last category.

When the Mod is off, its settings categories are hidden and saved choices are retained; the log-folder and problem-report actions remain available.
In Team rules, click a numeric value or use Confirm to open its editor. Enter confirms input, Esc cancels, and Tab / Shift+Tab move between controls.
Confirmation keeps an unsaved edit; save it from the main panel. The panel compares original, saved and edited values.
The editor shows the selected player count, available game references, range and step. References describe the part controlled by the setting, not final enemy statistics.

On first entering a game after launch, the Mod shows the Mod and game versions and official Nexus Mods / GitHub links
in the local chat log. **About and Updates** settings include the version, both download pages, a welcome-message
toggle, automatic update checks and **Check for updates**. The settings also show the game version and the local time of the last completed check since launch. Checks distinguish connection failures, timeouts, GitHub request limits, service errors and invalid update data; repeat checks are disabled while a request is running. Welcome messages and automatic checks default
to on while the Mod is enabled; these preferences are saved on your device, not in a character save.
GitHub is checked in the background once after entering a game. Stable versions check stable releases;
test versions also check newer test releases. Checks never install files, and a newer version does not
guarantee compatibility with your game version. Network failures do not prevent playing; use the settings
to retry or open either official download page.

Hover over or select a Mod setting to read its purpose, controls and limits. Backpack, map and battle-report
screens show their own controls; hints follow your current bindings. Use the keyboard or gamepad settings
to rebind Mod shortcuts.

In the Mod's **General** settings, **Map enhancements** controls the map tools, current-floor overlay,
NPC tracking and hidden-room option. Turn it off to use the original map. It defaults to on;
your hidden-room preference is retained while disabled.

**Backpack arrangement is experimental. Retry after defeat rolls back items and progress to the selected point.**
Read the corresponding setting's help before using these features.

## Feedback

Report bugs or suggestions on the [Nexus Mods page](https://www.nexusmods.com/sephiria/mods/24?tab=posts) or through [GitHub Issues](https://github.com/0xMashiro/SephiriaEnhancements/issues).
In the Mod's **About and Updates** settings:

1. Choose **Open Mod log folder**, find the `support*.log` files.
2. Choose **Copy report details**, paste the outline with your versions on Nexus Mods or where you already contact the author, and add what happened. Attach logs where attachments are supported. Copying sends nothing.

If you have a GitHub account, **Report on GitHub** opens a form with the game and Mod versions filled in. Chinese game languages open the Chinese form; other languages open the English form.
All three actions remain visible when the Mod's main switch is off. If the system cannot open the folder or browser, the action changes to **Copy path / Copy link**. Activate it again, then paste into your file manager's address bar or browser.
English and Chinese are welcome. When possible, include game and Mod versions, steps, expected and actual results,
solo/host/client status, other mods and relevant settings.
Missing some details? You're still welcome to describe what happened.

<details>
<summary>Attach logs (Windows)</summary>

Logs are generated automatically. Copy them soon after the issue, before another launch replaces older logs. If the game or Mod cannot start, use the locations below.

| Files | Location |
| --- | --- |
| All `support*.log` files | System **Documents** → `Saved Games\Sephiria\Mods\SephiriaEnhancements\Logs\Support` |
| `Player.log` for loading failures or crashes; `Player-prev.log` if you restarted | `%USERPROFILE%\AppData\LocalLow\TEAMHORAY\Sephiria` |

Use **Win+R → `shell:Personal`** to open Documents, including redirected OneDrive folders.
Review logs for private information and attach the `.log` files directly to the issue. Logs are not uploaded automatically.
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
