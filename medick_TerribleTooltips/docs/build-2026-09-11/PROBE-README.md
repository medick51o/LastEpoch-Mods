# D2 PROBE — Andrew's in-hand steps (3.1.0-dev, STAGED ONLY, not deployed)

MACHINE: WMW (the game box). Time needed: ~5 minutes. Nothing was copied into the game; step 1 is yours.

What this is: ONE cloned native pill placed on ONE affix line of the hovered item's tooltip, to prove whether the game's UI can host the D2 look you approved at 08:41. It is OFF by default, logs loudly, and if it finds no donor sprite it changes nothing. The greater-affix tint (#C990FF on T6/T7 sentences) is NOT in this probe; that lands with the 3.1.0 composer ticket. Built DLL: `medick_TerribleTooltips\medick_TerribleTooltips\bin\Release\net6.0\medick_Terrible_Tooltips.dll` (82,432 B, 09:10). Review: Gemini APPROVE_WITH_NOTES, 0 blockers.

1. Game CLOSED. Back up the live DLL: in `C:\Program Files (x86)\Steam\steamapps\common\Last Epoch\Mods\` copy `medick_Terrible_Tooltips.dll` to `medick_Terrible_Tooltips.dll.3.0.2-restore` (the 66,048 B one from Sep 10 22:04). Then copy the built DLL above over it.
2. Game still CLOSED. Open `C:\Program Files (x86)\Steam\steamapps\common\Last Epoch\UserData\medick_Terrible_Tooltips.cfg` and set:
   ```
   PillProbe = true
   PillTiers = "All"
   ```
   (`GreaterAffixTint = "#C990FF"` is there too; leave it. `DebugLog` can stay as it is.)
3. Launch. In the MelonLoader console the startup line must read `MedicK's Terrible Tooltips v3.1.0-dev loaded (9/9 patches)` and, right after, `[PillProbe] armed (PillTiers=All, GreaterAffixTint=#C990FF)`. If you see 8/9 and a warning naming "pill probe (D2)", the hook did not bind: STOP, that is a FAIL, send me the warning line.
4. Open your inventory and hover an exalted (anything with a Tier 5+ affix). Look at the console once:
   - `[PillProbe] IMG …` / `[PillProbe] TMP …` lines then `[PillProbe] dump done: N images, M tmps` — this is the evidence we needed regardless of the outcome.
   - `[PillProbe] donor try 1: … → …` up to `donor try 6`, then either `[PillProbe] donor = <path> sprite=<name> …` or `[PillProbe] no donor found — degrading to the text path (nothing changed)`.
   - On success: `[PillProbe] margin … → …` and `[PillProbe] pill placed on '<first affix>' tier=N grade=G at (x,y) size WxH donor=<path>`.
5. Look at the tooltip. PASS = the first affix line has a real game-art pill to its left reading `Tier N | G` in the palette colours, the sentence starts to the right of it, nothing overlaps, the tooltip is not taller than before. FAIL looks like: no pill but the console says "no donor found" (that is a clean, honest fail; the text path is untouched), or a pill in the wrong place / overlapping text / a white square (send a screenshot + the `pill placed` line), or any `[PillProbe] FAIL at …` line (send it; the probe turns itself off for the session).
6. If it passed, do the four quick abuses, watching for a stale or doubled pill: hover a second item fast; open the stash and hover an item with the comparison tooltip up; hold Alt on the item; set the master toggle off and on. Any leftover pill or a second pill = FAIL, tell me which step.
7. Optional: set `PillTiers = "GreaterOnly"` (game closed), relaunch, hover an item with a T6/T7 line and one without. Expected: pill only on the T6/T7 line; `[PillProbe] no T6/T7 line on this item — no pill (GreaterOnly)` on the other.
8. To go back: game closed, set `PillProbe = false` (the DLL can stay; off = zero work), or copy `medick_Terrible_Tooltips.dll.3.0.2-restore` back over the DLL.

What I need from you: the `dump done` line, the `donor =` or `no donor found` line, the `pill placed` line if any, and one screenshot of the tooltip. That decides D2 vs the D1 fallback (docs\build-2026-09-11\TICKET-3.1.0-D1-fallback.md).
