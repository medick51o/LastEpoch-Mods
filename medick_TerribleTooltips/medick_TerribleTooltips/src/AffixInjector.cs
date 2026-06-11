// ================================================================
//  AffixInjector.cs — injects tier+grade brackets into affix tooltip
//  strings so TooltipRecolor can read them and apply WoW-style colours.
//
//  Standalone replacement for the equivalent patches in
//  kg_LastEpoch_Improvements (credit: KG / war3i4i) — Terrible Tooltips
//  does NOT require KG to be installed. Bracket format is intentionally
//  identical to KG's Letter_Style output:
//
//    Tiered affix:   [<color=#A807FF>5</color><color=#FA9E3D>A</color>] text
//    Untiered affix: [<color=#FA9E3D>A</color>] text
//
//  Three hooks cover every stat line:
//    • AffixFormatter          — normal craftable affixes (prefix/suffix)
//    • UniqueBasicModFormatter — unique / legendary fixed mods
//    • ImplicitFormatter       — implicit stats on the item base
//
//  v2.0.0 — THE LEGENDARY GRADING FIX (zoundb's Nexus report):
//  v1 (and KG before it) RECONSTRUCTED the unique-mod roll from display
//  values: (modifierValue - min) / (max - min). On legendary-converted
//  uniques the LP roll multiplier and display semantics break that
//  arithmetic — every stat graded C, even max rolls. The game stores the
//  actual roll byte in ItemDataUnpacked.uniqueRolls, indexed by
//  UniqueItemMod.rollID (a field neither KG nor v1 ever read; discovered
//  via research/ApiProbe). v2 reads the stored truth.
// ================================================================

namespace medick_Terrible_Tooltips;

public static class AffixInjector
{
    // ── Shared bracket builder ────────────────────────────────────────

    private static string InjectBracket(string affixStr, float rollFloat, int tier)
    {
        double roll        = Math.Round(rollFloat * 100.0, 1);
        string gradeColor  = Colors.GradeLetterColor(roll);
        string gradeLetter = Colors.GradeLetter(roll);

        string bracket = tier > 0
            ? $"[<color={Colors.TierColor(tier)}>{tier}</color><color={gradeColor}>{gradeLetter}</color>] "
            : $"[<color={gradeColor}>{gradeLetter}</color>] ";

        // Prepending keeps the bracket on the first line of multi-line
        // strings. (v1 had a dead Insert(lastNewLine, "") branch here —
        // a no-op since forever; the fleet finally retired it.)
        return bracket + affixStr;
    }

    // ── Patch: normal craftable affixes (prefix / suffix) ────────────
    // affix.getRollFloat() is the game's own 0–1 roll — correct since v1.

    [HarmonyPatch(typeof(TooltipItemManager), nameof(TooltipItemManager.AffixFormatter))]
    internal static class Patch_AffixFormatter
    {
        private static void Postfix(ItemDataUnpacked item, ItemAffix affix,
                                    ref string __result)
        {
            if (!Prefs.EnableTooltips.Value) return;
            if (item == null || affix == null) return;
            try
            {
                __result = InjectBracket(__result, affix.getRollFloat(), affix.DisplayTier);
            }
            catch { }
        }
    }

    // ── Patch: unique / legendary fixed mods ─────────────────────────
    // THE FIX lives here. Roll source priority:
    //   1. Fixed stat (can't roll / min == max)      → 1.0  (perfect by definition)
    //   2. item.getUniqueRoll(mod.rollID) / 255      → the stored truth,
    //      exact on uniques AND legendaries (LP-multiplier-immune)
    //   3. KG's display-value reconstruction          → logged fallback only

    [HarmonyPatch(typeof(TooltipItemManager), nameof(TooltipItemManager.UniqueBasicModFormatter))]
    internal static class Patch_UniqueFormatter
    {
        private static void Postfix(ItemDataUnpacked item, ref string __result,
                                    int uniqueModIndex, float modifierValue)
        {
            if (!Prefs.EnableTooltips.Value) return;
            if (item == null) return;
            try
            {
                if (item.uniqueID >= UniqueList.instance.uniques.Count) return;   // v1 had > (off-by-one)
                if (uniqueModIndex < 0) return;
                if (UniqueList.instance.uniques[item.uniqueID] is not { } uniqueEntry) return;
                if (uniqueModIndex >= uniqueEntry.mods.Count) return;

                UniqueItemMod uniqueMod = uniqueEntry.mods[uniqueModIndex];

                float roll;
                if (!uniqueMod.canRoll || uniqueMod.value == uniqueMod.maxValue)
                {
                    roll = 1f;   // fixed stat — always shown as perfect (KG semantics, kept)
                }
                else
                {
                    // uniqueRolls fetched only for the bounds check; the read
                    // goes through the game's own accessor — the exact path
                    // the ApiProbe research validated.
                    var rolls = item.uniqueRolls;
                    byte rollId = uniqueMod.rollID;
                    if (rolls != null && rollId < rolls.Length)
                    {
                        roll = item.getUniqueRoll(rollId) / 255f;
                    }
                    else
                    {
                        // Fallback: KG's reconstruction. Known-wrong on
                        // legendaries — log once so a regression is visible.
                        float min = uniqueMod.value;
                        float max = uniqueMod.maxValue;
                        roll = (min == max || modifierValue > max)
                            ? 1f
                            : (modifierValue - min) / (max - min);
                        WarnFallbackOnce();
                    }
                }

                __result = InjectBracket(__result, roll, tier: 0);
            }
            catch { }
        }

        private static bool s_fallbackWarned;
        private static void WarnFallbackOnce()
        {
            if (s_fallbackWarned) return;
            s_fallbackWarned = true;
            MelonLogger.Warning(
                "unique-roll byte unavailable — using legacy reconstruction (grades may be wrong on legendaries)");
        }
    }

    // ── Patch: implicit stats ─────────────────────────────────────────
    // getImplictRollFloat [sic — the game's own typo] — correct since v1.

    [HarmonyPatch(typeof(TooltipItemManager), nameof(TooltipItemManager.ImplicitFormatter))]
    internal static class Patch_ImplicitFormatter
    {
        private static void Postfix(ItemDataUnpacked item, int implicitNumber,
                                    ref string __result)
        {
            if (!Prefs.EnableTooltips.Value) return;
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
