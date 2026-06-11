// ================================================================
//  TooltipRecolor.cs — v3: THE CLEAN LINE.
//
//  v1/v2 recolored EHG's tier-info essay. v3 kills the essay:
//  one line per affix — affix text + "Tier N" (tier color) + grade
//  letter (grade color) — and EHG's Tier/Range lines are folded away.
//  Hold Alt = deep view (the suppressed EHG detail returns live).
//
//  INPUT  (AffixInjector / KG Letter_Style output, unchanged):
//    [5A] 58% increased Lightning Damage
//    Tier: 5 (max craftable)
//    Range: 40% to 60%
//
//  OUTPUT (BadgeLeft, the default):
//    Tier 5·A  58% increased Lightning Damage
//
//  Laws (see ARCHAEOLOGY.md):
//  • Hook is UITooltipItem.UpdateLayout — UpdatePrefixAndSuffixesText is
//    intrinsically unpatchable (0xc0000005 even with an empty postfix).
//  • The full-scene TMP scan is the only reliable way to find tooltip
//    TMPs; it runs per tooltip layout, not per frame.
//  • Standalone Tier/Range TMPs (set/unique separate widgets) KEEP the
//    v2 recolor treatment — the essay-kill targets the craftable-affix
//    TMPs where main line + Tier + Range live in ONE TMP. Removing
//    whole game widgets is not our lane.
//  • EHG resets standalone Tier TMP colours after UpdateLayout — the
//    cache below re-applies them every LateUpdate.
//  • Alt re-render composes from CACHED ORIGINALS (keyed GetInstanceID —
//    Transforms don't hash), never from already-transformed text.
// ================================================================

namespace medick_Terrible_Tooltips;

public static class TooltipRecolor
{
    private const string Dim = "#8a8478";   // separator/dim ink (family palette)

    // Tier TMPs EHG resets after UpdateLayout — re-apply every LateUpdate
    private static readonly List<(TextMeshProUGUI tmp, Color color)> s_tierColorCache = new();

    // Original (pre-transform) text per composed TMP — the Alt deep view
    // re-composes from these. Keyed by instance ID (Transforms don't hash).
    private static readonly Dictionary<int, (TextMeshProUGUI tmp, string original)> s_originals = new();

    private static bool s_altHeld;

    // ── Called from TerribleTooltipsMod.OnLateUpdate() ────────────────
    public static void OnLateUpdate()
    {
        // Alt deep view: state change → re-compose every cached TMP live
        try
        {
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            if (alt != s_altHeld)
            {
                s_altHeld = alt;
                ReRenderFromOriginals();
            }
        }
        catch { }

        if (s_tierColorCache.Count == 0) return;
        s_tierColorCache.RemoveAll(p => p.tmp == null || !p.tmp.gameObject.activeInHierarchy);
        foreach (var (tmp, color) in s_tierColorCache)
            tmp.color = color;
    }

