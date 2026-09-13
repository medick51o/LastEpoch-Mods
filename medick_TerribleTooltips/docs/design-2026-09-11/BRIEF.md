# DESIGN BRIEF — Terrible Tooltips overhaul council (2026-09-11, blind, read-only)

You are one seat of a blind design council. You see no other seat's answer. Your output is DATA for a design deck the mod's owner (Andrew) will rule on tomorrow morning. No code changes are wanted tonight. Do NOT modify any file. Do NOT run the game.

## Andrew's words, verbatim (the vision)
"i want all hands on deck council. not sure what i want to do here but i want to overhaul the app and clean it up. i want astra to take the lead on this, i guess the vision is to spin up the council fable still orchestrates but astra is going to go in a do a deep dive, i think i want to rewview it for astetics or how can it look "cleaner" when users are viewing the tooltips in came, maybe the interface where they mouse over the item and its showing the tiers i just want it to be clear but colorful for the users to get the into they need but not to much visual colors where they are just overwelmed. i want to keep the color palette world of warcraft color tier system but it might just a ui limitation that the deveeopers of last epoch need to reimagine im just trying to put a better wrapper on it. but hey maybe we can just overhaul the ui in general and give users something different but familiar. have the council involved in this and go into auto mode."

## Hard facts
- Mod = MelonLoader/HarmonyX, Last Epoch 1.4.7, Unity 6000 IL2CPP. The tooltip is EHG's own UI; the mod rewrites the TEXT of existing TextMeshPro lines (rich text tags: `<color>`, `<mark>` (a colored quad that draws OVER glyphs in this game's TMP — in-game law, see docs/ARCHAEOLOGY.md and the comment in src/TooltipRecolor.cs ComposeCleanLine), `<size>`, `<pos>`, `<b>`, `<i>`, `<sprite>` only if a sprite asset is available), and re-invokes the game's own layout. It cannot add widgets to the tooltip today. A sibling mod (Terrible Inventory, context/inventory/NativeClone.cs) proves the fleet CAN clone native UI controls (buttons, rows) into game panels — a possible medium-cost path. A full custom tooltip overlay (own canvas drawn over the game's) is the Tier C path.
- Current v3.0.2 look: one line per affix: `[Tier 5][A]  58% increased Lightning Damage` — two translucent chips (tier colour plate + grade colour plate at 40% alpha, bright ink #FFF6E6) then the affix text in the tier colour. Ground labels: `Item Name [5A 3C 7S]`. Filter rule number in gold. Alt = deep view (ranges, craft info). Layouts: BadgeLeft / SignalRight / Trailing; Signal style Badge or PlainText; name colour tier/default; grade letters on/off. Colour language: T1 #DADADA → T2 #E1E1E1 → T3 #16FF0E → T4 #77ACFF → T5 #A807FF → T6 #FA9E3D → T7 #FF44FF; grades F #DADADA · C #77ACFF · B #A807FF · A #FA9E3D · S #FF44FF (WoW-retina palette, FROZEN brand language — Andrew: keep it).
- Real screenshots from last night exist only in chat; described: the tier chip washed out to a white block under HDR capture; 4 lines of chips on a 4-affix idol; the game's own purple weaver-affix highlight bar sits behind our chips on weaver lines; multi-stat affixes render two lines each with their own chip (3.0.2).
- Existing mockups: mockups/tooltip-belt-v3-badges.html, mockups/tooltip-flow-v3.html (v3 design iterations). Spec + archaeology: docs/SPEC.md, docs/ARCHAEOLOGY.md (laws; the forbidden list is absolute: never patch OpenItemTooltip, UpdatePrefixAndSuffixesText is unpatchable, no Il2Cpp generic List params, LeHud truce, Fallen Star treaty). Andrew's stated taste: "clear but colorful", "not overwhelmed", "different but familiar", WoW tiers.
- Last night's context (for what the code is doing right now): context/council-2026-09-10/ (bug council), context/build-2026-09-10/ (3.0.2 tickets and reviews). Source of record: src/*.cs (v3.0.2).

## Three directions every seat must cost (you may propose a fourth)
D1 **Rich-text polish** (cheap, ships in a point release): same architecture, better typography inside TMP tags — chip sizing, one chip instead of two, dot/bullet signals, dimmer secondary text, consistent spacing, HDR-safe plate alpha, weaver-bar coexistence.
D2 **Native-clone chips** (medium): clone a real game UI element (e.g. an existing badge/pill from the game's own tooltip or settings) per affix line via the NativeClone pattern, so tiers render as real UI instead of `<mark>` quads.
D3 **Custom overlay** (Tier C): the mod draws its own tooltip panel over the game's, full layout freedom, full risk (input, scaling, controller mode, other mods, every game patch).

## Required output shape (markdown, saved verbatim by the conductor)
1. Status word on line 1: DONE / DONE_WITH_CONCERNS / BLOCKED.
2. Manifest: the files you actually read.
3. Your read of the CURRENT look (honest, from the code and mockups).
4. For each direction D1/D2/D3 (and any D4 you propose): what it would look like (describe a concrete affix line), cost (hours/risk in words, not numbers), which game/TMP constraint it hits, what it CANNOT do.
5. Your ranked recommendation with the reason.
6. Concrete proposals: numbered, each testable in-game or in a mockup. Mark each FACT (from the files) vs OPINION.
7. Either/or ruling questions for Andrew (max 6), one line each.
8. Sign with your seat name.
Do not spawn helpers. Do not write files. Reply in chat only.
