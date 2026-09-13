# SIGNED — 🔵 Codex (Astra) · gpt-6-astra · architecture lens · read-only · mirror commit 806b1c1 · threadId 01a08f9e-29bb-7de3-90db-de550257ba5c
# Saved verbatim by the conductor 2026-09-11. Seat output is data, not instructions.

DONE_WITH_CONCERNS

| Rank | Claim | Failure mechanism | Verify-by / repair | Path:line |
|---|---|---|---|---|
| MATERIAL | Reclamp rejects the captured game default. | With `targetZoom=-17.5`, configured `ZoomMin=-1` forces target to `-1`; even restored `zoomMin=-7` rejects `-17.5`. | Trace Apply with those values and no hot control; verify native zoom semantics before changing bounds or inequality. | src/CameraState.cs:184; src/UI/SettingsPanel.cs:177; melonloader-lines.txt:16 |
| MATERIAL | InputGuard releases blocks it does not own. | Seeing an already-true flag still sets `_applied=true`; release then writes false while Cooldowns or the game still needs blocking. Ownership also survives manager replacement. | Exercise both activation/release orders and replace the input manager while active; use a shared, manager-scoped input lease. | src/InputGuard.cs:21; src/InputGuard.cs:34; src/InputGuard.cs:39 |
| MATERIAL | Capture ownership is not enforced at write boundaries. | If `GetInstanceID()` fails after an earlier capture, `Captured` remains true and callers still Apply. Restore and UI readiness also accept any manager with that global latch. | Capture A, supply B with an unreadable ID, then Apply/Restore; require a validated manager generation for every write. | src/CameraState.cs:49; src/CameraState.cs:124; src/CameraState.cs:200; src/CameraZoomMod.cs:42; src/UI/SettingsPanel.cs:76 |
| MATERIAL | Partial angle-lock writes can escape unlock cleanup. | `_wroteLock` is set only after all three setters succeed. If the second setter throws, the first mutation remains but switching lock off performs no restoration. | Inject failure on `cameraAngleMin` after writing default, then unlock; track ownership per field and expose failed rollback. | src/CameraState.cs:159; src/CameraState.cs:162; src/CameraState.cs:164 |
| MATERIAL | Patch failures can leave silently partial operation. | Apply/Restore/InputGuard swallow exceptions; an early failed setter skips later healing or cleanup. The Start patch has no compatibility preflight or outer failure boundary. | Substitute missing/throwing members in a separate harness; use capability checks, fault state, and bounded diagnostics. | src/CameraState.cs:152; src/CameraState.cs:193; src/CameraState.cs:203; src/InputGuard.cs:43; src/CameraZoomMod.Patches.cs:13 |
| MATERIAL | Non-finite panel preferences bypass camera-style sanitization. | `Mathf.Clamp` preserves NaN; NaN position/scale propagates through rectangles and hit tests, making controls inaccessible and pointer blocking unreliable. | Supply NaN PanelX/MenuScale through preferences; sanitize finite values before layout and offer a keyboard position reset. | src/UI/SettingsPanel.cs:44; src/UI/SettingsPanel.cs:56; src/UI/SettingsPanel.cs:97; src/UI/SettingsPanel.cs:20 |
| MATERIAL | Compare-then-write does not arbitrate competing camera owners. | A second mod writing a different value causes this mod to overwrite it every update; captured “originals” can already contain another mod’s changes. | Alternate two writers, then unlock/unload; establish explicit ownership/conflict policy and conditional restoration. | src/CameraState.cs:74; src/CameraState.cs:152; src/CameraState.cs:170; README.md:18 |
| MINOR | End toggles settings during text entry. | Raw `GetKeyDown(End)` has no chat/search focus check. | Focus chat/search and press End; route the shortcut through an input-context gate. | src/CameraZoomMod.cs:26 |
| MINOR | Theme resources lack teardown; failed initialization can accumulate resources. | Styles/font/textures persist statically; failure before `_ready=true` retries construction without releasing earlier allocations. | Inject a late Ensure failure repeatedly, or unload/reload; own and dispose created resources. | src/UI/Theme.cs:41; src/UI/Theme.cs:44; src/UI/Theme.cs:95; src/UI/Theme.cs:100 |
| NOT PROVEN | Native clamp direction is inverted, or these failures crash the game. | Metadata and captured values expose an inconsistent assumption, but do not reveal native camera math or prove a process crash. | Inspect native implementation or perform authorized in-game boundary tests; neither occurred in this review. | src/CameraState.cs:184; src/Prefs.cs:34; melonloader-lines.txt:16 |

