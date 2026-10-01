# MedicK's Terrible Deaths

*a death counter that tells you why.* A [MelonLoader](https://melonwiki.xyz) mod for **Last Epoch**: an always-on death counter, a permanent log of every death, and a Last Death panel that tells you who killed you, with what, and what to build so it does not happen again.

> Internal name `medick_DeathCounter` · **v0.1.0, not yet tested in game** (see [CURRENT-WORK.md](./CURRENT-WORK.md))

## What you get

**The counter.** A small `DEATHS 12` pill at the top of the screen, per character. It flashes red after a death and shows `Killed by Lagon · Insert for details` for a few seconds.

**The Last Death panel.** Press **Insert** (or click the counter):

- **Killed by** who, with which ability, for how much and which damage type, and whether it was a crit
- **How you died:** ONE-SHOT, BURST, DAMAGE OVER TIME, or WORN DOWN
- **Ailments on you:** Ignite, Bleed, Poison, Freeze, Shock, Armor Shred ...
- **Damage taken in the last 5 seconds** by damage type, and the **top threats**
- **To survive next time:** up to five suggestions picked from what actually killed you (cap fire resistance, stack armor, get critical strike avoidance to 100%, out-heal damage over time, stun avoidance ...)
- **‹ ›** to walk back through every earlier death of this character

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
| Shift-drag the counter | Move it |

## Settings

`UserData/medick_DeathCounter.cfg`:

| Entry | Default | |
|---|---|---|
| `Tracking` | `true` | Master switch: off = nothing is counted or logged |
| `ShowCounter` | `true` | The counter pill (Shift + PanelKey toggles it) |
| `ShowDeathToast` | `true` | "Killed by" line after a death |
| `PanelKey` | `Insert` | Any Unity KeyCode name |
| `HudX` / `HudY` | `0.5` / `0.015` | Counter position as a screen fraction |
| `HudScale` | `1.0` | Counter and panel size |
| `ProbeApi` | `false` | Dump the game's health/death/ailment API to the log at startup |
| `HookOverrides` | | Extra hooks, e.g. `hit:Il2Cpp.BaseHealth.ReceiveDamage; death:Il2Cpp.PlayerHealth.Die` |
| `DebugLog` | `false` | Verbose log |

## How it works

- **Death detection** has two independent signals: a hooked death method on the player, and a health watch that sees health cross from above 0 to 0. Either one records the death; a latch makes sure it counts once. A hooked "death" is ignored if the player's health is still above 0, so a wrong hook cannot invent deaths.
- **Hits** come from a hook on the player's health component. Damage is measured as health before minus health after, so it is what you actually lost after armor, resistances, block and ward. Nested hooked calls merge into one hit.
- **Game API by name.** The mod only hard-references game types earlier Terrible mods proved (`PlayerFinder`, `Actor`, `EpochInputManager`). Health, damage and ailment members are found by name at runtime, so a game patch that renames something costs one feature and a log warning, not a crash, and `HookOverrides` can fix it without a rebuild.
- **Suggestions** are computed when you view a death, from raw facts in the log, so improving the advice improves old deaths too.

## Installation

1. Install MelonLoader 0.7.x
2. Drop `medick_DeathCounter.dll` into `Last Epoch/Mods/`

## Building from source

```bash
dotnet build medick_DeathCounter -c Release -p:DeployToMods=false
```

Without the game installed (cloud box, CI):

```bash
dotnet run --project medick_DeathCounter/tests/CoreTests      # unit tests for the death analysis, advice and log
dotnet build medick_DeathCounter/tests/CompileCheck           # compiles the whole mod against NuGet MelonLoader/Unity + stubs
```

```
medick_DeathCounter/
  medick_DeathCounter.csproj
  src/
    DeathCounterMod.cs     MelonMod lifecycle, keys
    BuildInfo.cs / Prefs.cs
    Core/                  game-free: Elements + Ailments, HitEvent/HitBuffer, DeathAnalyzer, Advisor, DeathLog
    Game/                  PlayerProbe, GameHooks (Harmony taps by name), ArgReader, DeathTracker, Refl
    UI/                    Theme, CounterHud, DeathPanel, InputBlocker
  tests/
    CoreTests/             no-dependency test runner over src/Core
    CompileCheck/          builds src/ with NuGet references and GameStubs.cs
```
