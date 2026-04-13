// ================================================================
//  GroundLabels.cs  —  medick_Terrible_Tooltips
//
//  Appends tier+grade brackets to dropped item name labels.
//
//    PLATED BELT  [5A 7C 4C 1S]
//
//  Style options (set via settings menu):
//    None         — brackets never shown
//    TierAndRank  — [5A]  tier number + grade letter  ← default
//    TierOnly     — [5]   tier number only
//    RankOnly     — [A]   grade letter only
//
//  Filter Only — when ON, only show on loot-filter highlighted items
//  Hold Alt    — when ON, hide brackets until Left/Right Alt is held
//                (KG-style; off by default)
//
//  Works alongside Fallen_LE_Mods — see Core.cs for Fallen API notes.
//  Credits: logic adapted from KG / war3i4i's Experimental.cs.
// ================================================================

namespace medick_Terrible_Tooltips;

public static class GroundLabels
{
    // Zero-width space marker — prevents double-processing a label
    private const string Marker = "\u200B\u200B\u200B";

    // Matches EHG's trailing rule number format: "Ruby Ring (53)" or "<font=...>Ring</font> (53)"
    private static readonly Regex s_ruleNumRegex = new(@"^(.*?)\s+\((\d+)\)\s*$",
        RegexOptions.Compiled | RegexOptions.Singleline);

    // ── Alt-key cache ─────────────────────────────────────────────────
    // Stores (label, plainText, bracketedText) for active ground items
    // when "Hold Alt to Show" is enabled. Toggled every frame in OnUpdate.
    private static readonly List<(GroundItemLabel label, string plain, string bracketed)>
        s_altCache = new();

    private static bool s_altWasHeld = false;

    // Called from TerribleTooltipsMod.OnUpdate()
    public static void OnUpdate()
    {
        if (!TerribleTooltipsMod.LabelAltKey.Value) return;
        if (s_altCache.Count == 0) return;

        // Clean up labels that are gone / hidden
        s_altCache.RemoveAll(e =>
            e.label == null || !e.label.gameObject.activeInHierarchy);

        bool altHeld = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        if (altHeld == s_altWasHeld) return;   // no change — skip costly text updates
        s_altWasHeld = altHeld;

        foreach (var (label, plain, bracketed) in s_altCache)
        {
            try
            {
                if (label.itemText == null) continue;
                string target = altHeld ? bracketed : plain;
                // Re-apply marker so patch doesn't re-trigger
                label.itemText.text = (label.emphasized ? target.ToUpper() : target) + Marker;
                label.sceneFollower?.calculateDimensions();
            }
            catch { }
        }
    }

