# Adversarial Review — v3.1.1-beta1 (shard-stutter diagnostic beta)
Reviewer: Anthropic seat (second lens, after a zero-finding Google-seat pass)

## Verdict: APPROVE_WITH_NOTES

No finding here rises to a proven, reproducible-in-the-diff-alone BLOCKER, but one
finding (#1) is a real robustness regression I could not fully clear and would want
answered before this reaches more than the one reporter. Everything else checked out.

---

## Findings

### 1. MATERIAL — `FilterRuleTooltip.MonitorUpdate` weakens exception recovery around
   the ownership check, risking a permanent per-frame exception loop for players who
   never hover a shard at all
`FilterRuleTooltip.cs:154-161`

```csharp
if (s_owner == null || !s_owner.tooltipActive ||
    s_owner != UITooltipItem.instance || s_owner.target != s_target ||
    s_owner.targetType != s_targetType)
{
    if (s_pendingItem != null && TooltipPerf.Enabled) TooltipPerf.RuleReuse();
    ClearPending();
    return;
}
```
This whole block sits inside `MonitorUpdate`'s single outer `try` whose `catch`
(`FilterRuleTooltip.cs:222-227`) is:
```csharp
catch
{
    if (Time.frameCount >= s_startFrame + SettleFrames || Time.unscaledTime >= s_deadline)
        GiveUp();
}
```
**Mechanism:** the pre-diff code wrapped exactly this property read in its own
defensive try/catch, separate from the method-level one:
```csharp
bool active = false;
try { active = tooltipUI.tooltipActive; } catch { }
if (!active) { s_pendingItem = null; s_injected = false; return; }
```
(visible in the removed lines of the patch, `FilterRuleTooltip.cs` old hunk). That the
original authors bothered to nest a *second* try/catch around one specific property
access, on top of the outer one, is strong evidence this call has thrown in the wild
for this codebase before — plausibly during scene/UI teardown, not just object
destruction. The new code drops that inner guard for `.tooltipActive`, `.target`, and
`.targetType`.

If any of those three reads throws while `s_owner` is stale, execution lands in the
outer `catch`, which **does not call `ClearPending()`**. `GiveUp()` only touches
`s_injected/s_repairPending/s_hadDestination/s_destination` — it leaves `s_owner`,
`s_target`, `s_targetType`, `s_pendingItem`, `s_ruleResolved` all pointing at the same
stale state. The next frame reruns the same property reads on the same stale `s_owner`
and throws again — an exception raised and caught every single `OnUpdate` frame,
indefinitely, for as long as the player goes without hovering an item whose captured
content differs from what's cached (which is the only path back into `ClearPending()`,
via `Capture`'s "not same" branch).

**Repro path:** hover any ordinary item (gear, not a shard) so `Capture` sets `s_owner`.
Cause whatever real condition made the original authors defend `.tooltipActive` with
its own try/catch (closing the panel mid-transition, a scene load, an inventory close —
I don't have `ARCHAEOLOGY.md` in this packet to confirm which one it was). If that
condition throws on read, and the player doesn't immediately hover a *different* item,
every frame from then on pays a thrown-and-caught exception — a genuinely new
per-frame cost that affects the 99% who never touch a crafting shard.

**What I could not verify:** whether `UITooltipItem.instance` is a persistent
pooled singleton (in which case `.tooltipActive` merely goes false and never throws,
and this finding is moot) or gets recreated/destroyed per tooltip. The `s_owner !=
UITooltipItem.instance` comparison in the same condition suggests a singleton, which
would argue against the throw actually firing under ordinary play — but that only
strengthens the case that the ORIGINAL inner try/catch existed for a real, narrower
reason I can't see from this diff alone. Either way, the recovery contract is
objectively weaker than before (old: any failure here self-heals to a clean empty
state on the very next frame; new: a failure here can wedge until an unrelated
successful hover). This is a two-line fix (reintroduce a narrow try/catch around the
ownership reads that defaults to "not owned, clear cleanly") and cheap insurance
regardless of how often it actually fires. I'd want it before a wider beta, not
necessarily before this one-reporter beta.

### 2. MATERIAL — discovery window is `min(12 frames, 0.5s)`, not `up to 0.5s`; high
   refresh rates get a much shorter real-world budget than documented/tested
`FilterRuleTooltip.cs:184-189, 219-220`, `docs/regression-output.txt:30`

```csharp
if (s_lastAttemptFrame >= 0 &&
    (frame > s_startFrame + SettleFrames || Time.unscaledTime > s_deadline))
```
`SettleFrames = 12`, `SettleSeconds = 0.5f`, combined with OR — the window expires on
whichever bound is hit *first*. At 60 fps that's ~200 ms (frame bound dominates,
still well under the 500 ms wall-clock cap). At 240 Hz, 12 frames is 50 ms — the
frame bound now caps real-world discovery time to a tenth of the documented 0.5 s
figure in the changelog ("through 12 frames or 0.5 seconds (whichever expires
first)" — technically accurate, but the practical effect at high refresh rates is a
much tighter window than the number most readers will take away).

