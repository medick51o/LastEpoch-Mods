# Blind Review — beta2 repair (DELTA-beta2-repair.patch)

Reviewer: fresh Claude seat, read-only. Did not write the original code or the repair.
Scope: `docs\DELTA-beta2-repair.patch` applied to `src\FilterRuleTooltip.cs` (current
state read in full), cross-checked against `src\TooltipRecolor.cs` (TooltipPerf) and
`CHANGELOG.md`. No other source file touches `FilterRuleTooltip`'s statics.

## Verdict: APPROVE

Both claimed fixes are real, correctly scoped, and do what they say. One pre-existing,
diff-unrelated non-termination edge case is worth a follow-up ticket but is not a
regression from this patch. One dead field is confirmed dead and should be deleted
in a follow-up, not blocking.

---

## Q1 — Does the narrow try/catch close the per-frame-exception loop on every path?

**Yes, on every path checked.** `owned` is declared `false` above the inner
`try`, so any exception thrown while evaluating
`s_owner != null && s_owner.tooltipActive && s_owner == UITooltipItem.instance && s_owner.target == s_target && s_owner.targetType == s_targetType`
(`FilterRuleTooltip.cs:153-160`) leaves `owned == false` and falls straight into the
existing `if (!owned)` branch (`:162-167`), which calls `ClearPending()` unconditionally.

- **Throw while `s_pendingItem` is null:** `RuleReuse()` telemetry is skipped
  (`s_pendingItem != null` guard, `:164`), `ClearPending()` still runs — harmless,
  idempotent.
- **Throw after a successful injection:** `s_injected` is reset to `false` along with
  everything else in `ClearPending()` (`:108-121`) — the stale injected state can't
  survive to trigger anything on the next frame, because `s_owner` is also nulled.
- **Throw from inside `ClearPending()` itself:** not reachable. `ClearPending()`
  (`:108-121`) is ten plain field assignments — no property access, no native calls,
  nothing that can throw. This scenario is **NOT PROVEN** (no mechanism); it's outside
  the narrow catch's scope but also structurally impossible given the current body.
- **Throw on frame 1 before any state exists:** `s_owner` is a static, defaults to
  `null`. `s_owner != null` is the first operand of `&&` and evaluates to `false`
  without touching the object — no throw is even possible here.

The critical property is that `ClearPending()` nulls `s_owner`, so the *next* frame's
`owned` computation short-circuits on `s_owner != null` before touching the broken
native wrapper again. That's what makes this "close the loop" rather than "catch it
once and reopen it next frame" — confirmed by reading `ClearPending()` alongside the
`owned` block, not assumed.

One caveat, out of scope for defect 1 but worth naming: exceptions from *later* in
`MonitorUpdate` (e.g. `GetComponentsInChildren` at `:201`, inside the outer `try`) are
still governed by the pre-existing outer `catch { if (Time.unscaledTime >= s_deadline) GiveUp(); }`
(`:227-232`), which does *not* clear `s_owner`/`s_target`/`s_pendingItem`. That's the
same shape of bug the ticket describes for defect 1, but it isn't the code the ticket
is repairing, and it's bounded by `s_deadline` (≤0.5s) rather than unbounded, since
`s_lastAttemptFrame` is already set before that code runs. Flagging as **NOT PROVEN /
out of scope**, not a defect in this diff.

## Q2 — Is the De Morgan inversion correct?

**Yes, exact and safe.** Distributing the negation over the old condition
```
s_owner == null || !s_owner.tooltipActive || s_owner != UITooltipItem.instance ||
s_owner.target != s_target || s_owner.targetType != s_targetType
```
term-by-term gives
```
s_owner != null && s_owner.tooltipActive && s_owner == UITooltipItem.instance &&
s_owner.target == s_target && s_owner.targetType == s_targetType
```
which is exactly what `owned` computes (`:156-158`). Order was preserved, which matters:
the null check stays first on both sides, so `&&`'s left-to-right short-circuit protects
`.tooltipActive`/`.target`/`.targetType` from a null-reference the same way `||`'s
short-circuit did in the original. Verified by hand-tracing both operator chains against
each clause, not by inspection alone.

**Il2Cpp wrapper equality:** yes, `==`/`!=` on `UITooltipItem`/`GameObject` in this
Il2Cpp interop layer are the overloaded UnityEngine.Object operators (native-pointer /
"fake null" aware), not `ReferenceEquals`. That matters in general — but it does **not**
matter for this diff's correctness, because both the old and new code use the identical
operators on the identical fields/types. The repair only restructured `||`/`!=` into
`&&`/`==`; it introduced no new comparison and didn't swap any operator for a
reference-equality one. Confirmed by diff: the only symbols touched are the boolean
connectives, not the comparison operators themselves.

## Q3 — With `SettleFrames` gone, is there any path where discovery does not terminate?

**No new non-termination path from this diff.** All three surviving bounds
(`:190`, `:224`, `:230`) are now `Time.unscaledTime >[=] s_deadline` alone.

- **`Time.unscaledTime` frozen/stalled:** not a real risk — `unscaledTime` advances with
  real elapsed time independent of `Time.timeScale`, and if `Update()` itself stalled
  (app suspended, breakpoint), `MonitorUpdate` wouldn't be running either, so there's
  nothing to "not terminate." **NOT PROVEN** — no mechanism given the engine model.
