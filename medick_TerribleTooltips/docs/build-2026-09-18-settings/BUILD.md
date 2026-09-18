# Settings rows — staged, not deployed

Baseline: `settings/tier-signal-rows`, `d9caa943c71842c7e1233d1d67cef1699de42e74`.
No commit, deployment, config edits, or version bump. BuildInfo remains `3.1.1-beta3`.

## Source delta

- SettingsUi.cs: three boolean rows, all using `SaveAndRefresh(reRender: true)`:
  - `TT - Show Signal`: **Show Tier and Grade**
  - `TT - Compact Tier`: **Compact Tier Word (T7)**; ON maps to Compact, OFF to Spelled.
  - `TT - Unit Border`: **Tier and Grade Border**
- SettingsUi.cs: Name Color description names GreaterAffix as default and TierColor as restoring pre-3.1.0 coloured affix text.
- Prefs.cs: registers ShowSignal=true and includes it in the orphan-key allow-list.
- TooltipRecolor.cs: returns the independently coloured affix name before constructing signal links or layout spacing when ShowSignal=false. This also suppresses the folded sealed prefix. Existing Alt/pinned detail behavior is unchanged.
- UnitBorder.cs unchanged. Its existing IsCandidateText requires a `ttu` link; signal-off supplies none. Existing LateUpdate hides both previously allocated border and divider when text stops qualifying. CountUnitBoxes returns zero. Tier-only retains one `ttu` link and has no `ttd` link, keeping the box without a divider. Both-on output is byte-for-byte beta3 output.

Only these three production source files changed; all scan gates, root collection, frame handlers and telemetry remain unchanged. The pre-existing four-ZWSP ownership suffix remains at the end of composed TMP text to preserve scan behavior. No new markers or spacing are inserted by signal-off.

## Gates

From `medick_TerribleTooltips/`:

`dotnet build -c Release -p:DeployToMods=false --no-restore`

Build succeeded: **0 warnings, 0 errors**. The exact requested command without `--no-restore` could not complete: restore could not read `C:\Users\andre\AppData\Roaming\NuGet\NuGet.Config` (access denied). The successful build used existing restore assets. A temporary isolated restore configuration did not resolve the access failure and was removed. No project changes were made to work around it.

`pwsh -File docs/build-2026-09-18-settings/Run-Regression.ps1`

**94 assertions passed**, including all 76 beta3 assertions, actual Prefs/SettingsUi with recording doubles, and 192 layout/style/tier-word/divider/separator/grade/sealed combinations. Each signal-on variant is compared against the frozen beta3 composer. Each signal-off variant must equal the exact affix name without trimming or stripping markers, have zero border candidates/boxes, and hide previously active border/divider objects through the production LateUpdate method.

Six deliberate mutations were rejected at the intended assertions: extra divider, empty box link, trailing space, extra ZWSP, ignored ShowSignal, and disabled stale-border hiding. See regression-output.txt, mutation-results.txt, and per-mutation logs. Run with `-Mutation Divider` (or Box/Whitespace/Marker/IgnoreOff/StaleBox) to reproduce a failure. Mutations affect generated test copies only.

This is controlled-stub regression evidence, not Unity/Il2Cpp integration or visual proof. Border eligibility/count/hide/LateUpdate methods are extracted verbatim from production; geometry/engine objects are doubled. In-game appearance, TMP glyph spacing, and settings placement still require an in-hand check after deployment is authorized. No independent review was claimed.

Generated harness copies and binaries remain under this documentation directory; optional recursive cleanup was blocked by execution policy. The runner regenerates them.

## Artifacts

- Staged DLL: `medick_TerribleTooltips/bin/Release/net6.0/medick_Terrible_Tooltips.dll`
- Size: **101376 bytes**
- MD5: **3c8585996a58b600c32c580a04b95d15**
- Live `C:\Program Files (x86)\Steam\steamapps\common\Last Epoch\Mods\medick_Terrible_Tooltips.dll` MD5 before and after: **776a3036ed7a45b0f025c451eec34d06** (beta3).
- Protected-file Git blob comparisons and artifact hashes: artifact-evidence.json.
- `git diff --check -- .` passed.
