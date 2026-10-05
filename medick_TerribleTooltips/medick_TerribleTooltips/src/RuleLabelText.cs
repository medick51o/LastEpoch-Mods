using System.Text.RegularExpressions;
namespace medick_Terrible_Tooltips;
// Pure formatter: never hard-wrap or slice Unicode text. TMP owns wrapping.
internal static class RuleLabelText
{
    private static readonly Regex Tags = new(@"<[^>]*>");
    private static readonly Regex Space = new(@"\s+");
    internal static string Plain(string text, bool generated)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (generated) text = Tags.Replace(text, " ");
        // Neutralize ALL delimiters (including malformed/unclosed tags).
        // Readable angle glyphs cannot introduce TMP size/color/noparse tags.
        return Space.Replace(text.Replace('<', '\u2039').Replace('>', '\u203A'), " ").Trim();
    }
    internal static string Build(int number, string name, string hex, bool emphasized, bool bold)
    {
        string body = number <= 0 ? name : string.IsNullOrEmpty(name) ? $"[{number}]" : $"[{number}] {name}";
        if (emphasized) body = bold ? $"<b>{body}</b>" : $"<uppercase>{body}</uppercase>";
        return $"<color={hex}>{body}</color>";
    }
}
