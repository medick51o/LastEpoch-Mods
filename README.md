# Last Epoch Mods by medick

MelonLoader mods for **Last Epoch** (Il2Cpp · Unity 6000.0.42f1 · net6.0).

> Drop the compiled `.dll` into your `Last Epoch/Mods/` folder.  
> Requires **MelonLoader 0.6.x** — https://melonwiki.xyz

---

## Mods

### [medick_CooldownTracker](./medick_CooldownTracker) — v4.4
Floating skill icons that appear above your character's head while abilities are on cooldown.

- Live input-mode detection — auto-switches between **Keyboard**, **Xbox**, and **PS5** layouts
- Per-slot custom labels with two-line wrapping (type `Flame Ward` → displays stacked)
- **[▼] Button Picker** for Xbox & PS5 face buttons, bumpers, triggers, sticks, D-pad
- Per-mode independent label storage (keyboard labels never bleed into controller labels)
- **Movement Lock** toggle — blocks character movement while the settings panel is open  
  (uses `EpochInputManager.forceDisableInput`, the same mechanism as LEHelper)
- Slot 6 (evade/dodge) fully supported
- Persistent settings via MelonPreferences

**Key:** `Home` — toggle settings panel

---

### [medick_CameraZoom](./medick_CameraZoom) — v1.2
Extends Last Epoch's built-in scroll-wheel zoom and exposes all camera parameters live.

- Sets `CameraManager.zoomMin` to allow scrolling much further out (default −40 vs game default ≈ −15)
- Configurable **scroll sensitivity** (`zoomPerScroll`) and **lerp speed** (`zoomSpeed`)  
- Live **Current Zoom** slider — drag to reposition the camera instantly
- Optional **camera angle lock** with adjustable tilt
- Captures and displays original game values so you can always reset
- Persistent settings via MelonPreferences

**Key:** `End` — toggle settings panel

---

## Building from Source

Requirements:
- .NET SDK 6.0+
- MelonLoader 0.6.x installed in your Last Epoch directory
- Last Epoch at the default Steam path  
  `C:\Program Files (x86)\Steam\steamapps\common\Last Epoch\`

```bash
cd medick_CooldownTracker
dotnet build -c Release
# Output: bin/Release/net6.0/medick_CooldownTracker.dll

cd ../medick_CameraZoom
dotnet build -c Release
# Output: bin/Release/net6.0/medick_CameraZoom.dll
```

If Last Epoch is installed at a non-default path, edit the `<ML>` and `<GM>` property  
paths at the top of each `.csproj` file.

---

## License

MIT — fork freely, credit appreciated.