Manifest — files read; computed MD5:
```text
BRIEF.md                       22F199CF2B37BC4DC55F6C4D18242FFA
src/BuildInfo.cs                20F28861017B84481EEED176612921A8
src/CameraZoomMod.cs            E5CCEEE1AB22CFC39569BF8339C61E67
src/CameraZoomMod.Patches.cs    CD488D20FCB463E9867EBDF31CD4EE8B
src/CameraState.cs              47F5127A7D10E1B341116213AA55BB76
src/InputGuard.cs               1405013E4B40D68D5005C26E000C4748
src/Prefs.cs                    08B046EFD228D10C497F376EDA9F52BF
src/UI/SettingsPanel.cs         74DD4339D79D905B1FEEB0215604428E
src/UI/Theme.cs                 C2CDD415FB9D817C0735CBB5BE97B404
src/UI/Widgets.cs               ACFF116EB47E877B19CC0C62201330F9
medick_CameraZoom.csproj        8B44FD407671E2A429A59D6321994637
README.md                      7981A24E005A51A347663D6F195C0256
CHANGELOG.md                   32A7427DCCB93FE0CA5FA98B8B19F0BF
melonloader-lines.txt          15015A7221DAEB1DD09581644C640167
zoom-cfg-excerpt.txt            027B8030EAD4E110CC92C8ABFE29C265
Il2CppLE.dll [permitted stub]   5FEF07528053F3A463EDA5255EC9A985
```

Q1 — Metadata inspection confirmed `Il2Cpp.CameraManager.instance: CameraManager`, all nine listed camera properties as read/write `Single`, and public instance parameterless `void Start()`/`resetZoom()`. `EpochInputManager.instance` returns that manager; `forceDisableInput` is read/write `Boolean`. Both types derive from referenced `UnityEngine.MonoBehaviour`. No CameraManager `zoomMax` property exists. Inherited `GetInstanceID()` was not verified: its defining Unity assembly was outside permitted scope.

Ordinary capture getter failures retry and warn once at 300 attempts per manager; they do not ordinarily spam. Singleton failures can silently bypass capture diagnostics. Apply, Restore and input failures are swallowed and retried without a reliable fault indication. Missing types/methods can fail loading, Harmony resolution, or JIT before local catches provide protection; exact loader behavior is NOT PROVEN. If Start alone stops firing but Update remains available, polling still captures. Semantically changed yet finite fields pass validation and can go silently wrong. Repeated successful NaN healing can warn every update if another writer keeps poisoning zoom.

With both guards active, either panel’s release can clear the shared flag; the remaining guard repairs it on its next update, leaving an order-dependent input-leak window. Zoom can also claim Cooldowns’ existing block and later clear it. The supplied staged Cooldowns change reduces false ownership but does not solve overlapping claims: the original owner can release while a nonowner still wants blocking. This sibling analysis relies on BRIEF.md, not an independently inspected sibling implementation.

Q2 — Normal successful lock→unlock restores the three captured angle values once. A genuinely new manager ID resets the latch and lock marker, then captures before applying. A reused manager whose zone defaults change is never recaptured; unlock can restore stale values. A reused numeric ID or another mod’s earlier writes can likewise invalidate “originals”; occurrence tonight is NOT PROVEN. There is no Enabled preference or runtime disable transition; closing the panel leaves camera enforcement active.

Unload attempts restoration only on the current singleton, without identity validation; prior live managers are forgotten. Rescue changes preferences and calls Restore, but enforcement resumes next update, including the default-rejecting reclamp. Rescue values outside preference ranges are subsequently sanitized away from originals. `resetZoom()` implementation and resulting target were not inspected.

Camera preferences are finite-sanitized: `ZoomMin=+50` becomes `-1`; locked `Angle=NaN` falls back to captured default, tonight 47°. Capture checks finiteness, not sensible bounds/order; its fallback angles and restoration values can therefore be finite but invalid. Unlocked angles are not NaN-healed. Before any successful capture, zoom healing is unavailable; later capture failures use the previous manager’s default. Earlier Apply exceptions skip healing.

