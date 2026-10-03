# Experimental v0.1.13 (team review round 1)

- The next step card shows the exact-hit survival sentence, including the assumption that the reported damage was the whole hit after your defenses, including ward.
- If the game report is missing a damage type, the on-screen text supplies the type only. Damage and names stay as the formatter read them.
- Hovering the counter no longer freezes game input. Input pauses for an open log, a drag, or a held click on the counter.
- Endurance advice uses the recorded hit when the threshold is known. If raising endurance to 60% would still have killed you, it says so and stays off the death toast.
- Hit size includes ward lost. A hit that only broke ward is kept. A big report number does not reclassify a string of small hits.
- Curse, Shock, Frailty, and Chill of Aberroth are their own ailments. Curse of Aberroth is called out on the resistance card: 10 points per stack, no limit, and it cannot be cleansed. Shock of Aberroth is extra damage taken, not lightning shred.
- A torn counter reset file is left alone until a later read succeeds, so one new name cannot wipe the other characters.
- If you are alive again when a pending death would be saved, that death is dropped and its counter credit is removed. A late game-counter death is still saved.
- History shows about six deaths per page.
- When your resistance is already capped and the area level is known, the note says how much enemy penetration still lets through.
- Browse boss notes opens the encounter list in the panel.
- Boss attack cards and expanded boss notes show a short reworded Maxroll (community guide) line for how to spot a move and how to avoid it, plus a danger or one-shot flag and the guide's recommended resists.
- Majasa phase 1 lists physical resistance first, because the guide treats it as the highest priority for the whole fight.

Not installed or published. Native testing pending.

# Experimental v0.1.12 (team review polish)

- Assess my current gear no longer replays the original hit against your current resistance. Your gear has changed in more ways than one resistance, so it shows only the resistance comparison again, as v0.1.9 did. The v0.1.11 max health warning went with it. The exact-hit preview stays on the original death's resistance card.
- Numbers in hit previews use thousands separators (2,000), matching the death summary on the same card.
- Plainer preview wording: "the roughly 1,700 you had left" and "assuming the reported damage was the whole hit after your defenses, including ward".
- The Boss attack card looks up its notes once per death instead of on every frame.

Not installed or published. Native testing pending.

# Experimental v0.1.11 (team review round 2)

- The death toast now adds one next step when the death supports a concrete defensive change, for example "Next: Cap fire resistance: you had 41%." It says nothing extra when the record does not support a step.
- The Boss attack card no longer shows the Echo Slam tip on a plain Slam death (Harbinger of Defilement and Harbinger of Hatred).
- Assess my current gear warns when your max health is lower now than at death, since the exact-hit preview uses the health you had then.
- Counter resets are flushed to disk before they replace the old file, like the death history.

Not installed or published. Native testing pending.

# Experimental v0.1.10 (team review round 1)

- Death card and History no longer show a blank headline when the killing ability was not recorded; they fall back to the ailment or "Killed by" the attacker.
- When the game reports one damage type with damage and overkill, the resistance card and Assess my current gear show what capping (or your current) resistance would have done to that exact hit, with the report-unit assumption stated.
- The buffer card states the reported shortfall for that exact hit.
- Boss deaths show a Boss attack card on the Last death view with the killing move's reported damage, its authored tip and the guide's resistance priority, plus a Notes button.
- Boss notes list their sources (Maxroll, Tunklab, Last Epoch forum) with guide update dates under a collapsed Sources section, and mark the guide's main damage types.
- Patterns titles no longer quote one death's resistance number, and pattern copy matches the per-death advice.
- The Next step card no longer repeats the same explanation behind "Why this recommendation?".
- Fix a game-counter credit race that could hide a later counter-only death for up to 10 minutes.
- Old or hand-edited history with null lists or short damage arrays loads safely instead of breaking the panel and text mirror.
- reassessments.jsonl is read as strict UTF-8, so a damaged byte blocks saves instead of being rewritten.
- "1 point short" wording, and a CoreTest that only passed on Windows now runs everywhere.

Not installed or published. Native testing pending.

# Experimental v0.1.7

- Prioritize killing-type resistance gaps before smaller unrelated gaps.
- Correct unnamed DoT advice and preserve frozen saved-reassessment context.
- Move advice earlier, add visible scrolling, and make original game reports expandable.
- Retain v0.1.6 history protection and the working capture implementation.

293 tests pass. Both builds and interop checks pass. Native testing remains pending; not installed or published.
# Experimental v0.1.6

- Preserve torn, unreadable and future-version history rows through later saves.
- Commit flushed JSON snapshots atomically, preserve recovery copies, and block overwrites after failed reads or detected external changes.
- Keep failed session saves in memory for retry and distinguish JSON success from a failed text mirror.
- Show persistence warnings and export full JSON capture data alongside readable text.
- Preserve the tested death capture and v0.1.5 automatic logging behavior.

286 CoreTests pass. NoGame and local builds have zero warnings/errors and pass InteropGuard. Staged only; native testing is pending. Installed v0.1.4 and saved player history are unchanged.
# Experimental v0.1.5

- Remove the death-tracking pause button. Deaths are always counted and logged automatically.
- Keep counter visibility separate from logging and explain that hiding it does not stop the log.
- Ignore old disabled Tracking settings in all capture paths and migrate the legacy setting to true at startup.

275 CoreTests pass. Both builds have zero warnings/errors and pass InteropGuard. Staged while the game remains open. Native cause capture still needs an enabled test; no merge or public release.

# Experimental v0.1.4

- Keep new death messages that arrive before the health update instead of discarding them.
- Read the active game death label and preserve its original text and colors.
- Add a strict fallback for the explicit English death report, including killer, ability, damage type, killing damage and overkill. Unsupported reports remain text; nothing is guessed from boss names.
- Replace the packed resistance paragraph with colored cards. Show capped resistance first, then uncapped total, with capped cards first in the grid.
- Mark recorded killing damage and highlight matching resistance gaps with a red arrow and area-specific explanation.
- Give other defenses their own readable grid and show reassessed resistances beside their original values.
- Use the death's frozen zone level for advice when online defense snapshots omit area level.
- Rename Recording to Death tracking and explain its purpose.

275 CoreTests pass. Both builds have zero warnings/errors and pass InteropGuard. Live verification of this candidate is pending. No merge, publication or Nexus release.

# Experimental v0.1.3

- Removed nullable report hooks that prevented the game's death explanation.
- Added simple formatter observers and preserved original game callbacks.
- Replaced imported boss-guide prose with short original field notes and categorical attack facts.
- Kept source links collapsed and preserved uncertainty flags.

Live test restored the game's explanation but did not capture its cause in the mod. v0.1.4 addresses that remaining capture problem.


