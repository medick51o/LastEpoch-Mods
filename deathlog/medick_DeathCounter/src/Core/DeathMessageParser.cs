using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace medick_DeathCounter.Core
{
    // Fallback for native helpers that are inlined. Only explicit report
    // labels establish damage; a boss or ability name never establishes it.
    // Unsupported translations retain their complete text without guessing.
    public static class DeathMessageParser
    {
        const RegexOptions Options = RegexOptions.Multiline | RegexOptions.CultureInvariant;
        static readonly Regex TypeLine = new(@"^\s*Killing Blow Damage Types?:\s*(?<types>[^\r\n]+)\s*$", Options);
        static readonly Regex Damage = new(@"\bKilling Blow Damage:\s*(?<value>[0-9][0-9,]*)\b", Options);
        static readonly Regex Overkill = new(@"\bOverkill Damage:\s*(?<value>[0-9][0-9,]*)\b", Options);
        static readonly Regex Cause = new(@"^\s*You have been slain by (?:a |an )?(?<ability>[^\r\n]+?) from (?<killer>[^\r\n]+?)\s*$", Options);
        static readonly Regex Integer = new(@"^(?:0|[1-9][0-9]*|[1-9][0-9]{0,2}(?:,[0-9]{3})+)$", RegexOptions.CultureInvariant);

        public static DeathDetails Parse(string richText)
        {
            string text = DeathText.Plain(richText);
            var report = new DeathDetails { Text = text, RichText = richText, TextOnly = true };
            var typeLines = TypeLine.Matches(text);
            var damage = Damage.Matches(text);
            var overkill = Overkill.Matches(text);
            if (typeLines.Count != 1 || damage.Count != 1 || overkill.Count != 1) return report;
            var types = Regex.Split(typeLines[0].Groups["types"].Value.Trim(), @"\s+and\s+|\s*,\s*");
            if (types.Length < 1 || types.Length > 2 || types.Distinct(StringComparer.OrdinalIgnoreCase).Count() != types.Length
                || types.Any(t => !Elements.TryParse(t, out _))) return report;
            bool Number(Match m, out int value)
            {
                value = 0;
                string raw = m.Groups["value"].Value;
                return Integer.IsMatch(raw) && int.TryParse(raw,
                    NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value) && value >= 0;
            }
            if (!Number(damage[0], out int amount) || amount <= 0 || !Number(overkill[0], out int excess)) return report;
            Elements.TryParse(types[0], out var primary);
            report.PrimaryElement = Elements.Name(primary);
            if (types.Length == 2) { Elements.TryParse(types[1], out var secondary); report.SecondaryElement = Elements.Name(secondary); }
            report.Damage = amount; report.Overkill = excess;
            report.TextOnly = false;
            report.Source = "game death message (parsed)";
            var causes = Cause.Matches(text);
            if (causes.Count == 1)
            {
                report.Killer = causes[0].Groups["killer"].Value.Trim();
                report.Ability = causes[0].Groups["ability"].Value.Trim();
            }
            return report;
        }
    }
}
