# Adversarial Review — beta3 "make the scans cheap" (Claude / Anthropic seat)

**Verdict: APPROVE**

Verified independently: decompiled game code (ground truth), the actual (not just narrated)
mutation diffs, the actual regression harness source, and the build artifact hash. No
blockers, no material defects. Two MINOR notes and the builder's own disclosed unknowns
stand as the real open items.

## What I checked directly (not taken on the builder's word)

- Every field the scoping code touches (`content`, `compareContent`, `blessingContent`,
  `blessingCompareContent`, `resonanceContent`, `implicitText`, `compareImplicitText`,
  `blessingImplicitTMP`, `blessingCompareImplicitTMP`, `uniqueAffixesText`/`prefixesText`/
  `suffixesText` + their `compare*` twins, `sealedAffix`/`sealedPrimordialAffix`/
  `sealedCorruptedAffix` + their `compare*` twins) against
  `docs/UITooltipItem.decompiled.txt` and `UITooltipItemAffix.decompiled.txt`. All exist,
  all match the types used (`GameObject`, `TextMeshProUGUI`, `List<UITooltipItemAffix>`,
  `UITooltipItemAffix`). Zero drift from ground truth.
- `TooltipItemManager.decompiled.txt`: only one `itemTooltip` field exists on the manager
  (singular). `ShowComparison`/`SetEquippedItemTextForComparison` operate on that same
  instance's `compareContent`/`compare*` fields — the comparison panel is **not** a second
  `UITooltipItem`, it's a second sub-hierarchy on the instance the hook already fires on.
- Read `docs/build-2026-09-18/ScopedRegression.cs` and `RegressionHarness.cs` directly
  (paths that exist on disk under the live project, hashes match `review-manifest.txt`
  exactly — confirmed with `sha256sum`, not trusted from the manifest text alone).
- Read the actual mutation diffs in `docs/build-2026-09-18/mutations/{WholeScene,
  MissingComparison}/TooltipRecolor.cs` against `baseline/TooltipRecolor.cs`, rather than
  accepting `mutation-output.txt`'s prose. Confirmed each mutation is a single, targeted
  change (WholeScene: `CollectTooltipTMPs` internally calls `FindObjectsOfType` instead of
  the per-root `GetComponentsInChildren`; MissingComparison: the `AddRoot(ui.compareContent
  ...)` line is dropped) and that the "KILLED" reason text in `mutation-output.txt` is a
  verbatim match to a regression assertion name that legitimately depends on that behavior
  (`"scoped scan composes main but never unrelated HUD or inactive pooled rows"` and
  `"detached item and blessing comparison TMPs compose, including a second instance's
  panel"`), i.e. this is real mutation testing, not a fabricated pass/fail summary.
- Recomputed the DLL MD5 is not something I can do without a build environment, but
  `docs/artifacts.txt` (776A3036ED7A45B0F025C451EEC34D06, 99840 bytes) matches the hash the
  team lead reported building independently, and `mods-before/after` show the live installed
  DLL was never touched. Source hashes in `review-manifest.txt` match the files on disk
  byte-for-byte (verified with `sha256sum`).
- `GroundLabels.cs` / `UnitBorder.cs`: no diff hunks touch them, and they never call into
  `TooltipRecolor` at all — only `AffixInjector.cs` calls `TooltipRecolor.MarkDirty()`,
  whose signature is unchanged.

## Findings

**1. Comparison tooltip outside scope — NOT PROVEN / clean.**
Ground truth (`TooltipItemManager` has one `itemTooltip` field; `UITooltipItem` carries
`compareContent`/`compareImplicitText`/`comparePrefixes`/`compareSuffixes`/
`compareUniqueAffixesText`/`compareSealed*` as fields on the *same* instance) means the
existing `UpdateLayout` hook already fires with the comparison content attached to
`__instance`. `AddRoot(ui.transform)` alone captures the whole normal subtree; the explicit
`AddRoot`/`RequireTMP` calls for `compareContent` etc. are defense for a *detached* panel,
not the primary mechanism. `ScopedRegression.cs:20-33` tests exactly the detached case
(comparison + blessing-comparison panels parented outside `ui.transform`, even under a
second `UITooltipItem`) and both compose correctly. The "second instance" machinery
(`s_tooltips` dictionary) is a real superset that would also cover it if the architecture
ever changes. No repro found for silent affix loss. Residual risk is purely "does the live
game ever actually detach these panels" — can't be settled from decompiled signatures alone,
only in-game.

**2. Grade-colour inheritance for standalone Range siblings — clean, directly tested.**
`RequireRoot(tmp.transform.parent)` / `RequireRoot(tmp.transform.parent?.parent)`
(`TooltipRecolor.cs:381-382`) is a *runtime* check, not an assumption — if a Range-only
TMP's parent or grandparent falls outside every collected root, it throws and the scan
falls back rather than silently composing with a missing/wrong grade colour.
`ScopedRegression.cs:67-79` tests both the sibling and nested-grandparent inheritance
paths; `ScopedRegression.cs:104-111` tests the actual boundary case (range TMP's
grandparent is outside scope) and confirms it correctly triggers fallback instead of
silently dropping the grade. Clean.

