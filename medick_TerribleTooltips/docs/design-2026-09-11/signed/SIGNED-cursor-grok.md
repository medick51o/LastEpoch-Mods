# 🟣➤⚫ Cursor-Grok — LATE blind seat, adversarial lens (2026-09-11, read-only)
sessionId 0d9350eb-a8a8-48cb-ba51-5a8ffec8b383 · ♾️ INCLUDED (xAI · cursor-grok-4.6-high) · 67523 in / 12138 out
Brief given: BRIEF.md facts + the council's likely D1 recommendation stated as the thing to refute (three unit shapes a/b/c). Blind to every design-2026-09-11-* file and to the other seats' text.

DONE_WITH_CONCERNS

**Manifest:** `BRIEF.md`; `docs/SPEC.md`; `docs/ARCHAEOLOGY.md`; `src/TooltipRecolor.cs`; `src/Colors.cs`; `src/Prefs.cs`; `src/SettingsUi.cs`; `src/GroundLabels.cs`; `src/AffixInjector.cs`; `src/NativeSettings.cs`; `mockups/tooltip-belt-v3-badges.html`; `mockups/tooltip-flow-v3.html`; `context/inventory/NativeClone.cs`. No `design-2026-09-11-*` files opened.

---

### Current look (honest)

Defaults are BadgeLeft + Badge + TierColor names (`Prefs.cs` 89–94). One TMP line per affix composes as two tight `<mark>` chips at alpha `66` (~40%) with warm-white ink `#FFF6E6`, then the affix sentence in the **tier** colour:

`[Tier 5][A]  58% increased Lightning Damage`

That is not a CSS badge. In this game's TMP the mark quad paints **over** its own glyphs (`TooltipRecolor.cs` 822–828). Dark ink died at ~80% alpha in-game (2026-06-11); the shipped recipe is translucent plate + bright ink that survives the overlay. `Colors.BadgeTextColor` (luminance flip, `Colors.cs` 61–76) is unused by the composer.

Sealed adds a third dim chip. Untiered (unique/implicit) is grade-chip only, name borrows grade colour. Hybrid/weaver continuation lines either get their own chips (3.0.2, BRIEF) or inherit tier tint with no chip (`TooltipRecolor.cs` 700–711). Unbracketed multi-stat blocks synthesize a **tier** chip and honestly omit grade (`TooltipRecolor.cs` 721–722, 776–782). SignalRight uses `<pos=68%>` and bails to Trailing at 30 characters (`TooltipRecolor.cs` 886–893). Ground labels stay KG brackets, no plates: `PLATED BELT [5A 3C 7S]` (`GroundLabels.cs` 1–3). Filter rule stays gold.

The belt mockup is a **browser** 40% plate on a dark card. It cannot show HDR clip, weaver-bar multiply, or TMP overdraw. Last night's captures already contradict it: tier chip → white block under HDR; four chip-pairs on a 4-affix idol; weaver purple sitting **behind** the chips (BRIEF).

`SignalStyle.PlainText` and `AffixNameColorMode.GameDefault` already exist as knobs. Shipping "one unit + beige sentence" as **the** look is a default flip, not a new engine.

---

### D1 — rich-text polish (the thing I am here to kill)

**Look (council shape):** one signal unit, then a **neutral off-white** sentence.

- (a) no plate, coloured text: `Tier 5 · A    58% increased Lightning Damage`
- (b) one dark `<mark=#00000066>` over coloured ink: `[T5 · A]    58% increased Lightning Damage`
- (c) one 25%-alpha **tier-coloured** plate, white ink: `[Tier 5] A    58% increased Lightning Damage`

**Cost:** a point-release. Composer already emits PlainText vs Badge; GameDefault names already exist. Risk is **aesthetic lock-in**, not engineering hours. You can ship the wrong retina in a weekend and spend a season undoing it.

**Constraint:** still only TMP tags inside EHG's lines. No widgets (`BRIEF` hard facts). Mark-over-glyph is law.

**Cannot:** real padding/rounding; a plate under glyphs; dodge HDR on coloured quads; a true column next to wrapped names; weaver-bar z-order; per-compare-tooltip width.

#### Attacks (refute D1-as-stated)

