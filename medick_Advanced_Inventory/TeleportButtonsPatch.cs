// ================================================================
//  TeleportButtonsPatch.cs
//
//  Injects a teleport button column into the inventory panel,
//  grouped by in-game era (timeline order):
//
//    DIVINE ERA   — Circle of Fortune (Observatory)
//                   Merchant's Guild (Bazaar)
//                   Champion's Gate (Arena)
//    IMPERIAL ERA — Soulfire Bastion (Dungeon)
//    RUINED ERA   — Lightless Arbor (Dungeon)
//                   Temporal Sanctum (Dungeon)
//    END OF TIME  — Forgotten Knights (Shattered Road)
//                   The Woven (Haven of Silk)
//                   The End of Time (Town)
//
//  Travel strategy (per button click):
//    1. Instant travel via UIWaypointController.waypointsInMenu
//       (all 5 era controllers are pre-populated at scene load —
//        no need to open the map first).
//    2. Fallback: close inventory → invoke faction's "VISIT X" button
//       → opens map at correct era with node highlighted.
//    3. Last resort: MapKeyDown() to open map generically.
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

namespace medick_Advanced_Inventory
{
    [HarmonyPatch(typeof(EnableWovenEchoesTabIfRelevant), "Awake")]
    internal static class Patch_TeleportButtons
    {
        private const string GUARD = "medick_Tp0";

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
        // Searched case-insensitively as a substring of any Button's child text.

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
            { "Dun1Q10",     "RUINED"    },
            { "Dun2Q10",     "RUINED"    },
            { "Dun3Q10",     "IMPERIAL"  },
        };

        // ── Layout constants ──────────────────────────────────────────────────
        //
        //  Single column, left side of inventory panel:
        //
        //  ┌──────────────────────────┐
        //  │  6 · DIVINE ERA (note)   │
        //  ├──────────────────────────┤
        //  │  1 · Circle of Fortune   │
        //  ├──────────────────────────┤
        //  │  2 · Merchant's Guild    │
        //  ├──────────────────────────┤
        //  │  NEW· Champion's Gate    │
        //  ├──────────────────────────┤
        //  │  7 · END OF TIME (note)  │
        //  ├──────────────────────────┤
        //  │  4 · Forgotten Knights   │
        //  ├──────────────────────────┤
        //  │  5 · The Woven           │
        //  ├──────────────────────────┤
        //  │  3 · End of Time         │
        //  └──────────────────────────┘

        private const float BTN_H  = 48f;
        private const float HDR_H  = 44f;    // tall enough for 3-line era note
        private const float GAP    = 3f;
        private const float COL_X  = -88f;   // negative = bleeds left outside panel edge
        private const float COL_W  = 120f;
        private const float COL_Y  = -6f;    // down 1/4 box from previous

        // ─────────────────────────────────────────────────────────────────────

