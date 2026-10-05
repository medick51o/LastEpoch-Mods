# Terrible Cooldowns v1.0.2 validation

- Baseline GitHub source version: 1.0.1.
- Built against Last Epoch Unity 6000.4.8f1 and MelonLoader 0.7.3.
- Build: zero errors and warnings.
- Verified the Awake, activateCooldownBar, and deactivateCooldownBar hooks exist.
- Restored the original failing CoreModule and launched with only Cooldowns installed.
- Game log reports 0 Plugins, 1 Mod, successful embedded repair, support module loaded,
  and MedicK's Terrible Cooldowns v1.0.2 ready.
- User confirmed: "it loaded it works in game".
- Other installed mods were restored after the isolated test.
- Archive contains only medick_CooldownTracker.dll at its root.
