# 🟣➤🔷 GLM — failure policy, degrade path, T7 toggle (2026-09-11 14:42, blind, read-only)
sessionId 655916cc-f603-41e3-8185-1d535a945c01 · 💸 CREDITS (Zhipu · glm-5.2-high) · 34648 in / 2035 out

🟣➤🔷 GLM — SIGNED read on the BORDER problem (lens: FAILURE POLICY, DEGRADE PATH, T7 TOGGLE)

VERDICT: DONE_WITH_CONCERNS — the degrade path is essentially free (composer already emits the plain coloured text), but the probe's donor-clone approach is structurally fragile; the safest border path is a FRESH runtime Image, not a clone. The T7 toggle is a clean pref add. Concerns: master-off does not null `s_donor`; Badge+pill double-draw is undecided.

Q1 — ranked candidates (one-line in-game test each):
1. (b) Repurpose the row's own `corrupted-border` sibling. FACT: log line 64 shows `Prefix 1/corrupted-border sprite=T_UI_Tooltip_Corrupted_Effect type=Sliced border=(4,8,73,7)` — a real 9-slice border sprite already parented under every Prefix/Suffix row. Test: enable probe, recolor one `corrupted-border` to `#5a4670`, shrink its rect to the unit bounds, `SetSiblingIndex(0)` under the row → thin border around "Tier 5 | A" with text on top. (Caveat: border (4,8,73,7) is asymmetric — left 4px vs right 73px — NOT PROVEN it shrinks to a balanced pill; may need a different slice sprite.)
2. (a) Fresh runtime-drawn 9-slice on a NEW Image sibling. FACT-fleet: `Sprite.Create` + `Image.type=Sliced` + `SetSiblingIndex(0)` draws under the TMP. Test: build a 1px-border white texture, `Sprite.Create(border=(1,1,1,1))`, parent before the TMP, recolor `#5a4670` → border under glyphs. Avoids the entire donor-sprite-loss class of bug.
3. (c) Clone `item-tooltip-foreground-border-high-tier` (border 12,20,12,67). FACT: real frame-border, but borders are huge. Test: shrink to a 30px unit → corners dominate, not a thin pill. NOT PROVEN to read as a pill.
4. (d) Bracket glyphs `⟦ ⟧` + `<u>` rule. FACT: pure rich text, zero Image risk; OPINION: brackets are ink, not a frame. Test: `⟦`+`⟧` in tier colour → reads as brackets, not a pill.
5. (e) Low-alpha `<mark>`. FACT (ARCHAEOLOGY): `<mark>` draws OVER glyphs, no border tag exists. Already shipped as Badge style. Test: `<mark=#5a467040>` → tinted plate, no outline.

Q2 — why the clone lost its sprite + safest construction:
FACT: probe `Instantiate`s `Header/Name/lorehighlight` (sprite `dialogueOptionGlow`, Simple), strips children + non-Image components, then `CreatePill` re-reads `image.sprite` → null → `InvalidOperationException` (PillProbe.cs:288). The donor's own MonoBehaviour (a lorehighlight controller / pool re-initialiser) re-ran on the clone and cleared the sprite. NOT PROVEN which component. Safest construction (FACT-fleet, OPINION-here): do NOT `Instantiate` a donor. Build fresh: `new GameObject` + `AddComponent<RectTransform>` + `AddComponent<CanvasRenderer>` + `AddComponent<Image>`, then `image.sprite = Sprite.Create(ownTexture, rect, pivot, ppu, extrude, meshType, border)`, `image.type = Image.Type.Sliced`, `image.color = Color.white`, `image.raycastTarget = false`. A sprite you own cannot be cleared by a donor's component — there is no donor. Keep `AddComponent<LayoutElement>().ignoreLayout = true` (probe line 292) so the row's layout group never reflows the pill.