        [HarmonyPostfix]
        public static void Postfix(EnableWovenEchoesTabIfRelevant __instance)
        {
            LogScene();
            if (__instance.transform.Find(GUARD) != null) return;

            try
            {
                RectTransform panelRt = __instance.GetComponent<RectTransform>();
                if (panelRt != null)
                    MelonLogger.Msg($"[AdvancedInventory] Panel rect = {panelRt.rect.size}");

                TMP_FontAsset font = null;
                try { var t = GameObject.FindObjectOfType<TMP_Text>(); if (t != null) font = t.font; } catch { }

                Vector2 anc = new Vector2(0, 1);
                float y = COL_Y;

                void NextHdr(out float yOut) { yOut = y; y -= HDR_H + GAP; }
                void NextBtn(out float yOut) { yOut = y; y -= BTN_H  + GAP; }

                // ── DIVINE ERA group ──────────────────────────────────────────
                // Factions[0] = Circle of Fortune, [1] = Merchant's Guild, [2] = Champion's Gate
                NextHdr(out float yDivHdr);
                MakeInfoBox(__instance.transform, "medick_TpHdrDiv",
                    "DIVINE ERA", COL_X, yDivHdr, COL_W, HDR_H, font);

                NextBtn(out float yCof);
                var cof = Factions[0];
                MakeButton(__instance.transform, "medick_Tp0",
                    cof.line1, cof.line2, cof.color, cof.scene, font,
                    anc, anc, anc, new Vector2(COL_X, yCof), new Vector2(COL_W, BTN_H));

                NextBtn(out float yMg);
                var mg = Factions[1];
                MakeButton(__instance.transform, "medick_Tp1",
                    mg.line1, mg.line2, mg.color, mg.scene, font,
                    anc, anc, anc, new Vector2(COL_X, yMg), new Vector2(COL_W, BTN_H));

                NextBtn(out float yCg);
                var cg = Factions[2];
                MakeButton(__instance.transform, "medick_Tp2",
                    cg.line1, cg.line2, cg.color, cg.scene, font,
                    anc, anc, anc, new Vector2(COL_X, yCg), new Vector2(COL_W, BTN_H));

                // ── IMPERIAL ERA group ────────────────────────────────────────
                // Factions[8] = Soulfire Bastion
                NextHdr(out float yImpHdr);
                MakeInfoBox(__instance.transform, "medick_TpHdrImp",
                    "IMPERIAL ERA", COL_X, yImpHdr, COL_W, HDR_H, font);

                NextBtn(out float ySb);
                var sb = Factions[8];
                MakeButton(__instance.transform, "medick_Tp8",
                    sb.line1, sb.line2, sb.color, sb.scene, font,
                    anc, anc, anc, new Vector2(COL_X, ySb), new Vector2(COL_W, BTN_H));

                // ── RUINED ERA group ──────────────────────────────────────────
                // Factions[6] = Lightless Arbor, [7] = Temporal Sanctum
                NextHdr(out float yRuinHdr);
                MakeInfoBox(__instance.transform, "medick_TpHdrRuin",
                    "RUINED ERA", COL_X, yRuinHdr, COL_W, HDR_H, font);

                NextBtn(out float yLa);
                var la = Factions[6];
                MakeButton(__instance.transform, "medick_Tp6",
                    la.line1, la.line2, la.color, la.scene, font,
                    anc, anc, anc, new Vector2(COL_X, yLa), new Vector2(COL_W, BTN_H));

                NextBtn(out float yTs);
                var ts = Factions[7];
                MakeButton(__instance.transform, "medick_Tp7",
                    ts.line1, ts.line2, ts.color, ts.scene, font,
                    anc, anc, anc, new Vector2(COL_X, yTs), new Vector2(COL_W, BTN_H));

                // ── END OF TIME group ─────────────────────────────────────────
                // Factions[4] = Forgotten Knights, [5] = The Woven, [3] = End of Time town
                NextHdr(out float yEotHdr);
                MakeInfoBox(__instance.transform, "medick_TpHdrEoT",
                    "END OF TIME", COL_X, yEotHdr, COL_W, HDR_H, font);

                NextBtn(out float yFk);
                var fk = Factions[4];
                MakeButton(__instance.transform, "medick_Tp4",
                    fk.line1, fk.line2, fk.color, fk.scene, font,
                    anc, anc, anc, new Vector2(COL_X, yFk), new Vector2(COL_W, BTN_H));

                NextBtn(out float yWh);
                var wh = Factions[5];
                MakeButton(__instance.transform, "medick_Tp5",
                    wh.line1, wh.line2, wh.color, wh.scene, font,
                    anc, anc, anc, new Vector2(COL_X, yWh), new Vector2(COL_W, BTN_H));

                NextBtn(out float yEot);
                var eot = Factions[3];
                MakeButton(__instance.transform, "medick_Tp3",
                    eot.line1, eot.line2, eot.color, eot.scene, font,
                    anc, anc, anc, new Vector2(COL_X, yEot), new Vector2(COL_W, BTN_H));

                MelonLogger.Msg("[AdvancedInventory] Teleport buttons injected (4 era groups, 9 buttons).");
            }
            catch (Exception e)
            {
                MelonLogger.Error("[AdvancedInventory] TeleportButtons error: " + e);
            }
        }

        // ── Concurrency guard — only one TravelCoroutine at a time ───────────
        private static bool _travelInProgress = false;

        // ── Info header factory ───────────────────────────────────────────────
        // Creates a non-clickable label box with a gold border and dark bg.
        // Used for the "Open map if needed" notices above each column.

        private static void MakeInfoBox(Transform parent, string objName,
            string eraName,
            float x, float y, float w, float h, TMP_FontAsset font)
        {
            GameObject go = new GameObject(objName);
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta        = new Vector2(w, h);

            // Gold border
            go.AddComponent<Image>().color = GOLD;

            // Very dark inner background
            GameObject bgGO = new GameObject("BG");
            bgGO.transform.SetParent(go.transform, false);
            RectTransform bgRt = bgGO.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(2f, 2f); bgRt.offsetMax = new Vector2(-2f, -2f);
            bgGO.AddComponent<Image>().color = new Color(0.06f, 0.05f, 0.08f, 1f);

            // 3-line centered label:
            //   open map to          ← gray small
            //   DIVINE ERA           ← gold bold  (serves as the section title)
            //   if buttons don't work ← gray small
            GameObject lblGO = new GameObject("Label");
            lblGO.transform.SetParent(go.transform, false);
            RectTransform lrt = lblGO.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(4f, 2f); lrt.offsetMax = new Vector2(-4f, -2f);

            TextMeshProUGUI tmp = lblGO.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = "<color=#777777><size=8>open map to</size></color>\n" +
                       $"<b><color=#FFD700><size=10.5>{eraName}</size></color></b>\n" +
                       "<color=#777777><size=8>if buttons don't work</size></color>";
            tmp.alignment          = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode       = TextOverflowModes.Truncate;
        }

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
                    MelonLogger.Msg("[AdvancedInventory] TravelCoroutine already running — ignoring click");
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
        // Searches ALL Button objects (including inactive faction panels) for
        // a child text element containing the keyword, then invokes onClick.
        // This replicates exactly what "VISIT OBSERVATORY" does natively.

