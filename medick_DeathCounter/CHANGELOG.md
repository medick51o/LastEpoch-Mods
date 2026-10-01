# Changelog: MedicK's Terrible Deaths

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
- Built without the game (`-p:NoGame=true`) and checked by `tools/InteropGuard` against shipped Terrible DLLs; CI runs tests, build and guard on every push.
