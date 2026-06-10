# Cooldown Tracker

A [MelonLoader](https://melonwiki.xyz) mod for **Last Epoch** that floats your skill icons above your character while they're on cooldown — glance at your character, not your action bar.

> Nexus Mods: [Cooldown Tracker](https://www.nexusmods.com/lastepoch/mods/26) · by medick

## Features

- **Floating overhead icons** — skill icons hover above your head while on cooldown, with a sweep fill, ready-flash border, and key label
- **Gameplay-aware** — icons only render while a living player character is on screen; menus and the login screen stay clean
- **Settings panel that stays put** — drag it anywhere; the position is saved and restored every session
- **Live input-mode detection** — keyboard/mouse vs Xbox vs PS5, detected from real device activity, with manual override
- **Per-mode custom labels** — Keyboard, Xbox, and PS5 label sets are stored independently (20 chars; a space stacks the label onto two lines, e.g. `Flame Ward` → `Flame` / `Ward`)
- **[▼] Button Picker** — colour-coded controller glyph popup for Xbox & PS5 (face, bumpers, triggers, sticks, D-pad)
- **Movement lock** — optional toggle that blocks character input while the panel is open, via the game's own `EpochInputManager` (typing in a label field always blocks game hotkeys)
- **Per-slot tracking toggles** — disable any slot; choices persist between sessions
- **Evade slot** (slot 6) fully tracked and customisable

## Controls

| Input | Action |
|-------|--------|
| `Home` | Toggle settings panel |
| Drag title bar | Reposition panel (saved automatically) |

## Default Button Labels

| Slot | 0 | 1 | 2 | 3 | 4 | 5 | 6 (evade) |
|------|---|---|---|---|---|---|-----------|
| Keyboard | Q | W | E | R | RMB | T | Space |
| Xbox | X | Y | RB | LT | L | RT | B |
| PS5 | □ | △ | R1 | L2 | L3 | R2 | ○ |

In keyboard mode the mod reads your actual in-game keybinds off the action bar where possible.

## Installation

1. Install [MelonLoader 0.6.x](https://melonwiki.xyz) into Last Epoch
2. Drop `medick_CooldownTracker.dll` into `Last Epoch/Mods/`
3. Launch the game and load into a zone
4. Press `Home` to open the settings panel

Upgrading from v4.x: just replace the DLL — all your labels and sliders carry over.

## Building from source

```bash
dotnet build -c Release
```

Requires Last Epoch (with MelonLoader installed) at the default Steam path; edit `<ML>` and `<GM>` in the `.csproj` if yours differs. Source layout:

```
src/
  CooldownTrackerMod.cs          MelonMod lifecycle (update loop, GUI dispatch)
  CooldownTrackerMod.Patches.cs  Harmony patches on AbilityBarIcon
  BuildInfo.cs                   Name/version single source of truth
  Prefs.cs                       All persisted settings (MelonPreferences)
  SlotRegistry.cs                Slot state, liveness pruning, label cache
  InputDetection.cs              KB/controller detection, label resolution
  InputBlocker.cs                EpochInputManager wrapper (write-on-change)
  UI/
    SettingsPanel.cs             Home-key panel (persisted position)
    ButtonPicker.cs              Controller glyph picker popup
    OverheadRenderer.cs          Floating icon rendering
    Styles.cs                    Cached GUI styles + draw helpers
    UiState.cs                   Shared GUI state
```

See [CHANGELOG.md](CHANGELOG.md) for version history.
