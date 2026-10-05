
namespace medick_Terrible_Tooltips;

internal static class RollRanges
{
    internal readonly struct Span
    {
        public readonly double Min;
        public readonly double Max;
        public readonly int    Precision;
        public readonly bool   Valid;

        public Span(double min, double max, int precision)
        {
            Min       = min;
            Max       = max;
            Precision = precision;
            Valid     = !double.IsNaN(min) && !double.IsNaN(max)
                        && !double.IsInfinity(min) && !double.IsInfinity(max)
                        && max >= min;
        }

        public double Length => Max - Min;
        public int Digits => Precision >= 0 ? Precision : RollQuality.InferPrecisionDigits(Min, Max);
    }
    internal readonly struct AffixInfo
    {
        public readonly Span Range;
        public readonly char Rarity;    // RollQuality.NoLetter when common
        public readonly bool Found;

        public AffixInfo(Span range, char rarity, bool found)
        {
            Range  = range;
            Rarity = rarity;
            Found  = found;
        }
    }

    private static readonly Dictionary<long, AffixInfo> s_affixCache = new();
    private const int AffixCacheLimit = 4096;

    public static AffixInfo ForAffix(ItemAffix itemAffix)
    {
        if (itemAffix == null) return default;

        try
        {
            int tier = itemAffix.affixTier;
            long key = ((long)itemAffix.affixId << 16) | (uint)(tier & 0xFFFF);
            if (s_affixCache.TryGetValue(key, out AffixInfo cached)) return cached;

            AffixList list = AffixList.get();
            AffixList.Affix def = list?.GetAffix(itemAffix.affixId);
            if (def == null)
            {
                AffixInfo missing = new(default, RollQuality.NoLetter, false);
                s_affixCache[key] = missing;
                return missing;
            }

            Span tierSpan = FromTier(def, tier);
            Span singleSpan = default;
            Span multiSpan = default;
            var single = def.TryCast<AffixList.SingleAffix>();
            if (single != null)
                singleSpan = FromSingleRange(list, single, tier);
            else
            {
                var multi = def.TryCast<AffixList.MultiAffix>();
                if (multi != null) multiSpan = FromMultiRange(list, multi, tier);
            }

            Span best = Widest(tierSpan, Widest(singleSpan, multiSpan));

            AffixInfo info = new(best, RollQuality.RarityLetter(def.weighting), true);
            if (s_affixCache.Count < AffixCacheLimit) s_affixCache[key] = info;
            return info;
        }
        catch (Exception e)
        {
            WarnOnce("affix", e);
            return default;
        }
    }

    public static Span ForImplicit(ItemDataUnpacked item, int index)
    {
        if (item == null || index < 0) return default;
        try
        {
            var implicits = item.Implicits;
            if (implicits == null || index >= implicits.Count) return default;
            var implicitDef = implicits[index];
            if (implicitDef == null) return default;
            var definition = ItemList.get()?.GetItemImplicit(item.itemType, item.subType, index);
            if (definition != null && definition.property == implicitDef.property &&
                definition.tags == implicitDef.tags && definition.specialTag == implicitDef.specialTag &&
                definition.type == implicitDef.type)
            {
                double low = definition.implicitValue;
                double high = definition.implicitMaxValue;
                if (double.IsFinite(low) && double.IsFinite(high) && high >= low)
                    return new Span(low, high, -1);
            }
            var range = implicitDef.GetRange();
            double probeLo = double.NaN;
            double probeHi = double.NaN;
            try
            {
                probeLo = implicitDef.GetValue(0);
                probeHi = implicitDef.GetValue(byte.MaxValue);
            }
            catch (Exception) { }

            if (RollSourcePick.TryPick(range.Item1, range.Item2, probeLo, probeHi,
                                       implicitDef.implicitValue, implicitDef.implicitMaxValue,
                                       out double lo, out double hi))
                return new Span(lo, hi, -1);
            return default;
        }
        catch (Exception e)
        {
            WarnOnce("implicit", e);
            return default;
        }
    }

