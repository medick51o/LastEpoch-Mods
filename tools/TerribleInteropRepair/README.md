# Terrible Interop Repair v1.0.0

Shared MelonLoader plugin included with the four Season 5 Terrible mod packages.
Place medick_Terrible_InteropRepair.dll in Last Epoch/Plugins, not Mods.
Install only one copy; all four ZIPs contain the identical plugin.

Tested assembly-loading workaround: Last Epoch Unity 6000.4.8f1, MelonLoader
0.7.3, .NET 6.0.32. Rewrites the generated UnityEngine.CoreModule metadata using
MelonLoader's bundled Mono.Cecil. It does not remove or rename types. No Unity
or MelonLoader DLL is distributed. There is no networking or telemetry.

Runs in OnPreModsLoaded, after generation and before mod dependency loading.
A SHA256 receipt skips unchanged repaired files. Regenerated files are repaired
again. A hash-named original backup is preserved before atomic replacement.
Type/member signature comparison rejects a rewrite that changes those signatures.
This comparison is not a proof of semantic equivalence for all possible DLLs.

Close the game before installing or uninstalling. To undo the shared repair,
remove the plugin, then restore the matching .terrible-original-*.bak over
MelonLoader/Il2CppAssemblies/UnityEngine.CoreModule.dll and remove its
.terrible-repair.sha256 receipt. A subsequent game update may require regeneration.

Build: dotnet build tools/TerribleInteropRepair -c Release
Override ML when your MelonLoader/net6 directory is elsewhere.
