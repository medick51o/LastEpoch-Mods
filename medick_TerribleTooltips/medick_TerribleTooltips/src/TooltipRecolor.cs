// ================================================================
//  TooltipRecolor.cs — transforms tooltip lines into WoW-style
//  tier/grade colouring. (v1: TooltipPatch.cs — ported verbatim;
//  every regex and ritual here is battle-tested across seasons.)
//
//  INPUT  (AffixInjector / KG Letter_Style output):
//    [5A] 58% increased Lightning Damage
//    Range: 40% to 60%
//    Tier: 5 (max craftable)
//
//  OUTPUT:
//    58% increased Lightning Damage (A)   ← tier colour + grade letter
//    Range: 40% to 60%                    ← grade colour, no letter
//    Tier: 5 (max craftable)              ← tier colour
//
//  Laws (see ARCHAEOLOGY.md):
//  • Hook is UITooltipItem.UpdateLayout — UpdatePrefixAndSuffixesText is
//    intrinsically unpatchable (0xc0000005 even with an empty postfix).
//  • The full-scene TMP scan is the only reliable way to find tooltip
//    TMPs; it runs per tooltip layout, not per frame.
//  • The KG main line, EHG "Tier:" line AND "Range:" line all live in
//    ONE TMP per affix; set/unique Range-only TMPs inherit grade colour
//    from a sibling via parent GetInstanceID (Transforms don't hash).
//  • EHG resets standalone Tier TMP colours after UpdateLayout — the
//    cache below re-applies them every LateUpdate.
// ================================================================

namespace medick_Terrible_Tooltips;

public static class TooltipRecolor
{
    // Tier TMPs EHG resets after UpdateLayout — re-apply every LateUpdate
    private static readonly List<(TextMeshProUGUI tmp, Color color)> s_tierColorCache = new();

    // Called from TerribleTooltipsMod.OnLateUpdate()
    public static void OnLateUpdate()
    {
        if (s_tierColorCache.Count == 0) return;
        s_tierColorCache.RemoveAll(p => p.tmp == null || !p.tmp.gameObject.activeInHierarchy);
        foreach (var (tmp, color) in s_tierColorCache)
            tmp.color = color;
    }

    // ── Regex patterns (exact — load-bearing) ─────────────────────────

    // KG tier+grade bracket  "[<color=…>5</color><color=…>A</color>]"
    // Group 1 = tier number  Group 2 = grade colour hex  Group 3 = grade letter
    private static readonly Regex s_kgGradeRegex = new(
        @"\[(?:<color=[^>]+>)?(\d+)(?:</color>)?<color=([^>]+)>([SABCF])</color>\]",
        RegexOptions.Compiled);

    // KG grade-only bracket (unique/set)  "[<color=…>A</color>]"
    private static readonly Regex s_kgGradeOnlyRegex = new(
        @"\[<color=([^>]+)>([SABCF])</color>\]",
        RegexOptions.Compiled);

    // Matches the EHG tier number in a Tier TMP
    private static readonly Regex s_tierRegex = new(
        @"Tier:\s*(\d+)",
        RegexOptions.Compiled);

    // Strips ALL TMP <color=…> / </color> tags (TMP innermost-tag-wins:
    // outer wrapping fails until inner tags are stripped)
    private static readonly Regex s_colorTagRegex = new(
        @"</?color[^>]*>",
        RegexOptions.Compiled);

    // Strips ONE OR MORE KG brackets from the START of a line — the `+`
    // handles hybrid lines like "[F] [1S] +48 Armor" → "+48 Armor"
    private static readonly Regex s_kgBracketStripRegex = new(
        @"^(\[\d*[SABCF]\]\s*)+",
        RegexOptions.Compiled);

    // Strips KG's appended roll data  "[85.9%]"  or  "(0.923)"
    private static readonly Regex s_kgExtraDataRegex = new(
        @"\s*(?:\[\d+\.?\d*%?\]|\(\d+\.?\d*\))\s*$",
        RegexOptions.Compiled);

    // ── Patch ─────────────────────────────────────────────────────────

