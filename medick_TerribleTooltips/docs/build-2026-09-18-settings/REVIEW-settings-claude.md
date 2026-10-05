# Cross-vendor review — settings/tier-signal-rows (Anthropic seat)

Reviewer: Claude (Sonnet 5), read-only. Builder: OpenAI seat.

## Verdict: APPROVE

Reviewed `DELTA-settings.patch` against the live source (`Prefs.cs`, `SettingsUi.cs`,
`TooltipRecolor.cs`), read `UnitBorder.cs` in full (unchanged, load-bearing), read
`SignalRegression.cs` / `SettingsStubs.cs` / `Run-Regression.ps1` line by line rather than
trusting `regression-output.txt`, and read all six `mutation-*.txt` outputs plus
`mutation-results.txt`.

## What a player sees

Three new toggle rows under "Terrible Tooltips" (`SettingsUi.cs:85-101`), in this order:
- **Show Tier and Grade** (`TT - Show Signal`) — on by default. "Turn this off to hide the
  entire signal, including its divider and border."
- **Compact Tier Word (T7)** (`TT - Compact Tier`) — "ON = T7. OFF = Tier 7."
- **Tier and Grade Border** (`TT - Unit Border`) — pre-existing `UnitBorder` pref, now exposed.

`ShowGradeLetters` ("Show Grade Letters") is untouched, still its own row, no tier-only-off
switch added — matches the ruled edge case.

## The three combinations

Verified directly in `ComposeCleanLine` (`TooltipRecolor.cs:1024-1138`):

- **ON+ON** → unchanged from beta3. `tierPart`/`gradePart` both non-null → the `<link="ttu">`
  unit includes the `<link="ttd">` divider (`:1112-1113`). Confirmed byte-identical to beta3
  by the regression's `BaselineTooltipRecolor` diff (`SignalRegression.cs:28`, extracted from
  commit `d9caa94` via `Run-Regression.ps1:33-36`).
- **ON+OFF** → `gradePart` is `null` (`:1061`, gated on `ShowGradeLetters`), so the unit branch
  at `:1114-1115` fires (`tierPart != null || gradePart != null`), producing
  `<link="ttu">{tierPart}</link>` with **no** `divider` variable ever inserted. Box still drawn
  (link present → `UnitBorder.IsCandidateText` true). Confirmed by regression assertion
  `"signal ON + grades OFF: tier without divider"` (`SignalRegression.cs:46`) and mutation
  coverage.
- **OFF** → `ComposeCleanLine` returns bare `name` at `:1094`, *before* `sealedPart`/`unit`/
  `divider` are ever concatenated into the string. No links, no divider, no residue.

## Hunt items

