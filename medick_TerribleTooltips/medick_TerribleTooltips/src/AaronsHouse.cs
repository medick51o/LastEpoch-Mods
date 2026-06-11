// ================================================================
//  AaronsHouse.cs — the easter egg. "(don't press this button)
//  For the homies ♥ AaronActionRPG"
//
//  Teleports to the Bazaar via the game's own waypoint-click path
//  (LoadWaypointScene — unlock-gated like the map, after the softlock
//  scare). The Divine-era map-tab requirement and its workaround joke
//  in the button description are CANONIZED quirk — medick deliberately
//  declined to import Advanced Inventory's silent primer to avoid
//  cross-mod conflicts. The jank is part of the homage.
// ================================================================

namespace medick_Terrible_Tooltips;

public static class AaronsHouse
{
    private static bool s_travelInProgress = false;

    public static void TeleportToBazaar()
    {
        if (s_travelInProgress)
        {
            Dbg.Log("travel already in progress");
            return;
        }
        MelonCoroutines.Start(TravelCoroutine("Bazaar"));
    }

    private static IEnumerator TravelCoroutine(string scene)
    {
        s_travelInProgress = true;
        MelonLogger.Msg($"To Aaron's House! Travelling to '{scene}'...");
        yield return null;

        // Enable the waypoint system
        try
        {
            WaypointManager wm = WaypointManager.getInstance();
            if (wm != null) { wm.WaypointEnabled = true; wm.EnableWaypoint(); }
        }
        catch { }

        // Step 1: direct travel — works when Divine Era controllers are active
        UIWaypointStandard found = FindWaypointForScene(scene);
        if (found != null)
        {
            bool ok = false;
            try
            {
                found.LoadWaypointScene();
                ok = true;
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"LoadWaypointScene failed: {e.Message}");
            }
            if (ok)
            {
                MelonLogger.Msg("♥ To Aaron's House!");
                s_travelInProgress = false;
                yield break;
            }
        }

        // Step 2: Divine controllers not active — close settings, open the
        // map, click the Divine Era tab to populate them, then retry.
        Dbg.Log("Divine era not loaded — opening map to activate it");

        try
        {
            var panel = UnityEngine.Object.FindObjectOfType<SettingsPanelTabNavigable>();
            if (panel != null) panel.gameObject.SetActive(false);
        }
        catch { }
        yield return null;

        try
        {
            if (UIBase.instanceExists && UIBase.instance != null)
                UIBase.instance.MapKeyDown();
        }
        catch { }

        yield return null;
        yield return null; // give the map UI time to open

        TryClickEraTab("DIVINE");
        yield return null;
        yield return null; // give controllers time to initialise

        found = FindWaypointForScene(scene);
        if (found != null)
        {
            bool ok = false;
            try
            {
                found.LoadWaypointScene();
                ok = true;
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"LoadWaypointScene (retry) failed: {e.Message}");
            }
            if (ok)
            {
                MelonLogger.Msg("♥ To Aaron's House! (via map activation)");
                s_travelInProgress = false;
                yield break;
            }
        }

        // Last resort: map is open — the player can navigate manually
        // (and the button description tells them the ritual :P)
        MelonLogger.Msg("Could not auto-travel — map is open for manual travel.");
        s_travelInProgress = false;
    }

    private static UIWaypointStandard FindWaypointForScene(string targetScene)
    {
        try
        {
            UIWaypointController[] all =
                UnityEngine.Object.FindObjectsOfType<UIWaypointController>(true);
            if (all == null || all.Length == 0) return null;

            foreach (UIWaypointController ctrl in all)
            {
                int count = ctrl.waypointsInMenu?.Count ?? 0;
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        UIWaypointStandard w =
                            ctrl.waypointsInMenu[i]?.TryCast<UIWaypointStandard>();
                        if (w == null) continue;
                        if ((w.sceneName ?? "") == targetScene)
                        {
                            Dbg.Log($"found '{targetScene}' in '{ctrl.gameObject.name}'");
                            return w;
                        }
                    }
                    catch { }
                }
            }
        }
        catch (Exception e)
        {
            MelonLogger.Warning($"waypoint search error: {e.Message}");
        }
        return null;
    }

    // Clicks the world-map era tab matching the keyword so that era's
    // UIWaypointController populates.
    private static void TryClickEraTab(string eraKeyword)
    {
        string kw = eraKeyword.ToUpper();
        try
        {
            var candidates = new List<Transform>();

            foreach (var t in UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>(true))
                if (t != null && !string.IsNullOrEmpty(t.text) && t.text.ToUpper().Contains(kw))
                    candidates.Add(t.transform);

            foreach (var tr in candidates)
            {
                Transform cur = tr;
                for (int d = 0; d < 6 && cur != null; d++, cur = cur.parent)
                {
                    Button b = cur.GetComponent<Button>();
                    if (b != null) { b.onClick.Invoke(); Dbg.Log($"clicked era tab '{cur.name}'"); return; }
                    Toggle tog = cur.GetComponent<Toggle>();
                    if (tog != null) { tog.isOn = true; Dbg.Log($"toggled era tab '{cur.name}'"); return; }
                }
            }

            Dbg.Log($"era tab '{kw}' not found");
        }
        catch (Exception e)
        {
            MelonLogger.Warning($"era tab click error: {e.Message}");
        }
    }
}
