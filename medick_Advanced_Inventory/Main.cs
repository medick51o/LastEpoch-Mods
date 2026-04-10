// ================================================================
//  medick_Advanced_Inventory  v1.1
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
    "medick_Advanced_Inventory", "1.2.0", "medick")]
[assembly: MelonGame("Eleventh Hour Games", "Last Epoch")]

namespace medick_Advanced_Inventory
{
    public class AdvancedInventoryMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("[AdvancedInventory] Loaded v1.2.0 — STASH / TRADER / STASH ALL + collapsible teleport menu active.");
        }
    }
}
