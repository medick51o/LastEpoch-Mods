# Terrible Inventory v2.0.0 — council synthesis (2026-09-10 22:35)
Seats (blind, read-only, packet C:\Sync\Projects\ti-review-2026-09-10): 🔵 Astra (gpt-6-astra, architecture) · 🟢 Gemini (3.6 Flash High, robustness) · 🟣➤🌙 Kimi k3-high (code quality, 💸) · 🟣➤🔷 GLM 5.2-high (data safety, 💸). ⚫ Grok benched (seat hung twice tonight). Cursor allowance: 4 of 10/week used tonight.
Provenance: game runs a 2026-07-21 DLL (37,376 B, md5 cf953111…) whose source exists nowhere on disk (Haiku harvest of C:\Sync, Downloads, OneDrive Desktop, Documents, source). Repo = released v2.0.0 (35,328 B). Decision: repo is truth; the next build overwrites the mystery DLL.

## Agreement across seats (blind → counts)
| # | Finding | Seats | Rank | Fix |
|---|---|---|---|---|
| 1 | Quick Teleport unlock gate FAILS OPEN when the unlock list is unreadable (TravelService.cs:159-179) | Gemini, GLM, Astra | MATERIAL | fail closed: require positive unlock evidence |
| 2 | `WaypointEnabled=true` + `EnableWaypoint()` mutate game state BEFORE the load; not restored if the load throws (TravelService.cs:108-120); ARCHAEOLOGY already calls it a removal candidate | GLM, Astra | MATERIAL | move after confirmed fire, or remove |
| 3 | No combat/arena/dungeon/loading gate before `LoadWaypointScene` (TravelService.cs:54-120) | GLM, Astra | MATERIAL / NOT PROVEN that the game doesn't gate internally | add a state guard or in-hand test first |
| 4 | Two static `_keepAlive` delegate lists grow forever (NativeClone.cs:18,42; NativeSettings.cs:35,188) | Gemini, Kimi, Astra | MINOR→MATERIAL over a long session | clear on rebuild / own per control |
| 5 | Settings rows dead on 1.4.7: literal donor name "Toogle - Minion Health Bars" + fixed child indices (NativeSettings.cs:27-64). SAME pattern in Terrible Tooltips and fog_OF_war | all four | MATERIAL (degrades cleanly, but the panel is gone for everyone on 1.4.7) | resilient template search by component type; shared across the three mods |
| 6 | STASH ALL: per-item `catch { }` hides failures; "attempted N" log can't tell a partial dump from a full one (InventoryUi.cs:225-235) | Kimi, GLM | MATERIAL | count + Dbg.Log failures |
| 7 | STASH ALL snapshots coordinates, not items: an item moved mid-loop → its replacement gets stashed (InventoryUi.cs:200-233). Never sells/drops; equipped gear is another container | Astra MATERIAL · Kimi/GLM NOT PROVEN | MATERIAL | bind moves to item identity; cancel on context change |
| 8 | `_stashAllRunning` can stick for the session if the coroutine dies before `finally` (InventoryUi.cs:199,238) | GLM | MINOR | reset on scene load |
| 9 | Shared travel guard: any scene callback clears `_travelInProgress`; an older waiter can clear a newer op (TravelService.cs:38,138) | Astra | MATERIAL | operation identity |
| 10 | Teleport build failure after a successful footer can't self-heal on reinject (TeleportMenu.cs:126; InventoryUi.cs:39,104) | Astra | MATERIAL | track footer/menu completion independently |
| 11 | Primer snapshots only initially-inactive ancestors; no `finally` (TravelService.cs:248-272) — the world-map wipe area | Astra | MATERIAL, recurrence NOT PROVEN | full snapshot + finally; touch carefully |
| 12 | Cleanups: rename `medick_Advanced_Inventory.csproj` → Terrible_Inventory; version literal duplicated (BuildInfo.cs:11 / csproj:9); csproj auto-deploys on every build (add DeployToMods condition); README says MelonLoader 0.7.2+; HideIcon uses `GetChild(1)` (NativeClone.cs:80) | Kimi, Gemini, Astra | MINOR | mechanical |

Verdicts: Gemini/Kimi/GLM = PATCH everything, nothing needs rebuild. Astra = REBUILD NativeSettings + TravelService, PATCH the rest. Disagreement named, not smoothed: the three PATCH votes are on the shipped behaviour (safe, degrades cleanly); Astra's REBUILD is on ownership/lifetime structure. Conductor read: patch now (Tier A), and the NativeSettings rebuild is a SHARED job for all three mods (same broken pattern), worth its own ticket.

## Proposed Tier A (one builder ticket, one cross-vendor review, one in-hand pass) → v2.0.1
Items 1, 2, 4, 6, 8, 12 (+ item 7 if the builder can bind by item identity cheaply; else 2.0.2). Item 5 is the shared settings-adapter ticket. Items 3, 9, 10, 11 → Tier B (need design decisions / in-hand tests).
