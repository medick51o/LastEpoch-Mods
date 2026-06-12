// ================================================================
//  SettingsUi.cs — the in-game "Terrible Tooltips" settings category.
//
//  Row order is display order (each row is inserted after the last).
//  Titles keep their mythic-pink color tags — brand voice.
//  Legend rows are honest info rows (no checkbox, no apology).
//  The 69 stays. Forever.
// ================================================================

namespace medick_Terrible_Tooltips;

internal static class SettingsUi
{
    private const string Cat = "Terrible Tooltips";

    [HarmonyPatch(typeof(SettingsPanelTabNavigable), nameof(SettingsPanelTabNavigable.Awake))]
    internal static class Patch_SettingsPanel
    {
        private static void Postfix(SettingsPanelTabNavigable __instance)
        {
            try { Build(__instance); }
            catch (Exception ex)
            {
                NativeSettings.WarnDegradedOnce("settings build error: " + ex.Message);
            }
        }
    }

    private static void Build(SettingsPanelTabNavigable settings)
    {
        if (!NativeSettings.TemplatesAvailable(settings)) return;

        // ── Tooltip ───────────────────────────────────────────────────
        NativeSettings.CreateToggle(settings, Cat, "TT - Master",
            "<color=#FF44FF>Terrible Tooltips</color>",
            "Master on/off for all tooltip colouring. Turn this off to restore default item tooltips.",
            Prefs.EnableTooltips.Value,
            v => { Prefs.EnableTooltips.Value = v; Prefs.Save(); });

        NativeSettings.CreateToggle(settings, Cat, "TT - Tier Colors",
            "<color=#FF44FF>Tooltip: Tier Colors</color>",
            "Master switch for tier colouring — tints the 'Tier N' signal and (when Affix Name Color = TierColor) the affix text itself. T1 (gray) = weakest base, T7 (pink) = the best you can craft.",
            Prefs.TooltipTierColors.Value,
            v => { Prefs.TooltipTierColors.Value = v; Prefs.Save(); });

        NativeSettings.CreateToggle(settings, Cat, "TT - Rank Colors",
            "<color=#FF44FF>Tooltip: Rank Colors</color>",
            "Colours the S/A/B/C/F grade letters by roll quality. F (gray) = bottom of the range, S (pink) = near perfect. Same tier, very different power — the grades tell you the truth. ('Show Grade Letters' below controls whether the letters appear at all.)",
            Prefs.TooltipRankColors.Value,
            v => { Prefs.TooltipRankColors.Value = v; Prefs.Save(); });

        // ── v3 clean line — the layout toy box ────────────────────────
        // "by us giving the players the option to do B, do C, do A —
        //  an EHG dev can toy with it and create ideas" — the ship gate.
        NativeSettings.CreateEnumDropdown(settings, Cat, "TT - Layout",
            "<color=#FF44FF>Tooltip Layout</color>",
            "Where the Tier·Grade signal sits on each affix line. " +
            "BadgeLeft = 'Tier 5·A' leads the line. SignalRight = signal in a column at the right (long affix names gracefully fall back to Trailing). " +
            "Trailing = name — Tier 5 A. Pick how your eye likes to scan.",
            Prefs.Layout,
            i => { Prefs.Layout.Value = (TooltipLayout)i; Prefs.Save(); });

        NativeSettings.CreateEnumDropdown(settings, Cat, "TT - Signal Style",
            "<color=#FF44FF>Signal Style</color>",
            "Badge = the Tier and Grade render as colored chips — tier-colored plate, contrast text — so the signal reads as a label while the affix text keeps the color story. " +
            "PlainText = colored text only, no chips.",
            Prefs.Style,
            i => { Prefs.Style.Value = (SignalStyle)i; Prefs.Save(); });

        NativeSettings.CreateEnumDropdown(settings, Cat, "TT - Name Color",
            "<color=#FF44FF>Affix Name Color</color>",
            "TierColor = the affix text itself wears its tier color, so you identify it instantly. " +
            "GameDefault = the game's own text color — only the Tier·Grade signal is colored.",
            Prefs.NameColorMode,
            i => { Prefs.NameColorMode.Value = (AffixNameColorMode)i; Prefs.Save(); });

        NativeSettings.CreateToggle(settings, Cat, "TT - Grade Letters",
            "<color=#FF44FF>Show Grade Letters</color>",
            "The S/A/B/C/F roll grade on each affix line (coloured by 'Tooltip: Rank Colors' above). Turn this off if you only want tiers — the letters are gone, no hard feelings.",
            Prefs.ShowGradeLetters.Value,
            v => { Prefs.ShowGradeLetters.Value = v; Prefs.Save(); });

        NativeSettings.CreateToggle(settings, Cat, "TT - Pin Ranges",
            "<color=#FF44FF>Always Show Ranges</color>",
            "Pin the 'Range: X to Y' lines permanently. Default OFF — they're clutter; hold Alt while hovering to peek instead.",
            Prefs.AlwaysShowRanges.Value,
            v => { Prefs.AlwaysShowRanges.Value = v; Prefs.Save(); });

        NativeSettings.CreateToggle(settings, Cat, "TT - Pin Tier Details",
            "<color=#FF44FF>Always Show Tier Details</color>",
            "Pin the full 'Tier: 5 (max craftable)' line. Default OFF — the tier is already on the clean line; hold Alt while hovering to peek at the detail.",
            Prefs.AlwaysShowTierDetails.Value,
            v => { Prefs.AlwaysShowTierDetails.Value = v; Prefs.Save(); });

        NativeSettings.CreateInfoRow(settings, Cat, "TT - Alt Hint",
            "<color=#FF44FF>Hold Alt = deep view</color>",
            "While hovering an item, hold Alt to reveal the full detail under each affix — ranges, craft info, everything the clean line tucked away. Release Alt and it's clean again.");

        // ── Color legend (honest info rows) ───────────────────────────
        NativeSettings.CreateInfoRow(settings, Cat, "TT - Tier Legend",
            "<color=#FF44FF>T7</color>  >  <color=#FA9E3D>T6</color>  >  <color=#A807FF>T5</color>  >  <color=#77ACFF>T4</color>  >  <color=#16FF0E>T3</color>  >  <color=#E1E1E1>T2</color>  >  <color=#DADADA>T1</color>",
            "Tier color reference — T1 (gray) is the weakest base tier, T7 (mythic pink) is the strongest. Higher tier = stronger possible rolls, harder to land.");

        NativeSettings.CreateInfoRow(settings, Cat, "TT - Rank Legend",
            "<color=#FF44FF>(PoG)</color>    <color=#FF44FF>S</color>  >  <color=#FA9E3D>A</color>  >  <color=#A807FF>B</color>  >  <color=#77ACFF>C</color>  >  <color=#DADADA>F</color>    <color=#DADADA>(RiP)</color>",
            "Grade rank reference — how well an affix rolled within its tier. F (gray) = bottom of the range, S (mythic pink) = near perfect. Same tier, very different power.");

        // ── Ground labels ─────────────────────────────────────────────
        NativeSettings.CreateEnumDropdown(settings, Cat, "TT - Label Style",
            "<color=#FF44FF>Ground Label Style</color>",
            "What to show on dropped items.  TierAndRank = [5A]   TierOnly = [5]   RankOnly = [A]   None = no brackets",
            Prefs.LabelStyle,
            i => { Prefs.LabelStyle.Value = (GroundLabelStyle)i; Prefs.Save(); });

        NativeSettings.CreateToggle(settings, Cat, "TT - Filter Only",
            "<color=#FF44FF>Ground Labels: Filter Only</color>",
            "When ON, tier/rank brackets only appear on items your loot filter highlights. Everything else stays clean. Recommended if the ground feels too busy.",
            Prefs.LabelFilterOnly.Value,
            v => { Prefs.LabelFilterOnly.Value = v; Prefs.Save(); });

        NativeSettings.CreateToggle(settings, Cat, "TT - Hold Alt",
            "<color=#FF44FF>Ground Labels: Hold Alt to Show</color>",
            "Brackets are hidden by default and only appear while you hold Left Alt or Right Alt. Lets you check quality on demand without cluttering the screen during normal play.",
            Prefs.LabelAltKey.Value,
            v => { Prefs.LabelAltKey.Value = v; Prefs.Save(); });

        // ── Filter rule # ─────────────────────────────────────────────
        NativeSettings.CreateEnumDropdown(settings, Cat, "TT - Rule Display",
            "<color=#FF44FF>Tooltip: Show Filter Rule #</color>",
            "Adds the matched loot filter rule number to the item tooltip on hover. " +
            "Off = nothing. NumberOnly = 'Rule#69'. NumberAndName = 'Rule #69: Maxroll told me to pick this up blah blah'. " +
            "Works alongside Fallen Star's Improved Tooltips.",
            Prefs.ShowFilterRuleNumber,
            i => { Prefs.ShowFilterRuleNumber.Value = (FilterRuleDisplay)i; Prefs.Save(); });

        NativeSettings.CreateEnumDropdown(settings, Cat, "TT - Rule Position",
            "<color=#FF44FF>Ground Label: Rule # Position</color>",
            "Controls where the game's own filter rule number (the one EHG — Eleventh Hour Games, the devs — draws on ground labels) appears relative to our tier/rank brackets. " +
            "See the reference box below for a visual preview of each option. " +
            "Only applies when Ground Label Style is not set to None.",
            Prefs.LabelRulePosition,
            i => { Prefs.LabelRulePosition.Value = (RuleNumberPosition)i; Prefs.Save(); });

        NativeSettings.CreateInfoRow(settings, Cat, "TT - Position Example",
            "DEF: BELT <color=#FA9E3D>(69)</color> [<color=#A807FF>5</color><color=#FA9E3D>A</color>]      START: <color=#FA9E3D>(69)</color> BELT [<color=#A807FF>5</color><color=#FA9E3D>A</color>]      END: BELT [<color=#A807FF>5</color><color=#FA9E3D>A</color>] <color=#FA9E3D>(69)</color>",
            "Visual reference for the Rule # Position setting above — the orange 69 is EHG's filter rule number, the colored brackets are Terrible Tooltips' tier/rank info. Pick whichever layout feels cleanest to you.");

        // ── Easter egg — always LAST ──────────────────────────────────
        NativeSettings.CreateButton(settings, Cat, "TT - Aarons House",
            "<color=#FA9E3D>To Aaron's House</color>",
            "(don't press this button) For the homies ♥ AaronActionRPG ——— Button not working? ok, if you made it this far, open your map, click on the Divine Era, then close the map and then come back to this button :P",
            () => AaronsHouse.TeleportToBazaar());
    }
}
