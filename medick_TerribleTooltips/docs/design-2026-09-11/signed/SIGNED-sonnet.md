# 🟠 Claude Sonnet — player-familiarity seat (2026-09-11, blind, read-only subagent)
agentId a902fe5164560ac7e · model sonnet · same lineage as the conductor (Anthropic) — labelled, discounted as a vote · 85,328 tokens, 6 tool uses, 120 s

Status: DONE

# 🟠 Claude Sonnet — PLAYER FAMILIARITY seat

## 1. Manifest
Files actually read:
- `C:\Sync\Projects\tt-design-2026-09-11\BRIEF.md`
- `C:\Sync\Projects\tt-design-2026-09-11\src\Colors.cs`
- `C:\Sync\Projects\tt-design-2026-09-11\mockups\tooltip-belt-v3-badges.html`
- `C:\Sync\Projects\tt-design-2026-09-11\mockups\tooltip-flow-v3.html`
- `C:\Sync\Projects\tt-design-2026-09-11\docs\SPEC.md` (lines 130–214, "v3 — THE CLEAN LINE" through the lean-pass notes)

## 2. Current look (FACT, from the files)
One line per affix: two translucent square-cornered chips (tier-colour plate, then grade-colour plate, both 40% alpha, warm-white `#FFF6E6` ink) immediately followed by the affix text, itself coloured by tier (`Colors.TierColor`). Hybrid lines currently render **bare, no chips at all** (a known gap, not a design choice). Sealed lines get a third dim "Sealed" chip ahead of tier+grade. `SPEC.md` confirms the intended shape is `BadgeLeft` (Andrew's pick — "signal leads, text follows") with `SignalRight`/`Trailing` as alternates, name colour and grade-letter visibility both toggleable, ranges hidden by default behind Hold-Alt. `Colors.cs` header states the palette is explicitly modeled on "world of warcraft item colorization." The BRIEF also states the game's own **purple weaver-affix highlight bar** already sits behind these chips on weaver lines — i.e., LE's native UI already uses a translucent colour bar behind affix text as a convention, independent of this mod.

## 3. What reads instantly elsewhere (OPINION, from my own knowledge — flagged per-item, UNSURE where noted)

- **World of Warcraft (own knowledge):** quality colour (gray/white/green/blue/purple/orange) lives on the **item name and border glow**, not on individual stat lines — WoW does not tier-colour each affix. The palette Andrew wants is genuinely WoW's, but WoW's *application pattern* (one colour signal at the top, plain white stat lines below) is different from what this mod does (colour applied per-line). That's a real gap between "the palette he wants" and "the pattern that palette comes from" — worth naming explicitly to the council.
- **Diablo 4 (own knowledge, UNSURE on exact current-season UI):** the standout convention is the **greater-affix star/glyph** — a single small icon marking an exceptional roll, appearing only on the affixes that earned it, not on every line. D4 doesn't natively show a letter-grade or percentile per affix; that's closer to third-party sites/overlays. Tempered affixes show a bracketed range next to the value. The "only decorate the exceptional case" instinct is the transferable idea, not letter grades themselves.
- **Path of Exile 1/2 + community overlays, specifically Awakened PoE Trade and PoB item displays (own knowledge):** these overlay a **tier tag directly on the affix line** (e.g., "T1") plus a **translucent colour bar behind the line** scaled to roll quality — this is functionally the closest existing precedent to TT's chip-plate idea, and it's a "sweat" tool, meaning the sweats/WoW-D4-vets segment of Andrew's audience is plausibly already trained on exactly this pattern. Vanilla PoE's own in-client tooltip does NOT show tier numbers at all (overlay-only knowledge).
- **Last Epoch's own vanilla tooltip (FACT confirmed by BRIEF, OPINION on interpretation):** the "Tier: 5 (max craftable)" text line and the purple weaver bar are LE's native conventions — and TODAY's mockup literally shows this as "16 lines of essay." Because LE already tints affix lines with a bar in at least one case (weaver), a translucent plate behind a line is not a foreign import — it's already load-bearing in this engine. That's a strong "different but familiar" argument specifically for D1.

**Bottom line on "different but familiar" for one affix line:** the WoW palette signals *item quality at the top*; D4 signals *exceptional rolls with a single icon*; PoE overlays signal *tier via one tag + one background tint*. None of the three reference conventions puts **two full colour chips on every single line**. That's the one place current TT diverges from all of its named influences at once — not "different but familiar," just different.

## 4. D1/D2/D3 from the half-second read

**D1 — Rich-text polish.** Line looks like: `[T5][A]  58% increased Lightning Damage` (two chips, current). From the familiarity lens this is the safest — it's the LE weaver-bar pattern extended plus a PoE-overlay-style tier tag. Cost: low, ships fast. Constraint: TMP `<mark>` gives rectangles only, no rounding, no icon glyph unless a sprite asset exists — so it **cannot** produce a D4-style star or a WoW-style rounded pill; **cannot** background-tint the whole tooltip card, only per-line quads. Half-second read: works, but two stacked chips per line is more per-line signal than any of the three reference conventions use, which is likely exactly the "overwhelmed" risk Andrew flagged.

**D2 — Native-clone chips.** Line could look like a real rounded badge, closer to WoW's actual UI chrome, possibly with a real small icon for S-grade (closer to D4's star). Cost: medium, unproven at this density (NativeClone is proven for buttons/rows in a sibling mod, not for a repeating per-affix badge). Constraint: still bound to whatever native LE UI elements exist to clone — no arbitrary custom icon unless LE ships one. Cannot fully replicate D4's bespoke greater-affix glyph unless LE happens to have a usable spare icon asset.

