# Season 5 Terrible compatibility updates — 2026-10-02

Versions: Terrible Zoom 1.0.2; Terrible Inventory 2.0.2;
Terrible fog_OF_war 1.0.2; Terrible Tooltips 3.1.2.

Shared fix: all four archives contain the identical
Plugins/medick_Terrible_InteropRepair.dll v1.0.0. Only one copy is needed.
It rewrites generated CoreModule metadata after generation and before mod loads,
without deleting types. Original backups and a hash receipt are retained locally.
No generated Unity/game DLLs or MelonLoader DLLs are shipped.

Tooltips source incorporates the installed v3.1.1 implementation from commit
2271e27e4b3c47168f8705ce20319d12547d0c6f, then updates individualID to uint,
the parameterless ground-label hook, and the GlobalAssets master affix lookup.
Other mod sources use bf063c54eac16a5f56f17cd0c2ecd818172848e1 as their baseline.

Validation:
- All four mod builds and the repair plugin build: zero errors/warnings.
- Original CoreModule fails in a fresh .NET 6.0.32 load test.
- Rewritten CoreModule loads in a fresh .NET 6.0.32 process.
- Repair tests pass: backup integrity, unchanged-file no-op, repaired regeneration,
  and invalid-file preservation.
- Type/field/method signature comparison on the original manual repair: 40,513
  records per assembly and zero differences.
- The user confirmed all four mods worked after the initial shared repair.
- Live game launch verified: repair plugin ran before mods; all four new versions initialized; Tooltips loaded 9/9 patches. Full feature coverage still depends on gameplay testing.

The earlier gameplay log showed third-party FallenStar Improved Tooltips using
removed StashTabbedUIControls.instance and the old boolean ground-label hook.
That mod is not modified by these packages.

Each mod folder has an updated CHANGELOG.md and a BBCode changelog under docs/.
Archives are in each mod's release/ directory and are also provided separately.

