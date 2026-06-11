using HarmonyLib;
using Il2Cpp;
using Il2CppLE.UI.Minimap;

namespace medick_FogOfWar
{
    // Harmony patches live nested inside the partial MelonMod class —
    // MelonLoader's auto-discovery reliably finds them there (top-level
    // patch classes have silently failed to register in other LE mods).
    public partial class FogOfWarMod
    {
        // Each zone's minimap announces itself here: capture that zone's
        // default reveal radius, then apply the configured level.
        [HarmonyPatch(typeof(Minimap), "Awake")]
        private static class Patch_MinimapAwake
        {
            [HarmonyPostfix]
            public static void Postfix(Minimap __instance) =>
                FogController.OnMinimapAwake(__instance);
        }

        // Inject the "Terrible fog_OFwar" category into the game's own
        // settings screen.
        [HarmonyPatch(typeof(SettingsPanelTabNavigable), nameof(SettingsPanelTabNavigable.Awake))]
        private static class Patch_SettingsPanelAwake
        {
            [HarmonyPostfix]
            public static void Postfix(SettingsPanelTabNavigable __instance) =>
                SettingsUi.Inject(__instance);
        }
    }
}
