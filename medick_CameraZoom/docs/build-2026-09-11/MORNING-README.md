# MORNING KIT — Terrible Zoom v1.0.1 (council pass, overnight 2026-09-11)

**STAGED ONLY. Nothing deployed, committed, or pushed.** The game still runs v1.0.0 (Mods DLL 24,064 B, 2026-07-01 21:00, md5 14c58069… — verified unchanged after every step).

## Version
1.0.0 → **1.0.1** (BuildInfo.cs + csproj `<Version>`). Built DLL: `bin\Release\net6.0\medick_CameraZoom.dll` (25,088 B, 2026-09-11 01:58, md5 2ef9a852d165615397489966376be28e). Build: 0 warnings, 0 errors (conductor re-ran it with restore).

## What changed (one line per item; council finding # in ..\council-2026-09-11\SYNTHESIS.md)
1. Panel input lock no longer releases Terrible Cooldowns' lock: `_applied` is set only when THIS mod flipped the flag (InputGuard.cs). Mirror image of the Cooldowns fix staged earlier tonight; both sides now symmetric. All four seats. (#1)
2. Bad config values can't lose the panel or feed NaN into the camera prefs: NaN/Infinity in ZoomMin/ZoomPerScroll/ZoomSpeed/Angle/MenuScale/PanelX/PanelY reset to defaults on load, one warning line names what was repaired (Prefs.cs). Finite values are never touched. (#3)
3. End no longer opens/closes the panel while you are typing in chat or a search box: gated on the game's own `EpochInputManager.IsInputFieldActive` (CameraZoomMod.cs). (#4)
4. Diagnostic only: if `CameraManager.instance` itself throws for 300 consecutive updates (a future game patch), one warning line "camera manager unreachable for 300 updates — mod idle…" instead of silent idling (CameraZoomMod.cs). (#5)
5. Comment/doc sweep: "v2 / v1.x" narration retconned (CameraState.cs); Prefs descriptions now quote what 1.4.7 actually ships (zoomMin -7.0, default -17.5, perScroll 0.2, speed 2.5) — the "More negative = further out" wording is deliberately UNTOUCHED (see Tier B #2); README install line: MelonLoader 0.7.x (tested 0.7.2 Open-Beta, game 1.4.7). (#6)
6. csproj: `<Version>1.0.1</Version>` + `<Compile Remove="docs\**\*.cs" />`. NO copy-to-Mods target was added — this csproj never had one. CHANGELOG v1.0.1 section + a note listing the 5 inert pre-Terrible cfg keys (FOV, MinFOV, MaxFOV, SmoothSpeed, RmbModifier); the `[kg_CameraZoom]` cfg section is another mod's and is untouched. Nothing deletes anything. (#7, #8)

NOT changed, on purpose: zoom direction, the per-frame reclamp, the ZoomMin range (-200..-1), the live-zoom slider, Rescue. The council split on whether the mod's zoom sign is backwards (Tier B #2 below) — that is your call after a two-minute in-game test, not the night crew's.

## What Andrew must check in-hand (≤5 steps)
1. Copy `bin\Release\net6.0\medick_CameraZoom.dll` over `Last Epoch\Mods\medick_CameraZoom.dll` (back up the old one first), launch, load a zone. Melon log must show `MedicK's Terrible Zoom v1.0.1 ready` and NO "Repaired non-finite preferences" line (your cfg is clean). Camera must feel exactly as last night.
2. Press End; the panel opens where it always did (650, 310), same scale, ZoomMin -1, tilt locked 47°. Close it; movement returns.
3. The Cooldowns test (both mods at once): open Terrible Zoom's panel and hover it (lock on), then open AND close the Cooldowns panel — Zoom's lock must hold while the pointer is on Zoom's panel. Then the reverse: open Cooldowns' panel (its lock on), open and close Zoom's panel; Cooldowns' lock must still hold. (Only works fully once Cooldowns v1.0.1 is also in-hand; with v1.0.0 Cooldowns the Cooldowns→Zoom direction is unchanged from last night.)
4. Open chat (Enter), type a word, press End: NO panel; Esc out of chat, press End: panel. (Finding #4.)
5. THE DIRECTION TEST (Tier B #2, two minutes): with the panel open read the status row "live — zoom A → B". Scroll the wheel one way and watch whether B goes UP (toward 0) or DOWN (toward -40). Then drag "Zoom-out limit" from -1 to -40 and note whether the camera moves closer or further. Tell the next session which way is "out"; the code assumes more negative = further out, the game's own numbers (zoomMin -7.0 above default -17.5) suggest the opposite, and your daily -1.0 setting is the tiebreaker.

## Tier B queue (needs your decision or an in-game test)
- #2 zoom direction: after step 5, either flip the reclamp + labels + ZoomMin range (if higher = out) or document the convention. Also decides: should Rescue leave the camera at the game default (-17.5)? Today it pins to whatever ZoomMin is (Astra).
- #9 per-field fault isolation + one "compatibility" line at startup (a renamed CameraManager member today aborts the whole Apply block every frame, and a renamed `resetZoom` silently kills Rescue). Worth doing before the next game patch.
- #10 capture ownership at write boundaries (Astra), #15 panel taller than a small display at Menu scale 2.0.
- Deleting the 5 legacy cfg keys by hand (documented in CHANGELOG; no code deletes them).
## Tier C (rebuilds)
Cross-mod input-lock ownership service shared with Terrible Cooldowns (the real fix behind #1; both mods now carry the cheap version); camera-owner arbitration with a second camera mod (#11); per-frame OnGUI cache (#12); Theme teardown / retry-leak (#13); hotControl decoupling (#14).

## Seats and spend
🔵 Astra (gpt-6-astra) council + 🔵 Codex (default) build — subscription. 🟢 Gemini council + review — subscription; brain UNREPORTED both calls. 🟣➤🌙 Kimi k3-high: 34,364 in / 8,996 out. 🟣➤🔷 GLM 5.2-high: 52,553 in / 3,552 out. Cursor credits this mod: 86,917 in / 12,548 out (2 calls; 8 of the 10/week window used tonight after this mod). Neither Cursor seat could compute MD5s (read-only shell blocked); both said so.

## Open / notes
- Review verdict: 🟢 Gemini PASS, no findings, A1-A6 all DONE, MD5s match the mirror. Zero repair rounds.
- Provenance: the DLL in the game's Mods folder is byte-identical to the repo's July 1 Release build. No gap.
- Builder deviation: Codex's sandbox could not read NuGet.Config, so it built with `--no-restore` (ticket-authorized). The conductor re-ran the full command with restore from the repo root: green.
- The untracked `release/` folder in the repo predates tonight (July 1); not touched. `release\_archive\medick_CameraZoom_v2.0.0.zip` is the pre-renumbering build from June 10.
- Files: docs\council-2026-09-11\{SYNTHESIS.md, signed\SIGNED-{astra,gemini,kimi,glm}.md} · docs\build-2026-09-11\{TICKET-A.md, DELTA-A.patch, VERIFY-OUTPUT.txt, BUILDER-REPORT.md, REVIEW-gemini.md, baseline\} · CURRENT-WORK.md · mirror C:\Sync\Projects\zoom-review-2026-09-11 (2 commits: 806b1c1 pre, 9217df8 post).
