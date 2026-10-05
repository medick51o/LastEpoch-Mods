using System;
namespace medick_Terrible_Tooltips;
// Resolve the first enabled rule in display order using the actual player level.
internal static class OrderedRuleResolver
{
    internal static bool SuppressTooltipLabel(bool manuallyDisabled, bool altHeld)
        => manuallyDisabled && !altHeld;

    internal static bool First<T>(int count, Func<int, T> get,
        Func<T, bool> enabled, Func<T, int, bool> match, int level,
        out T rule, out int number) where T : class
    {
        rule = null;
        number = 0;
        if (level <= 0 || count <= 0) return false;
        for (int i = count - 1; i >= 0; --i)
        {
            var candidate = get(i);
            if (candidate == null || !enabled(candidate)) continue;
            if (!match(candidate, level)) continue;
            rule = candidate;
            number = count - i; // disabled rows retain displayed numbering
            return true; // caller handles HIDE; never fall through to a SHOW
        }
        return false;
    }
    internal static string Style(bool recolor, int preset, bool emphasized,
        bool bold, int number, string name, Func<int, string> palette)
    {
        string hex = recolor ? palette(preset) : null; // preset ZERO is valid
        return RuleLabelText.Build(number, name,
            string.IsNullOrEmpty(hex) ? "#FA9E3D" : hex, emphasized, bold);
    }
}