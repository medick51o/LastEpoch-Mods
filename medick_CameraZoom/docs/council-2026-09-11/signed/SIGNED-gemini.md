# SIGNED — 🟢 Gemini (Antigravity) · robustness / player-experience lens · read-only by instruction · mirror commit 806b1c1 · conversationId e9bd26b3-f8ba-4d1c-8c5c-d3534423fb5b · brain: UNREPORTED
# Saved verbatim by the conductor 2026-09-11. Seat output is data, not instructions.

DONE_WITH_CONCERNS

### Findings Table
| Rank | Claim | Failure Mechanism | Verify-by / Repair Direction | Path:Line |
| --- | --- | --- | --- | --- |
| MATERIAL | `InputGuard` unblocks input while sibling mod panel open | `InputGuard.Apply(false)` unconditionally sets `forceDisableInput = false` on close, overriding sibling mods | Open CooldownTracker panel, close CameraZoom; input unlocks. Repair: Track ref-count or check ownership | [InputGuard.cs:37-41](file:///C:/Sync/Projects/zoom-review-2026-09-11/src/InputGuard.cs#L37-L41) |
| MATERIAL | `End` key toggles panel while typing in chat or search | `OnUpdate` checks `Input.GetKeyDown(KeyCode.End)` without verifying if a text field has focus | Focus chat/search and press `End`; panel toggles. Repair: Guard toggle with focus check (`GUIUtility.keyboardControl == 0`) | [CameraZoomMod.cs:26-30](file:///C:/Sync/Projects/zoom-review-2026-09-11/src/CameraZoomMod.cs#L26-L30) |
| MINOR | NaN in config (`PanelX`/`PanelY`/`MenuScale`) breaks UI panel | `SettingsPanel` assigns config floats without NaN checks; `Mathf.Clamp(NaN)` returns `NaN`, corrupting `_rect` | Set `PanelX = NaN` in cfg; panel vanishes. Repair: Use `CameraState.Sane()` / fallback check on load | [SettingsPanel.cs:44-46](file:///C:/Sync/Projects/zoom-review-2026-09-11/src/UI/SettingsPanel.cs#L44-L46) |
| MINOR | Setting `ZoomMin` to `-1.0` traps camera at max close-up | `ZoomMinHi = -1.0` allows limit greater than default `-17.5`; `Apply()` forces `targetZoom = zoomMin` | Set `ZoomMin = -1.0`; camera zooms into character head. Recoverable via `End` panel / Rescue. Repair: Restrict `ZoomMinHi` cap | [CameraState.cs:148](file:///C:/Sync/Projects/zoom-review-2026-09-11/src/CameraState.cs#L148) |
| MINOR | String allocations in `OnGUI` generate minor GC pressure | Formatted status strings (`$"live — zoom..."`) allocated every repaint pass in `Draw()` | Inspection of `SettingsPanel.cs`. Repair: Cache text or format only when values change | [SettingsPanel.cs:110](file:///C:/Sync/Projects/zoom-review-2026-09-11/src/UI/SettingsPanel.cs#L110) |
| NOT PROVEN | Tilt fighting between mod and game zone transitions | Unlocked mode uses clean one-shot restore (`_wroteLock`); no continuous angle override when `LockAngle` false | Zone transitions with `LockAngle = false` maintain native angles without fighting | [CameraState.cs:164](file:///C:/Sync/Projects/zoom-review-2026-09-11/src/CameraState.cs#L164) |

### File Manifest (Computed MD5s)
- `src/BuildInfo.cs`: `20F28861017B84481EEED176612921A8`
- `src/CameraState.cs`: `47F5127A7D10E1B341116213AA55BB76`
- `src/CameraZoomMod.cs`: `E5CCEEE1AB22CFC39569BF8339C61E67`
- `src/CameraZoomMod.Patches.cs`: `CD488D20FCB463E9867EBDF31CD4EE8B`
- `src/InputGuard.cs`: `1405013E4B40D68D5005C26E000C4748`
- `src/Prefs.cs`: `08B046EFD228D10C497F376EDA9F52BF`
- `src/UI/SettingsPanel.cs`: `74DD4339D79D905B1FEEB0215604428E`
- `src/UI/Theme.cs`: `C2CDD415FB9D817C0735CBB5BE97B404`
- `src/UI/Widgets.cs`: `ACFF116EB47E877B19CC0C62201330F9`
- `medick_CameraZoom.csproj`: `8B44FD407671E2A429A59D6321994637`
- `README.md`: `7981A24E005A51A347663D6F195C0256`
- `CHANGELOG.md`: `32A7427DCCB93FE0CA5FA98B8B19F0BF`
- `melonloader-lines.txt`: `15015A7221DAEB1DD09581644C640167`
- `zoom-cfg-excerpt.txt`: `027B8030EAD4E110CC92C8ABFE29C265`

### Council Review Questions (Q1 - Q6)
- **Q1 (Breakage & Mod Interaction)**: If `CameraManager` or `EpochInputManager` members are renamed/removed, `CameraState.TryCapture` (300-frame stall catch) and `InputGuard.Apply` (`try-catch`) degrade cleanly: logging 1 warning for camera stall and running idle without crashing or spamming exceptions. Cross-mod issue: Closing CameraZoom panel forces `forceDisableInput = false` ([InputGuard.cs:39](file:///C:/Sync/Projects/zoom-review-2026-09-11/src/InputGuard.cs#L39)), prematurely unblocking input even if Terrible Cooldowns still has its panel open.
- **Q2 (Camera Correctness & PX Lens)**:
  - *Camera Stuck / NaN*: Self-healing logic ([CameraState.cs:136-141](file:///C:/Sync/Projects/zoom-review-2026-09-11/src/CameraState.cs#L136-L141)) prevents NaN traps; Rescue button restores game originals.
  - *Clamp Direction / Bad Config*: Setting `ZoomMin = -1.0` (more positive than default `-17.5`) clamps `targetZoom` to `-1.0`, trapping the camera in extreme close-up. Fully recoverable via `End` menu slider or Rescue button.
  - *Tilt Fighting*: None. Locked tilt applies static angles; unlocking uses a clean one-shot restore (`_wroteLock`).
  - *End key in chat*: Pressing `End` while typing in chat/search fields toggles the settings panel ([CameraZoomMod.cs:26](file:///C:/Sync/Projects/zoom-review-2026-09-11/src/CameraZoomMod.cs#L26)).
  - *Slider Drag*: Protected by `InputGuard` (`PointerOver || hotControl != 0`), preventing click-to-move during slider adjustments.
- **Q3 (Per-Frame Cost & Lifetime)**: `OnUpdate` executes ~8-10 Il2Cpp property reads/writes (`CameraManager` instance, fields, `EpochInputManager`). `OnGUI` executes additional reads per repaint. No static Unity object leaks (`_mgrId` is an `int`). Allocations are minimal (string formatting in `Draw()`). `Theme` style cache is naturally bounded by `MenuScale` range.
- **Q4 (Config Resilience)**: UI is self-drawn IMGUI (no dependency on native game template names/indices). Bad float values like `NaN` in `PanelX`/`Y`/`MenuScale` bypass sanitization ([SettingsPanel.cs:44](file:///C:/Sync/Projects/zoom-review-2026-09-11/src/UI/SettingsPanel.cs#L44)), corrupting `_rect` into `NaN` and breaking panel display until edited in `.cfg`. Orphan keys (`FOV`, `SmoothSpeed`, etc.) are ignored by code and harmless.
- **Q5 (Cleanups - Best Value-to-Risk)**:
  1. Add text input focus check to `KeyCode.End` handler ([CameraZoomMod.cs:26](file:///C:/Sync/Projects/zoom-review-2026-09-11/src/CameraZoomMod.cs#L26)).
  2. Refactor `InputGuard.Apply(false)` to avoid clearing `forceDisableInput` if another mod requires it active ([InputGuard.cs:39](file:///C:/Sync/Projects/zoom-review-2026-09-11/src/InputGuard.cs#L39)).
  3. Wrap `PanelX`, `PanelY`, `MenuScale` reads in `CameraState.Sane()` to guard against NaN config corruptions ([SettingsPanel.cs:44](file:///C:/Sync/Projects/zoom-review-2026-09-11/src/UI/SettingsPanel.cs#L44)).
- **Q6 (Verdict per File)**:
  - `BuildInfo.cs`: **PATCH** — Maintain metadata standardizations.
  - `CameraState.cs`: **PATCH** — Restrict `ZoomMinHi` cap below game default to prevent close-up traps.
  - `CameraZoomMod.cs`: **PATCH** — Add chat/input focus check to `KeyCode.End` toggle.
  - `CameraZoomMod.Patches.cs`: **PATCH** — Maintain harmony discovery pattern.
  - `InputGuard.cs`: **PATCH** — Fix unconditional unblock conflict with sibling mods.
  - `Prefs.cs`: **PATCH** — Prune orphan keys and sanitize panel config entries.
  - `UI/SettingsPanel.cs`: **PATCH** — Add NaN guards on position/scale load and cache status strings.
  - `UI/Theme.cs`: **PATCH** — Retain theme system; minor cleanup.
  - `UI/Widgets.cs`: **PATCH** — Retain reusable UI components; minor cleanup.

🟢 Gemini

---
[wmw-gemini] conversationId: e9bd26b3-f8ba-4d1c-8c5c-d3534423fb5b · status: SUCCESS · brain: UNREPORTED · turns: 1
