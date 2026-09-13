# 🟠 Claude Sonnet — Playwright render gate verdict (2026-09-11)
agentId acd11a4a85bcea01d · 47 tool uses · 348 s. The conductor never looked at pixels; this text is the gate.

All six mockups rendered cleanly with no broken tooltips, no empty `<pre>` blocks, and the tier/grade swatch strip present at the bottom of every page. Local `python -m http.server` was used since `file://` navigation is blocked by Playwright's security policy; server was stopped after captures. Note on 2x: `scale:"device"` produced a byte-identical image to `scale:"css"` (confirmed via md5sum — DPR is 1 in this headless context), so every `-2x.png` is an honest same-scale duplicate of its `-1x.png`, not a true higher-resolution render.

**D0 — current (BadgeLeft + Badge)**
1. Legible, but each affix line carries a hard-edged tier badge AND a grade badge stacked next to colored text — readable but busy.
2. No clipping/wrapping in this HTML render, but the renderer note itself warns these are "paddingless plate quads" approximating a known TextMeshPro overdraw problem in-engine.
3. Color load is heaviest of the six: tier-chip bg+text, grade-chip bg+text, and affix text color = up to 4-5 distinct colors per line. Feels busy.
4. Weaver row on idol (Tier 3 · B) gets a full-row solid purple highlight bar — dominant, easy to spot, but the tiny B-grade chip is dwarfed by the bar.
5. Alt deep-view stays readable with a muted "MAX CRAFTABLE / Range" secondary line.

**D1a — plain signal (no plate, neutral sentence)**
1. Very legible — just "Tier 5 · A" as plain colored text ahead of the affix, no chip geometry at all.
2. Nothing to clip or wrap; no chip quads drawn.
3. Lightest color load of the badge-less variants (~3 colors/line: tier number, grade letter, affix text). Feels calm.
4. Same full-row purple highlight on the idol's Tier 3 signal — unchanged and still clearly the strongest cue on the panel.
5. Alt deep-view reads cleanly, consistent with the main tooltip.

**D1b — dark chip, WoW-style colour on ink**
1. Legible — a single translucent dark plate (#00000066) sits only behind the tier abbreviation; grade and affix are plain text.
2. No clipping/wrap observed.
3. Slightly busier than D1a because of the added dark box's contrast, though still only ~3 colors/line. Reads as moderately calm.
4. Same purple full-row idol highlight, consistent across variants.
5. Alt deep-view readable, consistent.

**D1c — tier-coloured plate at 25%, grade unplated**
1. Legible — tier number sits in a soft ~25%-opacity tinted plate; grade rides as plain colored text.
2. No clipping/wrap; the low-opacity plate doesn't crowd the glyph.
3. Restrained color load — the translucent plate adds structure without hard edges, feels calmer than D0 while more scannable than pure D1a text.
4. Idol's Tier 3 row keeps the same purple bar; the S-grade row also picks up a ★ star icon, one extra signal but not overwhelming.
5. Alt deep-view consistent and readable.

**D2 — native cloned pill (design intent, not proven)**
1. Legible — "Tier 5 | A" sits inside a rounded, bordered pill with real padding, the cleanest chip treatment of the six.
2. No clipping; pill has visible margin around the glyphs, corners rounded.
3. Colors are contained inside one pill shape (border + separator + grade letter) rather than scattered — reads organized, not busy.
4. Idol's Tier 3 pill sits inside the row highlight, slightly better integrated than D0/D1's square badges.
5. Alt deep-view consistent. Caveat carried from the mockup's own caption: this needs a donor sprite in the game's tooltip hierarchy and is explicitly "not proven" feasible in-engine — it's a design-intent illustration, not a validated build.

**D3 — custom overlay panel (design intent, not proven)**
1. Very legible — two-column layout with tier/grade pushed to the right edge, affix text has the whole left side to itself; the longest line ("30% increased Cooldown Recovery Speed") doesn't crowd anything.
2. No clipping or wrapping anywhere, even on two-line entries.
3. Lowest color load of all six — text stays mostly single-color per column, with a purple rarity-glow border framing each panel as ambient color rather than competing ink. Feels the calmest and roomiest.
4. Idol's Tier 3 weaver row keeps the purple highlight, now confined more precisely to a right-column chip — still the clearest "our signal" cue.
5. Alt deep-view adds a vertical divider and "ALT DETAIL" label, very readable. Caveat: mockup's own caption states this needs its own rendered canvas over the game's native tooltip, "weeks of work," and must prove parity before it could hide the native tooltip — also explicitly design intent, not a proven build.

**Nothing broken** in any of the six: all three panels (belt/idol/belt-alt) rendered per page, the debug `<pre>` rich-text block under each panel was populated (not empty), and the Tiers/Grades swatch strip at the bottom was present and intact on every file.

**Calm-but-clear ranking (D1a/D1b/D1c only):** D1c > D1a > D1b — D1c's soft 25%-opacity tier plate gives the best scan-ability without adding hard edges; D1a is calmest overall but has slightly less structure to anchor the eye; D1b's dark chip box is the highest-contrast of the three and reads as the busiest despite being otherwise sparse.

**PNG paths written** (all in `C:\Users\andre\Downloads\LastEpoch-Mods\medick_TerribleTooltips\docs\design-2026-09-11\renders\`):
- design-2026-09-11-D0-current-1x.png / -2x.png (204,645 bytes each)
- design-2026-09-11-D1a-plain-signal-1x.png / -2x.png (184,681 bytes each)
- design-2026-09-11-D1b-dark-chip-1x.png / -2x.png (192,628 bytes each)
- design-2026-09-11-D1c-tier-plate-1x.png / -2x.png (199,663 bytes each)
- design-2026-09-11-D2-native-pill-1x.png / -2x.png (197,680 bytes each)
- design-2026-09-11-D3-overlay-1x.png / -2x.png (247,682 bytes each)

---
Conductor note: the "2x" files are same-scale duplicates (headless DPR = 1); a true 2x render is an open item. The render seat describes the D1b dark plate as sitting "only behind the tier abbreviation"; the file's `<pre>` string wraps `T5 · A` in one `<mark>` per the ticket, and Gemini's markup read confirms the single quad — see the deck for the resolution.