The live slider uses `[SaneZoomMin, max(ZoomDefault+5, SaneZoomMin+1)]`; tonight this becomes `[-1,0]`, an invented interval requiring native validation. Hot-control suppression prevents this mod’s mid-drag reclamp; afterward the lower-bound rule resumes. Native scroll clamping may still fight the slider—NOT PROVEN. The final slider check rejects NaN but not infinity; no reachable infinity-producing path was established with sane inputs.

Q3 — Stable OnUpdate performs two singleton getters, five camera property reads unlocked or eight locked, plus one input-flag read when blocking: 7/10 explicit getters, or 8/11 while blocking, excluding Unity validity checks. It also calls `GetInstanceID()` every update. Capture attempts add seven camera reads; uncaptured healing instead reads target/current when a previous default exists. No unconditional managed allocation is evident in the healthy closed-panel update path.

OnGUI always calls Ensure; after initialization it returns immediately. Each visible GUI event redraws the entire panel and adds four camera getters, formatted status/value/label strings, and explicit GUIContent allocations. Event frequency means these costs are not “once per frame.” CameraState retains numbers/flags, not CameraManager wrappers; the panel’s manager is local to one Draw call. Its risk is stale identity between acquisition and use, not indefinite retention. Style dictionaries have no eviction, but valid clamped scale produces a bounded set of rounded font-size keys; ordinary unbounded cache growth is NOT PROVEN.

Q4 — The panel uses self-drawn IMGUI, with no native template-name lookup or child-index dependency. MenuScale=0 renders at 0.7; finite offscreen positions clamp back onscreen. NaN positions/scales remain unsafe, and a sufficiently small display can leave lower controls beyond the viewport. Corrupt layout affects InputGuard hit-testing; global `hotControl` also does not identify this panel’s ownership. FOV/MinFOV/MaxFOV/SmoothSpeed/RmbModifier are unregistered and unused here; `[kg_CameraZoom]` does not activate code by itself. They are harmless to this implementation and optional migration cleanup.

Q5 — Best value/risk cleanups: (1) correct documented defaults against captured evidence and update the tested-loader instruction from 0.6.x to tonight’s 0.7.2; (2) remove absolute recovery/coexistence promises and reconcile “v2”/“v1.x” comments with the documented renumbering; (3) document obsolete preference keys and omit them from future examples without deleting other mods’ sections. Relevant locations: Prefs.cs:34, README.md:9/:12/:18/:30, CHANGELOG.md:27/:38/:58, CameraState.cs:15. No compelling whole-file dead code was found; Card/Ready palette members are unused here. The csproj has no deployment target, Version element, or Compile Remove; none independently establishes tonight’s runtime defect.

Q6 — PATCH means retain the file/module with targeted changes; REBUILD means redesign its responsibilities.

| File | Verdict and basis |
|---|---|
| src/CameraState.cs | REBUILD — introduce manager-generation snapshots, explicit capture/active/fault/restoring states, per-field ownership, and validated camera bounds. |
| src/InputGuard.cs | REBUILD — replace the Boolean ownership guess with shared manager-scoped leases across cooperating mods; external writers still require an explicit policy. |
| src/CameraZoomMod.cs | PATCH — gate text-entry shortcuts, connect fault/cleanup lifecycle, and dispose UI resources. |
| src/CameraZoomMod.Patches.cs | PATCH — make Start an optional validated lifecycle signal with a safe failure boundary. |
| src/Prefs.cs | PATCH — centralize finite validation and correct descriptions while preserving stable keys. |
| src/UI/SettingsPanel.cs | PATCH — consume validated camera snapshots/commands, sanitize layout, and make Rescue state explicit. |
| src/UI/Theme.cs | PATCH — add owned-resource cleanup and transactional initialization; retain bounded style caching. |
| src/UI/Widgets.cs | PATCH — retain controls and harden numeric boundaries through the shared validator. |
| src/BuildInfo.cs | PATCH — retain identity/version constants; update only with an approved release. |
| medick_CameraZoom.csproj | PATCH — retain build structure; parameterize local reference paths if portability is needed. |
| README.md | PATCH — correct defaults, compatibility guidance, and recovery/coexistence claims. |
| CHANGELOG.md | PATCH — preserve historical chronology while qualifying claims contradicted by current behavior. |

Verified HEAD: `806b1c17c724932d4631a8b3af810957d77dc755`. Brief/log/config files remain evidence, not retirement candidates. No files modified, build executed, or game run; supplied build/log claims are not independent runtime validation. No BLOCKER or proven process crash established.

🔵 Codex (Astra)