    // ── Patch ─────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(GroundItemLabel),
        nameof(GroundItemLabel.SetGroundTooltipText), typeof(bool))]
    private static class Patch_GroundLabel
    {
        private static void Postfix(GroundItemLabel __instance)
        {
            if (TerribleTooltipsMod.LabelStyle.Value ==
                TerribleTooltipsMod.GroundLabelStyle.None) return;
            MelonCoroutines.Start(DelayRoutine(__instance));
        }
    }

    // ── Label builder ─────────────────────────────────────────────────
    private static IEnumerator DelayRoutine(GroundItemLabel item)
    {
        // One-frame yield — let EHG finish writing the base label text first
        yield return null;

        if (item == null || !item) yield break;

        ItemDataUnpacked itemData;
        try
        {
            itemData = item.getItemData();
            if (itemData == null) yield break;
        }
        catch { yield break; }

        TextMeshProUGUI tmp = item.itemText;
        if (!tmp) yield break;

        // Already processed — skip
        if (tmp.text.Contains(Marker)) yield break;

        // Defer unique/set/legendary items to Fallen_LE_Mods which shows
        // LP, Weaver's Will, NEW, OWNED and stash comparison — much better.
        // We only handle normal crafting items with tier/rank brackets.
        if (itemData.isUniqueSetOrLegendary()) yield break;

        // Filter check
        if (TerribleTooltipsMod.LabelFilterOnly.Value)
            if (!TerribleTooltipsAPI.CheckFilter(itemData, out _, true)) yield break;

        // ── Read whatever EHG already wrote (includes rule number, e.g. "PLATED BELT 32") ──
        // Preserve this so EHG's native rule number display is never lost.
        string ehgText  = tmp.text.Replace(Marker, "").Trim();
        string ehgBase  = !string.IsNullOrEmpty(ehgText)
            ? ehgText
            : (item.emphasized ? itemData.FullName.ToUpper() : itemData.FullName);

        // ── Parse rule number out of EHG text (e.g. "PLATED BELT 32" → name="PLATED BELT", num="32") ──
        Match  ruleMatch  = s_ruleNumRegex.Match(ehgBase);
        bool   hasRuleNum = ruleMatch.Success;
        string cleanName  = hasRuleNum ? ruleMatch.Groups[1].Value : ehgBase;
        string ruleNum    = hasRuleNum ? ruleMatch.Groups[2].Value : "";

        // ── Build bracket ─────────────────────────────────────────────
        string bracket = BuildBracket(itemData);

        string plain     = Assemble(cleanName, ruleNum, hasRuleNum, "");
        string bracketed = Assemble(cleanName, ruleNum, hasRuleNum, bracket);

        // ── Alt-key mode ──────────────────────────────────────────────
        if (TerribleTooltipsMod.LabelAltKey.Value)
        {
            tmp.text = "";
            tmp.text = plain + Marker;
            item.sceneFollower?.calculateDimensions();

            s_altCache.RemoveAll(e => e.label == item);
            s_altCache.Add((item, plain, bracketed));
            yield break;
        }

        // ── Always-visible mode ───────────────────────────────────────
        tmp.text = "";
        tmp.text = bracketed + Marker;
        item.sceneFollower?.calculateDimensions();
    }

    // ── Bracket builder ───────────────────────────────────────────────
    private static string BuildBracket(ItemDataUnpacked itemData)
    {
        if (itemData.affixes.Count == 0) return "";

        var style = TerribleTooltipsMod.LabelStyle.Value;
        var sb    = new StringBuilder("[");

        bool first = true;
        foreach (ItemAffix affix in itemData.affixes)
        {
            if (!first) sb.Append(' ');
            first = false;

            double roll        = Math.Round(affix.getRollFloat() * 100.0, 1);
            int    tier        = affix.DisplayTier;
            string tierColor   = Colors.TierColor(tier);
            string letterColor = Colors.GradeLetterColor(roll);
            string letter      = Colors.GradeLetter(roll);

            switch (style)
            {
                case TerribleTooltipsMod.GroundLabelStyle.TierAndRank:
                    // e.g.  5A  (tier in tier colour, letter in grade colour)
                    if (tier > 0)
                        sb.Append($"<color={tierColor}>{tier}</color>");
                    sb.Append($"<color={letterColor}>{letter}</color>");
                    break;

                case TerribleTooltipsMod.GroundLabelStyle.TierOnly:
                    // e.g.  5  (tier in tier colour)
                    if (tier > 0)
                        sb.Append($"<color={tierColor}>{tier}</color>");
                    else
                        sb.Append("-");
                    break;

                case TerribleTooltipsMod.GroundLabelStyle.RankOnly:
                    // e.g.  A  (letter in grade colour)
                    sb.Append($"<color={letterColor}>{letter}</color>");
                    break;
            }
        }

        sb.Append(']');
        return sb.ToString();
    }

    // ── Static label assembler — avoids IL2CPP closure issues with local functions ──
    private static string Assemble(string cleanName, string ruleNum, bool hasRuleNum, string bracket)
    {
        string br = !string.IsNullOrEmpty(bracket) ? " " + bracket : "";

        if (!hasRuleNum)
            return cleanName + br;

        string num = "(" + ruleNum + ")";
        return TerribleTooltipsMod.LabelRulePosition.Value switch
        {
            TerribleTooltipsMod.RuleNumberPosition.Start => num + " " + cleanName + br,
            TerribleTooltipsMod.RuleNumberPosition.End   => cleanName + br + " " + num,
            _                                            => cleanName + " " + num + br
        };
    }
}
