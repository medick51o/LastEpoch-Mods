// ================================================================
//  medick_Advanced_Inventory  v1.3.0
//
//  Adds quality-of-life buttons to the inventory panel:
//    STASH    — opens the stash from anywhere on the map
//    TRADER   — opens the NPC vendor shop from anywhere
//    STASH ALL — moves all inventory items to the stash
//
//  Teleport column (left of inventory panel, grouped by era):
//    DIVINE ERA   — Circle of Fortune, Merchant's Guild, Champion's Gate
//    IMPERIAL ERA — Soulfire Bastion
//    RUINED ERA   — Lightless Arbor, Temporal Sanctum
//    END OF TIME  — Forgotten Knights, The Woven, The End of Time
//
//  Built with the help of Claude (Anthropic).
//  Inspired by war3i4i / KillingGodVH's LastEpochImprovements mod.
// ================================================================

using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(medick_Advanced_Inventory.AdvancedInventoryMod),
    "medick_Advanced_Inventory", "1.3.0", "medick")]
[assembly: MelonGame("Eleventh Hour Games", "Last Epoch")]

namespace medick_Advanced_Inventory
{
    public class AdvancedInventoryMod : MelonMod
    {
        // Kept alive so OnUpdate can force it visible when game hides it in controller mode
        internal static Transform ButtonBar;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("[AdvancedInventory] Loaded v1.3.0 — STASH / TRADER / STASH ALL + collapsible teleport menu active.");
        }

        public override void OnLateUpdate()
        {
            // The game hides Left_Buttons_Container every frame when controller input
            // is detected, which buries our injected buttons. LateUpdate runs AFTER
            // all game Updates so we always get the final say before the frame renders.
            // Guard: only force it active if the parent footer is still active
            // (i.e. inventory is actually open — don't fight a legitimately closed panel).
            if (ButtonBar == null) return;
            if (!ButtonBar.gameObject.activeSelf &&
                ButtonBar.parent != null &&
                ButtonBar.parent.gameObject.activeInHierarchy)
            {
                ButtonBar.gameObject.SetActive(true);
            }
        }
    }
}
