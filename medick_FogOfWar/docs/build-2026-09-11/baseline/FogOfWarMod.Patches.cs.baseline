using System;
using HarmonyLib;
using Il2Cpp;
using Il2CppLE.UI.Minimap;
using MelonLoader;

// Patches are applied manually (see ApplyPatches below) so that one renamed
// game method degrades that single feature with a warning instead of taking
// the whole mod down: MelonLoader's assembly-wide PatchAll throws at
// registration if ANY target is missing, killing even cfg-only fog control.
[assembly: HarmonyDontPatchAll]

namespace medick_FogOfWar
{
    public partial class FogOfWarMod
    {
        internal void ApplyPatches()
        {
            TryPatch(typeof(Patch_MinimapAwake),       "Minimap.Awake (fog control)");
            TryPatch(typeof(Patch_MinimapUpdate),      "Minimap.Update (BLIND re-assert)");
            TryPatch(typeof(Patch_SettingsPanelAwake), "SettingsPanel.Awake (settings UI)");
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
                    $"patch {label} failed — that feature is disabled this session: {ex.Message}");
            }
        }

        // Each zone's minimap announces itself here: capture that zone's
        // default reveal radius, then apply the configured level.
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.Awake))]
        private static class Patch_MinimapAwake
        {
            [HarmonyPostfix]
            public static void Postfix(Minimap __instance) =>
                FogController.OnMinimapAwake(__instance);
        }

        // The game can re-activate a hidden minimap container without a new
        // Awake (overlay-map close, cutscene end) — and the interop Minimap
        // declares NO OnEnable to hook (verified against Il2CppLE.dll:
        // Awake, OnDestroy, Update only). So the re-assert rides Update: a
        // hidden minimap receives no Updates, an escaped one does, and the
        // controller fast-exits for every non-BLIND level.
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.Update))]
        private static class Patch_MinimapUpdate
        {
            [HarmonyPostfix]
            public static void Postfix(Minimap __instance) =>
                FogController.OnMinimapUpdate(__instance);
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