**1. `<mark>` overdraw at every alpha — FACT** (`TooltipRecolor.cs` 822–828). The quad is a veil, not a background. Raising alpha drowns ink; lowering alpha dissolves the plate. 40% is already the negotiated ceiling (`tooltip-belt-v3-badges.html` 93–98: 70% drowns letters; 40% may be too faint). (c) at 25% is "plate theatre": a stain, not a chip. (b) puts **coloured** ink under **black** 40% — T3 `#16FF0E` and T5 `#A807FF` both mud. (a) never draws a quad.
**Test:** same T1 / T3 / T5 / T7 line at mark alpha `00/40/55/66/CC`, HDR off, 100% UI scale, screenshot the glyph vs the quad.

**2. HDR white-block — FACT** (BRIEF: last-night capture washed the tier chip to white). A coloured mark is a bright quad. Unity HDR / screenshot tonemap will clip it; the mockup's `rgba(...,.40)` on `#181222` never will. (c) inherits this 1:1 (same primitive, weaker). (b)'s black plate is the only mark that might *not* blow — ink still sits under it. (a) has no quad to clip; coloured **text** HDR-behaves like every other TMP in the game.
**Test:** in-game HDR on vs off vs NVIDIA/Steam capture of T3 green and T7 pink, plate vs PlainText.

**3. Weaver purple bar — FACT** (BRIEF: bar sits behind chips; `TooltipRecolor.cs` 700–711 weaver/hybrid continuations). Additive purple + 40% T5 purple plate = camouflage; + 25% T3 green (c) = brown. Two chips already fight the bar; **one combined unit does not remove the bar**. (b) is the only D1 shape that punches a dark hole so the letters can exist. (a) is coloured words on purple: T5 text dies, T3 text clashes, digit+letter still parse.
**Test:** Stout Weaver Idol, weaver line vs non-weaver line, all three shapes, HDR off, no Alt.

**4. Wrap at 1080p + UI scale — FACT** (`TooltipRecolor.cs` 827–828, 886–893; `SettingsUi.cs` 66; SPEC 189–191: char-count is not rendered width; belt mockup holds `T7` in reserve). Combining chips saves a few characters; spelling `Tier 5` in (a)/(c) **spends them again**. `<pos=68%>` is a full-tooltip guess — comparison halves the panel (SPEC 192–196). When the name wraps, line 2 is a beige orphan with no signal; the eye reads a new affix. Neutral sentence does not shorten "10% reduced Bonus Damage Taken from Critical Strikes".
**Test:** 1080p at 100/125/150% UI scale, that suffix, stash-compare (half width), BadgeLeft and SignalRight.

**5. Pooled TMP reuse — FACT** (`TooltipRecolor.cs` 22–24, 38–40, 278–284, 428–429). Re-render is marker-gated (4× ZWSP). D1 does not change the pool. Nested `<mark><color>` is more for EHG to strip on rewrite; a half-stripped line with Marker still set is sticky. All three shapes share this. Not a reason to pick (b) over (a).
**Test:** hover A → B instantly, stash compare, Alt hammer, 4-affix idol then unique, master toggle.

**6. Comparison side-by-side — FACT** (SPEC 192–196: comparison forces another UpdateLayout; white-wash was a fleet catch). Two half-width essays of beige + a chip is "cleaner" and **less WoW**. SignalRight `pos=68%` on a half tooltip overdraws (`TooltipRecolor.cs` 887–890). D1 cannot give a two-column compare. Noise halves if you kill plates; it does not if you keep (b)/(c) plus sealed chips.
**Test:** two 4-affix exalted compared at 1080p, all layouts.

