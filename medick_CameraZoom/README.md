# MedicK's Terrible Zoom

*a camera mod* — **Terrible Zoom** for short. A [MelonLoader](https://melonwiki.xyz) mod for **Last Epoch** that extends the camera's zoom-out range and exposes every camera parameter through a live in-game settings panel.

> Nexus Mods: [Camera Zoom](https://www.nexusmods.com/lastepoch/mods/27) · by medick · internal name `medick_CameraZoom`

## Features

- **Extended zoom-out** — push `zoomMin` far past the game's ≈ −15 default (mod default −40, slider to −200)
- **Live tuning** — scroll sensitivity, zoom lerp speed, and a live "current zoom" slider that moves the camera as you drag
- **Angle lock** — pin the camera tilt to any angle (20–85°); unlocking restores the game's *real* captured limits
- **Self-healing camera** — originals are captured only when every value reads back clean, nothing invalid is ever written, and a poisoned zoom is automatically snapped back to the game default. The "stuck camera, restart required" failure of v1.x is engineered out
- **Rescue button** — one click restores every camera value the game shipped with
- **Settings panel that stays put** — drag it anywhere; position is saved and restored every session

## How it works

`Il2Cpp.CameraManager` owns all camera state. The mod adjusts its fields (`zoomMin`, `zoomPerScroll`, `zoomSpeed`, `targetZoom`, `cameraAngle*`) — writing `Camera.main.fieldOfView` does nothing, the manager overrides it every frame. There is no `zoomMax` field. Values are compare-then-write: the mod only touches a field when it actually differs, so it coexists with per-scene resets and other camera mods.

## Controls

| Input | Action |
|-------|--------|
| Scroll wheel | Zoom in / out (native) |
| `End` | Toggle settings panel |
| Drag title bar | Reposition panel (saved automatically) |

## Installation

1. Install [MelonLoader 0.6.x](https://melonwiki.xyz) into Last Epoch
2. Drop `medick_CameraZoom.dll` into `Last Epoch/Mods/`
3. Load into a zone, scroll out, press `End` to tune

Upgrading from v1.x: just replace the DLL — your settings carry over.

> ⚠️ Camera mods alter how much of the world you can see. Recommended for offline play.

## Building from source

```bash
dotnet build -c Release
```

Requires Last Epoch (with MelonLoader installed) at the default Steam path; edit `<ML>` and `<GM>` in the `.csproj` if yours differs. Source layout:

```
src/
  CameraZoomMod.cs          MelonMod lifecycle
  CameraZoomMod.Patches.cs  Harmony patch on CameraManager.Start
  CameraState.cs            Capture / apply / sanitize / rescue
  BuildInfo.cs              Brand/version single source of truth
  Prefs.cs                  All persisted settings (MelonPreferences)
  UI/
    Theme.cs                Terrible-family design system
    Widgets.cs              Switch, sliders, rows
    SettingsPanel.cs        End-key panel (persisted position)
```

See [CHANGELOG.md](CHANGELOG.md) for version history.
