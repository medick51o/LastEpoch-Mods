# Local experimental test build 0.1.2, October 2, 2026

This build has passed 60 CoreTests, both NoGame and local game builds with warnings treated as errors, and InteropGuard in both modes. No fresh in-game death has verified these new report hooks yet. Keep this experimental and off Nexus.

Changes:
- Capture current PlayerActorSync.ReceiveDetailedDeathInfo and static DeathInformationText.UpdateDeathInfo rather than expecting online server-side ApplyDamage hooks to fire locally.
- Resolve game-localized killer/ability/ailment names. Preserve the original formatted death message and colors; plain logs remain readable.
- Wait 0.75 seconds after death detection for the report; late reports within five seconds enrich the same record without adding a death. Preserve character identity when a report is queued.
- Use PlayerFinder.getLocalPlayerPrecalculatedStatsHolder and getLocalPlayerWardHolder for online defense snapshots. Sample living defenses every 0.5 seconds, accept snapshots no older than two seconds, record their age, and never label a post-death read as combat defenses.
- Display a small number with a very faint deaths label while idle. Hover reveals Death counter, MOVE, help and session count. A hidden controller cursor cannot leave the HUD expanded or block movement. The menu also offers Move counter, Reset position, size controls and a Done control while moving.
- Replace the tiny 480-pixel panel with a readable, opaque 820-pixel panel, 18-pixel body text, scrollable content and Last death / History / Patterns / Options tabs. Font size can increase separately from the counter.
- Reset uses a saved display baseline per character, preserving all death history and the game's own count. It includes Medick's requested judgment warning. Export character history as readable text.
- Fire information is red. Other elements have consistent colors. The game's own formatted message retains its colors without allowing game size tags to shrink the UI.
- Recommendations cite recorded resistance gaps and killing-blow/max-health values where available. Capped resistance and capped-resistance Ignite receive different priorities. Reported killing blows are not fabricated into one-shot/burst timelines. Advice is limited to three items.
- Expected player-loading transitions now log through debug, rather than warnings. Missing legacy hook names are debug diagnostics; the current report hooks log availability once.

Preserved: internal DLL/config medick_DeathCounter, Insert menu, Shift+Insert counter toggle, Season 5 embedded CoreModule repair, one-DLL distribution, the existing Grok hardening work. No PR merge or Nexus publication.

Next local test:
1. Confirm startup logs show v0.1.2 and two death-report hooks, with no new UI or Harmony errors.
2. Load the character. Check idle/hover HUD, drag MOVE and menu Move counter, screen-edge clamping and saved position after restart. Check hidden-cursor controller behavior if available.
3. Open Insert, inspect History and Options, and test counter reset while retaining history.
4. Die once on a softcore character. Leave the death screen visible for a few seconds. Compare the game's cause message with the mod record for killer, ability/ailment, primary/secondary damage types, damage and crit. Count should increase once.
5. Inspect deaths.jsonl: DetailSource should say game death report. Defenses should include the available online stats and snapshot age. Missing stats should remain absent, not guessed.
6. Test a later death to an ailment when convenient, then inspect red fire/other colors and the advice.

Known limitation: the old 0.1.1 unknown death contains no captured combat details. Do not invent or backfill its killer from another death. Live native hook execution and snapshot units still require verification on this game install. Grokbot is being asked to validate current mechanics and advice; avoid claiming a gear change would definitely have saved a player without enough damage/ward/timeline information.

Build commands from repository root:
- dotnet run --project medick_DeathCounter/tests/CoreTests (SDK 8)
- dotnet build medick_DeathCounter -c Release -p:NoGame=true -p:DeployToMods=false -warnaserror
- dotnet run --project tools/InteropGuard -- medick_DeathCounter/bin/Release/net6.0/medick_DeathCounter.dll . "UnityEngine.CoreModule: UnityEngine.Application::OpenURL (Method)"
- dotnet build medick_DeathCounter -c Release -p:DeployToMods=false -warnaserror
- Run the same InteropGuard command again. Candidate must retain its exact DLL filename so it cannot prove itself from a release artifact.
