# Repair Review — v3.1.1-beta2 (repairs finding 1 and finding 2 from beta1 review)
Reviewer: Anthropic seat. Repair built by a Google seat. Scope: the repair diff only
(`docs\DELTA-beta2-repair.patch`, `src\FilterRuleTooltip.cs`), not a full re-review of
the beta1 diff.

## Verdict: APPROVE

Both findings are correctly closed. One MINOR cosmetic leftover, one NOT-PROVEN
curiosity. Nothing new introduced.

---

## 1. Does the narrow try/catch close the per-frame-exception loop, on every path?

Yes, on all three paths asked about, plus the general case.

`FilterRuleTooltip.cs:153-166`:
```csharp
bool owned = false;
try
{
    owned = s_owner != null && s_owner.tooltipActive &&
            s_owner == UITooltipItem.instance && s_owner.target == s_target &&
            s_owner.targetType == s_targetType;
}
catch { }

if (!owned)
{
    if (s_pendingItem != null && TooltipPerf.Enabled) TooltipPerf.RuleReuse();
    ClearPending();
    return;
}
```
If any read inside the `&&` chain throws, the assignment to `owned` never completes,
so `owned` keeps its `false` default and falls straight into the not-owned branch —
`ClearPending()` runs unconditionally. `ClearPending()`'s first statement is
`s_owner = null;` (`:110`), so the very next `MonitorUpdate` call short-circuits on
`s_owner != null` before touching any property again. The loop is closed in one
frame, not "eventually" — this fully resolves the finding.

- **Exception while `s_pendingItem` is null:** unrelated field, doesn't affect the
  `owned` computation. `if (s_pendingItem != null ...)` guards the counter increment
  correctly either way; `ClearPending()` still runs. No leak.
- **Exception after a successful injection:** `s_destination`/`s_hadDestination`/
  `s_injected` are already-set state at that point, but they're irrelevant to whether
  `owned`'s try/catch fires — if it does, `ClearPending()` tears all of it down
  (`:108-121`) in the same call. No wedge, no stale "successfully injected but now
  orphaned" state left dangling.
- **Exception during `ClearPending()` itself:** every statement in `ClearPending()` is
  a plain field assignment to `null`/`false`/`-1` (`:108-121`) — no property reads, no
  native calls, nothing capable of throwing under normal CLR operation. Not a real
  mechanism. Worth noting defensively: `s_owner = null;` is the *first* line, so even
  in a contrived partial-failure scenario the field that gates re-entry into the throw
  site is nulled before anything else could go wrong. Not a live concern.

## 2. Does removing `SettleFrames` leave a non-terminating path?

No. All three expiry checks (`:190`, `:224`, `:230`) are now `Time.unscaledTime [>=] 
s_deadline` only. `Time.unscaledTime` is Unity's accumulated unscaled delta-time,
monotonically non-decreasing for as long as `OnUpdate` is being called at all — and
`MonitorUpdate` can only be re-entered *from* `OnUpdate`, so "the loop runs but time
never advances" isn't a reachable state: if Update isn't firing, there's no loop to
worry about; if it is firing, unscaled time is advancing. `s_deadline` is armed fresh
at the start of every discovery generation (`Capture`'s "not same" branch, `:100`, and
`BeginReplacement`, `:258`) and is never extended by repeated identical-content
setters (the "same" branch in `Capture`, `:73-86`, never touches it) — so there's no
path to indefinite postponement either. I don't see a non-terminating path.

**`s_startFrame` is now dead state — MINOR.** It's still declared (`:38`) and still
written in both places that used to arm the frame bound (`Capture`, `:99`;
`BeginReplacement`, `:257`), but grep confirms zero remaining reads anywhere in the
file. It does nothing anymore except cost a `Time.frameCount` read and a field write
on every generation start (negligible) — the real problem is it *reads* as load-bearing
next to `s_deadline` to a future maintainer, when it isn't. Recommend deleting it in a
follow-up; not worth a beta3 respin on its own.

**NOT PROVEN / curiosity, not a finding:** a private field that is written but never
read is normally CS0414 ("field assigned but its value is never used") under default
Roslyn analysis, yet the reported build was 0 warnings. I don't have the `.csproj` in
this packet to check for suppressed warnings or analyzer configuration, so I can't say
whether that's expected. Doesn't affect correctness either way — flagging only because
it's a discrepancy I couldn't close.

## 3. Anything new introduced — behavior change, changed non-throw semantics, stale comments?

One **expected, in-scope** behavior change worth naming explicitly: since the 12-frame
bound was the dominant (shorter) expiry at any framerate above ~24fps, the *typical*
discovery window at ordinary framerates has effectively lengthened from ~200ms
(12 frames @ 60fps) to the full 500ms wall-clock budget — meaning up to ~30
`GetComponentsInChildren` attempts at 60fps instead of 12 before a genuine give-up.
This is exactly what fixing finding 2 requires (removing the shorter, framerate-coupled
bound) and stays within the same order-of-magnitude cost the beta1 diff already
accepted as fine, so I'm not raising it as a new issue — just confirming it was seen
and is intentional, not a silent side effect.

No other logic outside the two targeted blocks was touched (confirmed against the
patch hunks directly — only the `SettleFrames` declaration and the three expiry
conditions changed). The one-attempt-per-frame throttle
(`frame == s_lastAttemptFrame`, `:189`) is untouched, as claimed.

Comments checked for staleness: `:151-152`, `:198-199`, `:229`, `:237` — all still
accurate, none reference the removed `SettleFrames`/frame bound. `CHANGELOG.md`'s
beta2 entry now reads "allows target discovery through a 0.5-second wall-clock bound"
— correctly updated, no leftover "12 frames" language. `BuildInfo.Version` is
`3.1.1-beta2`, consistent.

## 4. Is the De Morgan inversion correct, including the null short-circuit?

Yes, exactly. Old: `A = (owner==null) || !tooltipActive || (owner!=instance) ||
(target!=s_target) || (targetType!=s_targetType)`, branch taken when `A` is true. New:
`owned = (owner!=null) && tooltipActive && (owner==instance) && (target==s_target) &&
(targetType==s_targetType)`, branch taken when `!owned`. By De Morgan,
`!owned == A` term-for-term (each new-code term is the exact negation of the matching
old-code term, in the same order). Short-circuit behavior is preserved identically:
old code stops evaluating on the first `true` OR-term (starting with the null check),
new code stops evaluating on the first `false` AND-term (starting with the same null
check, inverted) — same evaluation order, same set of terms skipped for every input
combination, including `s_owner == null` never reaching `.tooltipActive`. Confirmed
equivalent on the non-throwing path.

---

## What I did not independently verify
- Did not rebuild the project myself (no `.csproj` in this packet, only `src`/`docs`
  subset) — relying on the reported 0 warnings / 0 errors and the DLL hash.
- No in-game verification, as with the beta1 review — these are static-analysis
  conclusions about control flow, not measured runtime behavior.