The changelog and regression suite both frame this as a *low*-FPS concern (test 30,
`regression-output.txt:30`, "low FPS uses wall-clock deadline") — correctly proving
the wall-clock floor protects low-FPS players. There is no equivalent test proving the
*high*-FPS direction is safe, i.e. that 12 frames is always enough real time for the
`'requires'` TMP to activate. If that UI element is ever created a few frames late
(a hitch, a co-occurring layout pass, anything) on a 240 Hz+ system, `GiveUp()` fires
before it would have on a 60 Hz system, and that hover silently gets no Rule# — a
regression on ordinary gear that is invisible on the hardware the fix was likely
tested on.

I don't have evidence this actually happens in practice (I can't drive the real UI),
so I'm not calling it proven, but the arithmetic is exact and the asymmetry in test
coverage (low-FPS tested, high-FPS not) is a real gap in "must not make things worse
for the 99%." Cheap fix: use `Time.unscaledTime >= s_deadline` as the sole real-time
gate and keep the frame counter only as a *minimum* one-frame-per-attempt throttle
(which it already provides via `frame == s_lastAttemptFrame`), rather than a second
expiry race.

### NOT A BUG — checked and cleared: `RetireStaleOriginals` vs. Alt/master-off restore
I traced this in detail since it was explicitly flagged as a hunt target.
`RetireStaleOriginals` (`TooltipRecolor.cs:625-648`) runs at the *end* of `RunScan`,
after Pass 2 has already re-composed (and re-marked) any TMP whose text still
classifies as tier/range/grade content this pass. It only removes entries that are
**active and markerless after Pass 2 has already had a chance to re-claim them** — i.e.
genuinely repurposed TMPs. Both `ReRenderFromOriginals` (Alt) and
`RestoreVanillaOnMasterOff` already independently no-op on markerless entries
(`TooltipRecolor.cs:288`, `:254`) regardless of whether `RetireStaleOriginals` ran —
so early removal changes nothing observable for either restore path. I also traced the
Alt-toggle-in-`OnLateUpdate` ordering (`ReRenderFromOriginals` runs before the
catch-up `RunScan` in the same call) and confirmed the immediately-recomposed,
freshly-marked text is never mistaken for stale in the same pass. No blocker here.

### NOT A BUG — checked and cleared: replacement-window thrash
Traced `BeginReplacement`/`Capture`/`GiveUp` state transitions end to end
(`FilterRuleTooltip.cs:74-105, 230-256`). `s_hadDestination` is cleared by
`BeginReplacement` itself, so a failed replacement search cannot re-arm another
replacement window without an intervening successful `WriteRule`. Repeated
identical-content setters after a `GiveUp()` do nothing (`s_injected` gates the whole
block). Matches regression tests 16-25. No retire→re-add→retire cycle exists.

### NOT A BUG — checked and cleared: telemetry cost when `DebugLog` is off
`TooltipPerf.Tick()` runs unconditionally every `OnLateUpdate`, but the disabled path
(`TooltipRecolor.cs:1050-1054`) does the `Enabled` check and returns before any
`Time.unscaledTime` read, allocation, or string interpolation. Every event call site
(`RuleStart`, `Scan`, etc.) is guarded by `if (TooltipPerf.Enabled)` at the call site
in addition. Matches regression test 53.

### NOT PROVEN — Il2Cpp dictionary/array interop on `ItemDataUnpacked.id`
`Capture` reads `item.id` (presumably an Il2Cpp byte array) and manually copies it
byte-by-byte into a managed `byte[]`. I have no way to confirm the Il2Cpp array
wrapper behaves like a normal indexable array under interop here (no visibility into
`ItemDataUnpacked`'s generated bindings in this packet). No mechanism for a concrete
failure, flagging only for completeness per the Il2Cpp-trap instruction.

---

## What I could not verify
- Whether `UITooltipItem` is a persistent pooled singleton or gets recreated per
  tooltip — decides how real finding #1 is. Not in this packet; would need
  `ARCHAEOLOGY.md` or the decompiled game assembly.
- Actual in-engine latency for the `'requires'` TMP to activate after
  `SetAsItemTooltip`, at various refresh rates — decides how real finding #2 is.
- Runtime behavior of the 53 regression stub assertions — I read them as text; I did
  not execute them. They read as real assertions over a hand-rolled state-machine
  simulation (not tautologies), and several specifically encode negative behavior
  (e.g. test 18/21/25 "remains bounded despite equal-content setters," test 47 "failed
  scan does not retire unknown ownership") that would fail if the described bug were
  reintroduced — they look like they can genuinely fail, not decorative green checks.
  I did not, however, see a stub that exercises the exception-in-ownership-check path
  of finding #1, or the high-FPS-dominant-bound path of finding #2 — both gaps are
  consistent with what I found by hand.
- No in-game verification of either finding; per the brief's stakes, both are
  reported as MATERIAL rather than BLOCKER because I lack the missing fact (singleton
  lifetime; real UI activation latency) that would convert them to certain.
