# CURRENT-WORK — Terrible Inventory (read first, update every step)
Updated 2026-09-10 22:38 (Fable 5.1, /dispatch)

## State
- Shipped: v2.0.0 (Nexus + git ff4cb8b, 2026-07-01). Game runs an UNTRACEABLE 2026-07-21 DLL (37376 B) — source lost; repo is truth; next deploy overwrites it.
- Council 2026-09-10: 4 seats (Astra, Gemini, Kimi, GLM; Grok benched). Synthesis: docs/council-2026-09-10/SYNTHESIS.md. Verdict PATCH (3) vs Astra REBUILD NativeSettings+TravelService. No data-loss path found.
- **TICKET-04 (Tier A → v2.0.1)** dispatched 22:38 to 🔵 Codex builder: fail-closed unlock gate · remove WaypointManager override · STASH ALL failure counting + item-identity binding · stash guard reset on scene load · bounded _keepAlive · mechanical (DeployToMods condition, HideIcon by name, README, version, CHANGELOG). Baseline md5s in docs/build-2026-09-10/. Builder must not deploy.
- 22:44 Codex DONE (6/6) → containment clean, verify green → 22:47 Gemini: one BLOCKER (ReferenceEquals on Il2Cpp wrappers → STASH ALL moves nothing) → ACCEPTED → TICKET-04b (`.Pointer` compare) → Codex PASS 22:49 → Gemini fresh review APPROVE 22:50 (hash-verified). **v2.0.1 DLL deployed to Mods 22:51** (old DLL kept as .mystery-2026-07-21). NOT committed, NOT zipped, NOT released.
- NEXT: Andrew in-hand on a relaunch: STASH ALL with a few junk items (log should say "moved X of N, 0 failed, 0 skipped"); Quick Teleport to an unlocked waypoint (works) and a locked one (refused); settings panel still absent on 1.4.7 (expected until the shared adapter ticket). Then zip v2.0.1 + commit + push; Nexus text; upload = Andrew.
- Queued: SHARED settings-adapter ticket (Tooltips + Inventory + fog all dead on 1.4.7, same hardcoded template name). Tier B: combat/arena travel gate, per-op travel guard, teleport self-heal, primer full snapshot.

## 2026-10-08 18:39 — Quick Teleport broken by the Oct 2026 season (fix IN TEST, uncommitted)
- Symptom 1 ("not discovered"): era controllers' `unlockedScenes` now empty. Fix: `PlayerUnlockSources()` reads `PlayerFinder.TryGetLocalPlayerInfo(out p).getUnlockedScenes()` first (proven in-hand 18:18: gate passes). CharacterData/DataTracker lists read null; kept as fallbacks + in the debug dump.
- Symptom 2 (jump ignored until real map used): in-hand A/B 18:29-18:30: waypointEnabled=True both times; open+close real map alone arms it. Fix: primer pulses MapPanel (snapshot-restore, one frame). Deployed md5 7f640dfa8e63; awaiting first-click test + flicker check.
- Also added: `scene loaded` debug line, unlock dump, `travel ignored` 10s line. Game cfg DebugLog=true (backup MelonPreferences.cfg.pre-tpdebug-2026-10-08); old DLL = Mods\medick_Terrible_Inventory.dll.2.0.2-restore.
- NEXT: in-hand pass → Gemini review (Codex MCP down) → v2.0.3 → commit/zip; Nexus = Andrew. Turn DebugLog back off.

## 2026-10-08 18:55 — Quick Teleport FIXED IN-HAND (uncommitted, unreviewed)
- Andrew in-hand: Bazaar, Observatory, EoT all first-click (log 18:53-18:54). Deployed md5 1ecd05eda0c0.
- Root causes (Oct 2026 season): (1) unlock list moved -> PlayerFinder.TryGetLocalPlayerInfo().getUnlockedScenes(); (2) LoadWaypointScene ignored until the real map has been opened on the TARGET's section -> UIBase.openMap(zoomToSceneName: scene) + closeMap once per target per login (reset on character select / ignored jump). Interop gotcha: openMap's Nullable<> params must be non-null instances.
- Dead ends (recorded so nobody retries): CharacterData/DataTracker lists read null; one-frame MapPanel enable; openMap on the current era only.
- DebugLog set back to false in game cfg.
- NEXT: cross-vendor review (Gemini) -> v2.0.3 -> commit on a branch off main (NOT pr12-update) -> zip; Nexus = Andrew.

## 2026-10-08 19:22 — cross-vendor review done; reviewed build deployed for final in-hand
- Gemini r1 APPROVE_WITH_NOTES (2 MATERIAL, 1 MINOR) + Codex r1 CHANGES_REQUIRED (3 MATERIAL, 1 MINOR): all accepted except Gemini's characterselect-only reset (disputed on log evidence, hardened anyway).
- r2 (Codex + Gemini): 7/7 prior resolved except guard timeout (Codex NOT RESOLVED vs Gemini RESOLVED) -> ESCALATED to Andrew; 4 new accepted (primed reset, live list decisive/fail closed, close-failed log, dump isolation).
- r3/r4 (Codex gpt-6.1-sol): primer generation guard -> APPROVE_WITH_NOTES. Gate is now: live LocalPlayerInfo list decides alone; unreadable -> refuse.
- Codex CLI upgraded 0.157.1 -> 0.159.3 (gpt-6.1-sol needs >=0.158ish; 0.157 = "model not supported"). wmw-codex MCP still DOWN: `codex mcp-server` no longer exists; proposal = wmw_codex_mcp.py wrapper.
- Review artifacts: C:\Sync\Projects\_review-tmp\inv-2.0.3\ (delete after commit).
- Deployed reviewed DLL; awaiting Andrew's final in-hand. Then v2.0.3 + commit on branch off main (NOT pr12-update).
