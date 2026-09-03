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

Spend less time navigating menus and arranging your backpack, and get a clearer view of each fight.
Sephiria Enhancements adds combat information, keyboard controls, exploration tools and co-op options.
It uses the game's built-in AddOns system, so you do not need BepInEx.

This is a **beta release**. Backpack arrangement is still experimental; feedback is welcome.

## What it adds

- **Clearer combat:** damage and DPS displays, battle reports, hit streaks, BOSS health numbers and ally/enemy outlines.
- **More keyboard control:** navigate menus, choose rewards and manage your backpack without reaching for the mouse.
- **Targeting help:** automatic targeting and manual target lock, with adjustable controls.
- **Easier exploration:** town NPC names on the map, a current-floor map overlay and adjustable camera distance.
- **Backpack arrangement:** an experimental shortcut to arrange artifacts around your preferences.
- **Solo and co-op options:** an optional companion, retry after defeat, mid-run joining/reconnect support and 1–4 player rule presets.

Most display and control improvements are enabled by default. **Retry after defeat, hidden-room display,
mouse aim assist and the combat companion are off** until you enable them. Multiplayer rules start at
**Original**, and camera distance at **100%**. Your saved settings are kept when you re-enable the Mod.

## Install

1. Exit Sephiria.
2. Download the ZIP and `SHA256SUMS.txt` from the newest entry on [GitHub Releases](https://github.com/0xMashiro/SephiriaEnhancements/releases), including releases marked **Pre-release**.
3. Check the ZIP using the [download verification steps](#verify-a-release) below.
4. Extract it into the folder containing `Sephiria.exe`. Merge the `AddOns` folder if asked.
5. Start the game and open **Options → Gameplay → SEPHIRIA ENHANCEMENTS · by 0xMashiro** to adjust the features.

The Mod's files should be directly inside `AddOns\SephiriaEnhancements`, without another nested folder.
Extract the whole package, including its supporting files.

## Controls

Change bindings in the game's keyboard or gamepad settings under **SEPHIRIA ENHANCEMENTS SHORTCUTS**.
The table shows defaults; on-screen hints follow your current bindings.

| Action | Default control |
| --- | --- |
| Switch locked target | Tap middle mouse button or `L`; hold to release the lock |
| Show/hide the current-floor map overlay | `M` |
| View statistics outside combat | Tap `F7`, or choose **View statistics** in the pause menu |
| Hide/restore the damage display | Hold `F7` for half a second; recording continues |
| Close a visible battle report | `Esc`, or Start/Menu on gamepad; press again to open the pause menu |
| Arrange your open backpack | `F8`, or click the arrange button |
| Switch settings tabs | `Tab` / `Shift+Tab` |

If an automatic report is already visible, tapping `F7` closes it first. Gamepad target lock has no
default binding; assign one in controls. You can also bind the statistics shortcut, or use the pause menu.

In menus, use the game's navigation and confirm controls. After choosing a reward, select a backpack
slot and confirm again to place it. Moving the mouse, clicking or scrolling switches back to mouse control.
Item actions follow the game's current bindings and the hints shown on screen.

## Using the features

### Combat and statistics

Automatic targets use an **amber marker**; manually locked targets use a **red marker**.
With keyboard controls, melee attacks follow movement or your last aim direction unless you lock a target.
Ranged attacks keep a nearby visible, unobstructed target until it is no longer valid. Unlocked gamepad aiming works as usual.

Statistics offer **Recent battle** and **Current floor** pages. Multi-phase BOSS fights produce one combined
report after the final phase, once menus and cutscenes are out of the way. In **Combat and Display**, change
**Statistics size** to make the display smaller or larger.

Hiding the display does not stop recording. Floor totals cover combat recorded on your machine, so co-op
players may see different numbers, especially after joining late. They are temporary: changing floors,
reconnecting, reloading the game or turning statistics off can clear them. Browsing statistics follows
the game's pause rules, including in co-op.

### Backpack arrangement (experimental)

Open your backpack and press `F8` or click the arrange button. It only runs when you ask it to.
Start with **Automatic**; you can also choose the game's artifact-level arrangement.

To guide the Mod's arrangement, put artifacts you want active in the **priority queue**, with the most
important first. Put artifacts you would rather keep inactive in the **exclusion area**. Click or drag
marks to reorder them; right-click to remove one. Select an artifact and choose **Edit goals** to set an
automatic level target, keep it active, or request a minimum level. Combo targets are adjusted separately
and saved for future runs; artifact marks apply to the current run.

Goals default to **Try**. Choose **Must** if a goal must be met before any arrangement is applied.
If no arrangement meeting all Must goals is found, your backpack stays as it was. Green, yellow and red
marks show met, partly met and unmet goals. If item movement is interrupted, moves already made are not undone.

The Mod may skip upgrades that add
unwanted costs, or stop when it cannot check an item's effects. Some arrangements remain unsupported,
including multiple sources of the same-row companion effect. In co-op, non-host players currently cannot
use the Mod's optimization on backpacks containing position-based effects, even if those effects are inactive.

### Retry after defeat

Enable this in settings if you want another attempt after the whole party is defeated.
**Retry floor** returns items and progress to the start of that floor. **Retry BOSS** returns to the first
recorded start of that BOSS fight, beginning at phase one. **Items gained after the chosen point are rolled back.**

All players must be on the same floor. BOSS retry may be unavailable after players or the surroundings change,
after leaving the floor, or after defeating the BOSS. Some scripted fights only support retrying the floor;
availability also depends on the game's save and loading state.

### Maps and co-op

- The map overlay starts hidden; use `M` to show it. Camera distance can be set from **75% to 200%**.
- **Show hidden rooms** reveals undiscovered secret locations on supported maps. It is off by default;
  turning it off keeps rooms you have already discovered visible.
- Mid-run joining/reconnect support is on by default and controlled by the host. New arrivals use new
  characters; they do not take over disconnected characters or receive missed-route rewards.
- Multiplayer rule presets cover **1–4 players**. By default, the Mod does not combine its rules with
  detected multiplayer extensions, and leaves unsupported player counts to the game or extension.

## Reporting problems and logs

Use [GitHub Issues](https://github.com/0xMashiro/SephiriaEnhancements/issues) for bugs and suggestions.
Search for an existing report first, then [open an issue](https://github.com/0xMashiro/SephiriaEnhancements/issues/new/choose)
if needed. **English and Chinese are both welcome.**

For a bug, tell us your game and Mod versions, what you did, what you expected and what happened instead.
Mention whether you were playing solo, hosting or joining someone else's game, plus any other mods and
relevant settings. A screenshot or short video helps. For a suggestion, describe what you would like to improve.

### Find and attach logs (Windows)

Support logs are created automatically. You do not need a development build or extra launch options.

| What to send | Where to find it |
| --- | --- |
| All available `support*.log` files | Your system **Documents** folder → `Saved Games\Sephiria\Mods\SephiriaEnhancements\Logs\Support` |
| `Player.log`, for loading failures, crashes or when requested | `%USERPROFILE%\AppData\LocalLow\TEAMHORAY\Sephiria` |
| `Player-prev.log`, if you restarted after the problem and the file exists | Same folder as `Player.log` |

Press **Win+R** and enter `shell:Personal` to open Documents, even if it has moved to OneDrive.
For game logs, paste the path in the table into File Explorer's address bar. Logs are separate from the game installation.

Copy them soon after the problem, before starting the game again, because older logs can be replaced.
Review the files, ZIP them and attach the ZIP to your issue. Logs stay on your computer unless you upload them;
remove private information before sharing publicly. There is no need to send the whole save folder or game files.

**Can't find the logs? Report the problem anyway.** If the Mod did not load, send `Player.log` instead.

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

You need PowerShell 7, the .NET SDK listed in `global.json`, and a legally installed copy of Sephiria.
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
