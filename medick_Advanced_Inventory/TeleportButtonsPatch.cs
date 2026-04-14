// ================================================================
//  TeleportButtonsPatch.cs  — v1.2.0
//
//  Collapsible teleport menu attached to the inventory panel.
//
//  Layout:
//    [QUICK TELEPORT tab]  ← always visible at panel left border (A)
//    [column ←←←←←←←←←]  ← opens/closes to the left of the tab
//
//  Column is grouped by era (timeline order):
//    DIVINE ERA   — Circle of Fortune, Merchant's Guild, Champion's Gate
//    IMPERIAL ERA — Soulfire Bastion
//    RUINED ERA   — Lightless Arbor, Temporal Sanctum
//    END OF TIME  — Forgotten Knights, The Woven, The End of Time
//
//  Each era header is clickable (▼/▶) to collapse its buttons.
//  The master tab collapses/restores the entire column.
//
//  Travel strategy (per button click):
//    1. Instant travel via UIWaypointController.waypointsInMenu
//    2. Fallback: close inventory → invoke faction "VISIT X" button
//    3. Last resort: MapKeyDown()
// ================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Il2Cpp;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace medick_Terrible_Inventory
{
    [HarmonyPatch(typeof(EnableWovenEchoesTabIfRelevant), "Awake")]
    internal static class Patch_TeleportButtons
    {
        private const string GUARD = "medick_TpMaster";

        // ── Faction data ──────────────────────────────────────────────────────

        private static readonly (string line1, string line2, string scene, Color color)[] Factions =
        {
            // [0-2] Divine Era group
            ("Circle of Fortune", "The Observatory",  "Observatory",  new Color(0.07f, 0.16f, 0.52f)),
            ("Merchant's Guild",  "The Bazaar",       "Bazaar",       new Color(0.42f, 0.07f, 0.07f)),
            ("Champion's Gate",   "ARENA",            "ArenaLobby",   new Color(0.18f, 0.30f, 0.42f)),
            // [3] End of Time town hub
            ("The End of Time",   "TOWN",             "EoT",          new Color(0.26f, 0.13f, 0.21f)),
            // [4-5] End of Time faction hubs
            ("Forgotten Knights", "Shattered Road",   "M_Knight",     new Color(0.20f, 0.04f, 0.30f)),
            ("The Woven",         "Haven of Silk",    "WeaversHub",   new Color(0.04f, 0.25f, 0.25f)),
            // [6-7] Ruined Era dungeons — confirmed via in-game waypoint test
            ("Lightless Arbor",   "DUNGEON",          "Dun2Q10",      new Color(0.06f, 0.04f, 0.10f)),
            ("Temporal Sanctum",  "DUNGEON",          "Dun1Q10",      new Color(0.14f, 0.06f, 0.24f)),
            // [8] Imperial Era dungeon
            ("Soulfire Bastion",  "DUNGEON",          "Dun3Q10",      new Color(0.28f, 0.06f, 0.02f)),
        };

        // ── Keyword to find each faction's "Visit Hub" button ─────────────────

        private static readonly Dictionary<string, string> SceneToVisitKeyword = new Dictionary<string, string>
        {
            { "Observatory", "OBSERVATORY" },
            { "Bazaar",      "BAZAAR"      },
            { "ArenaLobby",  "CHAMPION"    },
            { "EoT",         "END OF TIME" },
            { "M_Knight",    "SHATTERED"   },
            { "WeaversHub",  "HAVEN"       },
        };

        // ── Era tab keywords for the generic map fallback ─────────────────────

        private static readonly Dictionary<string, string> SceneToEra = new Dictionary<string, string>
        {
            { "Observatory", "DIVINE"    },
            { "Bazaar",      "DIVINE"    },
            { "ArenaLobby",  "DIVINE"    },
            { "EoT",         "END OF"    },
            { "M_Knight",    "RUINED"    },
            { "WeaversHub",  "END OF"    },
            { "Dun2Q10",     "RUINED"    },
            { "Dun1Q10",     "RUINED"    },
            { "Dun3Q10",     "IMPERIAL"  },
        };

        // ── Layout constants ──────────────────────────────────────────────────
        //
        //  TAB  = the always-visible "QUICK TELEPORT" button at panel left border
        //  COL  = the collapsible column that opens to the LEFT of the tab
        //
        //  [  COL  ][ TAB ]|panel border|
        //   COL_X    TAB_X  x=0

        private const float BTN_H  = 48f;
        private const float HDR_H  = 44f;   // 3-line era header (restored)
        private const float GAP    = 3f;
        private const float COL_W  = 118f;
        private const float COL_Y  = -4f;   // top of column (from panel top-left anchor)

        // Tab sits inside the panel's left decorative border area (always visible)
        private const float TAB_X  = 28f;   // px inside panel left anchor
        private const float TAB_W  = 157f;
        private const float TAB_H  = 65f;   // taller tab

        // Column is to the LEFT of the tab  (28 - 3 - 118 = -93)
        private const float COL_X  = TAB_X - GAP - COL_W;

        // ── Collapse state ────────────────────────────────────────────────────

        private static bool _columnOpen = true;

        // One slot per group: [0]=Factions [1]=Dungeons [2]=Misc
        private static readonly bool[] _eraOpen = { true, true, true };

        // Ordered list of column items for reflow.
        // isEraBtn=false → group header, visible whenever column is open
        // isEraBtn=true  → destination button, visible when column open AND group open
        private static readonly List<(RectTransform rt, bool isEraBtn, int eraIdx)> _colItems
            = new List<(RectTransform, bool, int)>();

        // UI label refs for updating arrow glyphs
        private static TMP_Text _masterLabel;
        private static readonly TMP_Text[] _eraArrows = new TMP_Text[3];

        // Group header label strings
        private static readonly string[] ERA_NAMES = { "FACTIONS", "DUNGEONS", "HUBS" };

        // ── Reflow ────────────────────────────────────────────────────────────
        // Repositions all visible column items top-to-bottom from COL_Y.

        private static void Reflow()
        {
            float y = COL_Y;
            foreach (var item in _colItems)
            {
                bool show = _columnOpen && (!item.isEraBtn || _eraOpen[item.eraIdx]);
                item.rt.gameObject.SetActive(show);
                if (show)
                {
                    item.rt.anchoredPosition = new Vector2(COL_X, y);
                    y -= item.rt.sizeDelta.y + GAP;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────

        [HarmonyPostfix]
        public static void Postfix(EnableWovenEchoesTabIfRelevant __instance)
        {
            LogScene();
            if (__instance.transform.Find(GUARD) != null) return;

            try
            {
                // Reset state for fresh scene / new instance
                _colItems.Clear();
                _listeners.Clear();
                _columnOpen = true;
                for (int i = 0; i < 3; i++) _eraOpen[i] = true;

                TMP_FontAsset font = null;
                try { var t = GameObject.FindObjectOfType<TMP_Text>(); if (t != null) font = t.font; } catch { }

                // ── Master "QUICK TELEPORT" tab (always visible) ───────────────
                MakeMasterTab(__instance.transform, font);

                // ── Utility groups (left column, collapsible) ─────────────────

                // FACTIONS [0]: CoF, Merchant's Guild, Forgotten Knights, The Woven
                AddEraHeader(__instance.transform, 0, font);
                AddFactionBtn(__instance.transform, "medick_Tp0", Factions[0], 0, font); // Circle of Fortune
                AddFactionBtn(__instance.transform, "medick_Tp1", Factions[1], 0, font); // Merchant's Guild
                AddFactionBtn(__instance.transform, "medick_Tp4", Factions[4], 0, font); // Forgotten Knights
                AddFactionBtn(__instance.transform, "medick_Tp5", Factions[5], 0, font); // The Woven

                // DUNGEONS [1]: Lightless Arbor, Temporal Sanctum, Soulfire Bastion
                AddEraHeader(__instance.transform, 1, font);
                AddFactionBtn(__instance.transform, "medick_Tp6", Factions[6], 1, font); // Lightless Arbor
                AddFactionBtn(__instance.transform, "medick_Tp7", Factions[7], 1, font); // Temporal Sanctum
                AddFactionBtn(__instance.transform, "medick_Tp8", Factions[8], 1, font); // Soulfire Bastion

                // HUBS [2]: End of Time, Champion's Gate / Arena
                AddEraHeader(__instance.transform, 2, font);
                AddFactionBtn(__instance.transform, "medick_Tp3", Factions[3], 2, font); // End of Time
                AddFactionBtn(__instance.transform, "medick_Tp2", Factions[2], 2, font); // Champion's Gate

                // Initial layout pass
                Reflow();

                // First inventory open each session — silently prime all era controllers
                if (!_eraTabsVisited)
                    MelonCoroutines.Start(VisitAllEraTabsCoroutine());

                MelonLogger.Msg("[Terrible Inventory] Collapsible teleport menu injected (v1.3.0).");
            }
            catch (Exception e)
            {
                MelonLogger.Error("[Terrible Inventory] TeleportButtons error: " + e);
            }
        }

        // ── Master tab factory ────────────────────────────────────────────────
        // Horizontal button at panel left border, always visible.
        // Clicking toggles the entire column on/off.

        private static void MakeMasterTab(Transform parent, TMP_FontAsset font)
        {
            GameObject go = new GameObject(GUARD); // "medick_TpMaster"
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(TAB_X, COL_Y);
            rt.sizeDelta        = new Vector2(TAB_W, TAB_H);

            Image border = go.AddComponent<Image>();
            border.color = GOLD;

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = border;
            ColorBlock cb = btn.colors;
            cb.normalColor      = GOLD_DIM;
            cb.highlightedColor = GOLD_BRIGHT;
            cb.pressedColor     = GOLD_PRESS;
            cb.fadeDuration     = 0.08f;
            btn.colors          = cb;

            // Dark background
            GameObject bgGO = new GameObject("BG");
            bgGO.transform.SetParent(go.transform, false);
            RectTransform bgRt = bgGO.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(2f, 2f); bgRt.offsetMax = new Vector2(-2f, -2f);
            bgGO.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.10f, 1f);

            // Label
            GameObject lblGO = new GameObject("Label");
            lblGO.transform.SetParent(go.transform, false);
            RectTransform lrt = lblGO.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(4f, 2f); lrt.offsetMax = new Vector2(-8f, -2f);

            TextMeshProUGUI tmp = lblGO.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text               = "<b><color=#FFD700>< QUICK\nTELEPORT</color></b>";
            tmp.fontSize           = 13f;
            tmp.alignment          = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode       = TextOverflowModes.Truncate;
            _masterLabel = tmp;

            Action listener = new Action(() =>
            {
                _columnOpen = !_columnOpen;
                _masterLabel.text = _columnOpen
                    ? "<b><color=#FFD700>< QUICK\nTELEPORT</color></b>"
                    : "<b><color=#FFD700>> QUICK\nTELEPORT</color></b>";
                Reflow();
            });
            _listeners.Add(listener);
            btn.onClick.AddListener(listener);
        }

        // ── Era header factory ────────────────────────────────────────────────
        // Clickable row with ▼/▶ glyph. Clicking collapses/restores that era's buttons.

        private static void AddEraHeader(Transform parent, int eraIdx, TMP_FontAsset font)
        {
            string objName = $"medick_TpHdr{eraIdx}";
            string label   = ERA_NAMES[eraIdx];

            GameObject go = new GameObject(objName);
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(COL_X, COL_Y); // Reflow sets real position
            rt.sizeDelta        = new Vector2(COL_W, HDR_H);

            Image border = go.AddComponent<Image>();
            border.color = GOLD;

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = border;
            ColorBlock cb = btn.colors;
            cb.normalColor      = GOLD_DIM;
            cb.highlightedColor = GOLD_BRIGHT;
            cb.pressedColor     = GOLD_PRESS;
            cb.fadeDuration     = 0.08f;
            btn.colors          = cb;

            // Very dark background
            GameObject bgGO = new GameObject("BG");
            bgGO.transform.SetParent(go.transform, false);
            RectTransform bgRt = bgGO.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(2f, 2f); bgRt.offsetMax = new Vector2(-2f, -2f);
            bgGO.AddComponent<Image>().color = new Color(0.06f, 0.05f, 0.08f, 1f);

            // Label "▼ DIVINE ERA"
            GameObject lblGO = new GameObject("Label");
            lblGO.transform.SetParent(go.transform, false);
            RectTransform lrt = lblGO.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(4f, 2f); lrt.offsetMax = new Vector2(-4f, -2f);

            TextMeshProUGUI tmp = lblGO.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = $"<b><color=#FFD700><size=10.5>v {label}</size></color></b>";
            tmp.alignment          = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode       = TextOverflowModes.Truncate;
            _eraArrows[eraIdx]     = tmp;

            int idx = eraIdx;
            Action listener = new Action(() =>
            {
                _eraOpen[idx] = !_eraOpen[idx];
                string arrow = _eraOpen[idx] ? "v" : ">";
                _eraArrows[idx].text =
                    $"<b><color=#FFD700><size=10.5>{arrow} {ERA_NAMES[idx]}</size></color></b>";
                Reflow();
            });
            _listeners.Add(listener);
            btn.onClick.AddListener(listener);

            _colItems.Add((rt, false, eraIdx)); // header: show when column open
        }

        // ── Faction button helper ─────────────────────────────────────────────
        // Creates a button and registers it in _colItems for reflow.

        private static void AddFactionBtn(Transform parent, string objName,
            (string line1, string line2, string scene, Color color) f,
            int eraIdx, TMP_FontAsset font)
        {
            Vector2 anc = new Vector2(0, 1);
            MakeButton(parent, objName, f.line1, f.line2, f.color, f.scene, font,
                anc, anc, anc, new Vector2(COL_X, COL_Y), new Vector2(COL_W, BTN_H));

            // Find the RT we just created and register it
            Transform t = parent.Find(objName);
            if (t != null)
            {
                RectTransform rt = t.GetComponent<RectTransform>();
                if (rt != null) _colItems.Add((rt, true, eraIdx)); // show when era open
            }
        }

        // ── Concurrency guard — only one TravelCoroutine at a time ───────────
        private static bool _travelInProgress = false;

        // ── Era prime flag — runs once per session on first inventory open ────
        private static bool _eraTabsVisited = false;

        // ── Button factory ────────────────────────────────────────────────────

        private static readonly List<Action> _listeners = new List<Action>();

        private static readonly Color GOLD        = new Color(0.90f, 0.70f, 0.12f, 1f);
        private static readonly Color GOLD_DIM    = new Color(0.60f, 0.60f, 0.60f, 1f);
        private static readonly Color GOLD_BRIGHT = new Color(1.00f, 1.00f, 1.00f, 1f);
        private static readonly Color GOLD_PRESS  = new Color(0.40f, 0.40f, 0.40f, 1f);

        private static void MakeButton(Transform parent,
            string objName, string line1, string line2,
            Color bgColor, string scene, TMP_FontAsset font,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject(objName);
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;

            Image borderImg = go.AddComponent<Image>();
            borderImg.color = GOLD;

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = borderImg;
            ColorBlock cb = btn.colors;
            cb.normalColor      = GOLD_DIM;
            cb.highlightedColor = GOLD_BRIGHT;
            cb.pressedColor     = GOLD_PRESS;
            cb.fadeDuration     = 0.08f;
            btn.colors          = cb;

            string s = scene;
            Action listener = new Action(() =>
            {
                if (_travelInProgress)
                {
                    MelonLogger.Msg("[Terrible Inventory] TravelCoroutine already running — ignoring click");
                    return;
                }
                MelonCoroutines.Start(TravelCoroutine(s));
            });
            _listeners.Add(listener);
            btn.onClick.AddListener(listener);

            GameObject bgGO = new GameObject("BG");
            bgGO.transform.SetParent(go.transform, false);
            RectTransform bgRt = bgGO.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2( 3f,  3f);
            bgRt.offsetMax = new Vector2(-3f, -3f);
            bgGO.AddComponent<Image>().color = bgColor;

            GameObject lblGO = new GameObject("Label");
            lblGO.transform.SetParent(go.transform, false);
            RectTransform lrt = lblGO.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(4f, 4f);
            lrt.offsetMax = new Vector2(-4f, -4f);

            TextMeshProUGUI tmp = lblGO.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text               = $"<size=11.5><b>{line1}</b></size>\n<size=9.5><color=#dddddd>{line2}</color></size>";
            tmp.alignment          = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            tmp.overflowMode       = TextOverflowModes.Truncate;
        }

        // ── Faction visit button invoker ──────────────────────────────────────

        private static bool TryInvokeFactionVisitButton(string scene)
        {
            string keyword = "";
            if (!SceneToVisitKeyword.TryGetValue(scene, out keyword)) return false;
            keyword = keyword.ToUpper();

            try
            {
                Button[] allButtons = GameObject.FindObjectsOfType<Button>(true);
                MelonLogger.Msg($"[Terrible Inventory] TryInvokeFactionVisitButton '{keyword}': searching {allButtons?.Length ?? 0} buttons");

                foreach (Button btn in allButtons)
                {
                    if (btn == null) continue;

                    string goName = btn.gameObject.name ?? "";
                    if (goName.StartsWith("medick_Tp")) continue;

                    TMP_Text[] tmps = btn.GetComponentsInChildren<TMP_Text>(true);
                    foreach (TMP_Text t in tmps)
                    {
                        if (t == null || string.IsNullOrEmpty(t.text)) continue;
                        string up = t.text.ToUpper();
                        if (!up.Contains("VISIT")) continue;
                        if (!up.Contains(keyword)) continue;
                        MelonLogger.Msg($"[Terrible Inventory] Found faction button: '{t.text}' on '{btn.gameObject.name}' — invoking");
                        btn.onClick.Invoke();
                        return true;
                    }

                    UnityEngine.UI.Text[] legacyTexts = btn.GetComponentsInChildren<UnityEngine.UI.Text>(true);
                    foreach (var t in legacyTexts)
                    {
                        if (t == null || string.IsNullOrEmpty(t.text)) continue;
                        string up = t.text.ToUpper();
                        if (!up.Contains("VISIT")) continue;
                        if (!up.Contains(keyword)) continue;
                        MelonLogger.Msg($"[Terrible Inventory] Found faction button (legacy): '{t.text}' on '{btn.gameObject.name}' — invoking");
                        btn.onClick.Invoke();
                        return true;
                    }
                }

                MelonLogger.Warning($"[Terrible Inventory] Faction visit button for 'VISIT {keyword}' not found in {allButtons?.Length ?? 0} buttons");
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"[Terrible Inventory] TryInvokeFactionVisitButton error: {e.Message}");
            }
            return false;
        }

        // ── Era tab clicker (fallback) ────────────────────────────────────────

        private static bool TryClickEraTab(string eraKeyword)
        {
            if (string.IsNullOrEmpty(eraKeyword)) return false;
            string kw = eraKeyword.ToUpper();
            try
            {
                var candidates = new List<Transform>();

                foreach (var t in GameObject.FindObjectsOfType<TMP_Text>())
                    if (t != null && !string.IsNullOrEmpty(t.text) && t.text.ToUpper().Contains(kw))
                        candidates.Add(t.transform);

                foreach (var t in GameObject.FindObjectsOfType<UnityEngine.UI.Text>())
                    if (t != null && !string.IsNullOrEmpty(t.text) && t.text.ToUpper().Contains(kw))
                        candidates.Add(t.transform);

                MelonLogger.Msg($"[Terrible Inventory] TryClickEraTab '{kw}': {candidates.Count} text matches");

                foreach (var tr in candidates)
                {
                    Transform cur = tr;
                    for (int d = 0; d < 6 && cur != null; d++, cur = cur.parent)
                    {
                        Button b = cur.GetComponent<Button>();
                        if (b != null) { b.onClick.Invoke(); MelonLogger.Msg($"[Terrible Inventory] Era tab Button on '{cur.name}'"); return true; }
                        Toggle tog = cur.GetComponent<Toggle>();
                        if (tog != null) { tog.isOn = true; MelonLogger.Msg($"[Terrible Inventory] Era tab Toggle on '{cur.name}'"); return true; }
                    }
                    cur = tr;
                    for (int d = 0; d < 6 && cur != null; d++, cur = cur.parent)
                    {
                        try
                        {
                            var evd = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                            UnityEngine.EventSystems.ExecuteEvents.Execute(cur.gameObject, evd, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                            MelonLogger.Msg($"[Terrible Inventory] Era tab ExecuteEvents on '{cur.name}'");
                            return true;
                        }
                        catch { }
                    }
                }
            }
            catch (Exception e) { MelonLogger.Warning($"[Terrible Inventory] TryClickEraTab error: {e.Message}"); }
            return false;
        }

        // ── Waypoint finder ───────────────────────────────────────────────────
        // Searches ALL UIWaypointControllers (one per era, 5 total).
        // All controllers have waypointsInMenu populated at scene load.

        private static UIWaypointStandard FindWaypointForScene(string targetScene)
        {
            try
            {
                UIWaypointController[] all = GameObject.FindObjectsOfType<UIWaypointController>(true);
                if (all == null || all.Length == 0)
                {
                    MelonLogger.Msg("[Terrible Inventory] No UIWaypointControllers found");
                    return null;
                }

                MelonLogger.Msg($"[Terrible Inventory] Searching {all.Length} controllers for '{targetScene}'");

                foreach (UIWaypointController ctrl in all)
                {
                    int count = ctrl.waypointsInMenu?.Count ?? 0;
                    if (count == 0) continue;

                    for (int i = 0; i < count; i++)
                    {
                        try
                        {
                            UIWaypointStandard w = ctrl.waypointsInMenu[i]?.TryCast<UIWaypointStandard>();
                            if (w == null) continue;
                            if ((w.sceneName ?? "") == targetScene)
                            {
                                MelonLogger.Msg($"[Terrible Inventory] Found '{targetScene}' in ctrl '{ctrl.gameObject.name}' ({count} wp)");
                                return w;
                            }
                        }
                        catch { }
                    }
                }

                MelonLogger.Msg($"[Terrible Inventory] '{targetScene}' not found in any controller");
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"[Terrible Inventory] FindWaypointForScene error: {e.Message}");
            }
            return null;
        }

        // ── Travel coroutine ──────────────────────────────────────────────────

        private static IEnumerator TravelCoroutine(string scene)
        {
            _travelInProgress = true;
            MelonLogger.Msg($"[Terrible Inventory] TravelCoroutine for '{scene}'");
            yield return null;

            // Enable waypoints system
            try
            {
                WaypointManager wm = WaypointManager.getInstance();
                if (wm != null) { wm.WaypointEnabled = true; wm.EnableWaypoint(); }
            }
            catch { }

            // Step 1: instant travel via pre-populated waypoint controllers
            UIWaypointStandard wp = FindWaypointForScene(scene);
            if (wp != null)
            {
                try
                {
                    wp.LoadWaypointScene();
                    MelonLogger.Msg($"[Terrible Inventory] Instant travel → '{scene}'");
                    _travelInProgress = false;
                    yield break;
                }
                catch (Exception e) { MelonLogger.Warning($"[Terrible Inventory] LoadWaypointScene failed: {e.Message}"); }
            }

            // Step 2: waypoint not cached — close inventory, open map, click the correct
            //         era tab, wait for it to populate, then try again.
            MelonLogger.Msg($"[Terrible Inventory] Waypoint not cached — opening map to populate era for '{scene}'");

            try { if (UIBase.instanceExists && UIBase.instance != null) UIBase.instance.closeInventory(); }
            catch { }
            yield return new WaitForSeconds(0.15f);

            // Open world map
            try { if (UIBase.instanceExists && UIBase.instance != null) UIBase.instance.MapKeyDown(); }
            catch { }
            yield return new WaitForSeconds(0.6f); // let map finish opening

            // Click the era tab that contains this scene's waypoints
            string eraKeyword = "";
            SceneToEra.TryGetValue(scene, out eraKeyword);
            if (!string.IsNullOrEmpty(eraKeyword))
            {
                bool clicked = TryClickEraTab(eraKeyword);
                MelonLogger.Msg($"[Terrible Inventory] Era tab '{eraKeyword}' clicked={clicked}");
                if (clicked) yield return new WaitForSeconds(1.0f); // wait for controller to populate
            }

            // Try waypoint again with freshly-populated controllers
            wp = FindWaypointForScene(scene);
            if (wp != null)
            {
                try
                {
                    wp.LoadWaypointScene();
                    MelonLogger.Msg($"[Terrible Inventory] Map-assisted travel → '{scene}'");
                    _travelInProgress = false;
                    yield break;
                }
                catch (Exception e) { MelonLogger.Warning($"[Terrible Inventory] Map-assisted travel failed: {e.Message}"); }
            }

            // Step 3: last resort — faction visit button (leaves map open on success)
            MelonLogger.Msg($"[Terrible Inventory] Still no waypoint — trying faction button for '{scene}'");
            bool factionBtn = TryInvokeFactionVisitButton(scene);
            if (!factionBtn)
                MelonLogger.Warning($"[Terrible Inventory] All travel methods exhausted for '{scene}' — map left open");

            _travelInProgress = false;
        }

        // ── Silent era-controller primer ──────────────────────────────────────
        // Runs once per session on the first inventory open while in a game scene.
        // For each UIWaypointController (one per era), we temporarily activate its
        // full ancestor chain so OnEnable fires on the controller — this is what
        // the game needs before LoadWaypointScene will actually work.
        // The inventory stays open; the map never opens.

        private static IEnumerator VisitAllEraTabsCoroutine()
        {
            // Wait until we're in a real playable scene (not character select / loading)
            float waited = 0f;
            while (waited < 30f)
            {
                bool inGame = false;
                try
                {
                    string s = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name ?? "";
                    inGame = !string.IsNullOrEmpty(s)
                        && !s.ToLower().Contains("loading")
                        && !s.ToLower().Contains("menu")
                        && !s.ToLower().Contains("boot")
                        && !s.ToLower().Contains("splash")
                        && !s.ToLower().Contains("character");
                }
                catch { }
                if (inGame) break;
                yield return new WaitForSeconds(0.5f);
                waited += 0.5f;
            }

            yield return new WaitForSeconds(0.5f); // let the scene settle

            UIWaypointController[] all = null;
            try { all = GameObject.FindObjectsOfType<UIWaypointController>(true); } catch { }

            if (all == null || all.Length == 0)
            {
                MelonLogger.Warning("[Terrible Inventory] VisitEras: no controllers found");
                _eraTabsVisited = true;
                yield break;
            }

            MelonLogger.Msg($"[Terrible Inventory] VisitEras: priming {all.Length} era controllers silently");

            foreach (UIWaypointController ctrl in all)
            {
                // Remember original state so we restore exactly what was there
                bool wasActive = ctrl.gameObject.activeSelf;

                // Collect every inactive ancestor from this controller up to the scene root
                var inactiveAncestors = new System.Collections.Generic.List<GameObject>();
                try
                {
                    Transform t = ctrl.transform.parent;
                    while (t != null)
                    {
                        if (!t.gameObject.activeSelf)
                            inactiveAncestors.Add(t.gameObject);
                        t = t.parent;
                    }
                }
                catch { }

                // Enable root → leaf so each level is activeInHierarchy before its child
                inactiveAncestors.Reverse();
                foreach (var go in inactiveAncestors)
                    try { go.SetActive(true); } catch { }

                // Toggle the controller — SetActive false → true fires OnEnable
                try
                {
                    if (ctrl.gameObject.activeSelf) ctrl.gameObject.SetActive(false);
                    ctrl.gameObject.SetActive(true);
                }
                catch { }

                yield return null; // one frame for OnEnable to complete

                // Restore controller to exactly what it was before we touched it
                try { ctrl.gameObject.SetActive(wasActive); } catch { }

                // Restore ancestors back to inactive (they were all inactive when we found them)
                inactiveAncestors.Reverse();
                foreach (var go in inactiveAncestors)
                    try { go.SetActive(false); } catch { }
            }

            _eraTabsVisited = true;
            MelonLogger.Msg("[Terrible Inventory] VisitEras: all era controllers primed — teleport ready.");
        }

        // ── Logging ───────────────────────────────────────────────────────────

        private static void LogScene()
        {
            try
            {
                string cur = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                MelonLogger.Msg($"[Terrible Inventory] Current scene: '{cur}'");
            }
            catch { }
        }
    }
}
