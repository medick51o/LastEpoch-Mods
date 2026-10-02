# Terrible Death Counter and Log: expanded advice

Latest update October 2, 11:57 AM PT: `CODEX-DEATH-REASSESSMENT-HISTORY.md` covers saved updates, scoped deletion and defense progress grades. Current tests: 198/198. The source and review ZIP are refreshed. Earlier descriptions/counts/hashes below are historical.

Update October 2, 11:36 AM PT: read `CODEX-DEATH-CURRENT-ASSESSMENT.md` for the latest 182-test snapshot, current-stat review button and stricter health/ward evidence gates. The refreshed ZIP includes that work and season history. Older counts/hashes and pool-priority descriptions below are historical.

Update October 2, 11:20 AM PT: the current source and review ZIP also include per-death season/Legacy history. Read `CODEX-DEATH-SEASON-CONTEXT.md` for the latest 163-test verification and hashes. Counts and hashes below describe the earlier advice snapshot.

Local review build only. Nothing installed, merged, pushed or published. The installed 0.1.2 DLL remains unchanged. These changes have not been tested in game.

## What changed

One shared rule engine now serves the death card and Patterns. The card shows at most three distinct actions, ordered by priority: observed crit protection issues, measured resistance gaps, recorded defensive debuffs, damage-over-time recovery, then supported pool, control, hit-defense and behavior checks. A recorded gap outranks a missing-stat check. Ties have stable ordering. No filler actions are added.

Each resistance owns its gap, matching shred and repeated-gap context. Crit and Critical Vulnerability share one card. All damage-over-time ailments share one recovery card. Armor and Armor Shred share one card. A repeated ability, repeated attacker and boss context compete for one behavior card. Patterns excludes capped facts and diagnostic fallbacks, and combines recovery across different ailments without claiming that one ailment appeared in every death.

The engine considers both killing damage types without dividing a mixed hit in half. Known ailment types can supply missing damage types. Seven elemental resistance-shred names retain their element through the existing ailment tap. Generic shred does not acquire an invented element. Freeze and Chill now have different advice. Damned's regen penalty, Poison's resistance loss, Frostbite's freeze effect, Time Rot's longer stuns and Doom's melee vulnerability add context to the same recovery card.

Unknown resistance asks the player to check the sheet, rather than claiming a gap. A capped resistance is a recorded fact, excluded from actions. Capped endurance and full reduced crit bonus protection do not prompt further investment. An observed Critical Vulnerability does not override recorded full reduced crit bonus protection. Missing dodge or block does not become zero. Snapshots older than two seconds are not used to prescribe a measured gap.

Recent history is limited to the last 20 deaths on the same character, at or before the death being reviewed. A repeated named ability takes priority over a repeated attacker. Repeated elemental gaps are included in the existing resistance card, rather than producing another recommendation.

Confidence names report availability, health-loss events, defense timing/coverage and crit-flag availability. Crit-on-DoT and crit-with-full-snapshot-avoidance conflicts lower confidence. High coverage requires a report, timeline, max health, known crit flag and fresh relevant defense values. It is not confidence that a gear change would have prevented death.

## Capture and math

- A missing crit argument remains unknown and cannot overwrite a previously known crit. An explicit false still updates the record.
- Boss context and the irregular-source enum name are saved. An unmapped enum is not classified as a ground effect or damage over time.
- The snapshot now also reads reduced bonus damage from crits, block effectiveness and area level. The member names were checked against this laptop's Season 5 Il2CppLE metadata. Reflection access and value units still need an in-game check.
- Area level comes from DifficultyManager.areaLevel, never character level. Block chance and block effectiveness retain different units. Stun avoidance stays a rating.
- Known area level enables a modeled resistance damage ratio, armor mitigation and standard dodge/block estimates. Known reduced crit bonus enables a comparable-critical-hit ratio. These descriptions are estimates with other modifiers unchanged, not reconstructions of the fatal hit.
- A resistance snapshot already reflects its debuffs. The ratio does not subtract shred again. No stack count, mixed-hit split, ward history, extra-health survival amount or guaranteed survival is invented.
- HUD movement, compact idle state, readable panel, colors, reset/history controls, settings, embedded Season 5 repair and DLL/config identity are preserved.