**7. Controller — OPINION** (no tooltip-gamepad code; Aaron's "controllers" are Divine Era map, `AaronsHouse.cs`). D1 adds no widgets, so it is the only direction that cannot steal focus. Not a reason to ship beige names.
**Test:** DualSense, ground tooltip + inventory tooltip, no keyboard.

**8. LeHud / Fallen — FACT.** Never patch `OpenItemTooltip` (ARCHAEOLOGY 137–139). Fallen owns unique/set/legendary **ground** labels (`GroundLabels.cs` 103–105). Ground writes use SetText faceColor save/restore (`GroundLabels.cs` 193–201). **Tooltip** compose assigns `tmp.text` raw (`TooltipRecolor.cs` 538) — no faceColor truce on the path that will churn more mark tags. More mesh rebuilds, more chance LeHud rarity tint dies. (a) fewer tags. ABI name stays frozen regardless.
**Test:** LeHud + Fallen + TT: unique on ground (Fallen), rare on ground (TT), inventory tooltip with LeHud colours, HDR off.

**9. Colour-blind — FACT.** Grade hues **are** the tier hues: C=T4 `#77ACFF`, B=T5 `#A807FF`, A=T6 `#FA9E3D`, S=T7 `#FF44FF` (`Colors.cs` 27–59). One unit `T5 · B` is **one purple blob**. Two chips at least spatially split identical hues. Neutral sentence **removes the redundant name colour** that currently doubles the tier channel (SPEC 10–12; `SettingsUi.cs` 80). Deuteranope is left with a letter shape and a digit. That is the opposite of "zero learning curve".
**Test:** Windows deuteranopia filter, T5-B vs T4-C vs T1-F vs T7-S, with vs without coloured names.

**10. T1 / T2 greys — FACT.** `#DADADA` vs `#E1E1E1` (`Colors.cs` 30–31), 7/255 apart. No plate, 25% plate, or 40% plate will separate them on a dark tooltip. The **digit** is the signal. (c)'s white ink on 25% grey is chrome, not a chip. (b)'s grey ink on black is quieter still. (a) at least lets you read `Tier 1` vs `Tier 2` in the same colour language as T3–T7.
**Test:** adjacent T1 and T2 affixes, squint, 1080p, HDR on/off.

**11. Neutral sentence kills the WoW retina — FACT, and this is the hill.** SPEC 9–12: a stranger's WoW-trained eyes just *know*. ARCHAEOLOGY 109–111: **THE** direction — affix **NAME** coloured by tier; grade letter in grade colour. Default `AffixNameColorMode.TierColor` (`Prefs.cs` 93–94). Andrew: keep the WoW palette, "clear but colorful," not overwhelmed (`BRIEF`). Overwhelmed is **two plates + coloured name + weaver bar**, not colour on the words. `tooltip-flow-v3.html` Option C already has coloured names and a trailing `Tier N · grade` with **no plates**. Beige sentences are EHG's broken vision with a luggage tag. That is not this mod.
**Test:** 500ms squint, 4-affix exalted, plates-off + coloured names vs one-chip + beige. Whichever IDs the T7 first without reading wins. The mockup already told you to squint (`tooltip-flow-v3.html` 129–130).

**(a)/(b)/(c) survival:** **(a) survives**. It is the only shape that does not fight TMP overdraw, HDR quads, or 25%-alpha cosplay. It is already `SignalStyle.PlainText` (`Prefs.cs` 41–43, `TooltipRecolor.cs` 843). It loses weaver contrast vs (b) and loses "label look." That is acceptable; plates were never labels.

**(b)** is the least-bad **if** you insist on a plate: dark quad is the only HDR-plausible mark and the only weaver hole-punch. It still overdraws its own ink and still merges T5+B into one hue.

**(c) dies first.** It is tonight's white-block with a weaker alpha and white ink that T1/T2 cannot support. Do not ship it.

**D1 with beige names should not ship even as (a).** Keep (a)'s engine; keep **TierColor names**. That is not the council's D1.

---

### D2 — native-clone chips

**Look:** `Tier 5 | A` on a real cloned Image/pill to the left of EHG's TMP, sentence still EHG's (beige or tinted by us). One widget per affix row, not a mark.

**Cost:** medium, and the medium is **parenting**, not cloning. `NativeClone.cs` clones a Sort **button** into inventory panels (9-slice, native font, kill localization, destroy-in-catch). `NativeSettings.cs` clones settings rows. Neither is an affix-row badge. `UpdatePrefixAndSuffixesText` is unpatchable (ARCHAEOLOGY 141–144). You must find each affix TMP after `UpdateLayout`, inject a sibling, and survive the lean-law **re**-`UpdateLayout` (`TooltipRecolor.cs` 25–28, 544–547) without duplicating clones. Comparison = two trees. Pooling = destroy-or-rebind every hover.

**Hits:** Il2Cpp, no generic List in patches, tooltip prefab churn every league.

**Cannot:** guaranteed donor sprite that means "tier"; controller nav if the clone is a Button; LeHud if you fight the same TMP.

**Dodges D1 cannot:** mark-over-glyph (attack 1), HDR white quads if the sprite isn't a blown additive colour (2), z-order vs weaver bar if the Image is opaque and in front (3). Does **not** dodge wrap (4), pooling (5), compare width (6), colour-blind palette (9), T1/T2 (10), or beige-vs-WoW (11).

---

### D3 — custom overlay

**Look:** own canvas; true columns; weaver bar as a designed strip; plates as Images **under** glyphs; wrap with hanging indent so line 2 still belongs to the chip.

**Cost:** season-grade. Input, UI scale, controller cursor, every game patch, LeHud/Fallen covering or being covered (`BRIEF` D3). Forbidden `OpenItemTooltip` patch still forbidden — you hide or overlay, you do not trampoline (ARCHAEOLOGY 137–139).

**Cannot:** be "familiar" on day one; be a point release; promise other mods' tooltip hooks still fire.

**Dodges D1 cannot:** 1, 2, 3, 4 (real layout), 5 (you own the texts), 6 (you size both compares), maybe 7 if you do controller. Palette attacks 9–11 only die if you **add non-hue language** (icons, weight, position) — D3 *can*, D1 **cannot**.

---

### D4 (proposed) — kill the plate, keep the retina

**Look:** `Tier 5·A    58% increased Lightning Damage` with **no `<mark>`**, `Tier 5` in T5, `A` in grade colour, **name still T5**. Compact `T5·A` if 1080p wrap fails. Weaver line: no extra quad to mud the purple; the words are the signal. This is `tooltip-flow-v3.html` Option B/C and SPEC's original line (`SPEC.md` 142–144), minus the chips v3 bolted on.

**Cost:** default `Style = PlainText`; leave name colour alone. Point release. Risk: Andrew misses the "label." He can turn Badge back on — the knob exists.

**Cannot:** real pills, weaver hole-punch, HDR-proof plates (it simply has none).

---

### Ranked recommendation

1. **D4** — PlainText default, **TierColor names stay**. Cheapest fix for overwhelm (two veils gone) without betraying SPEC 9–12.
2. **D2** — only if a **tooltip-native** donor (not the Sort button) is found in-game this weekend; otherwise it is D3 in a stolen 9-slice.
3. **D3** — if "overhaul the UI" is the real brief. That is what Andrew said. Do not pretend D1 is that overhaul.
4. **D1 (a)** — acceptable **engine** if and only if the sentence stays coloured. As the council's beige-sentence point release: **reject**.
5. **D1 (b)** — research spike for weaver+HDR, not a ship shape.
6. **D1 (c)** — do not.

Reason: the mockup cannot show the law at `TooltipRecolor.cs` 822–828, the HDR white block, or the weaver bar. Combining two broken plates into one broken plate, then bleaching the name, spends the brand on a problem the compositor created.

---

### Concrete proposals

1. **FACT** — Default `SignalStyle` to PlainText; do not change `AffixNameColor`. Test: Plated Belt + Weaver Idol, HDR on, 1080p 125%. Pass = no white blocks, T7 still reads from the **words**.
2. **FACT** — Compact `T5` vs `Tier 5` is already reserved in the belt mockup (lines 101–104). Test wrap before inventing plates. Mockup + in-game 150% scale.
3. **OPINION** — If plates stay, one **dark** mark (b) at a **measured** alpha, never a coloured mark (c). Test: HDR capture of T3 and T7, weaver line. Pass = not a white rectangle, letters still parse on purple.
4. **FACT** — T1/T2 will never be a colour story (`Colors.cs` 30–31). Do not spend design on greyer greys. Test: squint; if the digit is the tell, stop.
5. **FACT** — `GameDefault` beige is already a sweat knob (`Prefs.cs` 93–94, `SettingsUi.cs` 78–83). Do not make it the homie default. Test: fresh install, 4-affix exalted, 500ms. If they ask "where did the purple go," you shipped the wrong D1.
6. **OPINION** — Weaver coexistence is a D2/D3 job (opaque widget / own strip), not a 25% stain. Test: if (a) is readable enough on Stout Weaver, do not clone UI for v3.1.

---

### Either/or (Andrew)

1. Coloured affix **names** (WoW retina, today's default) **or** beige sentences with colour only in the chip?
2. Ship **no plates** (PlainText / D4) this point release **or** keep `<mark>` and accept HDR white blocks until D2?
3. Is "overwhelmed" **two chips + weaver bar** **or** the palette on the words — if the latter, stop, that is the product.
4. Point-release inside EHG's TMP **or** a real overhaul (D2 donor hunt / D3 overlay) as the Nexus look?

🟣➤⚫ Cursor-Grok

---
Conductor note (not seat text): this seat was told the likely recommendation as its refutation target (the adversarial lens requires it); it did not see any seat's text or the mockups. Its "FACT" on the tmp.text raw assignment (TooltipRecolor.cs:538) and on the NumberOnly injection path are consistent with the source. Its "#11 — the hill" is the strongest single argument in the council against the neutral sentence; the deck names it.
