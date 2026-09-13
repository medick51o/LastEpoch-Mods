# 🟣➤🌙 Kimi — second cross-vendor read of the six mockups, typography lens (2026-09-11)
sessionId bb4e5abf-d9d0-4729-98be-f84562c0e15a · 💸 CREDITS (Moonshot · kimi-k3-high) · 44842 in / 5584 out · builder 🔵 Codex (OpenAI) → cross-vendor holds

All six files read against the ticket. Review below.

## Per-file checklist

### D0-current.html
| Check | Result |
|---|---|
| (a) Spec content | ✓ Three items; exact affixes/tiers/grades (T5/A, T4/C, T7/C, Sealed T1/B; idol T5/S, T1/F, T1/F, weaver T3/B); caption names variant ("current composer" — no seat, correct for D0); `<pre>` string byte-exact; 7+5 swatches; renderer note present |
| (b) TMP law | ✓ `.mark::after` — absolute quad, `inset:0`, `z-index:2` over the glyphs, `border-radius:0`, no padding. True overdraw approximation, not a background |
| (c) Palette | ✓ All frozen hexes exact (lines 9–10, 20–28, 57). Non-palette hexes (#FF8BFF title, #9b8e6d, #C9A653, #ff5544, #0d0c10, #38304a…) are frame chrome from the reference frame CSS, not palette drift |
| (d) External assets | ✓ None |
| (e) Weaver bar | ✓ `.weaver::before` full-width `rgba(120,60,180,0.35)` at z-index 0, chips drawn over it |

**Verdict: MATCH**

### D1a-plain-signal.html
| Check | Result |
|---|---|
| (a) Spec content | ✓ Item A shown twice (neutral + labeled "name-colour option"); implicits are grade-letter-only; Sealed dim at 90%; `<pre>` string byte-exact; caption with seat (Astra); swatches; renderer note |
| (b) TMP law | n/a — no chips in this direction, none drawn |
| (c) Palette | ✓ No drift |
| (d) External assets | ✓ None |
| (e) Weaver bar | ✓ Purple bar behind the weaver row |

**Typography:** The `<pre>` string is pure `<color>` runs + plain text — TMP renders it exactly as drawn, including the dim `·` and double space. Nothing in the CSS exceeds TMP (90% Sealed = `<size=90%>`).

**Verdict: MATCH**

### D1b-dark-chip.html
| Check | Result |
|---|---|
| (a) Spec content | ✓ One chip per line, `T5 · A` form; Sealed dim text unplated before the chip; implicits = letter-only chip; `<pre>` byte-exact; caption (Gemini); swatches; renderer note |
| (b) TMP law | ✓ `.mark::after` `#00000066` quad, z-index 2 over glyphs, square, paddingless |
| (c) Palette | ✓ No drift (`#00000066` is the spec'd plate) |
| (d) External assets | ✓ None |
| (e) Weaver bar | ✓ Dark plate visibly over the purple bar (chip `::after` z2 inside weaver z1) — the exact judgment the deck needed |

**Typography:** `<mark=#00000066>` draws one quad over the whole `T5 · A` run including internal spaces — the mockup's `inset:0` inline-block quad matches that exactly. Only excess: CSS `letter-spacing:.5px` on the chip would need `<cspace>` in TMP, not free.

**Verdict: MATCH**

### D1c-tier-plate.html
| Check | Result |
|---|---|
| (a) Spec content | ✓ Tier plate at 25% (`#…40`) over white ink, grade unplated; S line gets single `S ★` in S colour; idol F lines show no letter; "grade-on-extremes-only option" caption on the idol; `<pre>` byte-exact; swatches; renderer note |
| (b) TMP law | ✓ Same overlay-quad pattern, `--plate:#A807FF40` etc. |
| (c) Palette | ✓ No drift |
| (d) External assets | ✓ None |
| (e) Weaver bar | ✓ Tier plate over the purple bar |

Notes: the F-no-letter rule is also applied to the belt's `+48 Armor` implicit (line 20) — a consistent extension of extremes-only, but the ticket only mandated it for the idol.

**Typography:** `<size=90%><mark=#A807FF40>…` renders as drawn — shrunk run, quad over white glyphs. Two TMP caveats: the `★` only renders if the game's font asset has the glyph (else fallback/sprite needed), and `letter-spacing` needs `<cspace>`.

**Verdict: MATCH_WITH_NOTES** — ★ glyph dependency + unrequested (though consistent) F-implicit extension.

### D2-native-pill.html
| Check | Result |
|---|---|
| (a) Spec content | ✓ Pill spec exact (radius 4px, border #6a5a80, bg #2a2238, padding 0 6px); 112px gutter column with aligned sentences; required caption "requires a donor sprite… not proven"; `<pre>` has both the intent line and the pill-TMP string; swatches; renderer note |
| (c) Palette | ✓ No drift (#6a5a80/#2a2238 are spec'd pill colors) |
| (d) External assets | ✓ None |
| (e) Grouping & weaver | ✓ `.shared-pill` spans both rows of the two-stat affix under ONE pill; weaver bar drawn in two halves (`weaver-signal`/`weaver-text`) covering the full row |

Minor: pill `min-height:26px` vs the ~31px line box — "height matching the line" is approximate, invisible at screenshot scale.

**Verdict: MATCH**

### D3-overlay.html
| Check | Result |
|---|---|
| (a) Spec content | ✓ Two-column grid (sentence left wraps, right-aligned `Tier 5   A` signal); grouped multi-stat shares one signal (`.group-signal` span 2); Alt detail region with dimmed sub-rows under the belt; name in rarity colour + glow (`--rarity` box-shadow/text-shadow); required caption verbatim; `<pre>` exact; swatches; renderer note |
| (c) Palette | ✓ No drift (rarity glows reuse frozen #A807FF/#FF44FF) |
| (d) External assets | ✓ None |
| (e) Two columns & weaver | ✓ Two columns everywhere; purple bar split across both cells of the weaver row |

**Verdict: MATCH**

## Summary

| File | Verdict |
|---|---|
| D0-current | MATCH |
| D1a-plain-signal | MATCH |
| D1b-dark-chip | MATCH |
| D1c-tier-plate | MATCH_WITH_NOTES (★ font dependency; F-implicit extension) |
| D2-native-pill | MATCH |
| D3-overlay | MATCH |

No TMP-law violations (all D0/D1 chips are overlay quads, never CSS backgrounds), zero palette drift in the frozen set, zero external assets, all `<pre>` strings byte-exact. Clean work by 🔵 Codex.

🟣➤🌙 Kimi
