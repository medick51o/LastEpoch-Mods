# 3.0.2 — Nexus release text + bug-report replies (Andrew pastes; Claude never posts)

## Nexus version notes (paste into the file description / changelog box)
v3.0.2 — the bug-report release
- Weaver / multi-stat idol affixes now get their Tier and Grade in the inventory tooltip (reported by Kaazkulaas). The game formats those affixes one stat at a time without handing the mod the affix; the mod now looks the affix up from the item. Each stat line of a multi-stat affix carries its own signal.
- Mouseover stutter while moving with a ground tooltip open is fixed (reported by speedscalzone). The mod was rescanning the whole UI every frame the tooltip repositioned; it now only rescans when the tooltip content changes.
- Alt deep view no longer shows raw "[1B]" text on re-rendered lines.
- Turning the master toggle off now fully restores vanilla tooltips (tier colours and Range rows) and hands the game's own range setting back.
- Settings changes apply to an open tooltip immediately.
- Failures are logged instead of swallowed; unknown keys in the cfg are named in the log; DebugLog=true prints a formatter trace you can attach to a bug report.

## Reply to Kaazkulaas ("Weaver affixes not showing Tier/Grade in inventory")
Thanks for the clear report and screenshots, they were exactly what cracked it. Fixed in 3.0.2. The game formats multi-stat affixes (like your +Health / +Health Regen pair) one stat at a time and doesn't pass the affix to the formatter the mod hooks, so the tooltip line came through bare while the ground label, which reads the item directly, was fine. 3.0.2 resolves the affix from the item by the stat being formatted. Each stat line now carries its own Tier/Grade. If anything still looks off, set DebugLog = true in UserData/medick_Terrible_Tooltips.cfg and attach the MelonLoader log.

## Reply to speedscalzone ("Mouseover stutter")
Thanks, and sorry about that one. Confirmed and fixed in 3.0.2. A ground tooltip follows its label, so while you walk the game re-lays it out every frame, and 3.0.0 answered every one of those with a full UI scan. 3.0.2 only rescans when the tooltip's content actually changes, so positioning frames cost nothing. If you still feel a hitch, set DebugLog = true in UserData/medick_Terrible_Tooltips.cfg and send the MelonLoader log.

## Andrew's upload checklist
1. Launch once with the final 3.0.2 DLL (it will be in Mods after the review gate). Hover a two-stat idol, walk with a ground tooltip open, press Alt. Two minutes.
2. Upload release/medick_Terrible_Tooltips_v3.0.2.zip to Nexus mod page, version 3.0.2, paste the notes above.
3. Reply to both bug reports with the drafts above; mark them fixed.
