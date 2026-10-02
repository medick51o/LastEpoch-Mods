# Terrible Death Log, Assessment and Counter

*a death counter that tells you why.* A [MelonLoader](https://melonwiki.xyz) mod for **Last Epoch**: a compact death counter, a permanent log, captured causes and evidence-based advice to prepare for another attempt.

> Experimental v0.1.2 review source. Advice, reassessments, season/zone context and boss tips are staged and not yet verified in game. Read [CURRENT-WORK.md](./CURRENT-WORK.md) for the latest checks. Assembly/config identifier remains `medick_DeathCounter`.

## Original deaths and saved reassessments

Open the panel with **Insert**, choose a death, and click **Reassess and save new update** while that character is alive. Every successful click saves a separate dated update with current stats, comparisons, advice and a defense progress grade. Choose **Saved updates** to review older assessments, or **View original death** to return to the actual death. Reassessing an older update creates another update and compares it with both the original death and the selected assessment. There is no artificial update limit.

Reassessments never replace `deaths.jsonl` or `deaths.txt`, increment the death counter, or enter Patterns. Each update preserves the original capture it used, its stat reading, recommendation text, grade and calculation version. Game-supplied late killer details can still enrich the original death without changing saved reassessments. Death-time season/Legacy labels stay with their original records after a transfer to Legacy.

**Delete this update** removes only that reassessment after confirmation. **Delete all these updates** removes the selected death's reassessments. Neither deletes the original death or another death's updates. A deleted parent update does not remove the updates derived from it. Data is created automatically in `UserData/medick_DeathCounter/reassessments.jsonl`; the review ZIP still contains only the DLL.

The **defense progress grade** is an explained assessment of captured defensive changes:

| Grade | Meaning |
|---|---|
| A | Relevant core checks are covered, max health has not fallen, and a measured defense improved without a major supported risk remaining in the advice |
| B | Core checks are covered, but improvement, heavy-hit, recovery, debuff or capture uncertainties remain |
| C | A known gap remains, or a measured tradeoff needs review |
| D | Known gaps remain and a measured defense has worsened |
| ? | The cause or relevant current stats are insufficient to rate |

These are defense progress grades, not calibrated fight survival probabilities. Resistance changes can show modeled damage-reduction percentages when the old/current resistance and original area level are known. Missing values, instantaneous ward, unverified overkill units, unseen attacks, movement and returning debuffs prevent a reliable whole-fight survival percentage. A grade A is not a promise that the next fight is safe. See [full request review](docs/CODEX-DEATH-FULL-REQUEST-REVIEW.md) for the latest verification and native test queue.

## Zone context and Boss tips

The Boss tips tab contains five starter profiles: Lagon, Emperor of Corpses, Heorot, Chronomancer Julra and Harbinger of Hatred. Mechanics and source links are visible; tactical recommendations are labeled inferences. Julra has published encounter element coverage. Other profiles leave unverified typing unknown, and captured killing-blow elements stay separate from catalog data.

A confirmed boss link needs the game's explicit boss flag and an exact attacker alias. An offline/local report with a matching name can offer an Attacker guide while keeping boss classification unknown. Manual browsing never changes a saved death. Adds, proxy actors and unmatched variants are not guessed. English aliases are a starter set, not a complete localized boss roster.

Deaths preserve the exact internal scene ID and positive game zone level when available. The UI labels this Scene because a localized area lookup has not been established. Scene names do not prove boss encounters. A live death's context freezes before respawn; a game-counter fallback after respawn leaves missing original context unknown instead of using town stats. See [full request review](docs/CODEX-DEATH-FULL-REQUEST-REVIEW.md).

## What you get

**The counter.** A compact number with faint death text when idle. Hover to reveal its purpose and MOVE handle, or use **Move counter** in the panel. Its position is saved. A brief death notification can be enabled in Options.

**The Last Death panel.** Press **Insert** (or click the counter):

- **Killed by** who, with which ability, for how much and which damage type, and whether it was a crit
- **How you died:** ONE-SHOT, BURST, DAMAGE OVER TIME, or WORN DOWN
- **Ailments on you:** Ignite, Bleed, Poison, Freeze, Shock, Armor Shred ...
- **Recorded health loss in the last 5 seconds**, incoming damage mix and top threats. Ward-only damage is not in this timeline; type shares are not measured post-mitigation amounts.
- **To survive next time:** up to three distinct actions based on the captured cause and defense gaps (cap fire resistance, stack armor, get critical strike avoidance to 100%, out-heal damage over time, stun avoidance ...)
- **‹ ›** to walk back through every earlier death of this character

**The Patterns tab.** Across this character's last 20 deaths:

- **Build priorities:** up to three distinct defensive priorities supported by recorded deaths, each with its count ("Cap fire resistance (75%): 4 of 8 deaths"). A specific defence (a resistance, armor, crit avoidance, an ailment counter) leads over generic advice unless the generic one is clearly more common.
- Who keeps killing you, incoming damage by type (each death weighted equally), the ailments on you most, and the mix of death kinds.

The earlier mockup in mockups/ predates the redesigned panel. Native visual review is pending.

**The log.** Every death, forever, in `UserData/medick_DeathCounter/`:

| File | What |
|---|---|
| `deaths.txt` | One readable line per death |
| `deaths.jsonl` | Full detail, one JSON record per line (source of truth) |

The panel has an **OPEN LOG FOLDER** button.

## Controls

| Input | Does |
|---|---|
| `Insert` | Open / close the Last Death panel |
| `Shift + Insert` | Show / hide the counter |
| Click the counter | Open / close the panel |
| Drag the hover MOVE handle or use Move counter | Reposition and save the counter |

## Settings

`UserData/medick_DeathCounter.cfg`:

| Entry | Default | |
|---|---|---|
| `Tracking` | `true` | Master switch: off = nothing is counted or logged |
| `ShowCounter` | `true` | The counter pill (Shift + PanelKey toggles it) |
| `ShowDeathToast` | `true` | "Killed by" line after a death |
| `PanelKey` | `Insert` | Any Unity KeyCode name |
| `HudX` / `HudY` | `0.5` / `0.015` | Counter position as a screen fraction |
| `HudScale` | `1.0` | Counter size |
| `PanelScale` | `1.0` | Death log text/panel size, up to 1.4x |
| `ProbeApi` | `false` | Dump the game's health/death/ailment API to the log at startup |
| `HookOverrides` | | Extra hooks, e.g. `hit:Il2Cpp.BaseHealth.ReceiveDamage; death:Il2Cpp.PlayerHealth.Die` |
| `DebugLog` | `false` | Verbose log |

## How it works

- **Death detection** uses three independent signals, and a latch makes sure each death counts once:
  1. **Hooks** on the player's `Dying.die`, `ActorSync.receiveDeath` and `ActorVisuals.Die`, plus a hit that takes health to 0. The death screen opening (`DeathScreen.toggle`) and `AnalyticsManager.PlayerDeath` also count, but only while the player's health reads 0 or below.
  2. **A health watch:** health crossing from above 0 to 0.
  3. **The game's own death counter** (`CharacterData.Deaths`) going up. This works even if every hook name is wrong, and in online play.

  A hooked death is ignored while the player's health is still above 0, so a wrong hook cannot invent deaths.
- **Hits** come from `ProtectionClass.ApplyDamage(DamageStats, DamageSource, float, Actor attacker, bool)`, the call every hit and every damage-over-time tick goes through on the player. It gives the attacker, the damage per type (`DamageStats.damage`), whether it was a DoT tick (`isHit`, or an `ActiveAilment` as the source), and the crit, freeze and stun flags (`HitEvents`). The damage recorded is health before minus health after, which is what you actually lost after armor, resistances, block and ward. `BaseHealth.HealthDamage` catches anything that bypasses it, and nested calls merge into one hit.
- **Ailments** come from `AilmentReceiver.ApplyAilment` and its variants (`AilmentID` names), plus the freeze and stun flags on hits.
- **Who you are** comes from `PlayerFinder.getPlayerData()`: name, level, class, hardcore. Validated death-report callbacks supply the game's own death text. Previous static death text is not reused as a new cause.
- **Game API by name.** The mod references no game assembly. Every name above is looked up at runtime, so a game patch that renames something costs one feature and a log warning, not a crash, and `HookOverrides` can add hooks without a rebuild. Sources for every name are in `docs/RESEARCH-game-api.md`.
- **Suggestions** are computed when you view a death, from raw facts in the log, so improving the advice improves old deaths too. The mechanics behind them are in `docs/RESEARCH-damage-and-defenses.md`.

## Installation

1. Install MelonLoader 0.7.x
2. Drop `medick_DeathCounter.dll` into `Last Epoch/Mods/`

## Building from source

On a PC with the game, build without installing:

```bash
dotnet build medick_DeathCounter -c Release -p:DeployToMods=false -warnaserror
```

Without the game (cloud box, CI), build against the NuGet dependency set. The local review ZIP uses the build against the installed game dependencies:

```bash
dotnet run --project medick_DeathCounter/tests/CoreTests                 # unit tests: analysis, advice, log
dotnet build medick_DeathCounter -c Release -p:NoGame=true -p:DeployToMods=false -warnaserror
dotnet run --project tools/InteropGuard -- medick_DeathCounter/bin/Release/net6.0/medick_DeathCounter.dll . "UnityEngine.CoreModule: UnityEngine.Application::OpenURL (Method)"
```

`InteropGuard` checks that every Unity call the DLL makes also appears in a Terrible mod DLL that already shipped and ran in game. A plain-Unity build can otherwise call members the game's IL2CPP build stripped, or read a field the game exposes as a property, and fail at runtime. GitHub Actions (`.github/workflows/death-counter.yml`) runs all three on every push and uploads the DLL.

```
medick_DeathCounter/
  medick_DeathCounter.csproj     default = game refs, -p:NoGame=true = NuGet refs
  src/
    DeathCounterMod.cs     MelonMod lifecycle, keys
    BuildInfo.cs / Prefs.cs
    Core/                  game-free: Elements + Ailments, HitEvent/HitBuffer, DeathAnalyzer, Advisor,
                           DeathPatterns, DeathCountLedger, DeathLog
    Game/                  PlayerProbe, GameHooks (Harmony taps by name), ArgReader, DeathTracker, Refl
    UI/                    Theme, CounterHud, DeathPanel, InputBlocker
  tests/CoreTests/         no-dependency test runner over src/Core
  mockups/                 build.py renders the panel from real Core output (sample/ = sample fights)
  docs/                    research notes (damage mechanics, game API)
```
