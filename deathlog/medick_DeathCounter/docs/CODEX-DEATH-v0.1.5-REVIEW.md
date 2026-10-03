# v0.1.5: remove the silent pause

October 2, 2026, PT. Experimental local candidate. Prepared and checked; not installed while Last Epoch remains running. No public release, push or merge.

The user requested removing the pause switch or clarifying its purpose. Removing it prevents another accidental interruption of the mod's central function. The v0.1.4 session added no record because the saved Tracking setting was false, written at 5:43 PM before installation. That attempt did not test the new cause-capture path with recording enabled. The earlier v0.1.3 record genuinely lacked cause details; that is a separate issue.

## Result

- No pause/resume button appears in Options.
- DeathTracker and DeathReportHooks do not consult the old Tracking preference. Existing player ownership, health, death-latch and report-correlation checks remain.
- The legacy entry is private to Prefs and retained for old config compatibility. On startup a false value is migrated to true and saved. Even if saving fails or another preference tool later writes false, the capture paths remain enabled.
- Options says deaths are counted and saved automatically and hiding the counter does not stop logging. Counter visibility and notifications remain optional.
- No original death, reassessment, counter-reset history or HUD position is changed by this source update.

## Checks

275 existing CoreTests pass. No new implementation-mirroring UI test was added for removal of the button. NoGame and local Release builds both use DeployToMods=false and warnings as errors: zero warnings/errors. Both InteropGuard checks pass, with the existing OpenURL allowance and no new Unity calls. Source search finds no remaining Prefs.Tracking references or pause/resume button text.

## Next native test

Close the game before installing the prepared DLL. On restart, confirm v0.1.5, no pause switch, and logging continues when the HUD is hidden. Repeat the enabled Rahyeh death test: exactly one new record should retain the displayed cause, type and numbers. Follow the v0.1.4 capture review if diagnostics still show missing details. Successful builds do not establish native capture or GUI rendering.

The v0.1.4 capture fallback, resistance cards, frozen-zone advice, original history and separate reassessments remain unchanged. Read docs/CODEX-DEATH-v0.1.4-REVIEW.md for capture limitations. Never detour Nullable-bearing death-report callbacks or replace MelonLoader runtime DLLs while chasing the capture issue.
