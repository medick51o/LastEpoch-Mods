# 🟣➤🌙 Kimi — TMP geometry and rich-text lens (2026-09-11 14:42, blind, read-only)
sessionId 502ef8e8-b80e-4359-ab0b-e68278f663ef · 💸 CREDITS (Moonshot · kimi-k3-high) · 101459 in / 5899 out

**SIGNED READ — 🟣➤🌙 Kimi — lens: TMP GEOMETRY & RICH-TEXT — verdict: DONE_WITH_CONCERNS**
(read: BRIEF.md, TooltipRecolor.cs, PillProbe.cs, Prefs.cs, ARCHAEOLOGY.md, LOG-pillprobe.txt)

---

**Q1 — ranked candidates for the border**

**1. (a′) Fresh-built Image sibling + runtime-drawn 9-slice, positioned from textInfo.** Not a clone — `new GameObject` + `Image` + `Sprite.Create(tex, rect, pivot, ppu, 0, MeshType.FullRect, new Vector4(1,1,1,1))` from a 4×4 runtime texture, `Image.type=Sliced`, parented to the row (`Prefix N`), `SetSiblingIndex` before `Text` so it draws under the glyphs (fleet-proven: TI NativeClone, Sprite.Create 9-slice, sibling-order law). Sidesteps the D2 sprite-loss failure entirely — nothing is cloned. Cost: one 64-byte texture cached for the session, one Image per affix row, bounds recompute only inside the composer's existing dirty window.
*In-game test:* hover a rare with 2+ affixes → thin frame around `Tier 5 · A`, glyphs crisp on top; Alt deep-view → border tracks; pooled tooltip on a second item → no stale border.

**2. (e) Low-alpha `<mark>` in tier colour + light ink — the "different background" Andrew also accepted.** FACT (ARCHAEOLOGY, in-game verified): `<mark>` draws OVER glyphs; dark ink dies, light ink survives. Exact string: `<mark=#A807FF59><color=#FFF6E6>Tier 5</color></mark>` (alpha ~0x59; HDR washed light marks to white last night, so mid-alpha + warm-white ink, not pastel). Zero new objects, works today.
*Test:* SignalStyle=Badge already ships this shape — confirm ink legibility in HDR at 40–60% alpha.

**3. (d) Rich-text pseudo-border (3-sided).** No border tag exists (FACT). Best achievable: brackets + underline — `<color={tierHex}>[</color>…<color={tierHex}>]</color>` with `<u>` inside the unit, `<cspace=2>` for air. ASCII `[ ]` guaranteed in font; `⟦ ⟧` (U+27E6/7) and box-drawing glyphs are **NOT PROVEN** — likely tofu in EHG's font; test before shipping. It reads as "bracketed", never as the mockup's rounded rect.
*Test:* compose one line with `⟦⟧`; if boxes appear, fall back to `[ ]`.

**4. (c) Re-slice `item-tooltip-foreground-border-high-tier`.** It IS a frame-border sprite (FACT, log line 142) but its slice is (12,20,12,**67**) — a 67px top band built for a 450×507 frame; squashed to a ~70×22 pill the corners dominate and it will look wrong. Only viable via `Sprite.Create` re-slice of its texture with a small border — at which point it's just (a′) with a native texture. **NOT PROVEN** the texture tiles cleanly at 1px.
*Test:* re-slice at border (2,2,2,2), pill on one line, screenshot at 150% UI scale.

**5. (b) Re-purpose `corrupted-border`.** Reject. Sprite `T_UI_Tooltip_Corrupted_Effect` slice is (4,8,**73**,7) — an asymmetric effect overlay, not a frame; the game owns its activity (corrupted items), and donor try 1 → none means it was inactive/filtered at probe time (FACT from log). Fighting the game for its own widget is the Range-widget war all over again.

**Q2 — why the clone lost its sprite.** FACTS: donor `lorehighlight`/`dialogueOptionGlow` had a sprite at SelectDonor; after `Instantiate` + `StripPillComponents` inside the same call, `image.sprite == null`. Exact mechanism **NOT PROVEN** — two live suspects: (i) a stripped MonoBehaviour's OnDisable/OnDestroy nulls the graphic it owns (DestroyImmediate still runs callbacks under IL2CPP); (ii) the donor's sprite is assigned per-frame by game code and the clone never gets that assignment. Survival rule: copy `sprite`/`material`/`color` into locals BEFORE stripping, re-assign AFTER; or better — never clone visuals, build fresh (candidate 1). *Test:* log sprite name pre-strip/post-strip/post-reassign on one clone.

