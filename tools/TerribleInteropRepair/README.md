# Embedded Terrible CoreModule repair

CoreModuleRepair.cs and EmbeddedRepairBootstrap.cs are compiled directly into
each of the four Terrible mod assemblies. Only the mod DLL is distributed.
Mono.Cecil is resolved from the existing MelonLoader/net6 runtime; it is not bundled.

The module initializer runs during mod discovery, after assembly generation and
before dependency loading. The first Terrible mod normalizes CoreModule metadata;
other mods observe the SHA256 receipt and skip the unchanged file. Regenerated
files are normalized again. No types are removed, renamed, or merged.

Original hash-named backups and a receipt are created in Il2CppAssemblies.
Type/member signatures are compared before atomic replacement. No networking or telemetry.

Verified in a live Last Epoch launch with the original failing CoreModule restored
and zero plugins installed: embedded repair runs and all four mods initialize.
Tooltips loads 9/9 patches. This does not establish every gameplay feature.

The csproj in this folder is a test library; EmbeddedRepairBootstrap.cs is excluded
from that project so tests do not modify a game installation. Mod csprojs link both
source files. No separate repair DLL is part of any release package.

Tests: dotnet run --project tools/TerribleInteropRepair/tests -- <original-coremodule> <scratch-folder>
To undo normalization: close the game, remove the updated mod DLLs, restore the
matching .terrible-original-*.bak over CoreModule, and remove its repair receipt.
