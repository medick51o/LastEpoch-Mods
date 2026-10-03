# v0.1.7 completed candidate

October 3, 2026, PT. Packaged locally; not installed or published. Includes v0.1.6 history protection and v0.1.5 automatic logging.

Changes:
- Prioritize captured killing-type resistance gaps, then gap size, within the three-action limit.
- Prevent unnamed DoT-only records from receiving armor or crit gear advice. Retain supported crit advice for mixed timelines with measured non-DoT loss.
- Use the frozen original capture for saved reassessment resistance cards and area calculations.
- Move advice ahead of detailed statistics, add a visible draggable scroll track, and collapse duplicate raw game text behind a button.
- Keep resistance card order, colors, readable font sizes, the compact HUD, and original capture data.

Validation: 293 CoreTests pass, including 18 regression tests added across v0.1.6 and v0.1.7. NoGame and game-linked Release builds passed with zero warnings/errors and DeployToMods=false. Both InteropGuard checks passed: 75 calls with the existing OpenURL allowance. These checks were completed before the usage pause; packaging reuses those results. Archive contents are separately hash-verified.

Preservation: the installed v0.1.4 DLL, configuration, deaths.jsonl, deaths.txt and reassessments.jsonl still match the pre-repair hashes. Capture hooks, parser and tracker remain unchanged. No game/runtime DLL was replaced and no saved death was backfilled. No installation occurred.

Remaining native checks: confirm one record per death, consecutive reports, hidden-HUD logging, restart history, reassessment freeze, scrollbar drag/wheel behavior, expandable raw report, and text sizes 1.0/1.4. The new GUI has not been rendered in game. Back up data and recheck that the game is closed before installing the candidate DLL.

Season/Legacy and localized zone lookup remain unresolved. Read-only metadata inspection confirmed the current cycle service member names but did not establish a safe online data source. Missing context remains unknown. Boss flags and unsupported report fields are not inferred.

History-safety details are in CODEX-DEATH-v0.1.6-REVIEW.md. Full snapshot persistence scales with history size; no artificial limit is imposed. Very large histories have not been profiled natively.

User preference: pause if the five-hour usage allowance is exhausted. Do not continue work using credits after that point. The October 3 resume was explicitly requested by the user.