## Research coverage

| Research rules | Implementation |
|---|---|
| R01, R11 | Crit protection and Critical Vulnerability; full bonus-protection and snapshot conflicts handled |
| R02, R03, R10, R14 | Measured resistance gap, typed shred, Shock, Poison self-shred, Marked for Death, armor shred; capped notices excluded |
| R04, R09 | One recovery card for DoT and observed attrition; ailment-specific recovery context |
| R07, R08 | Observed one-hit/burst buffer and hit-defense checks; report-only cause never creates a timeline |
| R12, R13 | Separate Freeze/Chill behavior, Frostbite context, stun avoidance as a rating |
| R16, R17, R18 | Physical-hit armor, recorded zero dodge/block, measured endurance gap and health-only threshold behavior |
| R19, R20, R21 | Recent same-character attacker/ability history and repeated measured elemental gaps; no separate repeated-resistance card |
| R22, R23 | Recorded boss context; known area-level penetration inside resistance advice |
| R24 | Honest missing-evidence fallback without a generic gear checklist |
| R05 | Deferred: damage/overkill units and the remaining health/ward pool have not been validated |
| R06 | Raw source captured; hazard advice deferred until source meanings and damage behavior are mapped in game |
| R15 | Deferred: a community health floor is not a measured deficiency for the current build |

This covers 21 of the 24 rule themes with evidence gates and merged actions. It does not implement the supplied numerical evidence-bonus scoring formula: explicit rule priority puts measured gaps above generic checks. Numeric freeze chance, ward-decay prescriptions, combined survival previews and encounter-specific tactics remain unwired where inputs or meanings are unverified.

## Validation

- CoreTests: 154/154 passed on .NET 8. Includes the earlier 124 tests, 30 new regressions and revised assertions for the intentionally safer missing-stat, capped-only and confidence wording.
- NoGame and local-game Release builds: DeployToMods=false, warnings as errors, zero warnings/errors in both modes.
- InteropGuard passed both modes: 75 calls checked against 42 reference DLLs; the existing fenced Application.OpenURL exception remains. This validates hard references, not reflection behavior at runtime.
- Installed DLL SHA256: 2A02FE16530697BD2765753D5B4B42D4C99C15ADA8266F4F9D6C75B8E19EF704.
- Review DLL SHA256: C2B00CBCBD751379A24AE0C383F93447052D84F72F76185DD56B6D57B09E9AAA.
- Assembly/mod version remains 0.1.2; the advice-review filename and hash distinguish this experimental build from the installed binary. The test ZIP contains only medick_DeathCounter.dll at its root.

## Inputs and remaining checks

The Grok Bot return was read first. The earlier hand-port preserved the newer local UI and game work instead of overwriting it with a958f94. The original research files remain in research/advice. This expansion replaces the initial hand-port's limited selector and wires the defensible math described above.

Primary source checks on October 2, 2026: [Negative Ailments](https://support.lastepoch.com/hc/en-us/articles/46361887879963-Negative-Ailments) supports the named debuff distinctions; [Critical Strikes](https://support.lastepoch.com/hc/en-us/articles/46361891709211-Critical-Strikes) supports the difference between avoidance and reduced crit bonus. The negative-ailment page displayed February 14, 2026, differing from dates in parts of the supplied research. The July guide formulas were not re-verified against a readable Season 5 patch body.

After installation is separately authorized: compare the new stats with the character sheet; verify report flags and late updates; trigger a typed shred and control ailment; verify same-character history and unchanged UI controls; measure damage, overkill and health/ward before enabling survival calculations. Report retention during a HasPlayer drop, late-packet identity across rapid deaths and ward-only hit coverage remain open native issues. Do not describe the complete death logger as proven until these are exercised in game.
