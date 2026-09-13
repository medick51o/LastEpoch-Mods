# 🔵 Codex (Astra) — LEAD, Unity UI / TMP engineering (2026-09-11 14:40, blind, read-only)
threadId 01a09266-d38d-71b0-8aad-fac2c3978a95 · gpt-6-astra · packet C:\Sync\Projects\tt-border-2026-09-11 (BRIEF md5 4a3fbe4a…)

**SIGNED READ — DONE_WITH_CONCERNS**

Read BRIEF.md first, all requested source files, ARCHAEOLOGY.md, NativeClone.cs, and the supplied hierarchy/run log. Read-only; no files changed. This is an engineering recommendation, not an in-game validation.

**Q1 — Ranked candidates; costs and decisive tests**

1. **(a) Build first: runtime-drawn 9-slice sprite on a fresh Image immediately before the existing TMP.** **OPINION:** highest reliability and closest match. Moderate implementation cost; one shared texture/sprite, one Image per displayed unit, bounds work only when dirty. Interleaved Image/TMP materials can increase draw calls; batching is **NOT PROVEN**.
   **In-game test:** outline every composed affix unit on the logged gloves, reopen repeatedly, compare two items, and change UI scale; pass only if borders remain under unchanged coloured glyphs, correctly fitted, without stale duplicates.

2. **(f) Additional native candidate: `border-nice`, copied onto a fresh Image.** **FACT:** the dump shows Sliced, border `(5.83,5.40,5.83,5.40)`, already displayed at `204×17.47` and `204×24` on footer tips. Lower asset-development cost than (a), identical positioning cost; transparency and visual suitability are **NOT PROVEN**. Better native starting proportions than the full-tooltip frame.
   **In-game test:** display its sprite at actual `T7 | S` and `Tier 7 | S` bounds; reject if the centre fills, corners crowd the letters, or baked colour resists tinting.

3. **(c) Shrink `background-fill-ornamentation`.** **FACT:** Content has `item-tooltip-foreground-border-high-tier`, Sliced, `(12,20,12,67)`, `450×507.5`; Compare-Content's same-named Image has **sprite=null**. Moderate adaptation cost: large asymmetric slices can compress badly at label height; test `pixelsPerUnitMultiplier`, not whole-object scaling. Prefer copying the sprite to a fresh Image over cloning its object.
   **In-game test:** fit the frame to both word forms at normal text height; reject decorative spikes, uneven stroke, collapsed corners, or missing comparison borders.

4. **(b) Clone/recolour `corrupted-border`.** **FACT:** `T_UI_Tooltip_Corrupted_Effect`, Sliced, `(4,8,73,7)`, normally `447.6×42`; nested Image has null sprite. **OPINION:** this looks semantically like corruption-effect art, not a neutral pill asset. Actual pixels and transparent centre are **NOT PROVEN**; slice metadata describes stretch regions, not stroke thickness. Directly repurposing the original adds ownership conflicts; cloning adds lifecycle risk.
   **In-game test:** render an owned copy at compact-unit size, then alternate corrupted and ordinary items; reject effect wash, asymmetric ornamentation, reset behaviour, or any change to native corruption indicators.

5. **(d) Rich-text brackets plus underline.** Low implementation/runtime cost; no extra GameObjects. **FACT:** `<u>` supplies a text-coloured line, not a closed outline; brackets add width. Fancy bracket/box-drawing glyph availability is **NOT PROVEN**.
   **In-game test:** show `[Tier 7 | S]` and fancy-bracket variants beside long affixes; reject missing glyphs, disconnected "borders," or additional clipping.

6. **(e) Low-alpha `<mark>`.** Cheapest background experiment, but lowest fidelity to the request. **FACT from supplied in-game evidence:** the quad overlays glyphs, is square, and has no padding; HDR previously washed highlights towards white. Existing Badge mode also substitutes cream ink, so it is not the requested fallback.
   **In-game test:** compare tier-coloured marks at alpha `0D/1A/26`—about 5/10/15%—while retaining tier/grade ink, in SDR and HDR; reject glyph washout or unreadable background distinction.

**Q2 — What actually failed, and what survives**

**FACT:** the exception occurs inside `CreatePill` (PillProbe.cs:274), in this order: `Instantiate` → destroy children → `StripPillComponents` → retrieve root Image → test `image == null || image.sprite == null` → throw.

"FAIL at UpdateLayout" names the enclosing postfix's catch. It does **not** establish that a working sprite survived construction and disappeared during a subsequent layout. The message cannot even distinguish a missing Image component from a missing sprite.

**FACT:** the accepted donor is a Simple `dialogueOptionGlow` on `Header/Name/lorehighlight`, with zero slice borders. It is neither a demonstrated pill nor a demonstrated closed frame.