Q3 — positioning:
FACT: `textInfo.characterInfo[i].bottomLeft / .topRight` per char after `ForceMeshUpdate`; unit = first K visible chars (tier word + digits + sep + grade). bottomLeft of first + topRight of last = unit rect in TMP-local space; `rectTransform.TransformPoint` → parent space for the Image. Recompute on: content change (probe already keys text hash), scale change (Canvas scaler — recompute on every scan, cheap), pooled reuse (probe detects `target.GetInstanceID` change), two tooltips (each UpdateLayout postfix is independent, keyed by `tooltipId`). Cheapest trigger: the existing dirty window (`ShouldScan` / `MarkDirty`) — `SyncGeometry` already only runs when pill is active. NOT PROVEN: char bounds for the `<color>` tag boundary chars — must skip non-rendering tag chars when picking K.

Q4 — colour rule:
FACT: mockup CSS `border:1px solid #5a4670` (muted purple-grey); ink = tier colour for tier word, grade colour for letter. OPINION: ONE border per line around the whole `Tier N | G` unit (not two) — calmer, matches the mockup's single pill. Border alpha: full `#5a4670` (already muted); tier colour reserved for ink so the border stays neutral across tiers. NOT PROVEN whether Andrew wantsthe border to take the tier colour on T6/T7 — ask.

Q5 (in depth) — `TierWord = Spelled | Compact`:
- Pref: `TierWord` enum, members `{ Spelled, Compact }`, order FROZEN from day one (cfg round-trips by name — same law as every other Prefs enum, Prefs.cs:8). Default `Spelled` (current behaviour; Compact opt-in). Register the new key in `WarnOrphanedKeys`'s registered set (Prefs.cs:166).
- Where produced: `TooltipRecolor.ComposeCleanLine` builds `tierPart` (line 870) — currently hardcoded `"Tier {tier}"`. Replace with `Prefs.TierWord.Value == TierWord.Compact ? $"T{tier}" : $"Tier {tier}"`. The probe's `PillProbe.cs:248` also hardcodes `"Tier {(hasTier ? …)}"` — must read the same pref so the probe label matches the composer.
- Ground labels: MUST NOT be affected. GroundLabels is a separate path with its own marker (TooltipRecolor.cs:434); the brief says ground labels are not this composer's territory. Pref is tooltip-signal-only. Comment the enum to freeze that scope.
- Alt/deep view: deep view restores EHG's "Tier: N (max craftable)" verbatim (line 680-687) — EHG's text, not ours; Compact must NOT apply. `TierAnnotation` (line 819) is EHG-derived; leave alone. Compact applies ONLY to our composed `tierPart` in `ComposeCleanLine` (and the probe label).
- Border geometry / wrap: Compact shortens "Tier 7" (6 chars) → "T7" (2 chars). Pill width (`Label.preferredWidth + 12f`, PillProbe.cs:388) auto-shrinks — no geometry code change. Wrap risk LOWER (shorter unit = more room for the affix sentence). NOT PROVEN: at 150% scale the 5px corner radius may look proportionally large on a 2-char unit — visual only.

Q6 (in depth) — failure policy / state machine / degrade path:
State machine (per affix line / per tooltip):
1. ARMED — pref on, donor selected / fresh-build ready, no pill yet. Entry `Arm()` (PillProbe.cs:688). Log: `armed (PillTiers=…, GreaterAffixTint=…)`.
2. PLACED — pill active, sibling-index 0, margin applied, geometry synced. Entry `ApplyMarginAndGeometry` (line 359). Log: `pill placed on '…' tier=… grade=… at … size …x… donor=…` (line 267).
3. HIDDEN — pill `SetActive(false)`, margin restored, PillState retained. Entry `Hide(state, reason)` (line 708). Log: `pill hidden: <reason>` (once). Triggers: tooltip inactive, content changed, target no longer parses, target changed.
4. DEGRADED — border cannot be placed; text path fine. Entry: donor selection null OR per-placement exception. Log: `no donor found — degrading to the text path (nothing changed)` (line 197).
5. DISABLED — master off or pref off. Entry `DisarmAtRuntime` (line 694) / `RestoreVanillaOnMasterOff` (TooltipRecolor.cs:246). Log: `runtime off — pills destroyed and margins restored`. Native range switch relinquished (Astra 2026-09-10 law, line 140).