        private static bool TryInvokeFactionVisitButton(string scene)
        {
            string keyword = "";
            if (!SceneToVisitKeyword.TryGetValue(scene, out keyword)) return false;
            keyword = keyword.ToUpper();

            try
            {
                Button[] allButtons = GameObject.FindObjectsOfType<Button>(true);
                MelonLogger.Msg($"[AdvancedInventory] TryInvokeFactionVisitButton '{keyword}': searching {allButtons?.Length ?? 0} buttons");

                foreach (Button btn in allButtons)
                {
                    if (btn == null) continue;

                    // Skip our own injected buttons — they're named medick_Tp0..medick_Tp4
                    string goName = btn.gameObject.name ?? "";
                    if (goName.StartsWith("medick_Tp")) continue;

                    // The game's faction visit buttons say e.g. "VISIT OBSERVATORY"
                    // or "VISIT THE OBSERVATORY". We require "VISIT" to be present so
                    // we never accidentally match our own label text ("The Observatory").

                    // Check TMP_Text children (include inactive)
                    TMP_Text[] tmps = btn.GetComponentsInChildren<TMP_Text>(true);
                    foreach (TMP_Text t in tmps)
                    {
                        if (t == null || string.IsNullOrEmpty(t.text)) continue;
                        string up = t.text.ToUpper();
                        if (!up.Contains("VISIT")) continue;
                        if (!up.Contains(keyword)) continue;
                        MelonLogger.Msg($"[AdvancedInventory] Found faction button: '{t.text}' on '{btn.gameObject.name}' — invoking");
                        btn.onClick.Invoke();
                        return true;
                    }

                    // Check legacy Text children
                    UnityEngine.UI.Text[] legacyTexts = btn.GetComponentsInChildren<UnityEngine.UI.Text>(true);
                    foreach (var t in legacyTexts)
                    {
                        if (t == null || string.IsNullOrEmpty(t.text)) continue;
                        string up = t.text.ToUpper();
                        if (!up.Contains("VISIT")) continue;
                        if (!up.Contains(keyword)) continue;
                        MelonLogger.Msg($"[AdvancedInventory] Found faction button (legacy): '{t.text}' on '{btn.gameObject.name}' — invoking");
                        btn.onClick.Invoke();
                        return true;
                    }
                }

                MelonLogger.Warning($"[AdvancedInventory] Faction visit button for 'VISIT {keyword}' not found in {allButtons?.Length ?? 0} buttons");
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"[AdvancedInventory] TryInvokeFactionVisitButton error: {e.Message}");
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

                MelonLogger.Msg($"[AdvancedInventory] TryClickEraTab '{kw}': {candidates.Count} text matches");

                foreach (var tr in candidates)
                {
                    Transform cur = tr;
                    for (int d = 0; d < 6 && cur != null; d++, cur = cur.parent)
                    {
                        Button b = cur.GetComponent<Button>();
                        if (b != null) { b.onClick.Invoke(); MelonLogger.Msg($"[AdvancedInventory] Era tab Button on '{cur.name}'"); return true; }
                        Toggle tog = cur.GetComponent<Toggle>();
                        if (tog != null) { tog.isOn = true; MelonLogger.Msg($"[AdvancedInventory] Era tab Toggle on '{cur.name}'"); return true; }
                    }
                    cur = tr;
                    for (int d = 0; d < 6 && cur != null; d++, cur = cur.parent)
                    {
                        try
                        {
                            var evd = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                            UnityEngine.EventSystems.ExecuteEvents.Execute(cur.gameObject, evd, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                            MelonLogger.Msg($"[AdvancedInventory] Era tab ExecuteEvents on '{cur.name}'");
                            return true;
                        }
                        catch { }
                    }
                }
            }
            catch (Exception e) { MelonLogger.Warning($"[AdvancedInventory] TryClickEraTab error: {e.Message}"); }
            return false;
        }

