# Terrible Death Log, Assessment and Counter

October 2, 2026, 12:45 PM PT. Experimental v0.1.2 local review build. This report supersedes the earlier advice, season and reassessment reports. No Nexus release, GitHub push, merge, installation or game launch was performed.

## Request coverage

| Request | Implemented behavior | Verification limit |
|---|---|---|
| Smaller, quieter counter | Number stays visible; background and deaths label are faint while idle. Hover reveals Death counter, MOVE and help. Hidden controller cursor does not expand it. | Needs live mouse/controller inspection |
| Intuitive movement | Drag the visible MOVE handle or use Move counter in the panel; Done saves the position. | Needs live drag/restart test |
| Readable review panel | Larger text, wrapped paragraphs, separate panel/HUD size options, five tabs, a wrapped title using the new name | Needs live layout inspection |
| Normal loading should not look broken | Routine unresolved-player/loading state goes to optional debug output; a sustained detection failure can still warn. Missing optional hooks are debug details. | Latest available game log is the older v0.1.1 session |
| Capture the actual cause | Added local nine-argument and network ten-argument game death-report hooks, localized attacker/ability/ailment, primary/secondary elements, nullable crit flag, boss flag, irregular source and readable game-colored text. Health-loss hooks remain separate. | Method signatures verified in installed metadata; callbacks not yet verified in game |
| Avoid losing details during transitions | Freeze original death, hits, ailments, location and fresh defenses before delayed commit. Match late reports to a pending/recent death; reject ambiguous rapid deaths and another character. | Core regression tests pass; native ownership/timing still needs live testing |
| Reset without destroying history | Reset only the displayed per-character/session counter. Confirmation includes the requested Medick judgment joke. Saved deaths and game counter remain intact. | Core reset/persistence tests pass |
| Strong, concise advice | At most three distinct actions. Measured resistance gaps first; critical protection, recovery, typed debuffs and relevant mitigation follow evidence. Pool advice requires heavy-hit evidence and covered core gaps. Ward is conditional, not a universal prescription. | Core tests pass; native stat reads and Season 5 mechanics need live checks |
| Colored death log | Fire red, cold blue, lightning yellow, necrotic teal, void purple, poison green, physical neutral. The game's rich death text keeps its colors at our readable font size. | Parser tested; live colors/layout pending |
| Current gear reassessment | Reads fresh stats from the same living character, uses the original area level, reports measured changes and supported resistance damage ratios. Missing stats remain unknown. | Core tests pass; game stat reads pending |
| Preserve original and every update | Separate dated reassessment snapshots store original capture, current stats, advice, grade, algorithm version and optional previous-update link. Reassess repeatedly and browse paginated updates. | Persistence and immutability tests pass |
| Delete reassessments | Confirm deletion of one update or all updates for this death. Original deaths, counts, other deaths and derived updates remain intact. | Failure, corruption, duplicate and deletion-scope tests pass |
| Grade or percentage | Explained A/B/C/D or unrated defense progress; supported modeled damage-change percentages. No invented probability of surviving a whole fight. | Whole-hit units, ward absorption, future attacks and movement are not sufficiently captured for calibrated fight odds |
| Season and Legacy history | Death-time realm/localized season label is saved with the original death and every reassessment snapshot. Transfer does not relabel past deaths. No public season number inferred from internal IDs. | Core migration tests pass; native cycle service read pending |
| Zone and boss context | Freeze exact internal scene ID and positive ZoneInfoManager zone level when available. Scene IDs keep numeric suffixes. Game boss flag remains nullable; no inference from zone names. | Localized area name lookup not yet established |
| Boss tips menu | Five starter profiles: Lagon, Emperor of Corpses, Heorot, Chronomancer Julra and Harbinger of Hatred. Source links, known mechanics, concise tactical inferences and typing limits are visible. | Starter catalog, not a complete boss roster; historical sources are not Season 5 live validation |
| Automatic and offline boss guides | A confirmed boss link requires explicit game flag plus exact attacker alias. Offline/local reports can offer an attacker guide while keeping encounter classification unknown. Manual browsing never changes the death. | English aliases only so far; adds, proxies and unmatched variants stay unidentified |
| New identity, minimal install | Display brand changed to Terrible Death Log, Assessment and Counter. Internal Melon name remains Medick death log. DLL/config/log identifiers stay medick_DeathCounter. ZIP contains only the DLL; Season 5 repair remains embedded. | Experimental, staged only |

## Capture fixes in this pass

The delayed commit previously reread character and location after respawn or scene unload. It now freezes the verified death before those changes, including the last fresh living realm, class, level, hardcore status, raw scene, area level and defenses. Missing actors do not cancel an already verified pending death.

