# Terrible Death Counter and Log: season history

Update October 2, 11:36 AM PT: source and review ZIP now also include current-stat reassessment. Read `CODEX-DEATH-CURRENT-ASSESSMENT.md` for the latest 182-test count and candidate hash. This report's checks/hash describe the earlier season-history snapshot.

Updated October 2, 2026, 11:20 AM PT. Staged experimental source and review build only. Nothing installed, merged, pushed or published.

## Behavior

Each new death stores its own season or Legacy context in `deaths.jsonl`. Last Death, History and `deaths.txt` show that label. The snapshot belongs to the death, so transferring the same character to Legacy preserves its older seasonal entries and continues the existing counter. A delayed killer report can enrich the death without changing its season.

The probe checks the loaded character's name, reads `CharacterData.Cycle`, and asks the already initialized `CycleStatusService.EffectiveCycle` for its current realm. It uses `CycleInfo.LocalizationTag` with `Localization.GetText` for the season's display name. It does not initialize services or make a server request. Internal cycle IDs and fish names are saved for diagnostics but never presented as public season numbers.

Context is sampled while alive and frozen when the death becomes pending. The analyzer makes a separate copy for the saved record. History rows wrap the season and location metadata instead of shrinking it.

A readable effective seasonal realm without a localized name shows `Season (name unavailable)`. An unresolved realm shows `Season / Legacy unknown`. An explicit Legacy cycle can be recorded without the service. Old entries without this field remain unknown; their season cannot be reconstructed reliably from a date. Beta remains distinct from seasonal play. Offline status is stored when readable.

## Verification

- CoreTests: 163/163 pass, including nine new season-context regression tests.
- NoGame and local game builds: zero warnings, zero errors, with `DeployToMods=false` and warnings as errors.
- InteropGuard passes both builds: 75 interop calls checked against 42 reference DLLs. Existing fenced `Application.OpenURL` exception unchanged.
- Regression coverage includes season-to-Legacy save/reload and continuous numbering, delayed details plus file rewrite, copied death-time context, old JSON compatibility, unavailable service/name, future cycle labels, and localized text cleanup.
- Reflection member names and enum definitions were inspected in this laptop's generated Season 5 metadata. InteropGuard does not verify those reflection calls at runtime.

## Native checks still required

After the user authorizes installation, die once on a seasonal character and inspect the label in the panel and JSON. Repeat on a Legacy character. Check offline and online characters where available. If a character transfers to Legacy, verify its earlier seasonal records remain unchanged after reopening the game and its next death is Legacy. Confirm the loaded cycle service and localization return usable values; otherwise the UI must keep its honest unknown/name-unavailable label. No transfer was simulated against real game files.

## Staged artifacts

- `Terrible_Death_Counter_and_Log_v0.1.2-advice-review.zip`: exactly one root DLL, now includes season history and the earlier expanded advice.
- `CODEX-DEATH-ADVICE-PORTED-SOURCE.zip`: refreshed current source, excluding generated build files.
- Candidate DLL SHA256: `9DE9B4C56B97215B31335D060DBE315B2A1084A53AC4B214D01152A2D4B6A6B9`.
- Installed DLL SHA256, unchanged: `2A02FE16530697BD2765753D5B4B42D4C99C15ADA8266F4F9D6C75B8E19EF704`.

This is still a v0.1.2 review build, not a released version. Prior advice report counts and hashes describe the earlier snapshot. Native capture and advice field units remain unverified in this build.
