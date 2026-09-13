# MORNING KIT — Terrible Cooldowns v1.0.1 (council pass, overnight 2026-09-11)

**STAGED ONLY. Nothing deployed, committed, or pushed.** The game still runs v1.0.0 (Mods DLL 40,448 B, 2026-07-01, md5 c4210da0… — verified unchanged after every step).

## Version
1.0.0 → **1.0.1** (BuildInfo.cs + csproj `<Version>`). Built DLL: `bin\Release\net6.0\medick_CooldownTracker.dll` (41,984 B, 2026-09-11 01:28, md5 7b18d822422d4c0ce9c0e1f3dd6eab10). Build: 0 warnings, 0 errors (conductor re-ran it).

## What changed (one line per item; council finding # in SYNTHESIS.md)
1. Movement lock no longer releases Terrible Zoom's input lock: `_applied` is set only when THIS mod flipped the flag (InputBlocker.cs). All four seats. (#1)
2. Bad config values can't lose the panel: NaN/Infinity in Alpha/Size/Offsets/MenuScale/PanelX/PanelY reset to defaults, InputMode clamped 0-3, CtrlLayout 0-2, one warning line names what was repaired (Prefs.cs). (#3)
3. `Camera.main` looked up once per frame instead of on every GUI event, inside a catch (OverheadRenderer.cs). (#4)
4. Diagnostic only: if a slot gets re-registered from a different still-alive AbilityBarIcon (a second action bar / minion bar), one warning per session in the Melon log: "slot #N re-registered from a different live AbilityBarIcon — a second action bar may exist; report this line". Last-wins behaviour unchanged. (#2)
5. Stale "v4.x / v5" comments retconned (InputBlocker, InputDetection, OverheadRenderer, Theme). Comment-only. (#5)
6. README install line: MelonLoader 0.7.x (tested 0.7.2 Open-Beta, game 1.4.7). (#6)
7. csproj: `<Version>1.0.1</Version>` + `<Compile Remove="docs\**\*.cs" />` (the sibling mods have the same line; without it the review baselines under docs\ get compiled). NO copy-to-Mods target was added — this csproj never had one. CHANGELOG v1.0.1 section + a note listing the 12 inert v4.x config keys (nothing deletes them). (#7, #8)

## What Andrew must check in-hand (≤5 steps)
1. Copy `bin\Release\net6.0\medick_CooldownTracker.dll` over `Last Epoch\Mods\medick_CooldownTracker.dll` (back up the old one first), launch, load a zone. Melon log must show `MedicK's Terrible Cooldowns v1.0.1 ready` and NO "Repaired invalid preferences" line (your cfg is clean).
2. Press Home; the panel opens where it always did; labels, sliders, Xbox mode and all seven slots exactly as before (settings carry over untouched).
3. The Zoom test: open Terrible Zoom's panel with its input guard on, then open AND close the Cooldowns panel. Zoom's lock must still hold. Then close Zoom's panel; movement returns. (This is the fix for finding #1.)
4. Fire two skills with different cooldowns; icons float above the character with sweep fill as before; nothing drawn on the login screen after quitting to menu.
5. Grep the Melon log for "re-registered from a different live AbilityBarIcon". If it appears, that's the evidence the Tier B "second action bar" fix is real; if not, that finding stays NOT PROVEN.

## Tier B queue (needs your decision or an in-game test)
- #2 owner/instance identity for the slot registry — only if step 5 shows the warning.
- #9 spec-swap with DebugLog=true (does the game reuse an AbilityBarIcon with a new abilityNumber?), #10 death test (do icons stay anchored on the corpse?).
- #11 icons re-centre when a cooldown ends (Gemini calls it a bug; it's the current centred-cluster design). Your call: fixed grid vs centred.
- #12 Home doesn't close the panel while typing in a label field (deliberate today). Your call.
- #13 panel taller than a 1080p screen at Menu scale 2.0 with 7 slots. #14 Theme init latch / resource teardown.
- Deleting the 12 legacy cfg keys by hand (documented in CHANGELOG; Astra advises against silent code deletion).
## Tier C (rebuilds)
Cross-mod input-lock ownership service shared with Terrible Zoom (the real fix behind #1); Harmony capability gating with one compatibility diagnostic (#16); GUI allocation hoisting + layout dedupe (#15).

## Seats and spend
🔵 Astra (gpt-6-astra) council + 🔵 Codex (default) build — subscription. 🟢 Gemini council + review — subscription; brain UNREPORTED both calls. 🟣➤🌙 Kimi k3-high: 53,457 in / 7,247 out. 🟣➤🔷 GLM 5.2-high: 38,979 in / 4,009 out. Cursor credits total: 92,436 in / 11,256 out (2 calls; 6 of the 10/week window used tonight after this mod). Neither Cursor seat could compute MD5s (their read-only shell was blocked); both said so.

## Open / notes
- Review verdict: 🟢 Gemini PASS, no findings, A1-A7 all DONE, MD5s match the built files. Zero repair rounds.
- GLM's "Camera.main is deprecated in Unity 2023+" claim is false (Unity 6 keeps it); the per-frame cache was done on the other seats' MINOR grounds.
- The untracked `release/` folder in the repo predates tonight (July 1); not touched.
- Deviation: the Gemini review call ran a `dotnet build` inside the review MIRROR (C:\Sync\Projects\cooldown-review-2026-09-11 gained untracked bin\ and obj\). No tracked mirror file changed, the mirror csproj has no copy target, and the game's Mods DLL was re-verified unchanged afterwards. The repo was never touched by a reviewer.
- Files: docs\council-2026-09-11\{SYNTHESIS.md, signed\SIGNED-{astra,gemini,kimi,glm}.md} · docs\build-2026-09-11\{TICKET-A.md, DELTA-A.patch, VERIFY-OUTPUT.txt, BUILDER-REPORT.md, REVIEW-gemini.md, baseline\} · mirror C:\Sync\Projects\cooldown-review-2026-09-11 (2 commits).
