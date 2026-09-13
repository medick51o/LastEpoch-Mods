# TICKET-3.1.0-dev — the full 3.1.0 package for Andrew's in-hand test (written 14:09 by the team lead; Andrew's order 14:07: "pack everything into 3.1.0 we plan to change and ship and put the dev version of it in my game")
Builder: 🔵 Codex (workspace-write, fence below). Verify: `dotnet build medick_TerribleTooltips\medick_Terrible_Tooltips.csproj -c Release -p:DeployToMods=false`, 0 warnings. Reviewer: 🟢 Gemini AFTER staging, in parallel with the in-hand test (dev build; 3.0.2 restore kept). Deploy = team lead, not the builder.
Basis: docs\design-2026-09-11\RULINGS-andrew.md (D2 pill target, GREATER-AFFIX TINT #C990FF final, T1–T5 white) + TICKET-3.1.0-D1-fallback.md (council fallback shape) + the staged D2 probe (already in tree, UNCHANGED by this ticket).
Open rulings resolved by council default until Andrew overrides: "Tier N" spelled out (4–3) · no S★ (3–2) · Rule# 120% bold (unanimous) · PillTiers default All (matches the approved D2 render; GreaterOnly stays a cfg flip).

## WRITE SET (absolute fence)
1. `medick_TerribleTooltips\src\TooltipRecolor.cs` — ONLY `ComposeCleanLine`, the continuation-line colour at ~633 (`lastTierHex`), and the Alt-detail/sealed dimming touched below. No hook changes, no scan-gate changes.
2. `medick_TerribleTooltips\src\Prefs.cs` — `AffixNameColorMode` gains `GreaterAffix`; `NameColorMode` default → `GreaterAffix`; `Style` default → `PlainText`. Descriptions updated. NOTHING removed; every existing value keeps working (cfg ABI frozen).
3. `medick_TerribleTooltips\src\FilterRuleTooltip.cs` — line ~118 only: `<size=200%>` → `<size=120%><b>…</b>`.
4. `medick_TerribleTooltips\src\Colors.cs` — delete dead `BadgeTextColor`; add `GreaterAffixTintDefault = "#C990FF"` constant + header comment "greater-affix tint, ruled 2026-09-11, deliberately outside the palette".
5. `CHANGELOG.md` — replace the `## v3.1.0-dev` entry with the full list below. `README.md` — colour legend gains the #C990FF line.
FORBIDDEN: PillProbe.cs, TerribleTooltipsMod.cs, AffixInjector.cs, GroundLabels.cs, NativeSettings.cs, SettingsUi.cs, AaronsHouse.cs, TerribleTooltipsApi.cs, BuildInfo.cs (already 3.1.0-dev), the csproj, anything under mockups\ or docs\.

## Items
A. GREATER-AFFIX TINT in `ComposeCleanLine`: with `NameColorMode == GreaterAffix`: tier 1–5 → sentence gets NO colour tag (the game's own white); tier 6–7 → `<color={Prefs.GreaterAffixTint.Value}>sentence</color>`; untiered (unique/set/implicit, tier 0) → no colour tag. Validate the pref: if it is not `#RRGGBB` (or `#RRGGBBAA`) log ONE warning and use `Colors.GreaterAffixTintDefault`. `TierColor` and `GameDefault` behave exactly as today. Continuation lines of a multi-stat affix (the `lastTierHex` path) follow the same rule as their first line.
B. Signal unit default = `PlainText`: `Tier N` in the tier colour, `·` dim, grade letter(s) in the grade colour, then TWO spaces before the sentence (BadgeLeft). No other change to the unit; `Badge` still produces today's chips when selected.
C. Sealed: PlainText path → `<size=90%><color={Dim}>Sealed</color></size>` before the unit. Badge path unchanged.
D. Alt detail lines (the pinned/peeked `Range:` and `Tier: N (max craftable)` lines the composer already emits): wrap in `<size=90%>` with the existing colours; if the composer does not own those lines' text (they are the game's), SKIP this item and say so in the report — do not add a hook.
E. Filter rule number: `<size=120%><b><color={Gold}>Rule#{n}</color></b></size>`.
F. Dead code: `Colors.BadgeTextColor` removed (grep proves zero callers first; if a caller exists, leave it and report).
G. CHANGELOG `## v3.1.0-dev (unreleased)`: greater-affix tint · plain-text signal default · rule number 120% · sealed/Alt dimming · D2 pill probe (off by default) · dead code removed. README legend line for #C990FF.

## Report (≤20 lines)
Files changed = exactly the write set; build output path + size; the exact PlainText line the composer now emits for a T4/C line and a T7/A line (paste the tag strings); items skipped and why.
