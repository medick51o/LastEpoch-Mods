# 🟣➤🎼 Composer — LATE blind seat, fresh-eyes lens (2026-09-11, read-only)
sessionId 81062378-c400-46c2-9a07-d552a8b60209 · ♾️ INCLUDED (Cursor · composer-2.5) · 11174 in / 3889 out
Brief given: BRIEF.md ONLY (facts + verbatim vision). Blind to every other file and seat.

# DONE

**Manifest:** `C:\Sync\Projects\tt-design-2026-09-11\BRIEF.md` only.

---

## Ship-tomorrow layout: **Unified Badge (D1 polish)**

One merged chip per affix (`5A` not `[5][A]`), tier-coloured plate at ~50% alpha (HDR-safe), grade letter in grade colour inside the chip, affix body in full-brightness tier colour. Implicits dim, no chips. Sealed = dim prefix, chip unchanged. Filter rule in gold. Familiar WoW palette, half the colour plates per line.

```
┌──────────────────────────────────────────┐
│  Stormcaller Idol                 Lvl 78 │
├──────────────────────────────────────────┤
│  +12% increased Cast Speed               │  ← implicit (dim #C8C8C8)
│  +45 to Lightning Resistance             │  ← implicit (dim #C8C8C8)
├──────────────────────────────────────────┤
│ [5A] 58% increased Lightning Damage      │  ← T5/A
│ [4C] 3% of Lightning Damage leeched      │  ← T4/C
│ [7C] 🔒 +2 to Lightning Spell Skills     │  ← T7/C, sealed
│ [1B] -3 to All Attributes                │  ← T1/B
├──────────────────────────────────────────┤
│  Filter Rule: 3                          │  ← gold
└──────────────────────────────────────────┘

Chip anatomy:  [5A] = one <mark> plate (tier colour @ 50% α)
                     "5" in #FFF6E6 bold, "A" in grade colour on same plate
```

---

## Colour-load count (4 affix lines only)

| Hex | Role in layout |
|-----|----------------|
| `#A807FF` | T5 text + plate (line 1); B grade letter (line 4) |
| `#FA9E3D` | A grade letter (line 1) |
| `#77ACFF` | T4 text + plate (line 2); C grade letter (lines 2–3) |
| `#FF44FF` | T7 text + plate (line 3) |
| `#DADADA` | T1 text + plate (line 4) |
| `#FFF6E6` | Chip numeral ink (all four lines) |
| `#9A9A9A` | Sealed prefix only (line 3) |

**Proposed layout: 7 distinct hex colours** (6 if sealed prefix is omitted).

**Current v3.0.2 (two chips + tier-coloured sentence):**

| Hex | Role |
|-----|------|
| `#A807FF` | T5 chip + text (line 1); B grade chip (line 4) |
| `#FA9E3D` | A grade chip (line 1) |
| `#77ACFF` | T4 chip + text (line 2); C grade chips (lines 2–3) |
| `#FF44FF` | T7 chip + text (line 3) |
| `#DADADA` | T1 chip + text (line 4) |
| `#FFF6E6` | Ink on all 8 chips |

**Current layout: 6 distinct hex colours** — same palette entries, but **8 translucent plates** vs **4**, so it reads busier even when the hex count is similar. *(OPINION: plate count matters as much as hex count for "overwhelmed.")*

---

## Exact TMP rich-text string — first affix line (T5/A)

```
<mark=#A807FF80><color=#FFF6E6><b>5</b></color><color=#FA9E3D>A</color></mark>  <color=#A807FF>58% increased Lightning Damage</color>
```

*(OPINION: `#A807FF80` ≈ 50% alpha to reduce HDR wash-out vs v3.0.2's 40% plates. FACT: `<mark>` draws a coloured quad over glyphs; this is in-game law per brief.)*

---

## Critique of D1 / D2 / D3 (three lines each)

### D1 — Rich-text polish
1. **FACT:** Ships inside existing TMP rewrite; no new widgets, lowest patch risk, point-release viable.
2. **OPINION:** Unified single chip directly answers "clear but colorful, not overwhelmed" — biggest win per hour.
3. **FACT:** Cannot escape `<mark>` quad limits, weaver purple bar behind chips, or multi-stat lines spawning duplicate chips without layout-engine changes.

### D2 — Native-clone chips
1. **FACT:** Terrible Inventory's NativeClone pattern proves real UI elements can be cloned into game panels — tiers could render as native pills.
2. **OPINION:** Visually strongest "different but familiar" path, but medium cost and every tooltip line becomes a layout problem (positioning, cleanup, patch drift).
3. **FACT:** Still bound to EHG's tooltip shell; cannot fix unpatchable `UpdatePrefixAndSuffixesText` or add rows the game doesn't expose.

### D3 — Custom overlay
1. **FACT:** Full layout freedom — sealed state, filter rule, implicits, affixes in one designed panel.
2. **OPINION:** Overkill for "cleaner wrapper"; highest risk of feeling foreign and breaking on 1.4.x patches / other mods / controller mode.
3. **FACT:** Violates tonight's scope ("no code changes"); input, scaling, and LeHud truce make this a Tier C bet, not a tomorrow ship.

---

## Either/or ruling questions (3)

1. **Chip count:** One merged `[5A]` chip per line **or** keep two separate tier + grade chips but polish spacing/alpha?
2. **Affix body colour:** Full tier colour on the sentence **or** neutral `#FFF6E6` body with colour only in the chip (less saturation)?
3. **Tomorrow's ship target:** D1 rich-text polish in a point release **or** pause for D2 native-clone prototype before any user-facing change?

---

🟣➤🎼 Composer

---
Conductor note (not seat text): Composer's 50% alpha "HDR-safe" claim runs the opposite way from the in-game law (higher alpha drowns more ink; the brief says the 40% plate already washed out) — it is an OPINION with no test behind it. Its `#C8C8C8` implicit dim and `#9A9A9A` sealed dim are colours outside the frozen set (the mod's dim is `#8A8478`); the deck treats them as "a dim", not as new palette entries. The 🔒 glyph depends on the game font.
