using System;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;

// Patches are applied manually so a game update renaming one method degrades
// that single feature with a warning instead of killing the whole mod at
// registration (assembly-wide PatchAll throws if ANY target is missing).
[assembly: HarmonyDontPatchAll]

namespace medick_Terrible_Inventory
{
    public partial class TerribleInventoryMod
    {
        internal void ApplyPatches()
        {
            TryPatch(typeof(Patch_InventoryAwake), "inventory panel (buttons + teleport menu)");
            TryPatch(typeof(Patch_SettingsAwake),  "settings panel (options category)");
        }

        void TryPatch(Type patchClass, string label)
        {
            try
            {
                new PatchClassProcessor(HarmonyInstance, patchClass).Patch();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    $"patch for {label} failed — that feature is disabled this session: {ex.Message}");
            }
        }

        // Fires every time the inventory panel is (re)built — the same hook
        // KG's mod used; injection is idempotent per panel instance.
        [HarmonyPatch(typeof(EnableWovenEchoesTabIfRelevant), nameof(EnableWovenEchoesTabIfRelevant.Awake))]
        private static class Patch_InventoryAwake
        {
            [HarmonyPostfix]
            public static void Postfix(EnableWovenEchoesTabIfRelevant __instance) =>
                InventoryUi.Inject(__instance);
        }

        // Inject the "Terrible Inventory" category into the game's settings.
        [HarmonyPatch(typeof(SettingsPanelTabNavigable), nameof(SettingsPanelTabNavigable.Awake))]
        private static class Patch_SettingsAwake
        {
            [HarmonyPostfix]
            public static void Postfix(SettingsPanelTabNavigable __instance) =>
                SettingsUi.Inject(__instance);
        }
    }
}
