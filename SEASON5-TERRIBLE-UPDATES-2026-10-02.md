# Season 5 Terrible compatibility updates — 2026-10-02

Versions: Terrible Zoom 1.0.2; Terrible Inventory 2.0.2;
Terrible fog_OF_war 1.0.2; Terrible Tooltips 3.1.2.

Each archive contains exactly one DLL at its root. Install by replacing that DLL
in Last Epoch/Mods. No Plugins folder changes, scripts, or extra libraries are needed.
The CoreModule normalization repair is embedded directly into each mod, using
Mono.Cecil already bundled with MelonLoader 0.7.3. Generated Unity/game DLLs are not shipped.

The module initializer runs during mod discovery, after assembly generation.
The first mod normalizes the generated CoreModule metadata without deleting types.
Later mods skip the unchanged file using a SHA256 receipt. Regenerated files are
repaired again. Original backups are created locally before atomic replacement.

Tooltips includes the installed v3.1.1 source from commit
2271e27e4b3c47168f8705ce20319d12547d0c6f, then updates individualID to uint,
the parameterless ground-label hook, and GlobalAssets master affix lookup.

Validation:
- All four mod builds: zero errors/warnings.
- Original CoreModule fails in .NET 6.0.32; normalization makes it load.
- Repair tests verify backup integrity, idempotence, regenerated-file repair,
  and preservation of invalid input.
- Live game test restored the original broken CoreModule and removed the separate
  repair plugin. The log reports 0 Plugins, successful embedded normalization,
  all four updated mods initialized, and Tooltips 9/9 patches loaded.
- Full gameplay feature coverage is not established by startup verification.

The earlier plugin-based archives at these version numbers were replaced before
Nexus publication to meet the requested one-DLL installation footprint.
If you installed the earlier test plugin, close the game and remove
Plugins/medick_Terrible_InteropRepair.dll; it is no longer needed.

FallenStar Improved Tooltips is a separate third-party mod with its own Season 5
API failures. These releases do not modify it.

Each mod has CHANGELOG.md and docs/NEXUS-CHANGELOG-v<version>.txt for Nexus.
