# MedicK's Terrible fog_OFwar

*a fog control mod* — **fog_OF_war** for short. A [MelonLoader](https://melonwiki.xyz) mod for **Last Epoch** with a 6-level vision dial, living inside the game's own settings screen. Slide left to go in blind. Slide right because we all know why you're really here.

> Nexus Mods: The fog OF war · by medick · internal name `medick_The_fogOFwar`

## Levels

| # | Level | What you see |
|---|-------|--------------|
| 0 | **BLIND** | Minimap hidden, nothing reveals. Going in blind? OK @wudijo |
| 1 | **HARD** | Minimap visible but reveals nothing |
| 2 | **LIMITED** | 69% of the default reveal radius. Nice. |
| 3 | **NORMAL** | Default game state — the mod sits idle |
| 4 | **SCOUT** | 3× the default reveal radius |
| 5 | **ORACLE** | Radius 600 — the whole zone, no FPS tax. We already know why you came. |

## Where it lives

No overlay panel, no keybinds: open the game's **Settings** and find the **Terrible fog_OFwar** section — one dropdown plus a colour-coded legend where **clicking a level row selects it**.

**Everything applies instantly.** Switching into or out of BLIND hides/restores the minimap live — the v1 "full game restart required, both ways" days are over. One engine note: lowering the radius doesn't re-fog what you've already explored; the next zone starts clean.

## Installation

1. Install [MelonLoader 0.6.x](https://melonwiki.xyz) into Last Epoch
2. Drop `medick_The_fogOFwar.dll` into `Last Epoch/Mods/`
3. Open Settings → Terrible fog_OFwar → pick your poison

Upgrading from v1.0: just replace the DLL — your level carries over (`UserData/medick_The_fogOFwar.cfg`).

> ⚠️ Vision mods alter how much of the world you can see. Recommended for offline play.

## Credits

Settings-screen injection technique by **KG / war3i4i** ([LastEpochImprovements](https://github.com/war3i4i/LastEpochImprovements)) — the same approach that powers the Terrible family's native settings integration.

## Building from source

```bash
dotnet build medick_FogOfWar -c Release
```

Requires Last Epoch (with MelonLoader installed) at the default Steam path; the build auto-copies the DLL into the game's Mods folder. Source layout:

```
medick_FogOfWar/
  medick_FogOfWar.csproj
  src/
    FogOfWarMod.cs          MelonMod lifecycle (manual per-patch application)
    FogOfWarMod.Patches.cs  Harmony patches (Minimap.Awake, settings panel)
    FogController.cs        Capture / apply / live BLIND hide+restore
    FogLevels.cs            Level → radius contract (see SPEC.md)
    SettingsUi.cs           The Terrible fog_OFwar settings category
    NativeSettings.cs       KG-derived native settings injection helpers
    BuildInfo.cs            Brand/version single source of truth
    Prefs.cs                Persisted settings (frozen cfg path)
```

See [SPEC.md](SPEC.md) for the behavior contract and [CHANGELOG.md](CHANGELOG.md) for history.
