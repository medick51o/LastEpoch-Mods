# COUNCIL BRIEF — Terrible Tooltips 3.0.1 incident (2026-09-10)

Read-only review. Do not modify any file. Answer every numbered question. Cite `path:line`.
A finding without a failure mechanism and a reproduction path is NOT PROVEN; say so.
First line of your report: a status word (DONE / DONE_WITH_CONCERNS / NEEDS_CONTEXT / BLOCKED).
Then a manifest: every file you read, with an MD5 you computed yourself.
Then findings as a table: rank (BLOCKER / MATERIAL / MINOR / NOT PROVEN) · claim · failure mechanism · verify-by / repair direction.

## The boss's words, verbatim
> "we are going to revisit terrible tooltips 3.0 some folks posted some bugs lets try and fix them"
> "i threw a idol on the ground from my inventory and now it wont show up and i cant pick it up this is very dangerous for players we had this problem before when itemms on the ground would drop"
> "and sometimes the tier wont show up ... its not showing tier all the time and i have to hit alt for it to show up soemtimmes"
> "actually all my items im throwing on the ground are not showing up right now" / "lot a few good items"

## What this mod is
MelonLoader + HarmonyX mod for Last Epoch (Unity 6000.0.42f1, IL2CPP, game 1.4.7, MelonLoader 0.7.3).
Source: `medick_TerribleTooltips/src/*.cs` (this folder's parent). Build: `medick_TerribleTooltips/medick_Terrible_Tooltips.csproj` (net6.0), post-build copies the DLL into the game's `Mods\` folder.
Game assemblies for signature checks: `C:\Program Files (x86)\Steam\steamapps\common\Last Epoch\MelonLoader\Il2CppAssemblies\Il2CppLE.dll` (types live under the `Il2Cpp` namespace).

## Timeline tonight (all 2026-09-10, local)
- 19:40 — v3.0.1 DLL built and copied into `Mods\` (diff vs the shipped 3.0.0: `DIFF-3.0.0-to-3.0.1.patch` in this folder; the 3.0.0 base is git commit 9ae8d93).
- 19:43 — game launched. Log: `MedicK's Terrible Tooltips v3.0.1 loaded (9/9 patches).` No exception from this mod anywhere in the log.
- 19:44 and 19:48 — two `NullReferenceException` from a DIFFERENT mod, Fallen Star's Improved Tooltips, in `GameReferencesCache.Postfix(LoadingScreen)` (see `melonloader-excerpt.log`).
- ~19:50 — the boss drops an idol from inventory onto the ground in town. The item's 3D model is visible on the ground; NO ground label appears; it cannot be picked up (Last Epoch pickup is by clicking the label). Later: every item he drops behaves the same. Loot filter panel shows "NO FILTER" selected.
- 19:51 — settings panel opened; log shows all three medick mods report "settings hierarchy changed (toggle row template missing)" — their settings UI is unavailable this session. Config therefore comes only from `medick_Terrible_Tooltips.cfg` (copy in this folder). Note: `GroundLabelFilterOnly = true`, `GroundLabelAltKey = true`, `AlwaysShowRanges = true`, `AlwaysShowTierDetails = true`, `ShowFilterRuleNumber = "NumberOnly"`.
- 20:00 — rolled back on disk to the 3.0.0 DLL (the July backup). The boss is relaunching to test whether dropped-item labels return. Result not yet known at brief time.

## Tooltip screenshots taken tonight on 3.0.1 (described; images not available to seats)
- Adorned Arcane Idol, no Alt: two single-stat affix lines each show a (washed-out by HDR) tier chip plus a grade chip (B, C). A third affix with TWO stat lines ("18% increased Critical Strike Chance" / "+9% Critical Strike Multiplier") shows NO chip at all, plain blue text.
- Same idol with Alt held: headers PREFIXES/SUFFIXES appear, ranges appear, and the two-line affix now shows a "Tier 6" chip (no grade), plus "ENCHANTMENT ONLY" and two Range lines.
- Stout Weaver Idol, no Alt: two two-line affixes (penetration pair; cold damage pair) show NO chip. A single-line affix "+7% Chance to Chill on Hit" (with the game's purple weaver highlight) shows a tier chip + grade chip.
- Stout Lagonian Idol, no Alt: "[chip][B] 18% increased Critical Strike Chance", "[chip][ ] +3% Elemental Resistance", and a two-line "+2% to All Resistances / +2% to Minion All Resistances" with NO chip.
- Same Lagonian idol with Alt held: the lines render as RAW text "[1B] 18% increased ..." and "[1F] +3% Elemental Resistance" — the bracket is visible as literal characters, not composed into chips. The two-line affix still has no chip.

## The two Nexus reports that started tonight's work (game 1.4.7, ML 0.7.3, TT 3.0.0, Fallen 3.2.1)
1. Kaazkulaas, 2026-08-06: "Weaver affixes on idols show the Tier/Grade correctly on the ground label but not when viewed in the inventory/stash. Non-weaver affixes on the same idol work fine." His inventory screenshot: one affix line with chips ("12 Ward On Potion Use"), and a two-line affix ("+17 Health" / "+2 Health Regen") with no chips. Ground label for the same idol showed a two-entry bracket.
2. speedscalzone, 2026-08-30: "If my character is moving while I mouse over a ground loot item, the game stutters. I tried various settings configurations in the menu to no avail. Disabling Terrible Tooltips in the settings menu fixes the issue."

## What 3.0.1 changed (facts only; judge the diff yourself)
- `AffixInjector.cs`: a new Harmony postfix on `TooltipItemManager.FormatAffix(ItemDataUnpacked, ItemAffix, ...)` that injects a bracket when the result has none. Registered in `TerribleTooltipsMod.cs`.
- `TooltipRecolor.cs`: the full-scene `FindObjectsOfType<TextMeshProUGUI>` scan in the `UITooltipItem.UpdateLayout` postfix is now gated (`ShouldScan`: dirty flag / 0.5 s fallback / marker-loss check), a `RunScan` extraction, a LateUpdate "dirty catch-up" that runs the scan when the tooltip is active, `MarkDirty()` when the native range switch flips, and continuation lines after a bracketed line are tinted in the tier colour.
- `FilterRuleTooltip.cs`: the `SetAsItemTooltip` / `SetAsGroundTooltip` postfixes now call `TooltipRecolor.MarkDirty()`.
- Version strings 3.0.0 → 3.0.1. `GroundLabels.cs` is UNCHANGED.

## Questions every seat must answer
Q1. Missing ground labels on player-dropped items: is there a mechanism in the 3.0.1 diff that can hide, blank, deactivate, or prevent a `GroundItemLabel` from appearing? Trace the code path or state there is none. If none, list the other candidates you can support from the evidence in this folder (other mod, config, game), ranked.
Q2. The "raw [1B] text under Alt" screenshot on 3.0.1: what code path produces literal uncomposed brackets, and does 3.0.1's scan gate have a hole through which fresh game-written text is never composed? Give the exact sequence of calls.
Q3. Two-stat affixes ("+2% to All Resistances / +2% to Minion All Resistances") get no bracket in the normal view on 3.0.1, and get a synthesized "Tier N" chip only under Alt. Does the new `FormatAffix` postfix reach them? Why or why not? What would? (You may inspect `Il2CppLE.dll` type/method signatures; note that IL2CPP interop assemblies contain stubs, not bodies.)
Q4. The mouseover-stutter fix: is the gating sound? Name any case where the tooltip's content changes and no scan follows, and any case where the scan still runs every frame.
Q5. Anything else in the diff that would break 3.0.0 behaviour (Alt deep view, Always-Show pins, LeHud/Fallen coexistence, ground labels, filter rule number).

## Your lens (assigned per seat in the dispatch message; answer all five questions regardless)