**1. Residue when ShowSignal is off — CLEAN, verified against UnitBorder.cs directly (not the
builder's claim).** `UnitBorder.IsCandidateText` (`UnitBorder.cs:979-981`) requires the text to
both end with the 4×U+200B `Marker` **and** `.Contains(LinkOpen)` (`<link="ttu">`). When
`ShowSignal` is off, `ComposeCleanLine` never emits that link (return at `:1094` predates all
link construction), so `IsCandidateText` is false and the row is never registered — `Run()`
(`UnitBorder.cs:211-249`) simply skips it, and for a row that *was* previously tracked (user
toggles off with a tooltip open), `OnLateUpdate` (`:158-162`) hits
`!IsCandidateText(text) → Hide(row)`, which sets both `Border` and `Divider` GameObjects
inactive (`:1021-1036`). The 4×U+200B `Marker` is pre-existing, unconditional infrastructure
(appended at `TooltipRecolor.cs:990` regardless of this diff) used purely as a "processed"
sentinel — it is zero-width and was already present in beta3's output for every tooltip line;
this diff does not add, move, or duplicate it. The mutation harness independently proves the
exact-equality claim: `Marker` mutation (inject one extra ZWSP on signal-off) and `Whitespace`
mutation (inject a trailing space) are both **rejected** (`mutation-Marker.txt:79`,
`mutation-Whitespace.txt:79`), confirming the regression's `hidden != Affix` exact-equality
check (`SignalRegression.cs:35`) actually catches residue rather than passing by accident. The
`Box` and `Divider` mutations are likewise rejected. I consider the builder's claim ("existing
border-hiding logic covers it") verified, not just accepted — the actual mechanism is the
`IsCandidateText` link-presence gate plus `OnLateUpdate`'s per-row `Hide()` on disqualification.

**2. Upgrade safety — CLEAN.** `ShowSignal = Category.CreateEntry("ShowSignal", true, ...)`
(`Prefs.cs:131-132`) — standard MelonPreferences default-on-missing-key behavior; an existing
player's cfg has no `ShowSignal=` line, so it loads at `true`, rendering identically to beta3.
Confirmed present in the orphan-key allow-list at `Prefs.cs:208` (`"...AffixNameColor",
"ShowSignal", "ShowGradeLetters",...`) — I checked this against the live file, not the diff
context lines, since allow-list placement is easy to get subtly wrong (e.g. wrong HashSet, wrong
line continuation). It's correctly in the one HashSet that `WarnOrphanedKeys` reads.

**3. Toggle/enum binding round-trip — CLEAN.** `TierWord` is a pre-existing enum pref
(`Prefs.cs:104,159-160`, frozen since 3.1.0), unmodified by this diff — the new toggle only adds
a UI projection: `Prefs.TierWord.Value == TierWordStyle.Compact` for the initial bool
(`SettingsUi.cs:94`) and `v => Prefs.TierWord.Value = v ? Compact : Spelled` for the callback
(`:95`). Since this reads `Prefs.TierWord.Value` live at `Build()` time, a cfg already carrying
`TierWord=Compact` (e.g. hand-edited, or set before this branch existed) will show the toggle as
ON, not silently reset it to Spelled. Persistence itself is unchanged (same enum entry, same
`SaveToFile`). Regression confirms both directions explicitly:
`compact.Callback(true)` → `TierWord.Value == Compact` and re-renders (`:68-70`), then
`compact.Callback(false)` → restores `Spelled` (`:71-72`). The "toggle shows ON for a
pre-set-Compact cfg" case specifically (as opposed to toggling live) is not exercised by a named
assertion, but follows directly from the one-line binding — I judge this NOT a gap worth
blocking on since there's no code path where it could diverge (no caching, no separate "UI
state" variable).

**4. Performance — CLEAN, no blocker.** No change to scan/compose frequency: `ShowSignal`'s
toggle callback uses the same `SaveAndRefresh(reRender: true)` pattern as every sibling toggle
(`Layout`, `Style`, `NameColorMode`, etc.) — `ReRenderNow()` → `ReRenderFromOriginals()`
(`TooltipRecolor.cs:91-92`), a targeted rebuild of already-cached tooltip text, fired only on a
settings click, not per-frame. `UnitBorder.GateEnabled()` (`UnitBorder.cs:983-986`) is unchanged
and still doesn't reference `ShowSignal` — meaning `OnLateUpdate`'s per-frame loop still walks
tracked rows exactly as it did in beta3, and `IsCandidateText`'s `.Contains` check now short-
circuits false slightly more often (with signal off, rows never qualify) if anything a marginal
*reduction* in work, never an increase.
One genuine but minor note: in `ComposeCleanLine`, `tierPart` and `gradePart` (string/markup
construction, `:1046-1075`) are computed *before* the `if (!Prefs.ShowSignal.Value) return name;`
check at `:1094`, so that formatting work happens even when the signal is fully hidden. This
only runs at tooltip-compose time (on hover / text change), not per-frame, so it is not a
"scan/compose frequency" violation per the field-test gate — but it is avoidable waste. **MINOR**,
not a blocker: move the `ShowSignal` check above the `tierPart`/`gradePart` construction in a
follow-up, no urgency.

**5. Affix Name Color description accuracy — CLEAN, checked against both color sites.**
`TooltipRecolor.cs:1080-1090` (line 0 of the affix, inside `ComposeCleanLine`) and `:976-981`
(subsequent stat lines) both implement identically: `TierColor` tints every affix name with its
own `tierHex`; `GreaterAffix` tints only `tier == 6 || 7` with `GreaterAffixHex()`. The new
description ("GreaterAffix = only T6/T7 affix text is tinted (default). TierColor = restores the
pre-3.1.0 coloured affix text, with each affix wearing its tier color.") matches this exactly.
I also checked the historical claim: commit `601a9be` ("v3.1.0 — the clean-signal release")
introduced `AffixNameColorMode` and the `GreaterAffix` default; before that release the affix
name was tinted by `TooltipTierColors` per-tier directly (the behavior `TierColor` mode now
reproduces), so "restores the pre-3.1.0 coloured affix text" is accurate, not marketing.

**6. Il2Cpp traps / exception paths — CLEAN, no new surface.** The diff adds no new Il2Cpp
interop calls — `ShowSignal` is a plain `bool` MelonPreferences entry (same pattern as seven
existing bools), and the three new rows use the existing `NativeSettings.CreateToggle` path
(unchanged file, already used by 8 other rows). `SettingsUi.Build` is wrapped in try/catch
(`SettingsUi.cs:19-26`); a failure there only degrades the settings *panel* (falls back to
cfg-file editing via `WarnDegradedOnce`) and cannot leave `Prefs.ShowSignal.Value` in a bad
state, since the pref's value is independent of whether its UI row built successfully. No path
in `ComposeCleanLine` can throw before evaluating the `ShowSignal` check (pure string
formatting, no interop), so there's no way for an exception to strand the signal
permanently-on or permanently-off.

## On the builder's own gates — judged, not trusted

The regression harness is materially real, not a reimplemented stub decoupled from production:
- `Run-Regression.ps1:21` loads the **actual** `medick_TerribleTooltips/src/TooltipRecolor.cs`
  content at run time and does a literal-string mutation replace against it (`:30`) — I confirmed
  the target string `if (!Prefs.ShowSignal.Value) return name;` exists verbatim in the live file
  at `TooltipRecolor.cs:1094`, so this is a real coupling, not a decorative check (a stale/
  reformatted target string would make `.Replace` silently no-op in PowerShell — worth flagging
  as a latent fragility for whoever edits that line next, but not a defect today).
- `BorderProbe.Generated.cs` is built by anchor-substring extraction directly out of
  `UnitBorder.cs` (`:39-52`) — `OnLateUpdate`→`Run`, `CountUnitBoxes`→`GateEnabled` (which
  captures `CountUnitBoxes`, `CountLinks`, **and** `IsCandidateText` — the exact three methods
  this review's residue argument depends on), and `Hide(RowState row)`→`DestroyRows`. If the
  anchors ever go missing (a refactor), the script throws `'Border extraction anchor missing'`
  rather than silently testing stale code — a fail-loud design, which I consider adequate.
- I verified all six mutations actually kill (all six `mutation-*.txt` end in
  `Unhandled exception... rejected`, matching `mutation-results.txt`), and that the assertions
  they trip are the ones I'd independently write for this spec (exact-name equality with no
  trim, zero border candidates/boxes, stale-border retirement through the real `OnLateUpdate`).
  94/94 pass on the `None` mutation, matching `regression-output.txt`'s tail exactly.
- These gates are honest about their own limits — `BUILD.md` states plainly this is "controlled-
  stub regression evidence, not Unity/Il2Cpp integration or visual proof," and geometry/engine
  objects are doubled while eligibility/count/hide logic is production-extracted. I agree with
  that self-assessment; it is not overclaiming.

## Could not verify

- In-game rendering (actual TMP glyph spacing, box pixel placement, settings-panel visual
  layout) — no Unity/Il2Cpp runtime available to this review; the regression harness is explicit
  that it doesn't cover this either. Needs the author's in-hand check, as `BUILD.md` already
  says.
- The `TierWord`-cfg-preset-to-Compact-before-first-Build scenario (item 3) isn't exercised by a
  named assertion, though the code has no path to diverge from it.
- Build/hash reproduction (0 warnings/errors, DLL 101,376 B, matching MD5) — I did not rerun the
  build myself; taking the stated command and hash at face value since it's outside a read-only
  review's scope and the task described it as already verified.

## Out of scope, noted only

`git status` shows unrelated uncommitted changes in `medick_RighteousFire/` (a different mod).
These are not part of `DELTA-settings.patch` or this branch's diff against `d9caa94` (confirmed
via `git diff --stat d9caa94 -- .` — only the three `TerribleTooltips` source files differ), so
they don't affect this review's containment claim, which was scoped correctly.