What every failure must do:
- RESTORE: margin → `SavedMargin` (`RestoreMargin`), pill `SetActive(false)` or `DestroyImmediate`, native range switch → original (only on master-off).
- ONE log line: `Fail(stage, ex)` writes `FAIL at <stage>: <ExType>: <message>` exactly once (`s_failureLogged` latch, line 738). No spam across a session.
- LATCH or RETRY: LATCH structural failures (donor sprite lost, donor selection empty, CreatePill exception) — don't self-heal in-session. RETRY transient hides (HIDDEN) — `Hide` keeps PillState so the next UpdateLayout for the same target can re-PLACED without re-creating (line 158-175).

When retry is allowed:
- Next tooltip: YES for HIDDEN (watch's `existing.Current` re-activates when sentinel text hash matches). NO for LATCHED — `s_latchedOff` gates the whole probe for the session.
- Next session: YES always — `s_latchedOff`, `s_failureLogged`, `s_donor` are statics reset on mod reload. A game update that broke the donor path is re-evaluated fresh next launch.

"degrade to the plain coloured text" concretely:
- The composer ALREADY emits coloured text with no border when `SignalStyle.PlainText` (default, Prefs.cs:102). The border is an ADDITIONAL Image sibling on top of that text. Degrade = don't create/activate the Image; the text the composer already wrote is the final render. Concretely: `PlainText` + no pill = today's 3.1.0-dev look (coloured "Tier 5 · A  58% increased…"). The degrade path is FREE — it's the absence of the feature, not a separate render branch.

Exact single log line:
- PASS: `[PillProbe] pill placed on '<first30>' tier=<n> grade=<L> at <pos> size <w>x<h> donor=<path>` (line 267).
- DEGRADED (no donor): `[PillProbe] no donor found — degrading to the text path (nothing changed)` (line 197).
- DEGRADED (runtime exception): `[PillProbe] FAIL at <stage>: <ExType>: <message>` (line 743) → latch-off → next tooltip falls through to composer plain text.

Master-off and pref toggle interaction:
- `Enabled()` (line 680) = `PillProbe.Value && EnableTooltips.Value`. Master off → `Enabled()` false → `DisarmAtRuntime` → all pills destroyed, margins restored, `runtime off` log. Master back on → `Arm()` re-runs, donor re-selected. CONCERN (OPINION): `s_donor` is NOT cleared in `DisarmAtRuntime` — a stale/dangling donor transform could be reused after master toggle; NOT PROVEN safe. Recommend nulling `s_donor` (and `s_donorPath`) on disarm.
- Pref `PillProbe` off → same path as master-off via `Enabled()`.
- Pref `PillTiers` All ↔ GreaterOnly → `Run` detects `watch.Mode != mode` (line 156) → re-selects target, re-places pill.
- Composer's `SignalStyle` is INDEPENDENT: controls text (Badge vs PlainText), not the pill. NOT PROVEN whether Badge plate + pill border double-draw is desired — likely one OR the other, not both. Ask Andrew.

FACT vs OPINION summary: candidate sprite facts (corrupted-border border values, lorehighlight sprite, mockup CSS) are FACT from logs/BRIEF. "Safest construction = fresh build" is OPINION grounded in fleet precedent. Degrade-path-is-free is FACT (composer already emits the text). Master-off not nulling `s_donor` is a CONCERN/OPINION. Badge+pill double-draw is NOT PROVEN — needs Andrew's call.

🟣➤🔷 GLM

---
Conductor note: GLM alone ranks the corrupted-border reuse first and undercuts its own pick in the same line (asymmetric 4/73 slice, NOT PROVEN); five seats reject it as full-row corruption VFX. Its Q5/Q6 detail (Compact must not touch EHG's deep-view "Tier:" text nor ground labels; state machine; s_donor not cleared on disarm) is adopted by the ticket. The Badge+border double-draw question is resolved in the ticket: the border draws only on the PlainText path.
