# TICKET-3.1.0-D1-fallback — OUTLINE ONLY (not built). Runs if the D2 probe fails, or alongside it for the sentence rule.
Basis: RULINGS-andrew.md (08:41/08:42) + council v3 (docs\design-2026-09-11\SYNTHESIS.md). Version 3.1.0.

1. **GREATER-AFFIX TINT (lands regardless of the pill):** `ComposeCleanLine` sentence colour rule replaces `NameColorMode` default: T1–T5 sentence plain white (`#FFFFFF`; the game's own white if a `GameDefault` path exists), T6–T7 sentence `Prefs.GreaterAffixTint` (default `#C990FF`). Never bold, no plate, no glow. `AffixNameColorMode` gains `GreaterAffix` as the new default; `TierColor` and `GameDefault` keep working (cfg ABI frozen).
2. **Signal unit (council fallback shape):** `SignalStyle` default Badge → PlainText; one coloured-text unit `Tier N · G` per line, tier word in the tier colour, grade letter in the grade colour, dim `·`; two-space breathing room after the unit. Badge stays as the knob for anyone who wants plates. (Ruling text suggests approximating the pill with D1b/D1c; the council's round-two votes say those carry attacks and no-plate does not; Andrew's call, both are one tag apart.)
3. **PillTiers parity:** if the probe ships GreaterOnly, the text unit on T1–T5 lines is the small mark the council described (`Tier 3 · C` coloured text); if All, the unit is the same on every line.
4. Sealed = dim 90% text before the unit. Alt detail lines `<size=90%>` dim, range in grade colour.
5. FilterRuleTooltip NumberOnly `<size=200%>` → `<size=120%><b>` gold (Q7 open; council unanimous).
6. Remove dead `Colors.BadgeTextColor` (three seats, independent). Add `#C990FF` to the Colors.cs header comment and the README legend as the greater-affix tint (ruled, non-palette).
7. Untouched: ground labels, filter rule ownership, every frozen ABI, forbidden hooks.
8. Acceptance in-hand: belt, four-affix idol, weaver line, sealed affix, Alt on/off, SDR and HDR capture, 1080p at 100/125/150% UI scale, stash compare, LeHud + Fallen loaded; fresh eyes name the greater affix and the best roll in under a second.
9. Alpha strip (only if a plate is ever wanted back): T1/T3/T5/T7 at `<mark>` alpha 00/40/55/66, SDR and HDR, on and off the weaver bar.
10. Cross-vendor review before deploy; 3.0.2 DLL kept as restore; deploy waits for Andrew.
