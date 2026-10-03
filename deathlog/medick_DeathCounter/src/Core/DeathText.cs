using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace medick_DeathCounter.Core
{
    public sealed class DeathTextRun
    {
        public string Text { get; set; }
        public string Color { get; set; }
    }
    public static class DeathText
    {
        static readonly Regex Tags = new("(<[^>]*>)", RegexOptions.Compiled);
        static readonly Regex ColorTag = new("^<color=[\"']?(#[0-9a-f]{8}|#[0-9a-f]{6}|#[0-9a-f]{3}|red|white|yellow|orange|green|blue|purple|cyan|teal|grey|gray|black)[\"']?>$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static string Plain(string text) => string.Concat(Parse(text).ConvertAll(r => r.Text)).Trim();
        public static List<DeathTextRun> Parse(string text)
        {
            var runs = new List<DeathTextRun>();
            var colors = new Stack<string>();
            foreach (string part in Tags.Split(text ?? ""))
            {
                if (part.Length == 0) continue;
                if (part.StartsWith("<", StringComparison.Ordinal) && part.EndsWith(">", StringComparison.Ordinal))
                {
                    var match = ColorTag.Match(part);
                    if (match.Success) colors.Push(Canonical(match.Groups[1].Value));
                    else if (part.Equals("</color>", StringComparison.OrdinalIgnoreCase) && colors.Count > 0) colors.Pop();
                    else if (part.Equals("<br>", StringComparison.OrdinalIgnoreCase) || part.Equals("<br/>", StringComparison.OrdinalIgnoreCase))
                        runs.Add(new DeathTextRun { Text = "\n", Color = colors.Count > 0 ? colors.Peek() : null });
                    continue; // game font/size/sprite tags cannot shrink our readable UI
                }
                runs.Add(new DeathTextRun { Text = part, Color = colors.Count > 0 ? colors.Peek() : null });
            }
            return runs;
        }

        static string Canonical(string raw)
        {
            if (raw.Length == 4 && raw[0] == '#')
                return $"#{raw[1]}{raw[1]}{raw[2]}{raw[2]}{raw[3]}{raw[3]}";
            return raw.ToLowerInvariant() switch
            {
                "cyan" => "#13A8C9", "teal" => "#29AB85",
                "grey" or "gray" => "#808080", "black" => "#000000",
                _ => raw,
            };
        }
    }
}