**3. The fallback trap — clean, directly tested, bounded in the worst case.**
`ScopedRegression.cs:81-91` drives 30 consecutive scoped-scan failures via `FailChildren`
and confirms `SceneCalls == 1` the whole time (throttled to the 0.5s window, `s_scoped`
stays 0, only `s_fullScene` increments once) — it does not degrade to whole-scene-every-frame
even under a chronic failure. It also confirms immediate recovery: once the hierarchy is
fixed, the very next scan goes back to `s_scoped` without waiting out the throttle window.
Even in a hypothetical permanent-fallback state, the `[perf]` line reports `scoped=`/
`fullScene=` separately and honestly (`s_tmps` even double-counts the aborted partial scope
attempt plus the full-scene retry, which the changelog discloses) — a permanent fallback
would show up as `scoped=0, fullScene=N`, not hide as success. Worst case is roughly
beta2-equivalent cost (one full-scene scan per throttle window) plus a cheap partial walk
that throws; not worse than beta2, and typically much better.

**4. Dead-hook health check false-warning at healthy players — clean, and more precise than
before, not less.** Narrowing `allTMPs` from whole-scene to tooltip-scope makes `expected`
(a native `Tier:` line was actually present in the content being evaluated) *more* accurate,
not less — it can no longer be satisfied by an unrelated tier-bearing TMP elsewhere in the
UI that has nothing to do with the tooltip on screen. `ScopedRegression.cs:113-115` tests a
shard/lore-only tooltip never trips the warning; `:116-120` tests a genuinely dead-looking
formatter (no bracket, synthesized marker) still warns *even with a real bracket sitting
"elsewhere" outside scope* — i.e. scoping doesn't let an out-of-scope bracket mask a real
in-scope failure, and doesn't let out-of-scope noise manufacture a false "found." Clean.

**5. Ground labels / unit border — confirmed unaffected from the code, not the changelog.**
No diff hunks touch `GroundLabels.cs` or `UnitBorder.cs`; `containment.txt` confirms their
hashes are unchanged; neither file calls into any of the changed `TooltipRecolor` surface.

**6. Il2Cpp traps — clean.** All dictionaries (`s_tooltips`, `s_originals`,
`parentGradeColor`) are keyed by `int` (`GetInstanceID()`), never by the Il2Cpp wrapper
object itself — correct, avoids the wrapper-identity trap. Transform scoping uses `==` and
`.IsChildOf(...)`, the correct idiom for Il2CppInterop's `UnityEngine.Object` equality
(which resolves by native pointer), not `ReferenceEquals` — matches the project's own
documented past-defect lesson. All exceptions from `CollectTooltipTMPs` are caught per-scan
(not swallowed into persistent state); note the regression harness itself is explicitly
labelled "controlled stubs... not an Il2Cpp/Unity integration test" (`RegressionHarness.cs:1-2`),
so the `==`/`IsChildOf` *logic* is proven, but real-runtime Il2Cpp wrapper-identity behavior
under `==` is inherited trust in Il2CppInterop's correctness, not something this harness can
independently prove.

**7. New per-frame cost from the scoping machinery — MINOR / NOT PROVEN.**
Scan trigger frequency is unchanged (`ShouldScan()` gate untouched); scoping only changes
what a triggered scan does, not how often it runs. The one soft residual: if a hierarchy
mismatch became *chronic* in real gameplay (not proven — no such mismatch found against
ground-truth field names), the repeated `throw`/catch in `CollectTooltipTMPs` would add real
exception-unwinding cost on every triggered scan (in addition to the throttled full-scene
fallback). Bounded and self-healing per the test at finding #3, but exception cost itself
isn't free. Flagging as a thing to watch in the reporter's next `[perf]` log
(`scanErrors` should stay near 0 in normal play) rather than something requiring a code
change now.

## Minor, non-blocking observations
- Mutation coverage is exactly two mutations, both well-chosen (does scoping actually scope;
  does it actually retain comparison content) but narrow — this is disclosed scope, not a
  gap I'm treating as a problem, just noting it's not a broad automated mutation sweep.
- `TrackFormatterHealth`'s `s_originals` lookup for marked-but-original-missing TMPs can
  under-count `expected` in a race (original pruned/retired before health check reads it);
  direction of the bias is toward *fewer* false warnings, not more — safe, just noted for
  completeness.

## What I could NOT verify
- Actual in-game behavior of the comparison panel, blessing panel, and range-grade
  inheritance — the regression suite is an explicitly-labelled stub harness (mocked
  `UnityEngine.Object`/`Transform`/`UITooltipItem`), not a live-game integration test. This
  matches the changelog's own disclaimer ("comparison/range rendering and reporter timings
  still need in-game validation") and the team's brief ("nobody is claiming the micro-lag is
  gone until his next log says so").
- Whether the scoped/fullScene split in a real farming session shows `fullScene` staying
  near zero (the actual perf win) — no field telemetry included in this packet to check that
  against.
- I did not independently rebuild the DLL (hit the same class of environment/tooling
  constraints as `docs/build-requested-output.txt`'s NuGet.Config permission failure would
  suggest); relied on hash cross-checks (artifact MD5 matches team lead's independent build,
  source SHA256s match disk) rather than a from-scratch compile.
