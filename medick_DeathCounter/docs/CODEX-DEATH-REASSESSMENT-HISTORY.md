# Terrible Death Counter and Log: saved reassessment history

October 2, 2026, 11:57 AM PT. Experimental v0.1.2 review source. Nothing installed, merged, pushed or published. Earlier current-stat assessment, season history and advice work remain included.

## Original death and updates

Every successful **Reassess and save new update** click creates a dated record in `UserData/medick_DeathCounter/reassessments.jsonl`. It stores current readable stats, the original death capture used, comparisons, advice, grade, algorithm version and an optional link to the selected earlier update. The original `deaths.jsonl`, `deaths.txt`, death count and Patterns are untouched by reassessment.

New deaths receive unique IDs. Older deaths use a deterministic link from their recorded character, death number and timestamp without being rewritten. Later killer details, changed gear or a transfer to Legacy do not change that link. The old seasonal death remains seasonal. Each assessment owns copies of its capture and result, so later game details or advice changes cannot silently rewrite the saved update.

Open a death, click **Saved updates**, and choose an assessment. The list has five readable entries per page with newer/older navigation and no artificial history limit. **View original death** returns to the actual death. Reassessing a selected update creates another record; it retains original-to-current comparisons and records changes since the selected update. Assessments never count as new deaths.

**Delete this update** removes only that assessment after confirmation. **Delete all these updates** removes the current death's reassessments after confirmation. The original and other deaths' updates remain. Derived updates have complete snapshots and survive deletion of an earlier update. These operations write only the reassessment file. Failed writes/deletions keep the existing saved entries and show a clear message. An unsaved result is not presented as successfully saved.

The log writes through a temporary file, flushes, and atomically replaces its own file. Unreadable/future-schema data is retained on rewrites. Duplicate copies of a deleted ID are removed so they cannot resurrect it on the next load. A failed initial read blocks writes rather than replacing inaccessible history.

## Grades and percentages

The interface gives an explained **defense progress grade**, not a calibrated chance to survive the entire fight.

| Grade | Interpretation |
|---|---|
| A | Relevant core checks covered, max health not lower, a measured defense improved, and no major supported risk remaining in the advice |
| B | Core checks covered, but improvement or hit/recovery/debuff/capture risks remain unresolved |
| C | Known gaps remain, or a measured tradeoff needs review |
| D | Known gaps remain and a measured defense worsened |
| ? | Cause or relevant current stats are insufficient |

Known low fire resistance produces a repeat-risk verdict with the actual gap. Newly closed resistance/crit gaps improve the assessment. A large reported overkill, mixed damage with missing resistance, recurring debuffs or an instantaneous ward peak cannot become a declaration of safety. Missing stats are never treated as zero.

Where old/current resistance and the original area level are known, the comparison shows a modeled damage-change percentage for that damage type. It does not substitute the current town level or split a mixed hit evenly. Unverified damage/overkill units, absent ward history, recovery uptime, movement and unrecorded attacks prevent a reliable whole-fight survival percentage. Even grade A is limited to the measured checks.

Recommendations stay at three distinct actions, with measured resistance gaps first. Ward remains conditional on existing combat ward use; a single reading does not prove sustain. A larger current health pool is acknowledged before asking for more investment.

## Verification

- CoreTests: **198/198 pass**. Sixteen new history/grade tests cover original death-file byte preservation, continuous counter, frozen snapshots, stored advice/rating reload, repeated updates, one/all deletion scope, derived updates, legacy links, late details, corrupt/future data, duplicate resurrection, disk failures, cross-parent/character rejection, and evidence-based grades.
- NoGame and local game warnings-as-errors builds: zero warnings and errors with `DeployToMods=false`.
- InteropGuard passes both builds: 75 calls checked against 42 reference DLLs; existing fenced `Application.OpenURL` exception unchanged.
- Installed DLL remains unchanged: `2A02FE16530697BD2765753D5B4B42D4C99C15ADA8266F4F9D6C75B8E19EF704`.

## Native checks queued

After installation is authorized, save an assessment, change gear, save another, close/reopen the panel and restart the game. Verify both updates, their timestamps, original capture, grades and differences survive. Select the earlier update and reassess it again. Delete one and all updates through the confirmation controls while checking original death files, count and other deaths' updates. Review a pre-ID death, a seasonal/Legacy death, and a death that receives later killer details. Compare grades and resistance readings with the character sheet; check missing stats, mixed damage and ward peaks. Check pagination, text wrapping and controller/mouse interaction at the user's resolution. No new behavior was tested in game here.

## Staged files

- `Terrible_Death_Counter_and_Log_v0.1.2-advice-review.zip`: exactly one root `medick_DeathCounter.dll`.
- `CODEX-DEATH-ADVICE-PORTED-SOURCE.zip`: refreshed source plus required guard/repair tools, excluding generated build files.
- Candidate DLL SHA256: `5B79C79F3257330E52DFA0C7882707A728A1BEB128633637228B7556BB50AD5D`.

This remains a review build, not a public versioned release. Earlier reports' hashes, counts and temporary-assessment behavior describe their prior snapshots.
