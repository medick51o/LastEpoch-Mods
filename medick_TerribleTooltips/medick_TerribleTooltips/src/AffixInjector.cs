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
//  Hooks cover ordinary stats and native description templates:
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

    private static void Trace(string hook, string result)
    {
        try
        {
            if (!Prefs.DebugLog.Value) return;
            string text = (result ?? "")
                .Replace("\r\n", " | ")
                .Replace("\n", " | ")
                .Replace("\r", " | ");
            if (text.Length > 90) text = text.Substring(0, 90);
            MelonLogger.Msg($"[trace] {hook}: {text}");
        }
        catch { }
    }

    private static string InjectBracket(string affixStr, float rollFloat, int tier,
                                        double min = double.NaN, double max = double.NaN, double shown = double.NaN, int precision = 0, char rarity = RollQuality.NoLetter)
    {
        if (string.IsNullOrEmpty(affixStr) || affixStr.StartsWith("[<color=")) return affixStr;
        double roll        = Math.Round(rollFloat * 100.0, 1);
        string gradeColor  = Colors.GradeLetterColor(roll);
        string gradeLetter = Colors.GradeLetter(roll);

        if (double.IsFinite(min) && double.IsFinite(max) && max >= min)
        {
            int digits = precision;
            if (double.IsNaN(shown))
            {
                if (DisplayValuePick.TryPick(affixStr, min, max, out double parsed))
                {
                    shown = parsed;
                    digits = DisplayValuePick.DisplayPrecision(affixStr, shown, digits);
                }
            }
            double fraction = rollFloat;
            if (double.IsFinite(shown) && shown >= min - 1e-6 && shown <= max + 1e-6)
                fraction = RollQuality.FractionFor(min, max, shown);
            if (max != min && (!double.IsFinite(fraction) || fraction < 0 || fraction > 1)) return affixStr;
            char grade = RollQuality.LetterForRange(min, max, digits, fraction);
            if (grade == RollQuality.NoGrade) return affixStr;
            gradeLetter = grade.ToString();
            int index = RollQuality.Ladder.IndexOf(grade);
            gradeColor = Colors.GradeLetterColor(new double[] { 0, 30, 60, 90, 100 }[index]);
        }
        if ((!double.IsFinite(min) || !double.IsFinite(max) || max < min) &&
            (!float.IsFinite(rollFloat) || rollFloat < 0 || rollFloat > 1)) return affixStr;

        string bracket = tier > 0
            ? $"[<color={Colors.TierColor(tier)}>{tier}</color><color={gradeColor}>{gradeLetter}</color>] "
            : $"[<color={gradeColor}>{gradeLetter}</color>] ";

        // Keep rarity metadata in the source so settings can re-render cached text.
        // The first letter is affix rarity; the last is always roll quality.
        if (rarity != RollQuality.NoLetter)
        {
            int index = RollQuality.Ladder.IndexOf(rarity);
            string rarityColor = Colors.GradeLetterColor(new double[] { 0, 30, 60, 90, 100 }[index]);
            string tierText = tier > 0 ? "<color=" + Colors.TierColor(tier) + ">" + tier + "</color>" : "";
            bracket = "[" + tierText + "<color=" + rarityColor + ">" + rarity + "</color>] "
                + "[<color=" + gradeColor + ">" + gradeLetter + "</color>] ";
        }
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
        // Codex trace finding 2026-09-10: multi-stat calls pass affix=null, so resolve by SP.
        private static void Postfix(ItemDataUnpacked item, ItemAffix affix,
                                    SP modProperty, int implicitIndex, int uniqueModIndex,
                                    ref string __result)
        {
            try
            {
                if (!Prefs.EnableTooltips.Value) return;
                if (item == null)
                {
                    LogItemNullOnce();
                    return;
                }

                if (affix == null && implicitIndex >= 0)
                {
                    Patch_ImplicitFormatter.Postfix(item, implicitIndex, ref __result);
                    return;
                }
                if (affix == null && uniqueModIndex >= 0)
                {
                    Patch_UniqueFormatter.Postfix(item, ref __result, uniqueModIndex, float.NaN);
                    return;
                }
                ItemAffix resolved = affix;
                if (resolved == null && implicitIndex < 0 && uniqueModIndex < 0)
                    resolved = ResolveByProperty(item, modProperty);
                if (resolved == null) return;

                var info = RollRanges.ForAffix(resolved);
                var range = info.Range;
                double min = range.Valid ? range.Min : double.NaN, max = range.Valid ? range.Max : double.NaN;
                bool idol = item.isIdol();
                if (idol)
                {
                    double factor = 1.0 + ItemList.get().getAffixEffectModifier(item);
                    if (double.IsFinite(factor) && factor >= 0.05 && factor <= 4)
                    {
                        double scaledMin = min * factor, scaledMax = max * factor;
                        if (DisplayValuePick.TryPick(__result, scaledMin, scaledMax, out _))
                        { min = scaledMin; max = scaledMax; }
                    }
                }
                int tier = idol && resolved.DisplayTier == 1 ? 0 : resolved.DisplayTier;
                __result = InjectBracket(__result, resolved.getRollFloat(), tier, min, max, precision: range.Digits, rarity: info.Rarity);
                TooltipRecolor.MarkDirty();
            }
            catch (Exception ex) { Dbg.Log("multi-stat affix lookup failed: " + ex.Message); }
            finally
            {
                Trace($"AffixFormatter item={(item == null ? "null" : "set")} " +
                      $"affix={(affix == null ? "null" : "set")}", __result);
            }
        }

        private static ItemAffix ResolveByProperty(ItemDataUnpacked item, SP modProperty)
        {
            ItemAffix match = null;
            int matches = 0;
            foreach (ItemAffix ia in item.affixes)
            {
                if (ia == null) continue;
                AffixList.Affix def = AffixList.get().GetAffix(ia.affixId);
                if (def != null && def.HasProperty(modProperty))
                {
                    match = ia;
                    matches++;
                    if (matches > 1) break;
                }
            }

            if (matches == 1) return match;
            if (matches == 0)
            {
                if (!s_noMatchLogged)
                {
                    s_noMatchLogged = true;
                    Dbg.Log($"multi-stat affix: no match for property {modProperty}");
                }
            }
            else if (!s_ambiguousMatchLogged)
            {
                s_ambiguousMatchLogged = true;
                Dbg.Log($"multi-stat affix: ambiguous match for property {modProperty}");
            }
            return null;
        }

        private static void LogItemNullOnce()
        {
            if (s_itemNullLogged) return;
            s_itemNullLogged = true;
            Dbg.Log("AffixFormatter: item=null");
        }

        private static bool s_itemNullLogged;
        private static bool s_noMatchLogged;
        private static bool s_ambiguousMatchLogged;
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
        internal static void Postfix(ItemDataUnpacked item, ref string __result,
                                    int uniqueModIndex, float modifierValue)
        {
            try
            {
                if (!Prefs.EnableTooltips.Value) return;
                if (item == null) return;
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

                var range = RollRanges.ForUnique(uniqueMod);
                __result = range.Valid ? InjectBracket(__result, roll, 0, range.Min, range.Max, precision: range.Digits)
                    : InjectBracket(__result, roll, 0);
                TooltipRecolor.MarkDirty();
            }
            catch { }
            finally { Trace("UniqueBasicModFormatter", __result); }
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
        internal static void Postfix(ItemDataUnpacked item, int implicitNumber,
                                    ref string __result)
        {
            try
            {
                if (!Prefs.EnableTooltips.Value) return;
                if (item == null) return;
                float roll = item.getImplictRollFloat((byte)implicitNumber);
                var range = RollRanges.ForImplicit(item, implicitNumber);
                __result = range.Valid ? InjectBracket(__result, roll, 0, range.Min, range.Max, precision: range.Digits)
                    : InjectBracket(__result, roll, 0);
                TooltipRecolor.MarkDirty();
            }
            catch { }
            finally { Trace("ImplicitFormatter", __result); }
        }
    }
    // Match native range placeholders to their rendered values, not unrelated
    // numbers in the description (durations, cooldowns, or proc thresholds).
    [HarmonyPatch(typeof(TooltipItemManager), nameof(TooltipItemManager.FormatUniqueModAffixString))]
    internal static class Patch_DescriptionFormatter
    {
        private static void Postfix(string tempString, ref string __result)
        {
            if (!Prefs.EnableTooltips.Value || string.IsNullOrEmpty(tempString) ||
                string.IsNullOrEmpty(__result) || __result.StartsWith("[<color=")) return;
            try
            {
                if (!DisplayValuePick.TryTemplateRoll(tempString, __result,
                    out double min, out double max, out double value, out int digits)) return;

            char grade = RollQuality.LetterForRange(min, max, digits,
                    RollQuality.FractionFor(min, max, value));
                if (grade == RollQuality.NoGrade) return;
                int index = RollQuality.Ladder.IndexOf(grade);
                string color = Colors.GradeLetterColor(new double[] { 0, 30, 60, 90, 100 }[index]);
                __result = "[<color=" + color + ">" + grade + "</color>] " + __result;
                TooltipRecolor.MarkDirty();
            }
            catch (Exception ex) { Dbg.Log("description grade: " + ex.Message); }
        }
    }
    [HarmonyPatch(typeof(TooltipItemManager), nameof(TooltipItemManager.GetUniqueDescription))]
    internal static class Patch_FixedDescriptions
    {
        private static void Postfix(ItemDataUnpacked item, Il2CppSystem.Collections.Generic.List<string> __result)
        {
            if (!Prefs.EnableTooltips.Value || item == null || __result == null) return;
            try
            {
                var entries = UniqueList.instance?.uniques;
                if (entries == null || item.uniqueID >= entries.Count) return;
                var mods = entries[item.uniqueID]?.mods;
                if (mods == null) return;
                for (int i = 0; i < __result.Count; i++)
                {
                    string text = __result[i];
                    string stat = DisplayValuePick.StatWithoutRangeDetails(text);
                    if (DisplayValuePick.HasGradeBracket(text) || !DisplayValuePick.IsSignedSingleValueStat(stat)) continue;
                    UniqueItemMod match = null;
                    int hits = 0;
                    foreach (UniqueItemMod mod in mods)
                    {
                        if (mod == null || !mod.hideInTooltip) continue;
                        var range = RollRanges.ForUnique(mod);
                        if (!range.Valid || !DisplayValuePick.TryPick(stat, range.Min, range.Max, out _)) continue;
                        match = mod; hits++;
                    }
                    if (hits != 1 || match == null || match.canRoll) continue;
                    var fixedRange = RollRanges.ForUnique(match);
                    if (fixedRange.Valid && fixedRange.Min == fixedRange.Max)
                        __result[i] = InjectBracket(text, 1, 0, fixedRange.Min, fixedRange.Max);
                }
            }
            catch (Exception ex) { Dbg.Log("fixed description grade: " + ex.Message); }
        }
    }
}
