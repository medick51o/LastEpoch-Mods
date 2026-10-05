# MedicK's Terrible Mods for Last Epoch

The **Terrible family**: five MelonLoader mods for **Last Epoch** (Il2Cpp · Unity · net6.0), built to look and feel like they shipped with the game. "Filling the void, terribly."

> Drop the compiled `.dll` into your `Last Epoch/Mods/` folder.
> Requires **MelonLoader 0.7.3+**: https://melonwiki.xyz

Released July 1, 2026: the Terrible era. Internal DLL names never change, so upgrades just work.

**Season 5 (October 2026):** every mod below is updated for Season 5 (Unity 6000.4.8). Each release ZIP is still one DLL; the CoreModule compatibility repair is built in, no extra plugin needed. Grab the newest ZIP from each mod's `release/` folder.

---

## The mods

### [Terrible Tooltips](./medick_TerribleTooltips) — v3.1.2 · THE ULTIMATE OVERHAUL
One line per affix: the essay dies. Tier and grade chips in the colour language every ARPG player already speaks, Hold-Alt deep view, ground label brackets, filter rule numbers. The flagship.
`medick_Terrible_Tooltips.dll` · [Nexus #30](https://www.nexusmods.com/lastepoch/mods/30)

### [Terrible Inventory](./medick_Advanced_Inventory) — v2.0.2
STASH and STASH ALL buttons plus a collapsible Quick Teleport column, every visual cloned from the game's own UI. Formerly Advanced Inventory. *Known issue (Season 5): Quick Teleport is unreliable; use the world map for now.*
`medick_Terrible_Inventory.dll` · [Nexus #29](https://www.nexusmods.com/lastepoch/mods/29)

### [Terrible Cooldowns](./medick_CooldownTracker) — v1.0.2
Your skill icons float above your character while they cool down: real skill artwork, console-style button badges (Xbox/PS5/KB auto-detect), Move mode. Formerly Cooldown Tracker (v4.x).
`medick_CooldownTracker.dll` · [Nexus #26](https://www.nexusmods.com/lastepoch/mods/26) · Key: `Home`

### [Terrible Zoom](./medick_CameraZoom) — v1.0.2
Extended zoom-out, live camera tuning, tilt lock, and a Rescue button that restores everything the game shipped with. Formerly Camera Zoom (v1.x).
`medick_CameraZoom.dll` · [Nexus #27](https://www.nexusmods.com/lastepoch/mods/27) · Key: `End`

### [Terrible fog_OF_war](./medick_FogOfWar) — v1.0.2
A six-level vision dial living inside the game's own settings screen: BLIND, HARD, LIMITED (69%), NORMAL, SCOUT, ORACLE. Slide left to go in blind. Slide right because we all know why you're really here.
`medick_The_fogOFwar.dll` · [Nexus #31](https://www.nexusmods.com/lastepoch/mods/31)

---

## Building from source

Requirements: .NET SDK 6.0+, MelonLoader 0.7.3+ installed in Last Epoch, game at the default Steam path (or edit the `<ML>`/`<GM>` paths in each `.csproj`).

```bash
dotnet build <mod folder> -c Release -p:DeployToMods=false
```

Leave off `-p:DeployToMods=false` if you want the build copied straight into your game's `Mods` folder. The shared Season 5 repair code lives in `tools/TerribleInteropRepair` and is compiled into each mod.

Each mod folder carries its own README, CHANGELOG, and (where the build warranted one) a SPEC with the behavior contract.

---

## Contributing

Pull requests are welcome. Branch from `main` (it always holds the latest released source), keep each PR to one mod, and say what you tested in game. New settings should default to the current behaviour so existing players see no change until they opt in.

---

## License

MIT. Fork freely, credit appreciated.
