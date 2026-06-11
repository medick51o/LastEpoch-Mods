using MelonLoader;

[assembly: MelonInfo(typeof(medick_FogOfWar.FogOfWarMod),
    medick_FogOfWar.BuildInfo.Name,
    medick_FogOfWar.BuildInfo.Version,
    medick_FogOfWar.BuildInfo.Author)]
[assembly: MelonGame("Eleventh Hour Games", "Last Epoch")]

namespace medick_FogOfWar
{
    // Fog of war control for Last Epoch — a 6-level vision dial injected
    // into the game's own settings screen. No overlay panel, no keybinds:
    // the mod lives where the game's options live. See SPEC.md.
    public partial class FogOfWarMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            Prefs.Init();
            ApplyPatches();   // manual, per-patch degradation — see Patches file
            MelonLogger.Msg(
                $"{BuildInfo.OfficialName} v{BuildInfo.Version} ready — vision level {Prefs.FogLevel.Value}");
        }

        // Leave the map exactly as the game owns it if the mod unloads.
        public override void OnDeinitializeMelon() => FogController.RestoreDefaults();

        public override void OnApplicationQuit() => Prefs.Save();
    }
}