**NOT PROVEN:** whether Instantiate/Awake/OnEnable cleared the sprite; child destruction or component OnDisable/OnDestroy cleared it; IL2CPP wrapper type tests caused the blanket stripper to remove the Image; or another game component synchronously reinitialised it. The dump contains no component inventory or intermediate sprite checks. A later pooled reset is unsupported by this failure location.

**FACT:** `StripPillComponents` intends to retain Image/RectTransform/CanvasRenderer, but intention is not runtime evidence. NativeClone removes specifically identified behaviours and localisation bindings; it does not prove this blanket strip safe.

A decisive diagnostic would log Image presence, sprite/overrideSprite IDs, and component types before Instantiate, immediately afterward, after child deletion, and after each strip. Use IL2CPP-aware component casts when inspecting wrappers.

Copying saved sprite/material/colour **after** stripping repairs construction-time changes only; surviving external writers can still reset them. Destroying behaviours after an active Instantiate cannot undo callbacks that already ran.

**Recommendation:** construct an owned `GameObject + RectTransform + CanvasRenderer + Image`, with `LayoutElement.ignoreLayout=true`; explicitly assign sprite, default UI material, tint, enabled state, and `raycastTarget=false`. No donor behaviours, animation, localisation, or cloned TMP.

Create one white RGBA rounded-outline texture at startup—for example 32×32, radius 5, stroke 1, transparent centre, six-texel slice borders—then `Apply` and `Sprite.Create(..., FullRect, new Vector4(6,6,6,6))`. Use Clamp, no mipmaps, and retain texture/sprite references. Set `Image.type=Sliced`, `fillCenter=false`; calibrate sprite PPU against canvas reference PPU.

Parent with `worldPositionStays=false`, unit local scale, and insert at the TMP's current sibling index. This places it immediately before Text, including Text's descendants, under normal shared-canvas ordering. Pool survival, masking, and external hierarchy manipulation still require testing.

**Additional probe defects, established by source/log:**

- The selected "affix" was **Sealed Affix Header**, not `Prefix N/Text`. PlainText output starts with `<color=...>`, so `StartsWith("Tier ")` misses it; a TMP named `Text` also misses the name heuristic.
- `NearbyImage` does **not** filter inactive objects. The dump logs neither `activeSelf` nor `activeInHierarchy`; therefore corrupted-border activity is **NOT PROVEN**, and donor try 1 does not imply inactivity.
- `PillTiers=All` still selects only `firstAffix`; one `watch.Current` cannot decorate every row.
- Had construction succeeded, the probe would clone a second label, retain the original signal, and enlarge the original margin. Its LateUpdate would also repeatedly force both meshes. Replace that design rather than extending it.

**Q3 — Exact bounds and recomputation recipe**

1. Have the composer record each unit's **start/end source indices in the final rich-text string**, alongside TMP ID, object reference, and composition revision. Record multiple spans for multi-signal TMPs. The four trailing U+200B characters remain the ownership marker; they are not a span delimiter.
2. Select spans from composer metadata, not object-name guesses or a fixed first-K count. `Sealed`, Trailing/SignalRight layouts, hybrid grades, hidden grades, and tier-only signals change the relevant substring. Never invent a missing grade.
3. After the composer's game relayout returns, queue measurement once layout is settled. Call `tmp.ForceMeshUpdate()` for each dirty, active target, then inspect `textInfo.characterInfo[0..characterCount)`.
4. Include characters whose `characterInfo[i].index` lies within that recorded span and whose `isVisible` is true: tier letters, digits, separator, and all displayed grade letters/separators. Exclude rich-text tags, four ZWSPs, whitespace quads, `Sealed`, sentence text, and deep-view annotations. Interior spaces still contribute through the positions of neighbouring visible glyphs.
5. Union each included character's `bottomLeft` and `topRight` in TMP-local coordinates. These are rendered geometry bounds, not raw screen pixels or character-count estimates. Require nonempty, finite, positive bounds and a single `lineNumber`.
6. For the approved style, expand by 9 horizontal and 2 vertical reference UI units. TMP margins—including `(32.4,3,19.2,-4.8)`—already affect character positions: **do not add them again**. Check sentence clearance; the existing two spaces do not guarantee room for padding. Reserve measured space in the composer and relayout when needed.
7. Transform all four expanded rectangle corners with `parent.InverseTransformPoint(tmp.rectTransform.TransformPoint(corner))`; union in parent coordinates. With fixed anchors, centre pivot, identity rotation/scale, set border `sizeDelta=max-min` and `localPosition=(min+max)/2`.
8. Keep dimensions in UI units: canvas scaling already converts them to screen pixels. A one-unit stroke becomes approximately 1/1.25/1.5 screen pixels at the requested scales. If one **physical** pixel is required, adjust using effective screen scale; never multiply character bounds by scale again.
9. If the unit wraps across lines, suppress that border rather than drawing a rectangle across the sentence. Keeping the unit unbroken can be a separate tested composer change; compact wording reduces this risk.