The two report signatures do not contain a sequence ID. A report arriving across two rapid deaths is rejected rather than attached by guesswork. A matching local report preserves the extra boss flag from an immediately preceding network report of the same blow. It cannot carry that flag across a different attacker, amount or explicit false flag. A previous death's static text is no longer read as this death's cause.

If only the game counter confirms a death after respawn, the record stays counted but does not substitute town location, current defenses, current realm, unrelated hits or previous death text for missing original context. A toast for the previous character also no longer appears on a newly selected character.

## Checks

- CoreTests: **214 passed** on .NET 8.
- NoGame Release build with DeployToMods=false and warnings as errors: **0 warnings, 0 errors**.
- Local game-reference Release build with DeployToMods=false and warnings as errors: **0 warnings, 0 errors**.
- InteropGuard: **both modes passed**, 75 interop member calls, compared with 42 shipped DLLs. Existing Application.OpenURL allowance retained.
- InteropGuard checks direct member references. It does not prove reflection reads, Harmony callbacks, report ownership, native timing, rendering or advice accuracy in combat.
- Final local DLL SHA256: **39A715596FA64B0747FEED42B0A6269A61D26FB60DACDCF09094E6537150D386**.
- Installed DLL SHA256 remains **2A02FE16530697BD2765753D5B4B42D4C99C15ADA8266F4F9D6C75B8E19EF704**. No game files changed.
- Full validation transcript: work/death-full-request-validation.txt.

The available Latest.log still describes v0.1.1: one counted death without a cause, and no new game-report hook execution. It cannot validate this v0.1.2 candidate.

## Remaining native limits

Ward-only hits are not part of the health-loss timeline. A sampled ward value does not establish sustain or absorption. Network report ownership still requires the currently resolved local ActorSync pointer; the local report is the fallback during missing-player state. We need a live test before deciding whether a cached native pointer is safe or necessary.

The exact raw scene is retained and explicitly labeled Scene, not presented as a verified localized area name. The zone level is context; numeric advice still uses the original fresh defense snapshot's area level. Old records retain unknown new fields rather than being backfilled from today's location.

Old unknown deaths cannot be reconstructed if their cause was never recorded. New hooks aim to capture future deaths. A defense grade describes progress against captured evidence, not whether the player can face-tank an unseen attack.

## Native test queue for your return

1. Authorize/install the staged local DLL with the game closed. Launch and check the new brand, ordinary loading output and both game-report hooks.
2. On a softcore character, inspect zero counter, idle/hover behavior, Move counter/Done, persistence after restart, hidden controller cursor and panel size controls.
3. Record a normal hit death and an ailment death. Compare the game's displayed killer, ability, primary/secondary type, crit, boss flag and count with JSON/text and the panel. Verify one death gives exactly one record, including quick respawn/scene load.
4. Confirm resistance units and snapshot age using probe output. Test a resistance gap, a capped heavy hit, critical protection and DoT recovery. Confirm ward-only loss is not mislabeled as health loss.
5. Reassess the original death with new gear, reassess an older saved update, restart and review both. Delete one and then all updates for one death; confirm original log bytes and counts remain unchanged.
6. Test Lagon/Julra or another supported boss: compare raw scene/area level and game flag. Test an add or proxy killer, offline missing flag and manual browsing. A manual choice must never relabel the death.
7. Check a seasonal and Legacy character. Preserve the old death label if the character transfers later. A real transfer test remains pending until one is available.

## Boss source notes

Catalog research was delegated as requested and checked against the installed metadata and source pages. Only Julra currently carries published encounter element coverage, from [Tunklab's dungeon data](https://lastepoch.tunklab.com/dungeon/temporal_sanctum). Other profiles deliberately avoid assigning damage types from attack names or community guesses. Death-specific primary and secondary types come from the game report.

Lagon's ability names come from [EHG 0.9 notes](https://forum.lastepoch.com/t/the-convergence-update-beta-0-9-patch-notes/51975). Emperor of Corpses' Soul Bomb distance behavior and arena context come from [EHG 0.9l](https://forum.lastepoch.com/t/beta-0-9l-patch-notes/58685) and [0.9i](https://forum.lastepoch.com/t/beta-0-9i-patch-notes/57471). Heorot's Ice Spike/freeze interaction is documented in [EHG 1.0.3 notes](https://forum.lastepoch.com/t/last-epoch-patch-1-0-3-patch-notes/68385). Harbinger of Hatred's distinct Dive Bomb variant appears in [EHG 1.2.1 notes](https://forum.lastepoch.com/t/last-epoch-patch-1-2-1-notes/76245). Julra and Temporal Shift are documented in [EHG's 0.8.4 announcement, reproduced by LE Tools](https://www.lastepochtools.com/news/article/eternal-legends-beta-0-8-4-patch-notes-45588). Fight-plan recommendations are labeled inferences, not claims of measured avoidance or current bugs.
