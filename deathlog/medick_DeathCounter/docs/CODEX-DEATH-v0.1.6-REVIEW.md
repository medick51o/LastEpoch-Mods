# Experimental v0.1.6: protect saved history

October 2, 2026, PT. Prepared locally after the 7:44 PM resume. Not installed or published. Installed v0.1.4 remains the native-tested baseline, with seven saved deaths and confirmed causes on records 6 and 7. v0.1.5 pause removal is retained.

## Changes

DeathLog prepares complete JSONL snapshots and flushes the temporary file before replacement. Unterminated tails are retained on their own line so the next death remains readable. Unreadable and unsupported-version rows remain in the file. Unchanged rows retain their text; changed records merge changed fields into their original JSON so unrelated unknown fields survive.

Loads are transactional. Failed reads block primary writes while preserving the prior in-memory view and new session deaths. Unexpected disk changes also block replacement. Failed writes leave records in memory for a later save attempt. A successful JSON save remains successful if the human-readable text mirror fails.

The first pre-rewrite file is retained as deaths.jsonl.recovery without overwriting it. deaths.jsonl.bak retains the previous version on subsequent writes. These complement retention of unreadable lines in the main file; they are not an automatic corruption-repair mechanism. Numbering uses the highest known character number as well as the readable count to avoid reusing a number after a skipped row.

The panel displays persistence warnings and counts confirmed saved deaths separately. Export character log now writes full JSON data, including unsaved session deaths, alongside the text export. A recovery export does not mark the primary history as saved.

## Checks

286 CoreTests pass, including 11 new tests for torn tails, complete unterminated records, unknown fields and future schemas, failed initial reads/reloads, failed writes/replacements, text-mirror failure, external file changes, invalid encoding, and full recovery exports. Fault tests use disposable directories, never player files.

NoGame and local game-linked Release builds pass with DeployToMods=false and warnings as errors: zero warnings and errors. Both InteropGuard checks pass with 75 calls and the existing OpenURL allowance. No new Unity calls were added. Game hooks, tracking, report parsing, pending capture, advice, formulas, reassessment storage and runtime repair sources are byte-identical to v0.1.5.

Installed DLL, config, deaths.jsonl, deaths.txt and reassessments.jsonl hashes match the pre-repair inspection. No installation, new native death, or in-game rendering test occurred.

## Remaining work

Native test this candidate only after backing up user data and confirming the game is closed for installation. Verify exactly one record for each death, preserved game explanation, logging while the HUD is hidden, restart history, and original/reassessment separation. The new persistence warning and export messages need native layout inspection.

The earlier review's advice ranking, unnamed-DoT handling, frozen reassessment rendering, visual hierarchy, season/Legacy and localized-zone work remain separate changes. Capture code was deliberately preserved for this history-safety candidate.

Full snapshot saves have work proportional to history size, with no artificial history limit. Very large logs have not been profiled in the game. An external concurrent writer is unsupported; detected changes stop replacement rather than merging an uncertain history.
