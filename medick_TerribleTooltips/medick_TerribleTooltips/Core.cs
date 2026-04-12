// ================================================================
//  Core.cs  —  medick_Terrible_Tooltips  v1.2.0
//
//  Entry point, all settings, and public API.
//
//  Settings (in-game "Terrible Tooltips" tab + cfg file):
//
//    TOOLTIP
//      Terrible Tooltips     — master on/off          (default: ON)
//      Tier Colors           — colour affix names by crafting tier (default: ON)
//      Rank Colors           — colour grade letters by roll quality (default: ON)
//
//    GROUND LABELS
//      Ground Label Style    — None / TierAndRank / TierOnly / RankOnly
//                              (default: TierAndRank)
//      Filter Only           — show only on loot-filter highlighted items
//                              (default: OFF = show on all items)
//      Hold Alt to Show      — hide brackets until Alt is held;
//                              KG-style opt-in, default: OFF
//
//    EASTER EGG
//      Teleport to Aaron's House — teleports to the Bazaar
//
//  Fallen_LE_Mods compatibility:
//    MelonMod.RegisteredMelons.Any(m => m.Info.Name == "Terrible Tooltips")
// ================================================================

[assembly: MelonInfo(typeof(medick_Terrible_Tooltips.TerribleTooltipsMod),
    "Terrible Tooltips", "1.3.0", "medick")]
[assembly: MelonGame("Eleventh Hour Games", "Last Epoch")]

namespace medick_Terrible_Tooltips;

public partial class TerribleTooltipsMod : MelonMod
{
    // ── Preferences ──────────────────────────────────────────────────
    internal static MelonPreferences_Category Category;

    // Tooltip
    internal static MelonPreferences_Entry<bool> EnableTooltips;
    internal static MelonPreferences_Entry<bool> TooltipTierColors;
    internal static MelonPreferences_Entry<bool> TooltipRankColors;

    // Ground Labels
    internal static MelonPreferences_Entry<GroundLabelStyle> LabelStyle;
    internal static MelonPreferences_Entry<bool>             LabelFilterOnly;
    internal static MelonPreferences_Entry<bool>             LabelAltKey;

    // ── Ground label style enum ───────────────────────────────────────
    public enum GroundLabelStyle
    {
        None,         // disabled — brackets never shown
        TierAndRank,  // [5A 3C 7S]   ← default
        TierOnly,     // [5 3 7]
        RankOnly      // [A C S]
    }

    // ── Lifecycle ─────────────────────────────────────────────────────
    public override void OnInitializeMelon()
    {
        Category = MelonPreferences.CreateCategory("medick_Terrible_Tooltips");

        // Tooltip
        EnableTooltips   = Category.CreateEntry("EnableTooltips",   true,
            "Terrible Tooltips", "Master on/off for WoW-style tier/grade tooltip colours");
        TooltipTierColors = Category.CreateEntry("TooltipTierColors", true,
            "Tooltip Tier Colors", "Colour affix names by their crafting tier (T1 gray → T7 mythic)");
        TooltipRankColors = Category.CreateEntry("TooltipRankColors", true,
            "Tooltip Rank Colors", "Colour grade letters by roll quality (F gray → S mythic)");

        // Ground Labels
        LabelStyle      = Category.CreateEntry("GroundLabelStyle", GroundLabelStyle.TierAndRank,
            "Ground Label Style", "What to show on items on the ground (None / TierAndRank / TierOnly / RankOnly)");
        LabelFilterOnly = Category.CreateEntry("GroundLabelFilterOnly", false,
            "Ground Labels: Filter Only", "Only show ground labels on loot-filter highlighted items");
        LabelAltKey     = Category.CreateEntry("GroundLabelAltKey", false,
            "Ground Labels: Hold Alt to Show", "Hide ground brackets until you hold Alt (KG-style)");

        Category.SetFilePath("UserData/medick_Terrible_Tooltips.cfg", autoload: true);
        MelonLogger.Msg("[Terrible Tooltips] v1.3.0 loaded.");
    }

    public override void OnUpdate()
    {
        GroundLabels.OnUpdate();
    }