- **Deadline never re-armed:** not an issue — a deadline is armed once per item capture
  (`Capture()`, `:100`) and the discovery loop either injects, exhausts to `GiveUp()`
  within that window, or (for the "same item" path) skips processing entirely once
  `s_injected` is true (`:187`).
- **Deadline re-armed repeatedly by incoming setter calls:** this *is* a real
  mechanism — `BeginReplacement()` (`:249-261`) resets `s_deadline = Time.unscaledTime + SettleSeconds`
  every time it runs, and it can run on every `Capture()` call for the same item
  (`:79-84`) if `UsableDestination()` keeps failing. A pathological tooltip that
  perpetually invalidates its `requires` row could keep pushing the deadline forward
  forever. **However, this is pre-existing, not introduced by this patch**: before the
  repair, `BeginReplacement()` reset `s_startFrame` in the exact same call
  (`s_startFrame = Time.frameCount;`, unchanged by the diff, still present at `:257`),
  so the frame-count bound was *equally* re-armable in lockstep with the wall-clock
  bound. Removing `SettleFrames` didn't remove a termination guarantee that existed
  here — both bounds were always coupled to the same re-arm trigger. Rank: **NOT
  PROVEN as a regression** (it's a real theoretical hang, but it predates this diff and
  the ticket didn't claim to fix it — separate ticket if it's worth chasing).
- **Exception path's expiry check:** same `s_deadline`-gated `GiveUp()` as the others
  (`:230`), same re-arm caveat above, nothing new.

## Q4 — Is `s_startFrame` now dead state?

**Yes, confirmed dead.** Grepped the whole packet: `s_startFrame` is declared
(`:38`) and assigned in exactly two places — `Capture()` (`:99`) and
`BeginReplacement()` (`:257`) — and read nowhere. No reflection, no Harmony attribute,
no other file references it (`grep -rn s_startFrame` across `src/` returns only those
three lines). This is a genuine stale-reader trap: a future edit that tries to
resurrect a frame-based bound by reading `s_startFrame` would silently get whatever
value was left over from the *previous* item, not the current one, unless the
assignment sites are audited too.

The "0 warnings" claim in the ticket is a build-time assertion I can't verify from
this packet — there's no `.csproj` here to check for suppressed `CS0414` / analyzer
config, and I have no build output to inspect. So I can't reconcile *why* the reported
build shows 0 warnings for an assigned-but-unread private field; I can only confirm
from source that the field meets the shape of that warning. **Rank: MINOR** — dead
code, not a behavior risk, but real and worth a one-line cleanup in the next pass
(delete the field and its two assignments), plus a note to whoever ran the "0
warnings" verification to double check the analyzer actually saw this field.

## Q5 — Did the repair change anything beyond the two claimed fixes?

**No.** The full diff is: delete `SettleFrames` (defect 2), rewrap the ownership read
in a narrow try/catch (defect 1), and remove the `frame > s_startFrame + SettleFrames ||`
/ `frame >= s_startFrame + SettleFrames ||` / `Time.frameCount >= s_startFrame + SettleFrames ||`
disjunct from the three deadline checks (defect 2). No other line is touched.

- **Non-throwing-path semantics:** unchanged — Q2 proves the boolean result is
  identical to the pre-regression logic for every input that doesn't throw.
- **New allocation/cost on the per-frame path:** none. `owned` is a stack bool; a
  `try`/`catch` that doesn't throw has no meaningful runtime cost in .NET (no
  allocation happens unless an exception is actually thrown). The three deleted
  disjuncts *remove* a comparison per call, if anything marginally cheaper.
- **Stale comments:** checked the surrounding comments (`:151-152`, `:198-199`,
  `:228-229`) — none of them describe frame-count behavior specifically; the closest
  is "Discovery/native failures share the same deadline, never an endless retry"
  (`:228-229`), which is still literally true now that there's only one bound to
  share. No comment now describes code that no longer exists.
- **CHANGELOG:** the v3.1.1-beta2 entry describes the 0.5s wall-clock bound only, no
  frame-count number anywhere in it — consistent with the repair, nothing to update
  there.

---

## Summary of findings

| # | Finding | Rank | Mechanism + repro |
|---|---|---|---|
| 1 | Ownership-read exception loop | — | **Fixed correctly** — narrow catch + existing `ClearPending()` terminates on the next frame on every path checked (Q1). |
| 2 | De Morgan inversion | — | **Correct and equivalent**, including short-circuit order; Il2Cpp `==` semantics unchanged from before (Q2). |
| 3 | `SettleFrames` removal | — | **No new hang.** The one real re-arm-forever mechanism found (`BeginReplacement` under a perpetually-invalid destination) is pre-existing and untouched by this diff (Q3). Worth a separate ticket, not a blocker here. |
| 4 | `s_startFrame` dead field | MINOR | Assigned twice, read nowhere, confirmed by grep across `src/`. Stale-reader trap for future edits; "0 warnings" claim not verifiable from this packet (no .csproj/build output) (Q4). |
| 5 | Scope creep | — | None found — diff is exactly the two claimed fixes, no incidental semantic/perf/comment drift (Q5). |

**What I could not verify:** the "0 warnings" build claim (no `.csproj` or build log in
the packet — source-level dead-field fact is confirmed, the build-report reconciliation
is not). Everything else above is a direct read of the file at its current state plus
the patch, not an assumption.
