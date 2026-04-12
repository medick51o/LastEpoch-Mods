// ================================================================
//  AffixInjector.cs  —  medick_Terrible_Tooltips
//
//  Injects tier+grade brackets into affix tooltip strings so that
//  TooltipPatch.cs can read them and apply WoW-style colours.
//
//  This is the standalone replacement for the equivalent patches
//  in kg_LastEpoch_Improvements. Terrible Tooltips does NOT require
//  KG to be installed.
//
//  The bracket format written here matches the regex patterns in
//  TooltipPatch.cs exactly:
//
//    Tiered affix:   [<color=#A807FF>5</color><color=#FA9E3D>A</color>] affix text
//    Untiered affix: [<color=#FA9E3D>A</color>] affix text
//
//  Three hooks cover all affix types:
//    • AffixFormatter       — normal craftable affixes (prefix/suffix)
//    • UniqueBasicModFormatter — unique / legendary fixed mods
//    • ImplicitFormatter    — implicit stats on the item base
// ================================================================

namespace medick_Terrible_Tooltips;

public static class AffixInjector
{
    // ── Shared bracket builder ────────────────────────────────────────

    /// <summary>
    /// Prepends a tier+grade bracket to <paramref name="affixStr"/>.
    /// Bracket format is intentionally identical to what KG's
    /// Letter_Style_No_Percent mode produces so TooltipPatch regex hits.
    /// </summary>
    private static string InjectBracket(string affixStr, float rollFloat, int tier)
    {
        double roll        = Math.Round(rollFloat * 100.0, 1);
        string gradeColor  = Colors.GradeLetterColor(roll);
        string gradeLetter = Colors.GradeLetter(roll);

        string bracket;
        if (tier > 0)
        {
            string tierColor = Colors.TierColor(tier);
            // e.g.  [<color=#A807FF>5</color><color=#FA9E3D>A</color>]
            bracket = $"[<color={tierColor}>{tier}</color><color={gradeColor}>{gradeLetter}</color>] ";
        }
        else
        {
            // Untiered (unique/set/implicit): grade letter only
            // e.g.  [<color=#FA9E3D>A</color>]
            bracket = $"[<color={gradeColor}>{gradeLetter}</color>] ";
        }

        // Insert before the last newline if the string is multi-line
        // (same convention KG uses so the bracket stays on the first line)
        int lastNewLine = affixStr.LastIndexOf('\n');
        return lastNewLine == -1
            ? bracket + affixStr
            : bracket + affixStr.Insert(lastNewLine, "");
    }

    // ── Patch: normal craftable affixes (prefix / suffix) ────────────

    [HarmonyPatch(typeof(TooltipItemManager), nameof(TooltipItemManager.AffixFormatter))]
    private static class Patch_AffixFormatter
    {
        private static void Postfix(ItemDataUnpacked item, ItemAffix affix,
                                    ref string __result)
        {
            if (!TerribleTooltipsMod.EnableTooltips.Value) return;
            if (item == null || affix == null) return;
            try
            {
                __result = InjectBracket(__result, affix.getRollFloat(), affix.DisplayTier);
            }
            catch { }
        }
    }

    // ── Patch: unique / legendary fixed mods ─────────────────────────

    [HarmonyPatch(typeof(TooltipItemManager), nameof(TooltipItemManager.UniqueBasicModFormatter))]
    private static class Patch_UniqueFormatter
    {
        private static void Postfix(ItemDataUnpacked item, ref string __result,
                                    int uniqueModIndex, float modifierValue)
        {
            if (!TerribleTooltipsMod.EnableTooltips.Value) return;
            if (item == null) return;
            try
            {
                if (item.uniqueID > UniqueList.instance.uniques.Count) return;
                if (uniqueModIndex < 0) return;
                if (UniqueList.instance.uniques[item.uniqueID] is not { } uniqueEntry) return;

                UniqueItemMod uniqueMod = uniqueEntry.mods[uniqueModIndex];
                float min  = uniqueMod.value;
                float max  = uniqueMod.maxValue;
                float roll = (min == max || modifierValue > max)
                    ? 1f
                    : (modifierValue - min) / (max - min);

                __result = InjectBracket(__result, roll, tier: 0);
            }
            catch { }
        }
    }

    // ── Patch: implicit stats ─────────────────────────────────────────

    [HarmonyPatch(typeof(TooltipItemManager), nameof(TooltipItemManager.ImplicitFormatter))]
    private static class Patch_ImplicitFormatter
    {
        private static void Postfix(ItemDataUnpacked item, int implicitNumber,
                                    ref string __result)
        {
            if (!TerribleTooltipsMod.EnableTooltips.Value) return;
            if (item == null) return;
            try
            {
                float roll = item.getImplictRollFloat((byte)implicitNumber);
                __result = InjectBracket(__result, roll, tier: 0);
            }
            catch { }
        }
    }
}
