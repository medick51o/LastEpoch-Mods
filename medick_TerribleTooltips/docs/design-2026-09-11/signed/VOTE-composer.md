# 🟣➤🎼 Composer — round-2 vote/refute (2026-09-11, unblinded, read SYNTHESIS-v2 + RENDER-VERDICT + viewed PNGs)
sessionId 81062378-c400-46c2-9a07-d552a8b60209 · ♾️ INCLUDED (Cursor · composer-2.5) · 18867 in / 3649 out

**Viewed pixels:** yes — `design-2026-09-11-D0-current-1x.png`, `D1a-plain-signal-1x.png`, and `D1c-tier-plate-1x.png` from the Downloads render path (not present under workspace `renders\`); also read the matching HTML for markup.

---

## (1) Votes — ruling queue

**Q1 — Signal unit: NO PLATE or PLATE?**
**NO PLATE** (`Tier 5 · A` coloured text). D1a pixels are the calmest immediate ship; my blind merged chip was the right *count* (one unit) but the wrong *primitive* — any `<mark>` quad still fights HDR/weaver overdraw until the alpha strip proves otherwise.

**Q2 — Affix sentence: TIER COLOUR or NEUTRAL off-white?**
**KEEP TIER COLOUR.** That was my blind default and still matches genesis + `AffixNameColorMode=TierColor`; neutral (D1a panel 1) reads cleaner in the render but trades away the WoW-retina tier cue on the stat itself.

**Q3 — If plate: DARK or TIER-coloured?**
**TIER-coloured at low alpha (~25–40%)**, grade unplated — D1c pixels beat D1b; dark `#00000066` adds a second dark layer the weaver row doesn't need. Only after the in-game alpha strip; not for 3.1.0 day-one.

**Q4 — "Tier 5" or compact "T5"?**
**Compact `T5`.** My blind mockup used `[5A]`; spelled-out costs horizontal space for no scan gain once the plate is gone.

**Q5 — Grade letter: GRADE colour or NEUTRAL?**
**GRADE COLOUR.** Number + letter stay colour-independent; neutralising the letter saves one hue per line but weakens the grade-at-a-glance read the palette was built for.

**Q6 — Decorate S with ★?**
**No.** Font-dependent, one extra signal on the noisiest grade; `S` in `#FF44FF` is enough. F always keeps its letter.

**Q7 — Filter rule: 200% or 120% bold gold?**
**120% bold gold.** 200% dominates the footer in every PNG; unanimous council fix, no dissent.

**Q8 — Next build: D1 only or D1 + D2 donor probe?**
**D1 only (3.1.0).** Ship the signal-unit change first; D2 needs a proven donor in the tooltip hierarchy, not a same-cycle gamble.

---

## (2) Refutations

1. **FACT — synthesis correction accepted:** My blind `#C8C8C8` / `#9A9A9A` implicits/sealed colours are wrong; frozen dim is `#8A8478` (TooltipRecolor.cs:47 per synthesis). I should not have invented greys outside the palette.

2. **FACT — synthesis correction accepted:** My "50% alpha is HDR-safe" claim was untested and runs against the overdraw law; D1c mockup/HTML uses `#40` (25%), and raising alpha is the wrong direction until measured in-engine.

3. **OPINION — minor grouping error in Split 1:** Synthesis lists Sonnet with "keep tier colour" alongside me and Cursor-Grok, but Sonnet's read was colour on the *numeric value only*, not the full sentence — that's a third variant, not the same camp.

4. **OPINION — render gate ranking:** Verdict ranks D1c > D1a > D1b for calm-but-clear; on the belt PNG D1a reads *calmer* to my eye than D1c's soft plates — close call, not wrong, but I'd call it a tie not a clear D1c win until in-game HDR.

---

## (3) Fair representation of my blind read?

**Mostly fair, one meaningful distortion.**

Captured correctly: one signal unit (not two chips), D1 ships first, tier-coloured affix sentence, grade in grade colour, 🔒 sealed glyph, D3 parked.

**Distortion:** Split 2 buckets me with Sonnet/D1c as "tier-coloured plate at 50%." My blind layout was a **single merged `[5A]` chip** (compact numeral + grade letter *inside one plate*), not D1c's "Tier 5" plate with grade riding outside. Closer to Kimi's merged-inside proposal than to Astra #2 / Sonnet #1. The conductor's correction on 50% alpha is fair; the shape conflation is not.

---

## (4) Final one-line ship recommendation for 3.1.0

**Ship D1a engine (`SignalStyle=PlainText`, compact `T5 · G`, grade in grade colour, tier-coloured sentence, filter at 120% gold) — no plate, no D2 in the same cycle; if Andrew rules neutral on Q2, it's one `<color>` tag, not a re-architecture.**

---

🟣➤🎼 Composer

---
Conductor note (not seat text): distortion accepted; v3 files Composer's blind shape as "one merged chip, grade inside" (Kimi's family). The seat viewed three of six PNGs and says so.
