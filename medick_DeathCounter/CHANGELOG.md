# Latest experimental review: Terrible Death Log, Assessment and Counter

Staged v0.1.2, not installed or published. See docs/TERRIBLE-DEATH-EXPERIMENTAL-CHANGELOG.md for current player-facing notes and docs/CODEX-DEATH-FULL-REQUEST-REVIEW.md for evidence, limits and the native test queue. 214 CoreTests and both builds/guards pass. Historical notes below describe earlier candidates and their older verification state.
# Changelog: MedicK's Terrible Deaths

## Staged v0.1.2 review: reassessment history, not released

- Every reassessment saves a dated update linked to its original death. Current stats, advice, grades and the original capture used are frozen in a separate history file.
- Reassess previous updates repeatedly, review saved updates, return to the original death, and delete one update or all updates for that death after confirmation. Original death files, counters, other deaths and derived updates stay intact.
- Explained A/B/C/D or unrated defense progress grades use measured gaps, regressions and evidence coverage. Modeled resistance damage changes remain separate from unproven whole-fight survival odds.
- Resistance gaps rank first; health/ward advice requires supporting evidence. Each death preserves its own season/Legacy context.
- Current verification and native test queue are in docs/CODEX-DEATH-REASSESSMENT-HISTORY.md. No installation, merge or publication.

## v0.1.1: player resolution (not yet run in game)

Not verified in Last Epoch. Aimed at the monolith death that recorded nothing because the player object never resolved and the safety nets required that object.

- Player lookup tries `getPlayerActor`, then `getLocalPlayerHealth` and that health's actor, before the unproven names. Health is adopted even when the object route fails, and a missing health is tried again. One warning names the getters that failed, including the exception.
- `CharacterData.Deaths` is polled without a resolved player. The death screen and analytics signals are not gated on recording. A hooked death still does not count while readable health is above 0. Unreadable health waits for the game counter.
- A death is not filed under "Unknown Hero". The last real character name is kept.
- The review panel does not open with no character loaded. If it is already open, it says "Waiting for your character to load."
- Startup logs which of the five death-hook candidates this build actually has.

## v0.1.0: first build (not yet run in game)

Written on a machine without Last Epoch or MelonLoader. Core logic is unit-tested and the whole mod compiles against NuGet MelonLoader 0.7.2 / HarmonyX / Il2CppInterop / Unity 2021.3 with stubbed game types; the game hooks are educated guesses until the first in-hand launch (CURRENT-WORK.md).

- Always-on per-character death counter with a red flash and "killed by" toast; Shift + Insert hides it, shift-drag moves it.
- Last Death panel (Insert): killer, ability, killing blow and damage type, crit, death kind, ailments on you, last-5-seconds damage mix, top threats, up to five survival suggestions, and history navigation.
- Permanent log in `UserData/medick_DeathCounter/` (`deaths.txt` readable, `deaths.jsonl` full detail).
- Two independent death signals (hook + health watch) with a once-per-death latch; a hooked death never counts while health is above 0.
- Hooks resolved by name with per-hook degradation, `ProbeApi` dump and `HookOverrides` for fixing names without a rebuild.
- Hooks rewired to the real game API found in public Last Epoch mods (docs/RESEARCH-game-api.md): `ProtectionClass.ApplyDamage` for every hit and DoT tick (attacker, damage per type, crit/freeze/stun flags), `BaseHealth.HealthDamage`, `Dying.die`, `DeathScreen.toggle`, `AilmentReceiver.ApplyAilment*`, and the game's own `CharacterData.Deaths` counter as a third death signal. None of the first-draft guesses existed in the game.
- Character name, level, class and hardcore from `PlayerFinder.getPlayerData()`; the game's own death text shown as "Game says".
- Ailment and defense advice corrected from docs/RESEARCH-damage-and-defenses.md (Shock is a lightning resistance shred, not a damage-taken debuff; Frostbite, Time Rot, Doom and Damned riders; endurance; freeze).
- Patterns tab: build priorities across the last 20 deaths with "N of M deaths" counts and pattern-level wording, top killers, incoming damage by type (per death), ailments on you most, death kinds.
- Defences snapshot: your resistances (and headroom above the 75% cap), armor, dodge, block, endurance, crit avoidance and ward, read from the player's ProtectionClass while you are being hit. The advice names your numbers ("Cap void resistance: you had 38%, 37 points short"); a percent unit that cannot be told (75 vs 0.75) is never guessed.
- Built without the game (`-p:NoGame=true`) and checked by `tools/InteropGuard` against shipped Terrible DLLs; CI runs tests, build and guard on every push.
