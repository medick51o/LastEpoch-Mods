# 🟣➤🌙 Kimi — round-2 vote/refute (2026-09-11, unblinded, read SYNTHESIS-v2 + RENDER-VERDICT + viewed all six PNGs)
sessionId 5f7233ec-9757-48e3-aaed-7b23ea1ec1ef · 💸 CREDITS (Moonshot · kimi-k3-high) · 29434 in / 5019 out

**VIEWED PIXELS** — all six PNGs in `renders\` (D0, D1a, D1b, D1c, D2, D3), not just the HTML.

## 1. Votes on the ruling queue

1. **NO PLATE.** Coloured `Tier 5 · A` text. My blind merged-plate dies on its own sword: the overdraw law (TooltipRecolor.cs:823–826) means every render I just looked at *flatters* the plate — CSS paints the quad behind padded glyphs; TMP paints it over them. D1a is the only shape with zero attacks and it's already `SignalStyle.PlainText` in the shipped composer (TooltipRecolor.cs:843, 856).
2. **TIER COLOUR on the sentence.** My blind "neutral" was explicitly conditioned on Badge style ("keep TierColor names for PlainText"); with Q1 = no plate, the sentence colour is the line's main tier-hue carrier and the WoW retina read survives. One hue per line is preserved: signal and sentence share the tier hue, grade letter is the only second hue.
3. **Moot (no plate); if the alpha strip resurrects one: TIER-coloured plate, white ink, ≤40%.** D1b's dark quad stacks a second dark layer on the weaver bar and muddies T1/F ink — visible even in the flattering CSS render.
4. **"Tier 5" spelled out; "T5" stays the wrap-fallback reserve.** Audience order is homies first (SPEC.md:170); words beat codes for the half-second triage read, and the D1b render confirms "T5 · A" reads terser but colder.
5. **Grade letter in GRADE colour.** With no plate there's no overdraw tax on coloured ink; the letter position disambiguates the shared hue (C=T4, B=T5…), and number+letter remain the colour-independent signal.
6. **No ★.** Unverified glyph in the game's TMP font — tofu-box risk for zero information; S already owns mythic pink `#FF44FF`, the loudest ink on the palette.
7. **120% bold gold.** `<size=200%>` (FilterRuleTooltip.cs:117) doubles the line height of the `requires` line and shouts over the item name; 120% + `<b>` keeps prominence inside the type scale.
8. **D1 only.** 3.1.0 is a default-flip plus the Rule# size fix — ship it clean; couple nothing proven to the unproven D2 donor probe.

## 2. Refutations

- **The conductor's correction of me is correct; I accept it.** Coloured grade ink inside a tier plate (my blind proposal #1) is unproven — only warm-white `#FFF6E6` is verified to survive a 40% plate in-game (TooltipRecolor.cs:823–828). I flagged the overdraw law myself and then violated it in my own string; that proposal should have carried the same "in-game probe first" tag I gave `<nobr>`. Withdrawn as a ship candidate; it survives only as an alpha-strip test case.
- **The render gate's `D1c > D1a > D1b` ranking must not travel into the deck as evidence about TMP.** The verdict itself warns these are "paddingless plate quads approximating a known overdraw problem" (RENDER-VERDICT.md, D0 note 2) — i.e. the CSS plate sits *behind* the glyphs, the inverse of the engine. D1c's win is a win for CSS plates. In-engine, the 25% quad tints the glyphs themselves. The ranking is art direction only, consistent with the council's own agreement #9.
- **The gate's D1b prose is wrong; the pixels and markup agree.** The verdict describes the dark plate as sitting "only behind the tier abbreviation" (D1b note 1), but the conductor's note says the `<pre>` wraps `T5 · A` in one `<mark>` — and the PNG I viewed shows the dark box spanning the whole `T5 · A` unit, grade letter included. Trust the markup and the pixels, not the description.
- **Self-correction:** my round-1 citation of the dim const as TooltipRecolor.cs:48 was off by one — it's line 47. The synthesis's correction of Gemini stands, and my :48 does not.
- Nothing else in the synthesis fails a file check. Astra's and Cursor-Grok's FACT sets check out against the sources I read.

## 3. Did the synthesis represent my blind read fairly?

Mostly yes — merged plate at 40% with grade inside, neutral sentence, trailing `· sealed`, Rule# at 120%, the alpha strip, `BadgeTextColor` dead code, two-stat grouping cue, "Tier 5" with compact reserve all landed. **One distortion:** Split 1 files me under "Neutral `#E8E2D2`" without my condition. My blind read tied neutral names to the Badge/plated world and explicitly kept TierColor names for PlainText. Since the council converged on no-plate, my actual vote lands on tier-coloured — the neutral bloc in that table is overstated by one seat, and my Q2 vote above is not a reversal, it's my original conditional resolving.

## 4. Ship recommendation for 3.1.0

**Ship D1 as a default-flip to `SignalStyle.PlainText` — one coloured-text signal unit per affix line, sentence keeps tier colour (Andrew's genesis ruling stands), Rule# down to 120% bold gold, and run the SDR/HDR alpha strip before any plate ever returns.** The exact normal affix line — which the current composer already emits in PlainText + BadgeLeft (TooltipRecolor.cs:843, 856, 862, 878, 898), so 3.1.0 needs no composer change for this line:

```
<color=#A807FF>Tier 5</color><color=#8a8478>·</color><color=#FA9E3D>A</color>  <color=#A807FF>58% increased Lightning Damage</color>
```

🟣➤🌙 Kimi

---
Conductor note (not seat text): distortion accepted; v3 moves Kimi to "tier colour" on Q2. Kimi's closing FACT is the most useful line of round two: the recommended line is already what `PlainText + BadgeLeft + TierColor` emits, so the core of 3.1.0 is a default flip (`SignalStyle` Badge → PlainText), not a composer rewrite. One correction to the seat: the CSS mockups draw the plate ABOVE the glyphs (Gemini and Kimi's own markup review confirmed `.mark::after` at z-index 2); the seat's argument that a browser cannot prove the engine's blending still holds.
