# 🟣➤⚫ Cursor-Grok — adversarial lens (2026-09-11 14:41, blind, read-only)
sessionId 4e56fed1-507d-4550-8c03-00480451a66a · ♾️ INCLUDED (xAI · cursor-grok-4.6-high) · 89550 in / 8956 out

🟣➤⚫ Cursor-Grok — ADVERSARIAL read of BRIEF + PillProbe + TooltipRecolor + Prefs + ARCHAEOLOGY + NativeClone + LOG-pillprobe.

**Verdict: DONE_WITH_CONCERNS** — ship a **fresh Image + owned 9-slice**, never clone. Sprite-loss root cause and HDR-on-Image are NOT PROVEN.

---

### Q1 — ranked (border UNDER glyphs, tight on `Tier N | G`)

**1. (a)+(f) Fresh GO + `AddComponent<Image>` + one `Sprite.Create` 9-slice (1px ring, transparent centre). Never Instantiate a donor. `ignoreLayout`, `SetSiblingIndex` before `Text`, bounds from unit `characterInfo` only.**
FACT: `<mark>` cannot draw under glyphs; sibling Image can (BRIEF + archaeology). Probe died on clone, not on Image-as-sibling.
Breaks if: per-frame `ForceMeshUpdate` (stutter); parented into a pooled Prefix row without a TT_* name + generation key (wrong-item leftover); Harmony postfix runs *before* composer+`RequestRelayout` (bounds from essay, not clean line); whole-`lineInfo[0]` used on a wrap (box covers sentence start); ZWSP counted as visible (usually 0-width, still index-shift); UI scale cached in px not TMP-local; two tooltips sharing one Image; layout group without `ignoreLayout` (row jump); LeHud rewrite drops marker and the box stays. Survives: `corrupted-border` rename, clone sprite-loss, comparison (per-TMP instances). HDR wash of a real Image: NOT PROVEN (last night was `<mark>` quads).
**Test:** T7 long affix, 150% UI, comparison + walk a ground tooltip; box must hug `T7|S` with no hitch. Kill: hitch or box on wrapped sentence.

**2. (f′) Four 1px hairline Images.** Same placement/lifecycle as (a). No radius (mockup wants 5px). More objects. Same tests.

**3. (d) `⟦ ⟧`/`<u>` in tier colour.** FACT: no TMP border tag; `<u>` is an underline, not a box; game font may tofu box-draw. Survives pool/compare/scale/stutter/LeHud (it is text). Fails the ask (not under, not a pill). Wrap can split `⟧`.
**Test:** T1 wrap + exotic font; look for tofu / wrap split.

**4. (e) low-α `<mark>`.** FACT: quad OVER glyphs, no pad, square; HDR washed light marks white; Badge already did this and Andrew rejected the plate. Does not transfer the mockup border.
**Test:** HDR screenshot of T1 gray mark — wash to white.

**5. (b) `corrupted-border`.** FACT: dump `T_UI_Tooltip_Corrupted_Effect` Sliced `border=(4,8,73,7)` rect **447.6×42** on every Prefix/Suffix — full-row corruption VFX, not a chip. Active-state NOT PROVEN. NearbyImage would hit `Arrow (1)` first anyway. Rename kills it. Recolor steals corrupted look. 73px slice cannot shrink to a unit.
**Test:** hover a **corrupted** item — native smear gone or whole-row purple box.

**6. (c) clone `background-fill-ornamentation`.** FACT: main `item-tooltip-foreground-border-high-tier` Sliced `(12,20,12,67)` 450×507.5; **Compare-Content copy sprite=null**. Miniaturizing a tooltip frame ≠ 1px pill. Same Instantiate landmine.
**Test:** open comparison — clone null sprite, FAIL line repeats.

**Survivor:** (a) fresh Image. **Fastest kill:** 150% + wrapped T7 + comparison + walking ground tooltip. If that passes, HDR still to prove.

---

### Q2 — sprite loss
FACT: log `donor try 1/2 → none`; try 3 accepted `Header/Name/lorehighlight` (`dialogueOptionGlow`, Simple, border 0); **same postfix** `FAIL at UpdateLayout: InvalidOperationException: accepted donor clone lost its Image sprite`. Throw is in `CreatePill` immediately after Instantiate + `DestroyImmediate` children + `StripPillComponents` — not a later layout pass.
FACT: `FirstAffix` matched **`Sealed Affix Header`** (`name` contains `"Affix"`). Real lines are `Prefix N/Text` starting with `<color=…>Tier` so `StartsWith("Tier ")` misses them. Probe never targeted an affix TMP. Header parent has no Image sibling → try 1 none.
FACT: NativeClone Instantiate of Sort **works** because it strips only `SortInventoryButton` + localize, then **keeps** Image; probe DestroyImmediate **every** non-Image component.
OPINION: OnDestroy of an EHG glow behaviour nulled `Image.sprite`, or this Simple sprite is runtime-assigned and does not copy. NOT PROVEN which. Compare ornamentation already lives with sprite=null.
**Survive:** copy sprite+type+material+color from the **live donor Image before strip**; better: **never clone**. Do not cache `s_donor` on a header glow.

---

### Q3 — bounds
FACT: `textInfo` after `ForceMeshUpdate` is the verified hook. Probe used `lineInfo[0]` + a **second** label TMP — wrong for overlay-in-place and for wrap.
Do: after compose+marker, walk **visible** `characterInfo` of the unit span only (skip 4×ZWSP / 0x200B). `bottomLeft`/`topRight` → `TransformPoint` into the row parent. Recompute on composer dirty window only (`MarkDirty` 5 frames, marker loss, Alt, `TierWord` change) — **not** LateUpdate mesh updates (stutter). Positioning-only `UpdateLayout` (ground follow): sibling moves with parent, no recompute. Two tooltips: one Image per TMP instance. Pool: hide/destroy when marker gone. 100/125/150: stay in TMP-local, never cache screen px. Controller: NOT PROVEN different tree. Cheapest trigger: existing dirty window, after `s_relayouting` remeasure.

---

### Q4 — colour
Mockup FACT: `border:#5a4670`, fill `#1e1526`, ink = tier + grade. Sentence white / `#C990FF` settled.
OPINION: **one** calm `#5a4670` ring, centre fully transparent (not a dark plate — Andrew's ask). Not tier-tinted (T3 `#16FF0E` screams; T1 vanishes). Not two boxes (wrap + noise). Keep ink as now (PlainText).

---

### Q5 — `T7` vs `Tier 7`
Name **`TierWord = Spelled|Compact`** (new cfg key; not in Prefs today). Compact **does** shrink border width and **reduces** wrap. Composer/`AffixRule`/`s_tierRegex` today assume `"Tier "` — Compact must be composed and parsed or targeting dies. ReRenderNow on change.

---

### Q6 — failure
Degrade to current PlainText coloured `Tier N · G`, **log once**, latch the Image path off (probe pattern). Text path stays up.
**The line:** `FAIL at UpdateLayout: InvalidOperationException: accepted donor clone lost its Image sprite` (already proved clone-death). Production success: `border placed on '…' … size WxH`. Degrade: `border unavailable — falling back to plain coloured text`.

---
🟣➤⚫ Cursor-Grok

---
Conductor note: "Compact must be composed and parsed or targeting dies" is the one Compact-mode trap the ticket must honour: any regex in the composer that re-reads its own `Tier N` output (the re-entry / unbracketed paths) must accept `TN` too.
