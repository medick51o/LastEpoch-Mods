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

        // Shared top edge for the tab AND the column — aligned with the
        // helmet/amulet equipment row so the tab mirrors the amulet slot.
        // Tuning log: -4 (too low) → 62 (overshot, pass #3) → 22.
        const float TOP_Y = 22f;

        // Master tab sits to the RIGHT of the panel's decorative border bar
        // (~52 canvas units wide) so the frame art can't cover its text.
        const float TAB_X = 76f;
        const float TAB_W = 168f;
        const float TAB_H = 70f;

        // The column does NOT follow the tab — frozen at the original anchor
        // (28 - GAP - COL_W); only its TOP rides TOP_Y with the tab.
        const float COL_X = -93f;

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
        // Since the Oct 2026 season the game builds TWO inventory panels, so the menu is
        // injected twice; the singletons above only held the LAST one and the visible
        // panel's minimize toggled the hidden copy (Andrew 21:19). Every built copy is
        // registered here and all toggles act on all live copies.
        static readonly List<(GameObject master, GameObject column, TMP_Text label)> _menus = new();
        static readonly List<TMP_Text>[] _allGroupLabels = { new(), new(), new() };

        static void PruneDead()
        {
            _menus.RemoveAll(m => m.master == null || m.column == null);
            _items.RemoveAll(i => i.go == null);
            foreach (var l in _allGroupLabels) l.RemoveAll(t => t == null);
        }

        public static void Inject(Transform panel, GameObject template)
        {
            GameObject master = null, column = null;
            try
            {
                if (panel.Find(GUARD) != null) { ApplyVisibility(); return; }   // this panel already built

                // Fresh panel instance: drop entries of destroyed panels only (other live
                // copies stay registered). _columnOpen/_groupOpen survive rebuilds — the
                // player's collapse choices carry across zone changes.
                PruneDead();

                // Built into LOCALS: a failed second copy must never clean up the first
                // copy's objects (review, Codex 2026-10-08). Registered only on success.
                // ── Master tab (always visible at the panel border) ──
                master = NativeClone.Button(template, panel, GUARD, ToggleColumn);
                NativeClone.HideIcon(master);
                NativeClone.SetRect(master, new Vector2(TAB_X, TOP_Y), new Vector2(TAB_W, TAB_H));
                StripLayoutElement(master);
                NativeClone.SetRichLabel(master, MasterLabelText());
                TMP_Text masterLabel = NativeClone.Label(master);

                // ── Column container (VerticalLayoutGroup owns the layout) ──
                column = new GameObject("medick_TpColumn");
                column.transform.SetParent(panel, false);
                var crt = column.AddComponent<RectTransform>();
                crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0f, 1f);
                crt.anchoredPosition = new Vector2(COL_X, TOP_Y);
                crt.sizeDelta        = new Vector2(COL_W, 0f);
                var vlg = column.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = GAP;
                vlg.childForceExpandWidth  = false;
                vlg.childForceExpandHeight = false;
                vlg.childControlWidth      = true;
                vlg.childControlHeight     = true;
                vlg.childAlignment         = TextAnchor.UpperLeft;
                var fitter = column.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var newItems = new List<(GameObject go, int group)>();
                var newHeaders = new TMP_Text[Groups.Length];
                for (int g = 0; g < Groups.Length; g++)
                {
                    newHeaders[g] = AddGroupHeader(template, column.transform, g);
                    foreach (var d in Groups[g])
                        newItems.Add((AddDestination(template, column.transform, d), g));
                }

                _masterTab = master; _column = column; _masterLabel = masterLabel;
                _items.AddRange(newItems);
                for (int g = 0; g < newHeaders.Length; g++)
                    if (newHeaders[g] != null) { _groupLabels[g] = newHeaders[g]; _allGroupLabels[g].Add(newHeaders[g]); }
                _menus.Add((master, column, masterLabel));
                ApplyGroupStates();
                ApplyVisibility();
                Dbg.Log($"teleport menu injected ({_menus.Count} live cop{(_menus.Count == 1 ? "y" : "ies")})");
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.Error("teleport menu injection failed: " + e);
                // Destroyed in catch: the GUARD object is built first, so a
                // half-built column would otherwise latch forever and leave a
                // QUICK TELEPORT tab that toggles nothing.
                try
                {
                    if (master) UnityEngine.Object.DestroyImmediate(master);
                    if (column) UnityEngine.Object.DestroyImmediate(column);
                }
                catch { }
                PruneDead();
            }
        }

        // ── Builders ──────────────────────────────────────────────

        static TMP_Text AddGroupHeader(GameObject template, Transform parent, int group)
        {
            int g = group;
            GameObject go = NativeClone.Button(template, parent, $"medick_TpHdr{g}",
                () => ToggleGroup(g));
            NativeClone.HideIcon(go);
            NativeClone.SetLayoutSize(go, COL_W, HDR_H);
            NativeClone.SetRichLabel(go, HeaderText(g));
            return NativeClone.Label(go);
        }

        static GameObject AddDestination(GameObject template, Transform parent,
            (string line1, string line2, string scene, Color color) d)
        {
            string scene = d.scene;
            GameObject go = NativeClone.Button(template, parent, $"medick_Tp_{scene}",
                () => TravelService.RequestTravel(scene));
            NativeClone.HideIcon(go);
            NativeClone.SetLayoutSize(go, COL_W, BTN_H);

            string hex = ColorUtility.ToHtmlStringRGB(d.color);
            // Relative size tags + auto-sizing so "Circle of Fortune" and
            // "Forgotten Knights" shrink-to-fit instead of clipping silently.
            NativeClone.SetRichLabel(go,
                $"<b>{d.line1}</b>\n<size=78%><color=#{hex}>{d.line2}</color></size>",
                baseSize: 11.5f, autoMin: 8.5f, autoMax: 11.5f);
            NativeClone.AddAccentBar(go, d.color);   // faction identity, native art untouched
            return go;
        }

        // ── State ─────────────────────────────────────────────────

        // Explicit size: the donor label's auto-size leaves a nondeterministic
        // fontSize behind (whatever its own localized string last computed).
        static string MasterLabelText() =>
            _columnOpen ? "<size=12><b>< QUICK\nTELEPORT</b></size>"
                        : "<size=12><b>> QUICK\nTELEPORT</b></size>";

        // ASCII arrows on purpose — the game's TMP font has no ▼/▶ glyphs.
        static string HeaderText(int g) =>
            $"<b><size=10.5>{(_groupOpen[g] ? "v" : ">")} {GroupNames[g]}</size></b>";

        static void ToggleColumn()
        {
            _columnOpen = !_columnOpen;
            PruneDead();
            foreach (var m in _menus)
                try { if (m.label != null) m.label.text = MasterLabelText(); } catch { }
            ApplyVisibility();
        }

        static void ToggleGroup(int g)
        {
            _groupOpen[g] = !_groupOpen[g];
            PruneDead();
            foreach (var l in _allGroupLabels[g])
                try { l.text = HeaderText(g); } catch { }
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
            bool col = show && _columnOpen;
            PruneDead();
            foreach (var m in _menus)
            {
                try { if (m.master.activeSelf != show) m.master.SetActive(show); } catch { }
                try { if (m.column.activeSelf != col) m.column.SetActive(col); } catch { }
            }
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
