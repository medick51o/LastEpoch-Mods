using System;
using System.Collections.Generic;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace medick_Terrible_Inventory
{
    // The Quick Teleport menu: an always-visible master tab at the panel's
    // left border, expanding a collapsible column of native-skinned buttons
    // grouped FACTIONS / DUNGEONS / HUBS.
    //
    // Geometry constants are Andrew's hand-tuned April values (flush with
    // the weapon slots, clear of the panel's decorative border) — the layout
    // was always right; only the rendering was flat. v2 keeps the numbers
    // and replaces every visual with clones of the game's own button.
    // The column lives in a VerticalLayoutGroup (collapse = SetActive);
    // v1's hand-rolled Reflow list is retired.
    internal static class TeleportMenu
    {
        const string GUARD = "medick_TpMaster";

        // ── Andrew's geometry (do not retune without in-game evidence) ──
        const float BTN_H = 48f;
        const float HDR_H = 30f;
        const float GAP   = 3f;
        const float COL_W = 118f;
        const float COL_Y = -4f;
        const float TAB_X = 28f;
        const float TAB_W = 157f;
        const float TAB_H = 65f;
        const float COL_X = TAB_X - GAP - COL_W;

        // ── Destinations (scene names verified in-game; see ARCHAEOLOGY.md —
        //    Dun1Q10 = Temporal Sanctum, Dun2Q10 = Lightless Arbor; the v1.0
        //    swap is not to be re-introduced) ──
        static readonly (string line1, string line2, string scene, Color color)[][] Groups =
        {
            new[]   // FACTIONS
            {
                ("Circle of Fortune", "The Observatory", "Observatory", new Color(0.25f, 0.42f, 0.95f)),
                ("Merchant's Guild",  "The Bazaar",      "Bazaar",      new Color(0.90f, 0.25f, 0.25f)),
                ("Forgotten Knights", "Shattered Road",  "M_Knight",    new Color(0.62f, 0.30f, 0.90f)),
                ("The Woven",         "Haven of Silk",   "WeaversHub",  new Color(0.15f, 0.75f, 0.72f)),
            },
            new[]   // DUNGEONS
            {
                ("Lightless Arbor",   "DUNGEON",         "Dun2Q10",     new Color(0.45f, 0.40f, 0.65f)),
                ("Temporal Sanctum",  "DUNGEON",         "Dun1Q10",     new Color(0.55f, 0.35f, 0.95f)),
                ("Soulfire Bastion",  "DUNGEON",         "Dun3Q10",     new Color(0.95f, 0.42f, 0.18f)),
            },
            new[]   // HUBS
            {
                ("The End of Time",   "TOWN",            "EoT",         new Color(0.80f, 0.65f, 0.95f)),
                ("Champion's Gate",   "ARENA",           "ArenaLobby",  new Color(0.55f, 0.75f, 0.95f)),
            },
        };

        static readonly string[] GroupNames = { "FACTIONS", "DUNGEONS", "HUBS" };

        static GameObject _masterTab;
        static GameObject _column;
        static TMP_Text   _masterLabel;
        static bool       _columnOpen = true;
        static readonly bool[]       _groupOpen   = { true, true, true };
        static readonly TMP_Text[]   _groupLabels = new TMP_Text[3];
        static readonly List<(GameObject go, int group)> _items = new();

        public static void Inject(Transform panel, GameObject template)
        {
            try
            {
                if (panel.Find(GUARD) != null) { ApplyVisibility(); return; }   // this panel already built

                // Fresh panel instance: reset per-panel state.
                _items.Clear();
                _columnOpen = true;
                for (int i = 0; i < _groupOpen.Length; i++) _groupOpen[i] = true;

                // ── Master tab (always visible at the panel border) ──
                _masterTab = NativeClone.Button(template, panel, GUARD, ToggleColumn);
                NativeClone.HideIcon(_masterTab);
                NativeClone.SetRect(_masterTab, new Vector2(TAB_X, COL_Y), new Vector2(TAB_W, TAB_H));
                StripLayoutElement(_masterTab);
                NativeClone.SetRichLabel(_masterTab, MasterLabelText());
                _masterLabel = NativeClone.Label(_masterTab);

                // ── Column container (VerticalLayoutGroup owns the layout) ──
                _column = new GameObject("medick_TpColumn");
                _column.transform.SetParent(panel, false);
                var crt = _column.AddComponent<RectTransform>();
                crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0f, 1f);
                crt.anchoredPosition = new Vector2(COL_X, COL_Y);
                crt.sizeDelta        = new Vector2(COL_W, 0f);
                var vlg = _column.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = GAP;
                vlg.childForceExpandWidth  = false;
                vlg.childForceExpandHeight = false;
                vlg.childControlWidth      = true;
                vlg.childControlHeight     = true;
                vlg.childAlignment         = TextAnchor.UpperLeft;
                var fitter = _column.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                for (int g = 0; g < Groups.Length; g++)
                {
                    AddGroupHeader(template, g);
                    foreach (var d in Groups[g])
                        AddDestination(template, g, d);
                }

                ApplyGroupStates();
                ApplyVisibility();
                Dbg.Log("teleport menu injected");
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.Error("teleport menu injection failed: " + e);
            }
        }

        // ── Builders ──────────────────────────────────────────────

        static void AddGroupHeader(GameObject template, int group)
        {
            int g = group;
            GameObject go = NativeClone.Button(template, _column.transform, $"medick_TpHdr{g}",
                () => ToggleGroup(g));
            NativeClone.HideIcon(go);
            NativeClone.SetLayoutSize(go, COL_W, HDR_H);
            NativeClone.SetRichLabel(go, HeaderText(g));
            _groupLabels[g] = NativeClone.Label(go);
        }

        static void AddDestination(GameObject template, int group,
            (string line1, string line2, string scene, Color color) d)
        {
            string scene = d.scene;
            GameObject go = NativeClone.Button(template, _column.transform, $"medick_Tp_{scene}",
                () => TravelService.RequestTravel(scene));
            NativeClone.HideIcon(go);
            NativeClone.SetLayoutSize(go, COL_W, BTN_H);

            string hex = ColorUtility.ToHtmlStringRGB(d.color);
            NativeClone.SetRichLabel(go,
                $"<size=11.5><b>{d.line1}</b></size>\n<size=9><color=#{hex}>{d.line2}</color></size>");
            NativeClone.AddAccentBar(go, d.color);   // faction identity, native art untouched

            _items.Add((go, group));
        }

        // ── State ─────────────────────────────────────────────────

        static string MasterLabelText() =>
            _columnOpen ? "<b>< QUICK\nTELEPORT</b>" : "<b>> QUICK\nTELEPORT</b>";

        // ASCII arrows on purpose — the game's TMP font has no ▼/▶ glyphs.
        static string HeaderText(int g) =>
            $"<b><size=10.5>{(_groupOpen[g] ? "v" : ">")} {GroupNames[g]}</size></b>";

        static void ToggleColumn()
        {
            _columnOpen = !_columnOpen;
            if (_masterLabel != null) _masterLabel.text = MasterLabelText();
            ApplyVisibility();
        }

        static void ToggleGroup(int g)
        {
            _groupOpen[g] = !_groupOpen[g];
            if (_groupLabels[g] != null) _groupLabels[g].text = HeaderText(g);
            ApplyGroupStates();
        }

        static void ApplyGroupStates()
        {
            foreach (var (go, group) in _items)
            {
                try { go.SetActive(_groupOpen[group]); } catch { }
            }
        }

        // Master visibility: the ShowTeleport pref hides everything;
        // _columnOpen folds the column behind the master tab.
        public static void ApplyVisibility()
        {
            bool show = Prefs.ShowTeleport.Value;
            try { if (_masterTab != null && _masterTab.activeSelf != show) _masterTab.SetActive(show); } catch { }
            bool col = show && _columnOpen;
            try { if (_column != null && _column.activeSelf != col) _column.SetActive(col); } catch { }
        }

        static void StripLayoutElement(GameObject go)
        {
            try
            {
                var le = go.GetComponent<LayoutElement>();
                if (le != null) UnityEngine.Object.DestroyImmediate(le);
            }
            catch { }
        }
    }
}
