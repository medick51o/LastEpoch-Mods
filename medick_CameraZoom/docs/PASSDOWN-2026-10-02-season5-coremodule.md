# Passdown: Terrible Zoom "does not load" on Season 5 (review request)

From: Claude (diagnosis below). To: Codex, independent review.
Ask: confirm or refute the diagnosis, and say whether the Nexus reply (end of file) is safe to post.
"I could not tell" is a good answer. Do not post anything or edit the repo.

## The report
- Nexus bug report on MedicK's Terrible Zoom (`medick_CameraZoom.dll`), user BL8NT, title "Season 5 error - does not load".
- Mod version in the log: **v1.0.0** (the repo is at v1.0.1).
- Last Epoch Season 5 / patch 1.5 launched 2026-10-01. The report came in a few hours later.
- Log as pasted by the user (an HTML-mangled copy):

```
[21:54:04.867] [ERROR] Loading Melon Dependency Failed: System.BadImageFormatException: Could not load file or assembly 'UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. An attempt was made to load a program with an incorrect format.
File name: 'UnityEngine.CoreModule, Version=0.0.0.0, ...' ---> System.BadImageFormatException: Duplicate type with name '<>O' in assembly 'UnityEngine.CoreModule, Version=0.0.0.0, ...'.
   at System.Runtime.Loader.AssemblyLoadContext.LoadFromPath(...)
   at System.Runtime.Loader.AssemblyLoadContext.LoadFromAssemblyPath(String assemblyPath)
   at MelonLoader.Resolver.SearchDirectoryManager.Scan(String requestedName)
   at MelonLoader.Resolver.AssemblyManager.SearchAssembly(String requestedName, Version requestedVersion)
   at MelonLoader.Resolver.AssemblyManager.Resolve(AssemblyLoadContext alc, AssemblyName name)
   ...
   at MelonLoader.InternalUtils.DependencyGraph`1.TryLoad(AssemblyName assembly)
[21:54:04.881] [ERROR] Resolving Melon Dependency Failed: System.BadImageFormatException: Duplicate type with name '<>O' in assembly 'UnityEngine.CoreModule, ...'
   ... at MelonLoader.InternalUtils.DependencyGraph`1.TryResolve(AssemblyName assembly)
[21:54:05.181] [WARNING] Some Melons are missing dependencies ...
- 'medick_CameraZoom' is missing the following dependencies:
    - 'UnityEngine.CoreModule' v0.0.0.0
[21:54:05.192] medick_CameraZoom v1.0.0 by medick  Assembly: medick_CameraZoom.dll
[21:54:05.192] 1 Mod loaded.
```

## Claude's diagnosis (with confidence levels)
1. **HIGH:** this is not a code bug in Terrible Zoom. The CLR refuses to load a `UnityEngine.CoreModule.dll` because it contains two type definitions with the same full name, `<>O` (the compiler-generated cached-delegate class). Zoom only *references* CoreModule, and every Unity mod does, so the mod loads with a missing dependency and does nothing.
2. **MEDIUM:** the bad file is the interop `MelonLoader/Il2CppAssemblies/UnityEngine.CoreModule.dll` that MelonLoader regenerated after the 1.5 update. Other Unity 6000.4.x games have hit the same duplicate-`<>O` error with MelonLoader 0.7.x.
   - **Unverified:** that LE 1.5 is on Unity 6000.4.x. Last Epoch has been on Unity 6 since patch 1.2.6 (June 2025); the exact 1.5 version is not confirmed. The Unity version line at the top of the user's `Latest.log` would settle it.
3. **NOT RULED OUT:** a *stray* `UnityEngine.CoreModule.dll` in another MelonLoader search directory (`Mods/`, `UserLibs/`, `Plugins/`, the game root). The stack goes through `SearchDirectoryManager.Scan`, which loads by file path from search directories, so the bad file's location is unknown.

## What to verify (please)
- [ ] `SearchDirectoryManager.Scan` in MelonLoader 0.7.2/0.7.3: which folders does it scan, and in what order? Is `Il2CppAssemblies` one of them, or would a hit here mean a file outside `Il2CppAssemblies`? (Source: github.com/LavaGang/MelonLoader, `MelonLoader/Resolver/`.)
- [ ] Is duplicate `<>O` in a generated interop assembly a known Il2CppInterop / MelonLoader 0.7.x issue on Unity 6000.4.x? Does any MelonLoader release or nightly fix it?
  - Claude's sources: LavaGang/MelonLoader issues #1142 (closed, duplicate of #1159), #1148 (closed "Invalid"), #1159 (open, milestone 0.8.0). **Caveat:** #1159's visible text is about a CLR crash in another game, so it may not be the right tracking issue.
- [ ] What Unity version does Last Epoch 1.5 ship? (Patch notes, or `UnityPlayer.dll` file version.)
- [ ] Is mleem97/Il2CppAssemblyFixer (Apache-2.0) safe to recommend? It removes "unreferenced duplicate type definitions" from generated DLLs. Check what it deletes and whether it could break mods that bind to the removed copy. Claude has **not** reviewed its code.
- [ ] Do the other Terrible mods (Tooltips, Inventory, Cooldowns, fog_OF_war) fail the same way on 1.5? Expected yes if the cause is the generated CoreModule.

## Repo facts (medick51o/LastEpoch-Mods)
- `medick_CameraZoom/medick_CameraZoom.csproj` references `$(GM)UnityEngine.CoreModule.dll` from `MelonLoader/Il2CppAssemblies/` with `Private=false`, so the mod does not ship its own copy of CoreModule.
- Checked 2026-10-02: no Terrible release zip (any mod, any version in the repo) bundles a Unity, Il2Cpp, Harmony or MelonLoader DLL. Our packaging cannot have put a stray CoreModule in `Mods/`; another mod or a manual copy still could.

## Draft reply to the user (Nexus, BBCode) — please flag anything wrong or overstated
- Claims "this affects all MelonLoader mods on Season 5": true only if diagnosis 2 holds.
- Links #1159 as the tracking issue: see the caveat above.
- Recommends the third-party fixer "at your own discretion".
- Asks the user for the first ~20 lines of `Latest.log` (the Unity version line).

Suggested safer wording, if any claim does not hold up: "This looks like a MelonLoader problem with the Season 5 game update rather than the mod; can you send the top of your Latest.log so I can confirm?"

## Output wanted
1. Verdict on diagnosis 1, 2 and 3: CONFIRMED / REFUTED / UNKNOWN, with evidence.
2. Which Nexus reply claims to keep, soften or cut.
3. Is the fixer safe to recommend: yes / no / unknown, and why.
4. Anything Claude missed.