        // ── Waypoint finder ───────────────────────────────────────────────────
        // ONLY uses UIWaypointController.waypointsInMenu — these are the world
        // MAP nodes that LoadWaypointScene() actually uses for travel.
        //
        // FindObjectsOfType<UIWaypointStandard> returns ~175 ZONE BEACON objects
        // (always in memory for every discovered zone). They have correct
        // sceneNames but LoadWaypointScene() on a beacon does NOT travel —
        // it only works on the map UI nodes created when the map is opened.
        //
        // Returns null if the map has never been opened this session,
        // which correctly triggers Path 2 (faction button) or Path 3 (MapKeyDown).

        // ── Waypoint finder ───────────────────────────────────────────────────
        // Searches ALL UIWaypointControllers (one per era, 5 total) for the
        // target scene. Observatory and Bazaar live in the Divine-era controller
        // (~63 waypoints); EoT/M_Knight/WeaversHub live in the EoT controller
        // (~14 waypoints). All controllers have waypointsInMenu populated at
        // scene load — no need to open the map first.

        private static UIWaypointStandard FindWaypointForScene(string targetScene)
        {
            try
            {
                UIWaypointController[] all = GameObject.FindObjectsOfType<UIWaypointController>(true);
                if (all == null || all.Length == 0)
                {
                    MelonLogger.Msg("[AdvancedInventory] No UIWaypointControllers found");
                    return null;
                }

                MelonLogger.Msg($"[AdvancedInventory] Searching {all.Length} controllers for '{targetScene}'");

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
                                MelonLogger.Msg($"[AdvancedInventory] Found '{targetScene}' in ctrl '{ctrl.gameObject.name}' ({count} wp)");
                                return w;
                            }
                        }
                        catch { }
                    }
                }

                MelonLogger.Msg($"[AdvancedInventory] '{targetScene}' not found in any controller");
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"[AdvancedInventory] FindWaypointForScene error: {e.Message}");
            }
            return null;
        }

        // ── Travel coroutine ─────────────────────────────────────────────────
        //
        //  PRIMARY goal : open the world map at the correct era with the
        //                 destination node highlighted (exactly like the
        //                 in-game faction "VISIT OBSERVATORY" button does).
        //  BONUS        : if map waypoints become available, auto-travel so
        //                 the user doesn't have to right-click.
        //
        //  Step 1  — close inventory (MapKeyDown is blocked while open)
        //  Step 2A — invoke the faction's own "VISIT X" button (preferred)
        //             → map opens to correct era, node highlighted
        //  Step 2B — fallback: MapKeyDown() + try to click era tab
        //             → map opens, may not be on correct era
        //  Step 3  — poll for UIWaypointController.waypointsInMenu to fill,
        //             then call LoadWaypointScene() for auto-travel

        private static IEnumerator TravelCoroutine(string scene)
        {
            _travelInProgress = true;
            MelonLogger.Msg($"[AdvancedInventory] TravelCoroutine for '{scene}'");
            yield return null;

            // Enable waypoints system
            try
            {
                WaypointManager wm = WaypointManager.getInstance();
                if (wm != null) { wm.WaypointEnabled = true; wm.EnableWaypoint(); }
            }
            catch { }

            // ── Step 1: instant travel — all 5 UIWaypointControllers are
            // populated at scene load. Observatory/Bazaar are in the Divine-era
            // controller; EoT/M_Knight/WeaversHub in the EoT controller.
            // No map-open required.
            UIWaypointStandard wp = FindWaypointForScene(scene);
            if (wp != null)
            {
                try
                {
                    wp.LoadWaypointScene();
                    MelonLogger.Msg($"[AdvancedInventory] Instant travel → '{scene}'");
                    _travelInProgress = false;
                    yield break;
                }
                catch (Exception e) { MelonLogger.Warning($"[AdvancedInventory] LoadWaypointScene failed: {e.Message}"); }
            }

            // ── Step 2: fallback — close inventory, open map, let user travel
            MelonLogger.Msg($"[AdvancedInventory] Waypoint not found — falling back to map open");
            try { if (UIBase.instanceExists && UIBase.instance != null) UIBase.instance.closeInventory(); }
            catch { }
            yield return null;

            bool factionBtn = TryInvokeFactionVisitButton(scene);
            if (!factionBtn)
            {
                try { if (UIBase.instanceExists && UIBase.instance != null) UIBase.instance.MapKeyDown(); }
                catch { }
            }

            MelonLogger.Msg($"[AdvancedInventory] Map opened — user can right-click to travel");
            _travelInProgress = false;
        }

        // ── Logging ───────────────────────────────────────────────────────────

        private static void LogScene()
        {
            try
            {
                string cur = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                MelonLogger.Msg($"[AdvancedInventory] Current scene: '{cur}'");
            }
            catch { }
        }
    }
}