**Cheapest trigger:** extend the composer's existing dirty workflow: composition/style/Alt changes queue border updates, flushed after its latched relayout. Preserve `s_relayouting`; a separate unordered postfix could measure before composition or during nested layout.

Reuse `DirtyFrameWindow=5`, the once-per-frame scan guard, and 0.5-second fallback; add no scene scan. On tracked rows, cheaply compare revision, rect, margins, font/layout settings, and TMP-to-parent transform. Force meshes only when these change; movement of the entire tooltip needs no relative-geometry work.

Track every active row independently, with owning content root and live reference as well as instance ID. Invalidate on marker loss, rewritten text, reparenting, destruction, or pooling; hide stale borders immediately. Content and Compare-Content can share one UITooltipItem, so tooltip ID alone is insufficient.

The composer currently stores only `s_lastTooltip/s_lastArgs`; those globals do not establish correct relayout of every simultaneously visible tooltip. Capture pending work/arguments per actual owner. Controller mode and scale changes must invalidate affected rows through the same route.

**Q4 — Colour and grouping**

**OPINION:** one border around the combined `Tier N | G` unit, neutral **`#5A4670` at 100% alpha**, transparent centre. That matches the approved outline and keeps tier/grade colours dominant. If a filled centre is later wanted, test `#1E1526` at 10–15% separately.

A tier-coloured alternative should start near 45% alpha, but it is a secondary experiment. Keep tier/grade ink from existing palette data and the settled white / `#C990FF` sentence rule. Two borders add noise and spacing cost.

**Q5 — Player option**

Add **`TierWord = Spelled | Compact`**, default **Spelled**, yielding `Tier 7` or `T7`. It does not exist in the supplied Prefs.cs. Register it in the existing category and orphan-key whitelist; preserve existing preference names and enum ordering.

Generate the wording centrally in `ComposeCleanLine`, including colours-off paths; recompose cached originals and invalidate geometry on change. Bounds automatically become narrower in Compact mode; reduced width lowers, but does not eliminate, wrap risk.

**Q6 — Failure policy and first-build acceptance**

Placement failure leaves **plain coloured text with no border**. Hide/destroy only owned decoration; preserve marker, glyph colours, and native margins. Log once per distinct failure cause. Retry transient unsettled geometry during the bounded dirty window; latch genuine resource/construction failure off for the session.

Proposed diagnostic, emitted only after placement validation:
`[TTBorder] path=runtime-sliced result=placed owner=<id> tmp=<id> span=<start:end> sprite=TT_Border rect=<x,y,w,h> order=before-Text`

Failure diagnostic:
`[TTBorder] path=plain-text result=fallback reason=<reason> logged-once`

These lines prove the code branch and checks executed, **not** visual correctness. The supplied log proves only `[PillProbe] FAIL at UpdateLayout: InvalidOperationException: accepted donor clone lost its Image sprite`.

**Build first:** (a), decorating existing composer-owned text. **Exact acceptance run:** verify the loaded DLL identity; show the logged T5/T4/T4/T7 plus sealed-T1 item, then a T6 item; toggle Spelled/Compact, grade visibility and Alt; compare items; cycle tooltips 50 times; repeat at 100/125/150%, with controller and LeHud/Fallen enabled; walk while a ground tooltip tracks position. Require all eligible units outlined, unchanged ink, no sentence overlap, no stale/duplicate borders, correct cleanup, and zero added mesh recomputes on unchanged positioning-only frames.

**Fallback:** plain coloured text. `border-nice` is the next controlled asset experiment, not an automatic runtime substitution.

**Remaining concerns:** exact clone failure cause, donor artwork/alpha, runtime border survival, rendering cost, and compatibility are **NOT PROVEN**. The log also warns that three prefs are unregistered although this source whitelists them; exact source-to-loaded-binary correspondence needs verification.

🔵 Codex (Astra)

---
Conductor note: the "unregistered prefs" warning in the log came from the pre-repair 14:17 DLL that was running; the source Astra read is the repaired one. Astra's finding that the probe targeted "Sealed Affix Header" instead of `Prefix N/Text` (the PlainText line starts with `<color=`, so `StartsWith("Tier ")` never matched) is a real defect in PillProbe.cs and is why the new feature must locate the unit from composer metadata (the `<link>` idea, Kimi) rather than name heuristics.