**Q3 — exact textInfo recipe (the deep answer).**
1. After composing, `tmp.ForceMeshUpdate()` once — required after any `.text` write before textInfo is valid. Cost: one mesh rebuild per changed affix TMP; fine inside the dirty window, **never per frame** (the stutter bug is fresh).
2. Locate the unit: wrap it in a `<link id="ttu">…</link>` at compose time. FACT: TMP populates `textInfo.linkInfo[i].linkTextfirstCharacterIndex` / `linkTextLength` and a bare `<link>` renders with zero visual change. This is robust to tag-stripping miscounts and to the 4-ZWSP marker (ZWSPs sit in characterInfo with `isVisible=false`). Fallback without link: walk `characterInfo`, skip `!isVisible`, take chars `[0 .. K)` where K = plain length of `Tier N · G` — fragile, link is better. **NOT PROVEN** in this exact TMP build; standard TMP behaviour.
3. Bounds: restrict to `lineInfo[0]` (`firstVisibleCharacterIndex`…`lastVisibleCharacterIndex`) — the first-line-only guarantee. xMin = min `bottomLeft.x`, xMax = max `topRight.x` over unit chars; y from `lineInfo[0].ascender`/`descender` (+2px pad). characterInfo positions are final local positions — margins (32.4,3,19.2,−4.8) are already baked in (FACT of TMP).
4. Wrap guard: if any unit char's `lineNumber != 0` (SignalRight/Trailing on a narrow tooltip can split the unit), degrade — no border that line. BadgeLeft puts the unit at line start; the sentence wraps, the unit doesn't.
5. To parent space: the PillProbe.SyncGeometry pattern is correct — `targetRect.TransformPoint(local)` → `parent.InverseTransformPoint(world)`; anchors (0,1)/(0,1), pivot (0,1), set `sizeDelta` + `localPosition`.
6. UI scale 100/125/150%: all math is canvas-local; CanvasScaler scales uniformly, so the pill stays glued for free (FACT of uGUI; per-scale screenshot test still advised).
7. Cheapest trigger: piggyback `ShouldScan()` — recompute only when the composer ran (`composed > 0`), marker lost, or Alt flip. Tooltip *position* changes never require recompute: the pill is a sibling under the same row parent, so parent-relative geometry is motion-invariant. Per-frame work: zero.

**Q4 — colour rule.** Ship the approved mockup: 1px `#5a4670` full-alpha, transparent centre, ONE border around the whole `Tier N · G` unit (the mockup is one pill). Tier-colour border at ~70% alpha as `BorderColorMode = Neutral|TierColor` later. Two separate borders = visual noise; OPINION, but the mockup Andrew approved is the neutral single pill.

**Q5 — `T7` compact.** Pref: `TierWord = Spelled|Compact` (new enum `TierWordStyle`; Prefs entry name `TierWord`, frozen once shipped). Effect: `Tier 7`→`T7` drops ~4 glyphs (~30–40px at tooltip size) — narrower pill, strictly LESS wrap risk, zero geometry-code change because bounds come from textInfo. Compact also helps SignalRight's `<pos=68%>` headroom. *Test:* long hybrid line (`+48 Armor / +12% …`) at Compact — no wrap where Spelled wrapped.

**Q6 — failure policy.** Any throw, empty `lineInfo`, or unit spanning 2 lines → leave the composed text exactly as-is (plain coloured text, no border), latch off for the session, log ONCE:
`[TT] border: OFF ({reason}) — plain text path` vs the success proof `[TT] border: placed {n}/{rows} path=runtime-9slice scale={uiScale}`.

**FACT vs OPINION:** Facts — mark-over-glyphs, no border tag, `<u>/<s>` in text colour, sibling-order draw law, Sprite.Create 9-slice, dirty-window gate, log lines quoted. Opinions — candidate ranking, one-pill-over-two, neutral-over-tier border. Not proven — linkInfo in this TMP build, `⟦⟧` glyph presence, clone sprite-loss mechanism, high-tier texture re-slicing cleanly.

**Verdict: DONE_WITH_CONCERNS** — the path is proven except the `<link>` locator and the exact clone-death mechanism, both one in-game test each.

🟣➤🌙 **Kimi**

---
Conductor note: the `<link="ttu">` locator is the cleanest answer in the council to "which characters are the unit" and is adopted by the ticket with a characterInfo fallback if linkInfo is empty in this TMP build.
