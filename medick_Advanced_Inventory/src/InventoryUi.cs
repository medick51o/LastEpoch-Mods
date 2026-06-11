using System;
using System.Collections;
using System.Linq;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace medick_Terrible_Inventory
{
    // Footer buttons (STASH / STASH ALL / VENDOR) injected after the game's
    // Transfer and Sort — as native-skinned clones of the Sort button.
    //
    // v2 covenant with the game's UI: WE DO NOT TOUCH IT. No resizing
    // Transfer/Sort, no squeezing the footer layout, no currency-row
    // mutations (v1's TryCompactCurrency debug fossil is gone).
    internal static class InventoryUi
    {
        // Known Sort-button paths, newest first (relative to the hook component).
        static readonly string[] SortPaths =
        {
            "Tab Contents/Items Tab/Inventory Tab Footer Base/Left_Buttons_Container/Sort",
            "Tab Contents/Items Tab/Footer/Left_Buttons_Container/Sort",
            "Tab Contents/Items Tab/Inventory Tab Footer Base/Buttons/Sort",
            "Tab Contents/Items Tab/Inventory Tab Footer Base/Left_Buttons_Container/SortButton",
        };

        internal static Transform ButtonBar;   // the keep-alive target (see mod OnLateUpdate)

        static GameObject _stashBtn, _stashAllBtn, _vendorBtn;
        static bool _pathWarned;

        public static void Inject(EnableWovenEchoesTabIfRelevant panel)
        {
            try
            {
                if (panel.transform.Find("Tab Contents") == null && panel.transform.childCount == 0)
                    return;

                // Idempotent per panel instance: rebuild only when absent.
                Transform existing = FindOurButton(panel.transform);
                if (existing != null)
                {
                    ApplyVisibility();
                    return;
                }

                Transform sortTransform = null;
                foreach (string p in SortPaths)
                {
                    sortTransform = panel.transform.Find(p);
                    if (sortTransform != null) break;
                }
                if (sortTransform == null)
                {
                    if (!_pathWarned)
                    {
                        _pathWarned = true;
                        MelonLogger.Warning(
                            "inventory footer changed — footer buttons unavailable (a game update likely moved the Sort button)");
                    }
                    return;
                }

                GameObject sortGO = sortTransform.gameObject;
                Transform bar = sortGO.transform.parent;

                _stashBtn = MakeFooterButton(sortGO, bar, "medick_StashBtn", "STASH",
                    () =>
                    {
                        if (UIBase.instanceExists && UIBase.instance != null)
                            UIBase.instance.openStash(true, false);
                    });

                _stashAllBtn = MakeFooterButton(sortGO, bar, "medick_StashAllBtn", "STASH ALL",
                    () => MelonCoroutines.Start(StashAllCoroutine()));

                _vendorBtn = MakeFooterButton(sortGO, bar, "medick_VendorBtn", "VENDOR",
                    () =>
                    {
                        if (UIBase.instanceExists && UIBase.instance != null)
                            UIBase.instance.openShop(true);
                    });

                // Survivor from the Steam Deck saga: stops the layout group
                // inflating buttons at small viewports. The ONLY layout
                // property we set on a game object, and it's additive-safe.
                try
                {
                    var hlg = bar.GetComponent<HorizontalLayoutGroup>();
                    if (hlg != null)
                    {
                        hlg.childForceExpandWidth  = false;
                        hlg.childForceExpandHeight = false;
                    }
                }
                catch { }

                ButtonBar = bar;
                ApplyVisibility();

                // Build the teleport column with the same native template.
                TeleportMenu.Inject(panel.transform, sortGO);

                // First inventory open each session: silently prime the era
                // controllers so every teleport works without the map.
                TravelService.EnsurePrimed();

                Dbg.Log("footer buttons injected");
            }
            catch (Exception e)
            {
                MelonLogger.Error("footer injection failed: " + e);
            }
        }

        static Transform FindOurButton(Transform root)
        {
            foreach (string p in SortPaths)
            {
                Transform sort = root.Find(p);
                if (sort != null) return sort.parent.Find("medick_StashBtn");
            }
            return null;
        }

        static GameObject MakeFooterButton(GameObject template, Transform bar, string name,
            string label, Action onClick)
        {
            GameObject go = NativeClone.Button(template, bar, name, onClick);
            NativeClone.HideIcon(go);                      // Sort's dots overlap custom text
            NativeClone.SetLabel(go, label, 8f, 13f);      // auto-size into the native shape
            NativeClone.SetLayoutSize(go, 64f, 26f);       // sized for ours only — natives untouched
            return go;
        }

        // Per-button visibility — the April wish, applied live from settings.
        public static void ApplyVisibility()
        {
            SetActiveSafe(_stashBtn,    Prefs.ShowStash.Value);
            SetActiveSafe(_stashAllBtn, Prefs.ShowStashAll.Value);
            SetActiveSafe(_vendorBtn,   Prefs.ShowVendor.Value);
            TeleportMenu.ApplyVisibility();
        }

        public static bool AnyFooterButtonShown =>
            Prefs.ShowStash.Value || Prefs.ShowStashAll.Value || Prefs.ShowVendor.Value;

        static void SetActiveSafe(GameObject go, bool on)
        {
            try { if (go != null && go.activeSelf != on) go.SetActive(on); } catch { }
        }

        // ── Stash All ─────────────────────────────────────────────
        // The flagship. Uses the game's own quick-move at KG's 3-frame
        // server-safe cadence. Respects stash affinity; when the priority
        // tab is full the game drops items in the open tab (game behavior —
        // this is a dump button, not a sorting service).

        static IEnumerator StashAllCoroutine()
        {
            ItemContainersManager mgr = ItemContainersManager.Instance;
            if (mgr == null) yield break;
            ItemContainer inv = mgr.inventory;
            if (inv == null) yield break;

            if (UIBase.instanceExists && UIBase.instance != null)
                UIBase.instance.openStash(false, false);   // items need somewhere to go

            yield return null;                              // let the stash UI settle

            Vector2Int[] positions;
            try
            {
                positions = inv.content.ToArray()
                    .Select(e => e._Position_k__BackingField)
                    .ToArray();
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Stash All — could not read inventory: " + e.Message);
                yield break;
            }

            foreach (Vector2Int pos in positions)
            {
                try
                {
                    mgr.TryQuickMove(ContainerID.INVENTORY, ContainerID.STASH, pos, false, false);
                }
                catch { }
                yield return null;                          // 3-frame cadence — do not speed up;
                yield return null;                          // this is what keeps the server calm
                yield return null;
            }

            Dbg.Log($"Stash All complete — attempted {positions.Length} items");
        }
    }
}