    [HarmonyPatch(typeof(UITooltipItem), "UpdateLayout")]
    internal static class Patch_UpdateLayout
    {
        private static void Postfix(UITooltipItem __instance)
        {
            if (!Prefs.EnableTooltips.Value) return;
            try
            {
                TextMeshProUGUI[] allTMPs =
                    UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>();
                if (allTMPs == null) return;

                // ── Pass 1: collect grade colour per parent instance ID so
                //    standalone Range-only TMPs (set/unique siblings) can
                //    inherit the correct grade colour.
                var parentGradeColor = new Dictionary<int, string>();
                foreach (TextMeshProUGUI tmp in allTMPs)
                {
                    string t = tmp?.text;
                    if (string.IsNullOrEmpty(t)) continue;
                    Match gm = s_kgGradeRegex.Match(t);
                    bool  itg = gm.Success;
                    if (!itg) gm = s_kgGradeOnlyRegex.Match(t);
                    if (!gm.Success) continue;
                    string gc = itg ? gm.Groups[2].Value : gm.Groups[1].Value;
                    if (tmp.transform.parent != null)
                        parentGradeColor[tmp.transform.parent.GetInstanceID()] = gc;
                }

                // ── Pass 2: apply colouring ───────────────────────────────
                foreach (TextMeshProUGUI tmp in allTMPs)
                {
                    try
                    {
                        string text = tmp?.text;
                        if (string.IsNullOrEmpty(text)) continue;

                        bool hasTier  = text.Contains("Tier:");
                        bool hasRange = text.Contains("Range:");
                        if (!hasTier && !hasRange) continue;

                        Match gm          = s_kgGradeRegex.Match(text);
                        bool  isTierGrade = gm.Success;
                        if (!isTierGrade) gm = s_kgGradeOnlyRegex.Match(text);
                        bool hasKgGrade = gm.Success;

                        // ── EHG standalone Tier TMP ───────────────────────
                        if (hasTier && !hasKgGrade)
                        {
                            Match tm = s_tierRegex.Match(text);
                            if (tm.Success &&
                                int.TryParse(tm.Groups[1].Value, out int tier))
                            {
                                string hex = Colors.TierColor(tier);
                                if (ColorUtility.TryParseHtmlString(hex, out Color col))
                                {
                                    tmp.color = col;
                                    s_tierColorCache.RemoveAll(p => p.tmp == tmp);
                                    s_tierColorCache.Add((tmp, col));
                                }
                            }
                            continue;
                        }

                        // ── Standalone Range-only TMP ─────────────────────
                        // No KG grade, no Tier — set/unique Range line in a
                        // separate widget. Inherit grade colour from a sibling
                        // KG-graded TMP via parent (then grandparent) lookup.
                        if (hasRange && !hasKgGrade && !hasTier)
                        {
                            string stripped = s_colorTagRegex.Replace(text, "");
                            stripped = s_kgExtraDataRegex.Replace(stripped, "").Trim();

                            string inheritedColor = null;
                            Transform parent = tmp.transform.parent;
                            if (parent != null)
                                parentGradeColor.TryGetValue(parent.GetInstanceID(), out inheritedColor);
                            if (inheritedColor == null && parent?.parent != null)
                                parentGradeColor.TryGetValue(parent.parent.GetInstanceID(), out inheritedColor);

                            if (inheritedColor != null)
                                tmp.text = $"<color={inheritedColor}>{stripped}</color>";
                            else
                            {
                                tmp.text  = stripped;
                                tmp.color = Color.white;
                            }
                            continue;
                        }

                        // ── KG Affix TMP (has KG grade bracket) ───────────
                        if (!hasKgGrade) continue;

                        string gradeColor, gradeLetter, tierHex;
                        if (isTierGrade)
                        {
                            tierHex     = int.TryParse(gm.Groups[1].Value, out int t)
                                              ? Colors.TierColor(t) : null;
                            gradeColor  = gm.Groups[2].Value;
                            gradeLetter = gm.Groups[3].Value;
                        }
                        else
                        {
                            tierHex     = null;
                            gradeColor  = gm.Groups[1].Value;
                            gradeLetter = gm.Groups[2].Value;
                        }

                        string[] lines  = text.Split('\n');
                        var      sb     = new StringBuilder();
                        bool     changed = false;

                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (i > 0) sb.Append('\n');
                            string line = lines[i];

                            // KG main affix line (contains the grade bracket)
                            bool isKgMain = s_kgGradeRegex.IsMatch(line)
                                         || s_kgGradeOnlyRegex.IsMatch(line);
                            if (isKgMain)
                            {
                                string clean = s_colorTagRegex.Replace(line, "");
                                clean = s_kgBracketStripRegex.Replace(clean, "");
                                clean = s_kgExtraDataRegex.Replace(clean, "");
                                clean = clean.Trim();

                                // Tiered: name in tier colour; untiered: grade
                                // colour; toggles gate each channel.
                                bool useTierColor = Prefs.TooltipTierColors.Value;
                                bool useRankColor = Prefs.TooltipRankColors.Value;

                                string nameColor = useTierColor ? (tierHex ?? gradeColor) : null;
                                string affixPart = nameColor != null
                                    ? $"<color={nameColor}>{clean}</color>"
                                    : clean;

                                string letterPart = useRankColor
                                    ? $"<color={gradeColor}>({gradeLetter})</color>"
                                    : $"({gradeLetter})";

                                sb.Append($"{affixPart} {letterPart}");
                                changed = true;
                                continue;
                            }

                            // Range line — grade colour, no letter
                            if (line.StartsWith("Range:", StringComparison.OrdinalIgnoreCase))
                            {
                                string stripped = s_colorTagRegex.Replace(line, "");
                                stripped = s_kgExtraDataRegex.Replace(stripped, "").Trim();
                                sb.Append($"<color={gradeColor}>{stripped}</color>");
                                changed = true;
                                continue;
                            }

                            // Tier line — strip EHG blue, apply tier colour
                            if (s_tierRegex.IsMatch(line))
                            {
                                string stripped = s_colorTagRegex.Replace(line, "");
                                string tc = tierHex;
                                if (tc == null)
                                {
                                    Match tm2 = s_tierRegex.Match(stripped);
                                    if (tm2.Success &&
                                        int.TryParse(tm2.Groups[1].Value, out int t2))
                                        tc = Colors.TierColor(t2);
                                }
                                sb.Append($"<color={tc ?? "#FFFFFF"}>{stripped}</color>");
                                changed = true;
                                continue;
                            }

                            // Everything else (sealed affix labels, etc.) — untouched
                            sb.Append(line);
                        }

                        if (changed)
                            tmp.text = sb.ToString();
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                // Latched: UpdateLayout fires on every tooltip layout — an
                // unlatched warning here would spam a whole farming session.
                if (!s_recolorWarned)
                {
                    s_recolorWarned = true;
                    MelonLogger.Warning(
                        $"tooltip recolor failing ({ex.Message}) — colors may be missing until restart");
                }
                else Dbg.Log("tooltip recolor error: " + ex.Message);
            }
        }

        private static bool s_recolorWarned;
    }
}