    public static Span ForUnique(UniqueItemMod mod)
    {
        if (mod == null) return default;
        if (!mod.canRoll) return new Span(mod.value, mod.value, -1);
        try
        {
            var range = mod.GetRange();
            double probeLo = double.NaN;
            double probeHi = double.NaN;
            try
            {
                probeLo = mod.getValue(0);
                probeHi = mod.getValue(byte.MaxValue);
            }
            catch (Exception) { }

            if (RollSourcePick.TryPick(range.Item1, range.Item2, probeLo, probeHi,
                                       mod.value, mod.maxValue,
                                       out double lo, out double hi))
                return new Span(lo, hi, -1);
            return default;
        }
        catch (Exception e)
        {
            WarnOnce("unique", e);
            if (mod.maxValue > mod.value) return new Span(mod.value, mod.maxValue, -1);
            return default;
        }
    }
    private static Span FromTier(AffixList.Affix def, int tier)
    {
        var tiers = def.tiers;
        if (tiers == null || tiers.Count == 0) return default;

        int idx = tier;  // ItemAffix.affixTier is zero-based (DisplayTier = tier + 1).
        if (idx < 0) idx = 0;
        if (idx >= tiers.Count) idx = tiers.Count - 1;   // greater/T8 rolls sit on the top tier

        var entry = tiers[idx];
        if (entry == null) return default;

        int digits = PrecisionOf(def, 0);
        Span best = new(entry.minRoll, entry.maxRoll, digits);

        var extras = entry.extraRolls;
        if (extras != null)
        {
            for (int i = 0; i < extras.Count; i++)
            {
                var pair = extras[i];
                if (pair == null) continue;
                best = Widest(best, new Span(pair.minRoll, pair.maxRoll, digits));
            }
        }
        return best;
    }

    private static Span FromSingleRange(AffixList list, AffixList.SingleAffix single, int tier)
    {
        try
        {
            var range = list.GetSingleAffixRange(single, 1f, tier);
            if (range.Item1 == 0f && range.Item2 == 0f) return default;
            return new Span(range.Item1, range.Item2, PrecisionOf(single, 0));
        }
        catch { return default; }
    }
    private static Span FromMultiRange(AffixList list, AffixList.MultiAffix multi, int tier)
    {
        Span best = default;
        try
        {
            var properties = multi.affixProperties;
            if (properties == null) return default;
            for (int p = 0; p < properties.Count; p++)
            {
                try
                {
                    var range = list.GetMultiAffixPropertyRange(multi, 1f, tier, p);
                    if (range.Item1 == 0f && range.Item2 == 0f) continue;
                    best = Widest(best, new Span(range.Item1, range.Item2, PrecisionOf(multi, p)));
                }
                catch { }
            }
        }
        catch { }
        return best;
    }
    private static Span Widest(Span a, Span b)
    {
        if (!a.Valid) return b;
        if (!b.Valid) return a;
        return b.Length > a.Length ? b : a;
    }

    private static int PrecisionOf(AffixList.Affix def, int propertyIndex)
    {
        try
        {
            PropertyRounding rounding;
            var single = def.TryCast<AffixList.SingleAffix>();
            if (single != null) rounding = single.GetPropertyRounding(propertyIndex);
            else
            {
                var multi = def.TryCast<AffixList.MultiAffix>();
                if (multi == null) return -1;
                rounding = multi.GetPropertyRounding(propertyIndex);
            }

            switch (rounding)
            {
                case PropertyRounding.Integer:    return 0;
                case PropertyRounding.Tenth:      return 1;
                case PropertyRounding.Hundredth:  return 2;
                case PropertyRounding.Thousandth: return 3;
                default:                          return -1;
            }
        }
        catch { return -1; }
    }

    private static bool s_rangeWarned;
    private static void WarnOnce(string what, Exception e)
    {
        if (s_rangeWarned) return;
        s_rangeWarned = true;
        MelonLogger.Warning($"[Terrible Tooltips] roll range read failed ({what}): " +
                            $"{e.GetType().Name} — grades fall back to fixed thresholds");
    }
}
