// DECOMPILED FROM: LastEpoch_Hud.dll v4.4.7
// NAMESPACE: LastEpoch_Hud.Scripts.Mods.Cosmetics
// CLASS: Cosmetics_Offline
//
// This is the complete cosmetics unlock implementation from Ash's LastEpoch_Hud mod.
// It patches CosmeticsManager.GetOwnedCosmetics() to return ALL cosmetic BackendIDs
// instead of only the ones the player has purchased.

using Il2Cpp;
using Il2CppLE.Services.Cosmetics;
using Il2CppCysharp.Threading.Tasks;
using Il2CppInterop.Runtime.Injection;
using Il2CppSystem.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace LastEpoch_Hud.Scripts.Mods.Cosmetics
{
    [RegisterTypeInIl2Cpp]
    public class Cosmetics_Offline : MonoBehaviour
    {
        // --- THE HARMONY PATCH ---
        [HarmonyPatch(typeof(CosmeticsManager), "GetOwnedCosmetics")]
        public class CosmeticsManager_GetOwnedCosmetics
        {
            [HarmonyPrefix]
            private static bool Prefix(ref CosmeticsManager __instance, ref UniTask<List<string>> __result)
            {
                // Build the list once, cache it forever
                if (list_id.IsNullOrDestroyed())
                {
                    // Find ALL Cosmetic ScriptableObjects loaded in memory
                    List<Cosmetic> list = new List<Cosmetic>();
                    foreach (Cosmetic item in Resources.FindObjectsOfTypeAll<Cosmetic>())
                    {
                        list.Add(item);
                    }
                    if (!list.IsNullOrDestroyed())
                    {
                        list_id = new List<string>();
                        foreach (Cosmetic item2 in list)
                        {
                            // Deduplicate by BackendID
                            if (!list_id.Contains(item2.BackendID))
                            {
                                list_id.Add(item2.BackendID);
                            }
                        }
                    }
                }
                // Return the complete list — game now thinks player owns everything
                __result = new UniTask<List<string>>(list_id);
                return false; // Skip the original method (no server call)
            }
        }

        // --- STATIC STATE ---
        public static bool Initialized;
        public static List<string> list_id;  // cached list of ALL cosmetic BackendIDs
        public static Cosmetics_Offline instance { get; private set; }

        public Cosmetics_Offline(IntPtr ptr) : base(ptr) { }

        private void Awake()
        {
            instance = this;
        }

        private void Update()
        {
            if (Scenes.IsGameScene())
            {
                if (!Initialized)
                {
                    Init();
                }
            }
            else
            {
                Initialized = false;
            }
        }

        // Hides the cosmetics store button from the bottom menu
        private void Init()
        {
            if (Refs_Manager.game_uibase.IsNullOrDestroyed())
                return;

            GameObject gameObject = ((Component)Refs_Manager.game_uibase.bottomScreenMenu).gameObject;
            if (gameObject.IsNullOrDestroyed())
                return;

            GameObject child = Functions.GetChild(gameObject, "BottomScreenMenuPanel");
            if (!child.IsNullOrDestroyed())
            {
                GameObject child2 = Functions.GetChild(child, "Cosmetics");
                if (!child2.IsNullOrDestroyed())
                {
                    child2.active = false;  // Hide the MTX store button
                    Initialized = true;
                }
            }
        }
    }
}
