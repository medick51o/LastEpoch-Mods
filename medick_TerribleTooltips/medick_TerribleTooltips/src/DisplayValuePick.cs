using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace medick_Terrible_Tooltips
{
    internal static class DisplayValuePick
    {
        private static readonly Regex s_tags = new Regex("<[^>]*>", RegexOptions.Compiled);
        internal static bool TryTemplateRoll(string template, string output,
            out double min, out double max, out double value, out int precision)
        {
            min = max = value = 0; precision = 0;
            if (string.IsNullOrEmpty(template) || string.IsNullOrEmpty(output) ||
                template.Length > 12000 || output.Length > 24000 || HasGradeBracket(output)) return false;
            string source = Regex.Replace(s_tags.Replace(template, ""), @"\s+", " ").Trim();
            string shown = Regex.Replace(StatWithoutRangeDetails(output), @"\s+", " ").Trim();
            var macros = Regex.Matches(source,
                @"\[\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*,\s*(\d+)\s*\]");
            if (macros.Count == 0 || macros.Count > 16) return false;
            var pattern = new System.Text.StringBuilder("^");
            int end = 0;
            foreach (Match macro in macros)
            {
                pattern.Append(Regex.Escape(source.Substring(end, macro.Index - end)));
                pattern.Append(@"([-+\u2212]?\d+(?:[.,]\d+)?)");
                end = macro.Index + macro.Length;
            }
            pattern.Append(Regex.Escape(source.Substring(end))).Append("$");
            Match rendered;
            try { rendered = Regex.Match(shown, pattern.ToString(), RegexOptions.None, TimeSpan.FromMilliseconds(40)); }
            catch (RegexMatchTimeoutException) { return false; }
            if (!rendered.Success) return false;
            double widest = -1;
            for (int i = 0; i < macros.Count; i++)
            {
                if (!TryParse(macros[i].Groups[1].Value, out double lo) ||
                    !TryParse(macros[i].Groups[2].Value, out double hi) ||
                    !TryParse(rendered.Groups[i + 1].Value.Replace((char)0x2212, (char)45), out double v) ||
                    !double.IsFinite(lo) || !double.IsFinite(hi) || !double.IsFinite(v) ||
                    hi < lo || v < lo - 1e-6 || v > hi + 1e-6) return false;
                if (hi - lo <= widest) continue;
                widest = hi - lo; min = lo; max = hi; value = v;
                string number = rendered.Groups[i + 1].Value;
                int dot = Math.Max(number.IndexOf((char)46), number.IndexOf((char)44));
                precision = dot < 0 ? 0 : Math.Min(6, number.Length - dot - 1);
            }
            return true;
        }
        private static readonly Regex s_numbers = new Regex(@"-?\d+(?:[.,]\d+)?", RegexOptions.Compiled);
        internal static List<double> Numbers(string line)
        {
            List<double> found = new List<double>();
            if (string.IsNullOrEmpty(line)) return found;

            string text;
            try { text = s_tags.Replace(line, " "); }
            catch { return found; }
            text = text.Replace('\u2212', '-');

            foreach (Match match in s_numbers.Matches(text))
            {
                double parsed;
                if (TryParse(match.Value, out parsed)) found.Add(parsed);
            }
            return found;
        }
        internal static bool TryPick(string line, double min, double max, out double value)
        {
            value = 0.0;
            if (double.IsNaN(min) || double.IsNaN(max) || max < min) return false;

            List<double> numbers = Numbers(line);
            if (numbers.Count == 0) return false;

            double tolerance = Math.Max(1e-6, Math.Abs(max - min) * 1e-9);
            for (int i = 0; i < numbers.Count; i++)
                if (InRange(numbers[i], min, max, tolerance)) { value = numbers[i]; return true; }

            for (int i = 0; i < numbers.Count; i++)
            {
                double percent = numbers[i] / 100.0;
                if (InRange(percent, min, max, tolerance)) { value = percent; return true; }
            }

            for (int i = 0; i < numbers.Count; i++)
            {
                double scaled = numbers[i] * 100.0;
                if (InRange(scaled, min, max, tolerance)) { value = scaled; return true; }
            }

            return false;
        }
        internal static string StatWithoutRangeDetails(string line)
        {
            string plain = Regex.Replace(line ?? "", @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            plain = s_tags.Replace(plain, " ");
            return Regex.Replace(plain,
                @"(?im)^[\t ]*(?:Range|\u0414\u0438\u0430\u043f\u0430\u0437\u043e\u043d)[\t ]*:[\t ]*[-+\u2212]?\d+(?:[.,]\d+)?[\t ]*%?[\t ]*(?:to|[-\u2013\u2014])[\t ]*[-+\u2212]?\d+(?:[.,]\d+)?[\t ]*%?[\t ]*\r?$", "");
        }
        internal static bool IsSignedSingleValueStat(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            string plain = s_tags.Replace(line, " ").TrimStart();
            if (plain.Length < 2 || (plain[0] != '+' && plain[0] != '-' && plain[0] != '\u2212')) return false;
            return Numbers(line).Count == 1;
        }
        internal static int DisplayPrecision(string line, double value, int inferred)
        {
            string plain = s_tags.Replace(line ?? "", " ");
            foreach (Match match in Regex.Matches(plain, @"(-?\d+(?:[.,]\d+)?)\s*%"))
                if (TryParse(match.Groups[1].Value, out double percent) &&
                    Math.Abs(percent / 100.0 - value) < 1e-6 && Math.Abs(percent - value) > 1e-6)
                    return Math.Max(inferred, 2);
            return inferred;
        }
        private static readonly Regex s_gradeBracket = new Regex(">[SABCDF]</color>\\]", RegexOptions.Compiled);
        internal static bool HasGradeBracket(string line)
            => !string.IsNullOrEmpty(line) && s_gradeBracket.IsMatch(line);

        private static bool InRange(double value, double min, double max, double tolerance)
            => value >= min - tolerance && value <= max + tolerance;
        internal static bool TryParse(string token, out double value)
        {
            value = 0.0;
            if (string.IsNullOrEmpty(token)) return false;
            return double.TryParse(token.Replace(',', '.'), NumberStyles.Float,
                                   CultureInfo.InvariantCulture, out value);
        }
    }
}
