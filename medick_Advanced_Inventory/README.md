# MedicK's Terrible Inventory

*an inventory mod* — **Terrible Inventory** for short. A [MelonLoader](https://melonwiki.xyz) mod for **Last Epoch** that puts the things you actually do — stashing, vendoring, getting around — one click from your inventory.

> Nexus Mods: [Terrible Inventory](https://www.nexusmods.com/lastepoch/mods/29) · by medick · internal name `medick_Terrible_Inventory` · formerly Advanced Inventory

## Features

- **STASH** — open your stash from anywhere
- **STASH ALL** — dump your whole inventory into the stash at a server-friendly pace (affinity tabs respected; it's a dump button, not a sorting service — blast and get back to the action)
- **VENDOR** — open the NPC vendor from anywhere (off by default — enable in settings)
- **Quick Teleport** — a collapsible menu on the inventory panel: FACTIONS (Circle of Fortune, Merchant's Guild, Forgotten Knights, The Woven) · DUNGEONS (Lightless Arbor, Temporal Sanctum, Soulfire Bastion) · HUBS (End of Time, Champion's Gate). Works without ever opening the map; only travels to waypoints your character has unlocked — these are the game's own waypoints, not teleport hacks
- **Native look** — every button is built from the game's own UI (sprites, font, hover behavior). v2 contains zero hand-drawn rectangles
- **Controller / Steam Deck** — buttons stay visible in controller mode (the right stick moves a cursor, so they're fully usable on pad); touch-sized targets
- **Choose what you see** — Settings → Terrible Inventory: every button and the teleport menu can be toggled individually

## Controls

Everything lives on the inventory panel. `QUICK TELEPORT` tab collapses/expands the column; group headers collapse their sections.

## Installation

1. Install [MelonLoader 0.7.2+](https://melonwiki.xyz) into Last Epoch
2. Drop `medick_Terrible_Inventory.dll` into `Last Epoch/Mods/`
3. Open your inventory

Upgrading from Terrible Inventory v1.5.0: just replace the DLL. Coming from the old Advanced Inventory (v1.4 or earlier)? **Delete `medick_Advanced_Inventory.dll` first** — the DLL was renamed in the rebrand, and running both causes duplicate buttons.

> *Fine print: stash/vendor-from-anywhere may conflict with online play — recommended for offline sessions.*

## Credits

Inspired by **war3i4i / KillingGodVH**'s LastEpochImprovements; the settings-screen injection uses his technique. Built with Claude (Anthropic).

## Building from source

```bash
dotnet build -c Release
```

The build auto-copies the DLL into the game's Mods folder. Source layout:

```
src/
  TerribleInventoryMod.cs          MelonMod lifecycle + controller-mode keep-alive
  TerribleInventoryMod.Patches.cs  Harmony patches (inventory + settings panels)
  InventoryUi.cs                   Footer buttons + Stash All
  TeleportMenu.cs                  Quick Teleport column (native clones)
  TravelService.cs                 Unlock gate · waypoint lookup · silent era primer
  NativeClone.cs                   The EHG-proud factory (clone, never paint)
  SettingsUi.cs                    The Terrible Inventory settings category
  NativeSettings.cs                KG-derived native settings injection helpers
  BuildInfo.cs / Prefs.cs          Identity + persisted settings
```

See [SPEC.md](SPEC.md) for the behavior contract and [ARCHAEOLOGY.md](ARCHAEOLOGY.md) for the recovered development history.
