# CURRENT-WORK — Terrible Inventory (read first, update every step)
Updated 2026-09-10 22:38 (Fable 5.1, /dispatch)

## State
- Shipped: v2.0.0 (Nexus + git ff4cb8b, 2026-07-01). Game runs an UNTRACEABLE 2026-07-21 DLL (37376 B) — source lost; repo is truth; next deploy overwrites it.
- Council 2026-09-10: 4 seats (Astra, Gemini, Kimi, GLM; Grok benched). Synthesis: docs/council-2026-09-10/SYNTHESIS.md. Verdict PATCH (3) vs Astra REBUILD NativeSettings+TravelService. No data-loss path found.
- **TICKET-04 (Tier A → v2.0.1)** dispatched 22:38 to 🔵 Codex builder: fail-closed unlock gate · remove WaypointManager override · STASH ALL failure counting + item-identity binding · stash guard reset on scene load · bounded _keepAlive · mechanical (DeployToMods condition, HideIcon by name, README, version, CHANGELOG). Baseline md5s in docs/build-2026-09-10/. Builder must not deploy.
- 22:44 Codex DONE (6/6) → containment clean, verify green → 22:47 Gemini: one BLOCKER (ReferenceEquals on Il2Cpp wrappers → STASH ALL moves nothing) → ACCEPTED → TICKET-04b (`.Pointer` compare) → Codex PASS 22:49 → Gemini fresh review APPROVE 22:50 (hash-verified). **v2.0.1 DLL deployed to Mods 22:51** (old DLL kept as .mystery-2026-07-21). NOT committed, NOT zipped, NOT released.
- NEXT: Andrew in-hand on a relaunch: STASH ALL with a few junk items (log should say "moved X of N, 0 failed, 0 skipped"); Quick Teleport to an unlocked waypoint (works) and a locked one (refused); settings panel still absent on 1.4.7 (expected until the shared adapter ticket). Then zip v2.0.1 + commit + push; Nexus text; upload = Andrew.
- Queued: SHARED settings-adapter ticket (Tooltips + Inventory + fog all dead on 1.4.7, same hardcoded template name). Tier B: combat/arena travel gate, per-op travel guard, teleport self-heal, primer full snapshot.
