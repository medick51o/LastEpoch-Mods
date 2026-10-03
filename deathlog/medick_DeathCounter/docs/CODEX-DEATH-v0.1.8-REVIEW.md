# Terrible Death Log v0.1.8 experimental candidate

Field journal implementation from Astra's design v1, with Andre's follow-up requests. Built locally with deployment disabled. Not installed, published or merged. Installed v0.1.7 remains untouched.

## Changes

- Five readable views: Last death, History, Patterns, Boss notes and Settings. The header uses Terrible / Death log; the formal product name is in About.
- Cause and the next supported action lead the report. A wide window uses two columns; larger text/narrower windows stack them. Warm charcoal surfaces, readable damage colors, original capped-first resistance cards, and a visible body scrollbar remain.
- Gear checks and death details are disclosures. Original raw game text and extra defenses stay in details. No source list, external guide button, forum link or publisher name appears in the player interface. Development provenance remains preserved. The contribution button opens the author's GitHub; Open log folder opens a local folder.
- Boss notes keep the full catalog and authored plans. The encounter browser collapses after selection. Existing timeline locations and parent relationships supply labels for monolith bosses and Harbingers. Campaign variants remain distinct. Unmapped relationships are not guessed. These labels are browse context and do not identify the zone of a saved death.
- History selection stays anchored by stable death key and viewed character. A new death offers an explicit View action without stealing the selected record. View/death scrolling and disclosure state are retained. Pattern evidence coverage and unknown-cause counts precede conclusions; repeated analysis is cached.
- Check my changes always compares with the original death. Check again under a selected saved check creates a new child. Grades and actions use the existing engine. New children additionally freeze the parent's stats and timestamp for readable comparisons after parent deletion. Old checks remain readable and keep their saved grades; no backfill or recalculation is performed.
- Checks are gated to the same living character. Save/delete failures keep existing checks intact; save first, then show a saved row. Scoped single/all deletion confirmations preserve the original and children. Escape dismisses a confirmation before exiting move mode or closing the panel.
- Settings keep automatic logging, independent HUD/notification controls, movement and text size, the exact reset joke, scoped export and troubleshooting. No recording/pause toggle.
- Menu focus separately gates EpochInputManager.Update and native input modules' Process dispatch. The actual Rewired module is checked explicitly; Unity fallbacks are attempted where present. Closing input stays blocked through held buttons/keys and a neutral frame. The implementation never writes a shared forceDisableInput flag. The compact HUD remains movable and quiet; menu/move focus reveals a hidden cursor when possible.

## Preservation

All 79 baseline source hashes match the v0.1.7 manifest before editing. The previous extracted tree also matched that archive, so the new candidate is in a separate directory. The baseline archives and installed v0.1.7 remain recoverable.

Capture hooks, tracker, parser, inbox, original history store, reset/count ledger, advice rules, mitigation calculations and grading engine were not changed. The only existing Core behavior change is additive optional PreviousStats / PreviousUtcTime storage for newly created child gear checks. Original deaths and earlier checks are never rebuilt from current gear.

Installed DLL, config and all seven monitored files match the pre-build hashes after the pass. No runtime/game DLL was replaced, game was launched, or player data was packaged.

## Automated validation

- 304/304 CoreTests pass, including 11 new regression tests for stable selection, character scope, late enrichment, closing/reopening input ownership, frozen deleted-parent comparisons, old check compatibility and timeline labels.
- NoGame Release: zero warnings/errors, DeployToMods=false.
- Game-linked Release: zero warnings/errors, DeployToMods=false.
- Both InteropGuard modes pass: 77 direct interop members, 246 reference members across 44 shipped DLLs; only the existing Application.OpenURL allowance is used.
- The guard's reference root is the existing full repository under work/repo-source, because the clean source archive deliberately contains no shipped DLLs. An initial diagnostic run against the clean archive had no reference set and was corrected before validation.
- ZIPs and patch are separately inspected and hashed. The install zip contains exactly one root DLL.

These results do not prove native rendering, reflection success or input interception. No browser mockup is presented as an in-game capture.

## Required native tests before calling this ready

1. Dead character: place the menu over Respawn and click tabs, blank space, buttons and scrollbar. No hidden Respawn activation. Repeat on the opening frame and closing click.
2. Living character: navigate, scroll and drag inside/outside the menu. No new movement or skill command. After closing/releasing, normal game controls return. This does not pause combat or world time.
3. Inspect the focus hook status in Settings > About & troubleshooting. Test alongside another mod menu and the character/inventory screens; closing one must not clear another screen's input lock.
4. Verify readable 1080p and smaller-screen layouts at text sizes 1.0 and 1.4. Check wrapped titles/buttons, resistance rows, confirmations, wheel/scrollbar drag and retained scroll positions.
5. Controller idle HUD remains quiet. Test cursor reveal/restore and switching to mouse while the panel is open. No new custom gamepad menu bindings are claimed.
6. Record two consecutive deaths, including repeated reports; confirm one record each, game explanation intact and automatic logging with HUD hidden. Open an older death while a new one occurs and confirm its context stays selected.
7. Check the same living character, restart, inspect both original and child checks, delete a parent, inspect the retained child, and delete-all scoped to one death. Verify original defenses/cause/grade snapshots do not change after late enrichment.
8. Reset the displayed counter after reading the joke. Original saved deaths and the game count must remain intact. Exercise export and durable error states with disposable test files.

Season/Legacy capture and localized zone lookup remain unresolved as in v0.1.7. Unknown boss flags, crit and missing hit timelines remain unknown. Large histories have not been profiled natively. Earlier child checks lacking the new optional parent snapshot retain their original saved changes; absent parent numbers are not invented.

## Research verification retained outside the interface

The ten established timeline / boss / Harbinger associations in the existing catalog were cross-checked on October 3, 2026 against [the timeline roster](https://lastepoch.tunklab.com/timelines). Examples: Lagon / Ending the Storm / Harbinger of Chaos; Heorot / The Age of Winter / Harbinger of Tyranny. This development reference is not displayed or linked by the app. Newer unmapped catalog encounters were not assigned a timeline from those examples.

## Installation boundary

BUILD-THIS-DESIGN.md says: “No publishing or installation in this pass without separate user direction.” This candidate follows that instruction. A separately authorized installation must first back up the current DLL and data, verify the game is closed and retain the v0.1.7 rollback.