    private static void ReRenderFromOriginals()
    {
        var dead = new List<int>();
        foreach (var kv in s_originals)
        {
            var (tmp, original) = kv.Value;
            if (tmp == null || !tmp.gameObject.activeInHierarchy) { dead.Add(kv.Key); continue; }
            try { tmp.text = Compose(original); } catch { }
        }
        foreach (int k in dead) s_originals.Remove(k);
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

    // Matches the EHG tier number in a Tier line/TMP
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

    // Strips ALL rich-text tags (for plain-length measurement)
    private static readonly Regex s_anyTagRegex = new(
        @"<[^>]+>",
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

                // Prune stale originals while we're here
                PruneOriginals();

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

                // ── Pass 2: compose ───────────────────────────────────────
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

                        // ── EHG standalone Tier TMP (separate widget) ─────
                        // v2 treatment kept: recolor, never remove.
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

                        // ── Standalone Range-only TMP (separate widget) ───
                        // v2 treatment kept: inherit grade colour via parent.
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

                        // ── KG affix TMP → THE CLEAN LINE ─────────────────
                        if (!hasKgGrade) continue;

                        // Fresh EHG-written text (bracket present) = the
                        // original. Store it, then compose. Re-runs on
                        // already-composed text never get here (no bracket).
                        s_originals[tmp.GetInstanceID()] = (tmp, text);
                        tmp.text = Compose(text);
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
                        $"tooltip clean-line failing ({ex.Message}) — tooltips may look vanilla until restart");
                }
                else Dbg.Log("tooltip clean-line error: " + ex.Message);
            }
        }

        private static bool s_recolorWarned;
    }

    private static void PruneOriginals()
    {
        if (s_originals.Count == 0) return;
        var dead = new List<int>();
        foreach (var kv in s_originals)
            if (kv.Value.tmp == null || !kv.Value.tmp.gameObject.activeInHierarchy)
                dead.Add(kv.Key);
        foreach (int k in dead) s_originals.Remove(k);
    }

    // ── The composer ──────────────────────────────────────────────────
    // Deterministic: always transforms ORIGINAL EHG text (bracket intact),
    // never its own output. Deep view = Alt held or per-detail pins.
    private static string Compose(string original)
    {
        bool deepTier  = s_altHeld || Prefs.AlwaysShowTierDetails.Value;
        bool deepRange = s_altHeld || Prefs.AlwaysShowRanges.Value;

        // TMP-level first grade colour — Range lines in deep view wear it
        // (v2 semantics).
        Match firstG = s_kgGradeRegex.Match(original);
        bool  firstTiered = firstG.Success;
        if (!firstTiered) firstG = s_kgGradeOnlyRegex.Match(original);
        string tmpGradeColor = firstG.Success
            ? firstG.Groups[firstTiered ? 2 : 1].Value
            : "#FFFFFF";

        string[] lines = original.Split('\n');
        var outLines = new List<string>(lines.Length);

        foreach (string line in lines)
        {
            // KG main affix line — compose the clean line
            Match lg = s_kgGradeRegex.Match(line);
            bool  lineTiered = lg.Success;
            if (!lineTiered) lg = s_kgGradeOnlyRegex.Match(line);

            if (lg.Success)
            {
                int    tier        = lineTiered && int.TryParse(lg.Groups[1].Value, out int t) ? t : 0;
                string gradeColor  = lg.Groups[lineTiered ? 2 : 1].Value;
                string gradeLetter = lg.Groups[lineTiered ? 3 : 2].Value;
                string tierHex     = tier > 0 ? Colors.TierColor(tier) : null;

                string clean = s_colorTagRegex.Replace(line, "");
                clean = s_kgBracketStripRegex.Replace(clean, "");
                clean = s_kgExtraDataRegex.Replace(clean, "");
                clean = clean.Trim();

                outLines.Add(ComposeCleanLine(clean, tier, tierHex, gradeColor, gradeLetter));
                continue;
            }

            // EHG Tier line — folded into the clean line unless deep view
            if (s_tierRegex.IsMatch(line))
            {
                if (deepTier)
                {
                    string stripped = s_colorTagRegex.Replace(line, "");
                    string tc = null;
                    Match tm2 = s_tierRegex.Match(stripped);
                    if (tm2.Success && int.TryParse(tm2.Groups[1].Value, out int t2))
                        tc = Colors.TierColor(t2);
                    outLines.Add($"<color={tc ?? "#FFFFFF"}>{stripped.Trim()}</color>");
                }
                continue;   // suppressed — the essay dies here
            }

            // EHG Range line — hidden unless deep view
            if (line.StartsWith("Range:", StringComparison.OrdinalIgnoreCase))
            {
                if (deepRange)
                {
                    string stripped = s_colorTagRegex.Replace(line, "");
                    stripped = s_kgExtraDataRegex.Replace(stripped, "").Trim();
                    outLines.Add($"<color={tmpGradeColor}>{stripped}</color>");
                }
                continue;   // suppressed
            }

            // Everything else (sealed headers, flavor) — untouched
            outLines.Add(line);
        }

        return string.Join("\n", outLines);
    }

    // One affix, one line. Layout per Prefs.Layout; every part honors its
    // own kill-switch (TierColors / RankColors / ShowGradeLetters / name mode).
    private static string ComposeCleanLine(string cleanName, int tier,
        string tierHex, string gradeColor, string gradeLetter)
    {
        bool tintTier  = Prefs.TooltipTierColors.Value;
        bool tintRank  = Prefs.TooltipRankColors.Value;

        string tierPart = tier > 0
            ? (tintTier && tierHex != null
                ? $"<color={tierHex}>Tier {tier}</color>"
                : $"Tier {tier}")
            : null;

        string gradePart = Prefs.ShowGradeLetters.Value
            ? (tintRank ? $"<color={gradeColor}>{gradeLetter}</color>" : gradeLetter)
            : null;

        // Name colour: tier colour by default (the WoW retina read);
        // untiered (unique/set) names borrow the grade colour, as in v2.
        string name = cleanName;
        if (Prefs.NameColorMode.Value == AffixNameColorMode.TierColor && tintTier)
        {
            string nameHex = tierHex ?? gradeColor;
            name = $"<color={nameHex}>{cleanName}</color>";
        }

        // No signal at all (untiered + letters off) → just the name
        if (tierPart == null && gradePart == null) return name;

        switch (Prefs.Layout.Value)
        {
            case TooltipLayout.SignalRight:
            {
                string signal = JoinSignal(tierPart, gradePart, " ");
                // <pos> moves the caret absolutely — a long name would be
                // overdrawn. Degrade gracefully to Trailing for long names.
                if (cleanName.Length <= 34)
                    return $"{name}<pos=68%>{signal}";
                return $"{name} <color={Dim}>—</color> {signal}";
            }
            case TooltipLayout.Trailing:
            {
                string signal = JoinSignal(tierPart, gradePart, " ");
                return $"{name} <color={Dim}>—</color> {signal}";
            }
            default:   // BadgeLeft — Andrew's pick
            {
                string signal = JoinSignal(tierPart, gradePart, $"<color={Dim}>·</color>");
                return $"{signal}  {name}";
            }
        }
    }

    private static string JoinSignal(string tierPart, string gradePart, string sep)
    {
        if (tierPart == null) return gradePart;
        if (gradePart == null) return tierPart;
        return tierPart + sep + gradePart;
    }
}
