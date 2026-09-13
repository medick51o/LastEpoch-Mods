# MORNING KIT — Terrible fog_OF_war v1.0.1 (council pass, overnight 2026-09-11)

**STAGED ONLY. Nothing deployed, committed, or pushed.** The game currently runs the PRE-Tier-A **1.0.1-dev** DLL you approved last night (Mods DLL 24,064 B, 2026-09-10 21:35, md5 9f168102… — verified unchanged after every step tonight). That DLL has the BLIND leaf-only fix but NONE of tonight's Tier A items. The whole 1.0.1 delta (last night + tonight) is still uncommitted in the repo.

## Version
Stays **1.0.1** (unreleased; last night's bump kept, not bumped again). Built DLL: `medick_FogOfWar\bin\Release\net6.0\medick_The_fogOFwar.dll` (26,112 B, 2026-09-11 02:29, md5 c09af22a31b806c316fc4dbf5b608309). Build: 0 warnings, 0 errors (conductor re-ran it from the project folder with restore).

## What changed — the WHOLE 1.0.1 delta (council finding # in ..\council-2026-09-11\SYNTHESIS.md)
0. (last night, 2026-09-10, already in the game) BLIND hides ONLY the Minimap leaf object, never a `*minimap*` ancestor — the ancestor also held the ground-label canvas, which made dropped loot invisible and unpickable. Hide uses a CanvasGroup (alpha 0, no raycasts) with `SetActive(false)` as fallback; if the leaf itself ever contains a `GroundItemLabel`/`UITooltipItem` the hide is refused and BLIND degrades to HARD with a warning. (TICKET-02b, Gemini APPROVE_WITH_NOTES)
1. The CanvasGroup hide is now transactional: the restore record is registered BEFORE the alpha/raycast writes; if a write throws mid-way the values roll back and the SetActive fallback runs; if even the rollback throws the record stays so leaving BLIND still restores it, and no second mechanism is applied (FogController.cs). Kimi + Astra. (#1)
2. Dead minimap entries from earlier BLIND zones are pruned when a new one is registered — the per-frame BLIND check no longer scans a growing list (FogController.cs). All four seats. (#3)
3. Diagnostics latch once per session instead of repeating every frame under a future game patch: the "BLIND — minimap not hidden" refusal and "apply failed" (FogController.cs). Astra + GLM. (#5)
4. Two silent catches now warn once: zone-default `RevealRadius` unreadable (LIMITED/SCOUT would scale off the 150 fallback) and prefs save failed (your level may not persist) (FogController.cs, Prefs.cs). Kimi + Astra + GLM. (#6, #7)
5. Settings-screen diagnostic: when the game's toggle template is missing, the ONE warning now also lists the settings root's child names (max 60) — next launch's MelonLoader log tells us the real 1.4.7 row name so the settings adapter can be repaired (NativeSettings.cs). All four seats. (#8)
6. Comment/doc sweep: the "hidden minimap receives no Updates / costs nothing" comment corrected (true only for the SetActive fallback); SPEC rule 4 rewritten to the leaf-only law, SPEC title 1.0.1; README: MelonLoader 0.7.x (tested 0.7.2, game 1.4.7), the Minimap.Update re-assert patch listed, and one honest paragraph that on 1.4.7 the settings rows do not appear and the cfg is the control until the adapter is fixed. (#4, #13, #14)
7. csproj: `<Compile Remove="docs\**\*.cs" />` (inert here — docs\ is a sibling of the project folder — kept for parity with the sibling mods); the `DeployToMods` Condition from last night verified, not duplicated. CHANGELOG v1.0.1 section extended. (#16)

NOT changed, on purpose: what any level does on its success path, the CanvasGroup restore path (no `SetActive(true)` added — Tier B #9 below), `CreateEnumDropdown` (kept; seats split), the deploy-by-default csproj target (your workflow; Tier B #17).

## What Andrew must check in-hand (≤5 steps)
1. Copy `bin\Release\net6.0\medick_The_fogOFwar.dll` (26,112 B) over `Last Epoch\Mods\medick_The_fogOFwar.dll` (the 1.0.0 backup `.1.0.0-restore` is already there; back up the 24,064 B dev DLL too if you want to A/B). Launch, load a zone with cfg `VisionLevel = "HARD"` as now. Melon log must show `MedicK's Terrible fog_OF_war v1.0.1 ready — vision level HARD` and no new warnings.
2. THE BLIND FRAME CHECK (open item from last night, NOT PROVEN by any seat): set `VisionLevel = "BLIND"` in the cfg (or via the rows if they build), zone in, look at the minimap corner. Expected: the map is gone. Tell the next session whether an EMPTY FRAME/border remains — that decides whether the hide target must widen to the smallest frame-holding subtree that is still below the ground-label canvas.
3. Loot check in BLIND (the incident): drop an item from the inventory, walk away and back. The ground label MUST show and the item MUST be pickable. Then open and close the overlay map; the minimap must stay hidden.
4. Leave BLIND (cfg → HARD then zone, or a row click): the minimap must come back, no frame ghost, no "minimap hide partially applied" warning in the log.
5. Open Settings once and quit. In the new MelonLoader log, find the line `settings hierarchy changed (template 'Toogle - Minion Health Bars' missing; root children: …)` and paste the `root children:` list to the next session — that is the input for repairing the settings rows on 1.4.7 (Tier B #8; the same fix will serve Terrible Tooltips and Terrible Inventory).

## Tier B queue (needs your decision or an in-game test)
- #8 the settings adapter for 1.4.7 (after step 5 reveals the row names; shared with the two sibling mods).
- #9 GLM's sequence: BLIND → a cutscene that deactivates the minimap object itself → leave BLIND. If the minimap stays gone, the CanvasGroup restore needs a `SetActive(true)` for objects the GAME parked while we owned the group — but that contradicts the "never resurrect a game-parked object" law, so it is your call after seeing it.
- #2 a spawn-time/reparent guard for a future game update that parents labels under the Minimap leaf (all three code seats: NOT PROVEN today).
- #10 `_writtenRadii` cap 32 / global radius provenance (Astra); #12 pin listener delegates like Tooltips 3.0.2 (Kimi, NOT PROVEN); #15 delete-or-keep `CreateEnumDropdown` (seats split 2-2); #17 make the csproj Mods-copy opt-in (Kimi/Astra); #19 verify what MelonLoader 0.7.2 does with a misspelled `VisionLevel` (Gemini: NORMAL fallback; GLM: could throw at init).
## Tier C (rebuilds)
Astra's ownership-tracked restore + per-lifetime radius provenance; mutation guards on every entry during teardown (#11); the vestigial `IsChildOf` clause in `IsHidden`; `RestoreDefaults` restoring only the last instance's radius; Settings toggle references never torn down.

## Seats and spend
🔵 Astra (gpt-6-astra) council + 🔵 Codex (default) build — subscription. 🟢 Gemini council + review — subscription; brain UNREPORTED both calls. 🟣➤🌙 Kimi k3-high: 46,313 in / 12,350 out. 🟣➤🔷 GLM 5.2-high: 41,217 in / 3,341 out. Cursor credits this mod: 87,530 in / 15,691 out (2 calls; **10 of the 10/week window now used** — no Cursor seats left this week). Neither Cursor seat could compute MD5s (read-only shell blocked); both said so.

## Open / notes
- Review verdict: 🟢 Gemini PASS, no findings, A1–A8 all DONE, MD5s match the conductor's. Zero repair rounds.
- Provenance: the DLL in the game's Mods folder is byte-identical to the repo's 21:32 Release build of the pre-Tier-A source. No gap.
- Settings rows tonight: the logs show the template probe failed in EVERY session (19:51, 21:11, 21:30, 21:37), so no rows were built; the level values that changed between launches came from the cfg at startup. If you did change levels through an in-game panel, it was not this mod's rows — say which, because the code has no other path.
- Files: docs\council-2026-09-11\{SYNTHESIS.md, signed\SIGNED-{astra,gemini,kimi,glm}.md} · docs\build-2026-09-11\{TICKET-A.md, DELTA-A.patch, VERIFY-OUTPUT.txt, BUILDER-REPORT.md, REVIEW-gemini.md, baseline\ (*.baseline + MD5SUMS.txt)} · CURRENT-WORK.md · mirror C:\Sync\Projects\fog-review-2026-09-11 (commits 8c26b08 pre, a36542f brief, c9e9482 post).