**D3 — Custom overlay.** Full freedom: could literally build a WoW-shaped tooltip (name-colour + border glow at the top, icon-based star only on standouts, gem-socket-style icon language). This is the only path that can fully match "familiar structure" from the named games rather than approximating it inside a single rewritten line. Cost: highest, and per BRIEF carries input/scaling/controller/mod-conflict/patch-fragility risk. From a familiarity lens specifically, it also raises the bar Andrew's WoW/D4-vet audience will judge against, since they'll now compare a full custom tooltip against the real thing, not just a stat line.

## 5. Ranked recommendation
1. **D1**, but only if the two-chip-per-line pattern is reduced to one dominant signal per line (see proposals below) — it already rhymes with LE's own weaver bar and with the PoE-overlay convention the sweats know, at the lowest cost and risk.
2. **D2**, as a follow-up experiment once D1's shape is validated in-game — real badge geometry (and maybe one real S-grade icon) gets meaningfully closer to D4/WoW native chrome without the full risk of D3.
3. **D3** last for this lens — the payoff (full WoW-shaped tooltip) is real, but it's the direction least aligned with "ships in a point release" and it invites the harshest comparison from the exact audience segment (WoW/D4 vets) it's trying to court.

(OPINION: this ranking is purely the familiarity/half-second-read lens; other seats' cost/risk lenses may reasonably rank differently.)

## 6. Concrete proposals (numbered, testable in a mockup)

1. **(OPINION)** Collapse the two chips into ONE combined pill per line: tier colour as the plate, grade folded in as a same-colour-family suffix inside the same chip, e.g. one plate reading `T5·A` — followed by the affix text. Matches the "exactly one primary signal per line" pattern shared by WoW/D4/PoE-overlay; testable side-by-side against current two-chip build in a mockup.
2. **(OPINION)** Reserve a glyph for the extremes only, D4-star-style: append a single unicode star (no sprite asset needed — TMP supports unicode glyphs) after S-grade lines only; C/B lines get the tier chip but no grade decoration at all. Directly targets "clear but colorful… not too much" by only decorating the lines that deserve attention.
3. **(OPINION)** Move the tier colour onto the numeric VALUE only, not the whole affix name: `<tier-colored>58%</> increased Lightning Damage [A]`. This is closer to WoW's habit of colouring the one thing that matters (there: item name; here: the roll number) rather than tinting the entire line, and it reduces colour area per line versus the mockup's Option C.
4. **(FACT-anchored test)** Ship BadgeLeft as spec'd (Andrew's pick) but A/B it in the mockup against Proposal 1's single-pill BadgeLeft — same layout preset, different chip count — so the council can compare noise level at equal position/consistency.
5. **(OPINION)** For hybrid lines (currently bare — a gap, not a choice per the mockup's own annotation), apply Proposal 1's single pill per stat, joined, e.g. `T1·S  +32 Armor` / `T1·S  10% reduced Crit Damage Taken` rather than leaving them unsignaled — closes the biggest current "different but NOT familiar" gap, since every reference convention marks the exceptional stat, and hybrids are currently the one place nothing is marked at all.

## 7. Ruling questions for Andrew (max 6)
1. One combined pill (`T5·A`) per line, or keep tier and grade as two separate chips?
2. Should grade decoration show on every line, or only call out the extremes (S, maybe F) the way D4 only stars greater affixes?
3. Should the tier colour live on the whole affix name (current), on just the numeric value, or only on the badge/chip itself?
4. Confirm BadgeLeft as default (per SPEC) after seeing it next to a single-pill version, or is SignalRight still in play?
5. Worth prototyping a real native-clone badge (D2) now, or hold until the D1 chip shape is proven in-game first?
6. Keep letter grades (F/C/B/A/S) as-is, or is that report-card convention itself up for a rename/rethink alongside the chip shape?

🟠 Claude Sonnet

---
Conductor note (not seat text): Sonnet's "hybrid lines render bare" is read from the June belt mockup's annotation; 3.0.2 (shipped last night) resolves the multi-stat affix and gives each stat line its own bracket, so that gap is closed in code. Sonnet did not read TooltipRecolor.cs or CHANGELOG.md. The unicode-star idea depends on the game font carrying the glyph (not verified). This seat shares the conductor's lineage and is discounted as a vote.
