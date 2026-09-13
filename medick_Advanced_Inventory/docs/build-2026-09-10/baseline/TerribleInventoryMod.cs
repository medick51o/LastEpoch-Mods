using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(medick_Terrible_Inventory.TerribleInventoryMod),
    medick_Terrible_Inventory.BuildInfo.Name,
    medick_Terrible_Inventory.BuildInfo.Version,
    medick_Terrible_Inventory.BuildInfo.Author)]
[assembly: MelonGame("Eleventh Hour Games", "Last Epoch")]

namespace medick_Terrible_Inventory
{
    // Quality-of-life inventory buttons (STASH / STASH ALL / VENDOR) and a
    // Quick Teleport menu — every visual cloned from the game's own UI.
    // See SPEC.md for the contract and ARCHAEOLOGY.md for the war stories.
    public partial class TerribleInventoryMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            Prefs.Init();
            ApplyPatches();   // manual, per-patch degradation — see Patches file
            MelonLogger.Msg(
                $"{BuildInfo.OfficialName} v{BuildInfo.Version} ready — STASH / STASH ALL / VENDOR + Quick Teleport");
        }

        // The game hides Left_Buttons_Container every frame while controller
        // input is detected (it swaps in Gamepad_Prompts_Container instead).
        // OnLateUpdate runs AFTER all game Updates, so we get the final say
        // before the frame renders — the load-bearing Steam Deck fix.
        // Guards: only while the footer itself is open (don't fight a closed
        // panel) and only while at least one of our buttons is enabled
        // (all hidden via settings → let the game behave natively).
        public override void OnLateUpdate()
        {
            if (!InventoryUi.AnyFooterButtonShown) return;
            var bar = InventoryUi.ButtonBar;
            try
            {
                if (bar == null) return;   // Unity-overloaded null: also true for destroyed
                if (!bar.gameObject.activeSelf &&
                    bar.parent != null &&
                    bar.parent.gameObject.activeInHierarchy)
                {
                    bar.gameObject.SetActive(true);
                }
            }
            catch { InventoryUi.ButtonBar = null; }   // collected reference — next panel re-injects
        }

        // The travel guard spans the scene transition; arrival releases it.
        public override void OnSceneWasLoaded(int buildIndex, string sceneName) =>
            TravelService.NotifySceneLoaded();

        public override void OnApplicationQuit() => Prefs.Save();
    }
}