    // ── In-game settings panel ────────────────────────────────────────
    [HarmonyPatch(typeof(SettingsPanelTabNavigable), nameof(SettingsPanelTabNavigable.Awake))]
    private static class Patch_SettingsPanel
    {
        private static void Postfix(SettingsPanelTabNavigable __instance)
        {
            try
            {
                const string Cat = "Terrible Tooltips";

                // ── Settings are registered top-to-bottom ─────────────
                // Utils inserts each item after the last, so call order =
                // display order (no need to reverse).

                // ── Tooltip ───────────────────────────────────────────
                __instance.CreateNewOption_Toggle(Cat,
                    "<color=#FF44FF>Terrible Tooltips</color>",
                    EnableTooltips,
                    v => { EnableTooltips.Value = v; Category.SaveToFile(); },
                    "Master on/off for all tooltip colouring. Turn this off to restore default item tooltips.");

                __instance.CreateNewOption_Toggle(Cat,
                    "<color=#FF44FF>Tooltip: Tier Colors</color>",
                    TooltipTierColors,
                    v => { TooltipTierColors.Value = v; Category.SaveToFile(); },
                    "Colours each affix name by its crafting tier. T1 (gray) = weakest base, T7 (pink) = the best you can craft. Higher tier = stronger rolls and harder to land.");

                __instance.CreateNewOption_Toggle(Cat,
                    "<color=#FF44FF>Tooltip: Rank Colors</color>",
                    TooltipRankColors,
                    v => { TooltipRankColors.Value = v; Category.SaveToFile(); },
                    "Rank = how well an affix actually rolled within its tier. F (gray) = bottom of the range, S (pink) = near perfect roll. Same tier, very different power — ranks tell you the truth.");

                // ── Color Legend ──────────────────────────────────────
                __instance.CreateNewOption_Button(Cat,
                    "◄─── TIER & GRADE LEGEND ───►",
                    "Left = best, right = worst. Use this as your quick reference while checking loot.",
                    () => { });

                __instance.CreateNewOption_Button(Cat,
                    "<color=#FF44FF>T7</color>  >  <color=#FA9E3D>T6</color>  >  <color=#A807FF>T5</color>  >  <color=#77ACFF>T4</color>  >  <color=#16FF0E>T3</color>  >  <color=#E1E1E1>T2</color>  >  <color=#DADADA>T1</color>",
                    "Tier color reference — T1 (gray) is the weakest base tier, T7 (mythic pink) is the strongest. Higher tier = stronger possible rolls, harder to land.",
                    () => { });

                __instance.CreateNewOption_Button(Cat,
                    "<color=#FF44FF>(PoG)</color>    <color=#FF44FF>S</color>  >  <color=#FA9E3D>A</color>  >  <color=#A807FF>B</color>  >  <color=#77ACFF>C</color>  >  <color=#DADADA>F</color>    <color=#DADADA>(RiP)</color>",
                    "Grade rank reference — how well an affix rolled within its tier. F (gray) = bottom of the range, S (mythic pink) = near perfect. Same tier, very different power.",
                    () => { });

                __instance.CreateNewOption_Button(Cat,
                    "◄─── TIER & GRADE LEGEND ───►",
                    "Left = best, right = worst. Use this as your quick reference while checking loot.",
                    () => { });

                // ── Ground Labels ─────────────────────────────────────
                __instance.CreateNewOption_EnumDropdown(Cat,
                    "<color=#FF44FF>Ground Label Style</color>",
                    "What to show on dropped items.  TierAndRank = [5A]   TierOnly = [5]   RankOnly = [A]   None = no brackets",
                    LabelStyle,
                    i => { LabelStyle.Value = (GroundLabelStyle)i; Category.SaveToFile(); });

                __instance.CreateNewOption_Toggle(Cat,
                    "<color=#FF44FF>Ground Labels: Filter Only</color>",
                    LabelFilterOnly,
                    v => { LabelFilterOnly.Value = v; Category.SaveToFile(); },
                    "When ON, tier/rank brackets only appear on items your loot filter highlights. Everything else stays clean. Recommended if the ground feels too busy.");

                __instance.CreateNewOption_Toggle(Cat,
                    "<color=#FF44FF>Ground Labels: Hold Alt to Show</color>",
                    LabelAltKey,
                    v => { LabelAltKey.Value = v; Category.SaveToFile(); },
                    "Brackets are hidden by default and only appear while you hold Left Alt or Right Alt. Lets you check quality on demand without cluttering the screen during normal play.");

                // ── Easter egg ────────────────────────────────────────
                __instance.CreateNewOption_Button(Cat,
                    "<color=#FA9E3D>To Aaron's House</color>",
                    "(don't press this button) For the homies ♥ AaronActionRPG ——— Button not working? ok, if you made it this far, open your map, click on the Divine Era, then close the map and then come back to this button :P",
                    () => TeleportToBazaar());
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Terrible Tooltips] Settings panel error: " + ex.Message);
            }
        }
    }

    // ── Easter egg: teleport to the Bazaar ───────────────────────────
    // Adapted from medick_Advanced_Inventory's proven travel system.
    // Uses the same path as the world map Travel button.
    private static bool s_travelInProgress = false;

    private static void TeleportToBazaar()
    {
        if (s_travelInProgress)
        {
            MelonLogger.Msg("[Terrible Tooltips] Travel already in progress.");
            return;
        }
        MelonCoroutines.Start(TravelCoroutine("Bazaar"));
    }

    private static IEnumerator TravelCoroutine(string scene)
    {
        s_travelInProgress = true;
        MelonLogger.Msg($"[Terrible Tooltips] To Aaron's House! Travelling to '{scene}'...");
        yield return null;

        // Enable the waypoint system
        try
        {
            WaypointManager wm = WaypointManager.getInstance();
            if (wm != null) { wm.WaypointEnabled = true; wm.EnableWaypoint(); }
        }
        catch { }

        // Step 1: direct travel — works when Divine Era controllers are already active
        UIWaypointStandard found = FindWaypointForScene(scene);
        if (found != null)
        {
            try
            {
                found.LoadWaypointScene();
                MelonLogger.Msg("[Terrible Tooltips] ♥ To Aaron's House!");
                s_travelInProgress = false;
                yield break;
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"[Terrible Tooltips] LoadWaypointScene failed: {e.Message}");
            }
        }

        // Step 2: waypoint controller not active (player is in a different era zone).
        // Close settings, open the world map, click the Divine Era tab to populate
        // those controllers, then travel.
        MelonLogger.Msg("[Terrible Tooltips] Divine era not loaded — opening map to activate it...");

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

        // Click the Divine Era tab so those waypoint controllers populate
        TryClickEraTab("DIVINE");
        yield return null;
        yield return null; // give controllers time to initialise

        // Try again now that Divine era is active
        found = FindWaypointForScene(scene);
        if (found != null)
        {
            try
            {
                found.LoadWaypointScene();
                MelonLogger.Msg("[Terrible Tooltips] ♥ To Aaron's House! (via map activation)");
                s_travelInProgress = false;
                yield break;
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"[Terrible Tooltips] LoadWaypointScene (retry) failed: {e.Message}");
            }
        }

        // Last resort: map is already open, player can navigate manually
        MelonLogger.Msg("[Terrible Tooltips] Could not auto-travel — map is open for manual travel.");
        s_travelInProgress = false;
    }

    // Searches all UIWaypointControllers (one per era, populated when that era is shown)
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
                            MelonLogger.Msg($"[Terrible Tooltips] Found '{targetScene}' in '{ctrl.gameObject.name}'");
                            return w;
                        }
                    }
                    catch { }
                }
            }
        }
        catch (Exception e)
        {
            MelonLogger.Warning($"[Terrible Tooltips] FindWaypointForScene error: {e.Message}");
        }
        return null;
    }

    // Clicks the era tab on the world map matching the keyword (e.g. "DIVINE").
    // This activates that era's UIWaypointController so its waypoints populate.
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
                    if (b != null) { b.onClick.Invoke(); MelonLogger.Msg($"[Terrible Tooltips] Clicked era tab '{cur.name}'"); return; }
                    Toggle tog = cur.GetComponent<Toggle>();
                    if (tog != null) { tog.isOn = true; MelonLogger.Msg($"[Terrible Tooltips] Toggled era tab '{cur.name}'"); return; }
                }
            }

            MelonLogger.Msg($"[Terrible Tooltips] Era tab '{kw}' not found.");
        }
        catch (Exception e)
        {
            MelonLogger.Warning($"[Terrible Tooltips] TryClickEraTab error: {e.Message}");
        }
    }
}

