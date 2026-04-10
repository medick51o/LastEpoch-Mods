// ================================================================
//  InventoryButtonsPatch.cs
//
//  Hooks into EnableWovenEchoesTabIfRelevant.Awake() — this fires
//  each time the inventory panel is built — and injects three
//  buttons next to the existing Sort button:
//
//    STASH     (green)  → UIBase.instance.openStash(true, false)
//    TRADER    (gold)   → UIBase.instance.openShop(true)
//    STASH ALL (red)    → moves every inventory item to stash
// ================================================================

using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using Il2Cpp;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace medick_Advanced_Inventory
{
    [HarmonyPatch(typeof(EnableWovenEchoesTabIfRelevant), "Awake")]
    internal static class Patch_InventoryButtons
    {
        [HarmonyPostfix]
        public static void Postfix(EnableWovenEchoesTabIfRelevant __instance)
        {
            try
            {
                // Guard: only inject once per panel instance
                if (__instance.transform.Find("medick_StashBtn") != null) return;

                // Try KG's known path first, then fall back to alternatives
                // Path is relative to the EnableWovenEchoesTabIfRelevant transform
                string[] sortPaths = new[]
                {
                    "Tab Contents/Items Tab/Inventory Tab Footer Base/Left_Buttons_Container/Sort",
                    "Tab Contents/Items Tab/Footer/Left_Buttons_Container/Sort",
                    "Tab Contents/Items Tab/Inventory Tab Footer Base/Buttons/Sort",
                    "Tab Contents/Items Tab/Inventory Tab Footer Base/Left_Buttons_Container/SortButton",
                };

                Transform sortTransform = null;
                string foundPath = null;
                foreach (string p in sortPaths)
                {
                    sortTransform = __instance.transform.Find(p);
                    if (sortTransform != null) { foundPath = p; break; }
                }

                if (sortTransform == null)
                {
                    MelonLogger.Warning("[AdvancedInventory] Sort button not found via any known path. Logging hierarchy (4 levels):");
                    LogHierarchy(__instance.transform, 0, 4);
                    return;
                }

                MelonLogger.Msg($"[AdvancedInventory] Sort button found at path: {foundPath}");
                GameObject sortGO = sortTransform.gameObject;
                Transform parent = sortGO.transform.parent;
                MelonLogger.Msg($"[AdvancedInventory] Parent='{parent?.name}' SiblingIdx={sortGO.transform.GetSiblingIndex()} ParentChildCount={parent?.childCount}");

                // Stash button (green)
                CreateButton(sortGO, "medick_StashBtn", "STASH",
                    new Color(0.1f, 0.55f, 0.1f),
                    () =>
                    {
                        if (UIBase.instanceExists && UIBase.instance != null)
                            UIBase.instance.openStash(true, false);
                    });

                // Stash All button (red) — before Trader
                CreateButton(sortGO, "medick_StashAllBtn", "STASH ALL",
                    new Color(0.55f, 0.1f, 0.1f),
                    () => MelonCoroutines.Start(StashAllCoroutine()));

                // Trader button (gold — opens NPC vendor shop) — last
                CreateButton(sortGO, "medick_TraderBtn", "TRADER",
                    new Color(0.55f, 0.42f, 0f),
                    () =>
                    {
                        if (UIBase.instanceExists && UIBase.instance != null)
                            UIBase.instance.openShop(true);
                    });

                // Resize buttons:
                //   Transfer & Sort (the two game-built buttons) — narrower & shorter
                //   Our three buttons — keep current size but bump font up
                if (parent != null)
                {
                    for (int i = 0; i < parent.childCount; i++)
                    {
                        GameObject child = parent.GetChild(i).gameObject;
                        bool ours = child.name.StartsWith("medick_");
                        if (ours)
                            ResizeButton(child, 68f, 26f, 12f);   // our buttons: same box, bigger text
                        else
                            ResizeButton(child, 55f, 22f, 10f);   // Transfer/Sort: narrower & shorter
                    }
                }

                // Squeeze the button bar — nearly touching
                try
                {
                    HorizontalLayoutGroup barHlg = parent.GetComponent<HorizontalLayoutGroup>();
                    if (barHlg != null)
                    {
                        barHlg.spacing = 1f;
                        barHlg.padding.left  = 2;
                        barHlg.padding.right = 2;
                    }
                }
                catch { }

                TryCompactCurrency(__instance.transform);
                MelonLogger.Msg("[AdvancedInventory] Buttons injected and resized successfully.");
            }
            catch (Exception e)
            {
                MelonLogger.Error("[AdvancedInventory] Postfix exception: " + e);
            }
        }

        // ── Button factory — clones the Sort button, rewires it ──────────────

        private static void CreateButton(GameObject sortGO, string name, string label,
            Color bgColor, Action onClick)
        {
            Transform parent = sortGO.transform.parent;

            // Don't create duplicates
            if (parent.Find(name) != null) return;

            GameObject newBtn = GameObject.Instantiate(sortGO, parent);
            newBtn.name = name;

            // Remove the Sort-specific component so it won't interfere
            SortInventoryButton sortScript = newBtn.GetComponent<SortInventoryButton>();
            if (sortScript != null) GameObject.DestroyImmediate(sortScript);

            // Child 0 — background Image: set our colour
            try
            {
                Image bg = newBtn.transform.GetChild(0).GetComponent<Image>();
                if (bg != null) bg.color = bgColor;
            }
            catch { }

            // Child 1 — icon Image (the Sort dots): hide it so it doesn't overlap our label
            try
            {
                Transform iconTr = newBtn.transform.GetChild(1);
                if (iconTr != null) iconTr.gameObject.SetActive(false);
            }
            catch { }

            // Child 2 — label TMP_Text: remove localization, set our text
            try
            {
                Transform labelTr = newBtn.transform.GetChild(2);

                // Destroy the localisation component so our text sticks
                var loc = labelTr.GetComponent<UnityEngine.Localization.Components.LocalizeStringEvent>();
                if (loc != null) GameObject.DestroyImmediate(loc);

                TMP_Text tmp = labelTr.GetComponent<TMP_Text>();
                if (tmp != null) tmp.text = label;
            }
            catch { }

            // Wire up the Button click
            Button btn = newBtn.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(new Action(onClick));
            }
        }

        // ── Stash All coroutine ───────────────────────────────────────────────

        private static IEnumerator StashAllCoroutine()
        {
            ItemContainersManager mgr = ItemContainersManager.Instance;
            if (mgr == null) yield break;

            ItemContainer inv = mgr.inventory;
            if (inv == null) yield break;

            // Open the stash first so items have somewhere to go
            if (UIBase.instanceExists && UIBase.instance != null)
                UIBase.instance.openStash(false, false);

            yield return null; // let the stash UI settle

            // Snapshot positions to avoid modifying the collection while iterating
            Vector2Int[] positions;
            try
            {
                positions = inv.content.ToArray()
                    .Select(e => e._Position_k__BackingField)
                    .ToArray();
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[AdvancedInventory] StashAll — could not read inventory: " + e.Message);
                yield break;
            }

            foreach (Vector2Int pos in positions)
            {
                try
                {
                    mgr.TryQuickMove(ContainerID.INVENTORY, ContainerID.STASH, pos, false, false);
                }
                catch { }

                // Three-frame delay between each move — same cadence KG used
                yield return null;
                yield return null;
                yield return null;
            }

            MelonLogger.Msg($"[AdvancedInventory] Stash All complete — attempted {positions.Length} items.");
        }

        // ── Button resizer ────────────────────────────────────────────────────

        private static void ResizeButton(GameObject btn, float width, float height, float fontSize)
        {
            try
            {
                // Set preferred size via LayoutElement (respects HorizontalLayoutGroup)
                LayoutElement le = btn.GetComponent<LayoutElement>();
                if (le == null) le = btn.AddComponent<LayoutElement>();
                le.preferredWidth  = width;
                le.preferredHeight = height;
            }
            catch { }

            // Resize the background image's RectTransform too, for buttons without LayoutElement
            try
            {
                RectTransform rt = btn.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                    rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   height);
                }
            }
            catch { }

            // Shrink the label font
            try
            {
                for (int i = 0; i < btn.transform.childCount; i++)
                {
                    TMP_Text tmp = btn.transform.GetChild(i).GetComponent<TMP_Text>();
                    if (tmp != null) tmp.fontSize = fontSize;
                }
            }
            catch { }
        }

        // ── Currency bar compactor ────────────────────────────────────────────
        // Tries to remove spacing between the gold/favor/coin icons so they
        // sit flush on the right side of the footer.

        private static void TryCompactCurrency(Transform instance)
        {
            try
            {
                Transform footer = instance.Find("Tab Contents/Items Tab/Inventory Tab Footer Base");
                if (footer == null)
                {
                    MelonLogger.Warning("[AdvancedInventory] Footer not found — logging hierarchy:");
                    LogHierarchy(instance.Find("Tab Contents/Items Tab") ?? instance, 0, 3);
                    return;
                }

                // Compact the footer-level HLG itself — this controls gap between button bar and currency block
                HorizontalLayoutGroup footerHlg = footer.GetComponent<HorizontalLayoutGroup>();
                if (footerHlg != null)
                {
                    MelonLogger.Msg($"[AdvancedInventory] Footer HLG spacing was {footerHlg.spacing} — setting to 4");
                    footerHlg.spacing = 4f;
                }

                MelonLogger.Msg("[AdvancedInventory] Footer children:");
                for (int i = 0; i < footer.childCount; i++)
                {
                    Transform c = footer.GetChild(i);
                    MelonLogger.Msg($"  [{i}] '{c.name}'  active={c.gameObject.activeSelf}  children={c.childCount}");

                    if (c.name == "Left_Buttons_Container") continue;

                    // Log children so we can identify currency rows
                    for (int j = 0; j < c.childCount; j++)
                    {
                        Transform gc = c.GetChild(j);
                        MelonLogger.Msg($"      [{j}] '{gc.name}'  active={gc.gameObject.activeSelf}  children={gc.childCount}");
                    }

                    // Compact any HLG that isn't the button bar
                    HorizontalLayoutGroup hlg = c.GetComponent<HorizontalLayoutGroup>();
                    if (hlg != null)
                    {
                        MelonLogger.Msg($"    -> HLG spacing was {hlg.spacing} — setting to 2");
                        hlg.spacing = 2f;
                        hlg.padding.left  = 0;
                        hlg.padding.right = 2;

                        // Hide spacer GameObjects inside the currency row
                        for (int j = 0; j < c.childCount; j++)
                        {
                            Transform child = c.GetChild(j);
                            string n = child.name.ToLower();
                            if (n.Contains("spacer") || n.Contains("space") || n.Contains("gap") || n == "divider")
                            {
                                child.gameObject.SetActive(false);
                                MelonLogger.Msg($"    -> Hid spacer '{child.name}'");
                            }

                            // Also compact any nested HLG (e.g. each currency entry is its own row)
                            HorizontalLayoutGroup innerHlg = child.GetComponent<HorizontalLayoutGroup>();
                            if (innerHlg != null)
                            {
                                innerHlg.spacing = 1f;
                                MelonLogger.Msg($"    -> Inner HLG '{child.name}' spacing set to 1");
                            }
                        }
                    }

                    // Try LayoutElement — make it flexible so it hugs the right edge
                    LayoutElement le = c.GetComponent<LayoutElement>();
                    if (le != null)
                        MelonLogger.Msg($"    -> LayoutElement flexW={le.flexibleWidth} preferW={le.preferredWidth}");
                }
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[AdvancedInventory] TryCompactCurrency error: " + e.Message);
            }
        }

        // ── Hierarchy logger (for debugging path changes) ─────────────────────

        private static void LogHierarchy(Transform root, int depth = 0, int maxDepth = 99)
        {
            if (depth > maxDepth) return;
            string indent = new string(' ', depth * 2);
            MelonLogger.Msg(indent + root.name + (root.gameObject.activeSelf ? "" : " [INACTIVE]"));
            for (int i = 0; i < root.childCount; i++)
                LogHierarchy(root.GetChild(i), depth + 1, maxDepth);
        }
    }
}
