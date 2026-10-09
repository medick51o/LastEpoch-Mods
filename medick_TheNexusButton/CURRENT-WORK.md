# CURRENT-WORK — A Terrible Button: The Nexus Button (read first, update every step)
Updated 2026-10-08 19:50 (Opus 5.5, /dispatch)

## State
- New standalone mod. SPEC.md = draft awaiting Andrew's OK (location option A assumed).
- Game API found: `Il2CppLE.Dev.Console.Commands.PanelSystemCommands.Nexus()` (static, returns Result<string,string>);
  `Il2Cpp.MonolithTimelinePanelManager.OpenNexus()` (private, interop-exposed). Monolith panel is only built
  when the pillar is clicked (Andrew).
- PROBE 1 (v0.1.0-probe): button at the crest spot; click -> call PanelSystemCommands.Nexus(), log the Result;
  dump the native THE NEXUS button recipe if it exists. NO travel.

## 2026-10-08 21:13 — state before compaction
- Deployed build = native-clone THE NEXUS button, matched to the ACTIVITIES tab (by label; hidden tabs ignored),
  height x0.74, top = tab top + 9u + tab height; follows the top-most Left Panel Stack panel; last spot kept when closed.
- Settings section "A Terrible Button": OffsetX/OffsetY live sliders, Reset, "Nexus Hotkey (N)" toggle (OFF default).
  Settings built by Codex (TICKET-01); rest by Claude. Grok's partial review finding (clone failure latch) FIXED.
- Click-through fix: UIMouseBlockerAdapter.Create on the button (game decides "over UI" via UIMouseListener). Awaiting in-hand.
- Nexus OPEN only works in Traveler's Rest (PanelSystemCommands.Nexus no-ops elsewhere). Open ruling: A teleport-to-Rest-then-open
  vs B probe forcing the pooled Monolith panel. Customization styles (Default/Transparent/MEDICK WAS HERE) pending; 2 style splits in
  C:\Sync\Projects\_review-tmp
exus-design\SYNTHESIS.md.
- RULE (Andrew 21:00): Claude builds directly while he tests; one review before commit; never kill a seat job unasked.
- Nothing committed. Inventory 2.0.3 teleport fix also deployed + reviewed, awaiting arrival-guard in-hand + commit go.