// ================================================================
//  TerribleTooltipsAPI  — public surface for other mods
//
//  Fallen_LE_Mods detection:
//    MelonMod.RegisteredMelons.Any(m => m.Info.Name == "Terrible Tooltips")
//  Add alongside the existing kg check:
//    || m.Info.Name == "Terrible Tooltips"
// ================================================================
public static class TerribleTooltipsAPI
{
    // Same signature as kg_LastEpoch_Improvements.CheckFilter.
    // Returns true when itemData passes the loot filter and the matched
    // rule is emphasized (starred ★). bypass=true skips that requirement.
    public static bool CheckFilter(ItemDataUnpacked itemData, out Rule rule, bool bypass = false)
    {
        rule = null;
        try
        {
            if (itemData == null) return false;
            if (itemData.rarity == 9) return true;

            ItemFilter filter = ItemFilterManager.Instance?.Filter;
            if (filter == null) return false;

            if (filter.Match(itemData, out _, out _,
                             out int matchingRuleNumber,
                             out _, out _, out _, out _, out _) == Rule.RuleOutcome.HIDE)
                return false;

            if (matchingRuleNumber <= 0) return false;
            int orderedIndex = filter.rules.Count - matchingRuleNumber;
            if (orderedIndex >= filter.rules.Count) return false;

            rule = filter.rules[orderedIndex];
            if (rule == null) return false;

            return bypass || rule.emphasized;
        }
        catch { return false; }
    }
}
