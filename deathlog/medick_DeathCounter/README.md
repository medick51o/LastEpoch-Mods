> v0.1.14 experimental candidate: rewritten Maxroll boss lines, ward-aware endurance and death kinds, and safer death capture. See CHANGELOG.md. Built and tested on Linux without the game, not installed.

> Experimental v0.1.8 Field journal candidate. Built and checked, not installed or published. See [the review](docs/CODEX-DEATH-v0.1.8-REVIEW.md) for required in-game tests. Insert opens the journal; Shift+Insert changes counter visibility only. Logging stays automatic. The views are Last death, History, Patterns, Boss notes and Settings. Boss notes remain experimental.

# Terrible Death Log, Assessment and Counter

Experimental MelonLoader mod for Last Epoch. A movable death counter, permanent character history, and advice based on captured death details and defenses. Internal name: Medick death log. DLL/config/log identifier: medick_DeathCounter.

**v0.1.7 is a local test candidate, not a Nexus release.** Read [CURRENT-WORK.md](CURRENT-WORK.md) and [the review notes](docs/CODEX-DEATH-v0.1.7-REVIEW.md) for actual checks and remaining limitations.

## Controls

| Input | Action |
|---|---|
| Insert | Open or close the panel |
| Shift + Insert | Show or hide the counter |
| Click counter | Open the panel |
| Hover MOVE handle or Move counter button | Drag the counter and save its position |

Options adjust counter/text size, position, visibility and death notifications. Deaths are always counted and saved automatically. Hiding the counter does not stop logging. The pause switch has been removed; old Tracking=false configurations no longer disable logging and are migrated to true at startup. Reset death counter resets the displayed count, with Medick's judgment warning, while retaining saved deaths and the game's count.

## Death history and defenses

Last death and History show whatever the mod actually captured: attacker, ability, damage type, damage, overkill, original game text, recorded hit/ailment information, defense snapshot, zone and season/Legacy context. Missing information stays unknown. A server-reported damage number is not silently turned into a measured client health-loss timeline.

Resistance cards show effective values first and overcap totals below, in character-sheet colors. Capped cards appear first. Red arrows mark matching uncapped defenses supported by the recorded damage. A gap may have contributed; closing it does not guarantee surviving the attack. Other defenses have separate values rather than a packed paragraph. Buffs and debuffs can change between snapshots.

The counter detects deaths independently of damage capture. v0.1.3 restored the game's own death explanation, but its tested death still lacked cause details in the mod. v0.1.4 retains messages arriving before the health update and falls back to explicit English report labels when formatter helpers are unavailable. Other translations keep their complete text without inventing structured details. v0.1.4 captured two verified native deaths. The v0.1.7 history-safety candidate still needs native verification.

## Reassessment

Choose a saved death while the same character is alive, then use Reassess and save new update. Each successful click saves a separate dated reading, advice, comparisons and defense progress grade. Saved updates can be reviewed or deleted individually; Delete all these updates is scoped to the selected death. Original death files are not replaced by reassessment. A deleted parent does not delete its child updates.

The grade describes captured defensive progress, not a calibrated probability of surviving a fight. Unknown cause, missing stats, temporary ward, returning debuffs, movement and unseen attacks limit the assessment. A high grade does not make a boss fight safe. Late genuine game reports can enrich an original death; previously saved reassessments retain their original capture and calculations.

## Boss tips

Experimental and still gathering data. Browse 54 encounters, including the ten timeline Harbingers. There are 32 authored preparation plans, 95 short reminders, 302 categorical attack facts and 45 uncertainty flags. Source links are collapsed by default. Community facts are not verified game data and do not overwrite a captured death or rate a whole fight. Raw research, guide quotes and copied guide prose are excluded from release/source packages.

Use Contribute on GitHub or [the project repository](https://github.com/medick51o/LastEpoch-Mods) to help collect data. References do not imply affiliation or endorsement. Exact boss flags and unique attacker aliases are required for a confirmed boss link. A name match alone can offer an attacker guide while leaving boss classification unknown.

## Files and build

The release ZIP contains only medick_DeathCounter.dll. Configuration and history are created automatically under the game directory:

| File | Purpose |
|---|---|
| UserData/medick_DeathCounter.cfg | Configuration, including Tracking and ProbeApi |
| UserData/medick_DeathCounter/deaths.jsonl | Original death records |
| UserData/medick_DeathCounter/deaths.txt | Readable death log |
| UserData/medick_DeathCounter/reassessments.jsonl | Separate saved updates |
| UserData/medick_DeathCounter/counter-resets.json | Display-counter reset history |

Open log folder opens that directory. Back it up before manually editing history. Existing identifiers are retained for compatibility.

From the source checkout, use a .NET 8 SDK. The mod targets the game's .NET 6 runtime:

```powershell
dotnet run --project medick_DeathCounter/tests/CoreTests
dotnet build medick_DeathCounter -c Release -p:NoGame=true -p:DeployToMods=false -warnaserror
dotnet build medick_DeathCounter -c Release -p:DeployToMods=false -warnaserror
```

Run tools/InteropGuard against each corresponding build, with the existing Application.OpenURL allowance. The local build reads your installed MelonLoader/Unity proxies through the project ML/GM paths. Always specify DeployToMods=false while building; install a reviewed candidate separately after closing the game. The single DLL contains the Terrible CoreModule repair. Do not replace the MelonLoader runtime or detour native Nullable report signatures.


