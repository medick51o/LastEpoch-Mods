# medick_CooldownTracker — v4.4

A **MelonLoader** mod for **Last Epoch** that shows floating skill icons above your character while abilities are on cooldown.

## Features

- **Floating overhead icons** — skill icons appear above your head while on cooldown, with a fill bar and key label
- **Two-line spell names** — type `Flame Ward` and it displays stacked as `Flame / Ward`; up to 20 characters per label
- **Live input-mode detection** — detects keyboard/mouse vs Xbox vs PS5 controller automatically
- **Per-mode custom labels** — each of the 3 input modes stores labels independently
- **[▼] Button Picker** — dropdown popup for Xbox & PS5 buttons (face, bumpers, triggers, sticks, D-pad, other)
- **Movement Lock** toggle — when ON, sets `EpochInputManager.forceDisableInput = true` to fully block character movement while the settings panel is open
- **Hotkey blocking while typing** — same flag prevents game hotkeys (inventory, skills) from firing while you type a label
- **Evade slot** (slot 6) fully tracked and customisable
- **All settings persist** between sessions via MelonPreferences

## Default Button Mappings

| Slot | 0 | 1 | 2 | 3 | 4 | 5 | 6 (evade) |
|------|---|---|---|---|---|---|-----------|
| Keyboard | Q | W | E | R | RMB | T | Space |
| Xbox | X | Y | RB | LT | L | RT | B |
| PS5 | □ | △ | R1 | L2 | L3 | R2 | ○ |

## Controls

| Key | Action |
|-----|--------|
| `Home` | Toggle settings panel |
| Drag title bar | Reposition panel |

## Settings Panel

- **Input Mode** — Auto / Keyboard / Xbox / PS5
- **Layout Override** — Auto / Xbox / PS5 (in Auto input mode)
- **Icon Opacity**, **Icon Size**, **Offset X/Y**, **Menu Size** sliders
- **Movement Lock** toggle
- Per-slot: icon preview, effective label display, custom label text field, [▼] picker, enabled toggle

## Installation

1. Install [MelonLoader 0.6.x](https://melonwiki.xyz) into Last Epoch
2. Drop `medick_CooldownTracker.dll` into `Last Epoch/Mods/`
3. Launch the game and load into a zone
4. Press `Home` to open the settings panel

## Building

```bash
dotnet build -c Release
```

Requires Last Epoch at the default Steam path. Edit `<ML>` and `<GM>` in the `.csproj` if your path differs.
