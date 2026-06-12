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
//  OUTPUT (BadgeLeft + Badge style, the defaults):
//    [Tier 5][A]  58% increased Lightning Damage
//
//  Laws (see ARCHAEOLOGY.md):
//  • Hook is UITooltipItem.UpdateLayout — UpdatePrefixAndSuffixesText is
//    intrinsically unpatchable (0xc0000005 even with an empty postfix).
//  • The full-scene TMP scan is the only reliable way to find tooltip
//    TMPs; it runs per tooltip layout, not per frame.
//  • Composed output is MARKED (four zero-width spaces) and skipped on
//    re-entry — deep-view output re-emits "Tier:"/"Range:" text that
//    would otherwise be misclassified on the next pass.
//  • LEAN LAW (Andrew, 2026-06-11): suppressed lines must not leave
//    blank real estate. We run AFTER layout measured the original essay,
//    so after composing we re-invoke the game's own UpdateLayout ONCE
//    (re-entrancy latched) and it re-measures the shortened text.
//  • Unbracketed multi-line affix blocks (hybrid / sealed multi-stat —
//    formatted by a game path the injector doesn't reach) get a
//    SYNTHESIZED tier chip from their own "Tier:" line; no roll data =
//    no grade chip, honestly.
//  • Standalone single-line Range widgets (unique/set/legendary item
//    sections) are HIDDEN unless deep view — "ranges off" means off
//    everywhere. Hidden widgets are tracked and restored on Alt.
//  • EHG resets standalone Tier TMP colours after UpdateLayout — the
//    cache below re-applies them every LateUpdate.
//  • Alt re-render composes from CACHED ORIGINALS (keyed GetInstanceID —
//    Transforms don't hash) and ONLY onto TMPs still showing our marked
//    output — a pooled TMP repurposed for another item is never stomped.
// ================================================================

namespace medick_Terrible_Tooltips;

public static class TooltipRecolor
{
    private const string Dim = "#8a8478";   // separator/dim ink (family palette)

    // Composed-output sentinel: four zero-width spaces, built from char
    // codes so the load-bearing bytes stay visible in review.
    private static readonly string Marker = new string((char)0x200B, 4);

    // Tier TMPs EHG resets after UpdateLayout — re-apply every LateUpdate
    private static readonly List<(TextMeshProUGUI tmp, Color color)> s_tierColorCache = new();

    // Original (pre-transform) text per composed TMP — the Alt deep view
    // re-composes from these. Keyed by instance ID (Transforms don't hash).
    private static readonly Dictionary<int, (TextMeshProUGUI tmp, string original)> s_originals = new();

    private static bool s_altHeld;
    private static bool s_relayouting;   // re-entrancy latch for our own UpdateLayout call

    private static bool DeepTier  => s_altHeld || Prefs.AlwaysShowTierDetails.Value;
    private static bool DeepRange => s_altHeld || Prefs.AlwaysShowRanges.Value;

    // ── Called from TerribleTooltipsMod.OnLateUpdate() ────────────────
    public static void OnLateUpdate()
    {
        // Alt deep view: state change → re-compose every cached TMP live.
        // Master toggle gates the re-render (a v2 fleet law: master OFF
        // means the mod touches nothing).
        try
        {
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            if (alt != s_altHeld)
            {
                s_altHeld = alt;
                if (Prefs.EnableTooltips.Value)
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
        bool changed = false;

        var dead = new List<int>();
        foreach (var kv in s_originals)
        {
            var (tmp, original) = kv.Value;
            if (tmp == null || !tmp.gameObject.activeInHierarchy) { dead.Add(kv.Key); continue; }
            // Only re-render TMPs still showing OUR composed output. A
            // pooled TMP repurposed for another item (vanilla text, or the
            // master toggled off and back) has no marker — drop it instead
            // of stamping a stale item's lines over it.
            try
            {
                if (!(tmp.text ?? "").Contains(Marker)) { dead.Add(kv.Key); continue; }
                tmp.text = HasBracket(original) ? Compose(original) : ComposeUnbracketed(original);
                changed = true;
            }
            catch { }
        }
        foreach (int k in dead) s_originals.Remove(k);

        // Line counts changed → let the game re-measure (lean law)
        if (changed)
        {
            bool active = false;
            try { active = s_lastTooltip != null && s_lastTooltip.tooltipActive; } catch { }
            if (active) RequestRelayout(s_lastTooltip, s_lastArgs);
        }
    }

    // UpdateLayout needs its original positioning arguments — the postfix
    // captures them (typed object[] via Harmony __args) and we replay them
    // verbatim for the re-measure.
    private static UITooltipItem s_lastTooltip;
    private static object[]      s_lastArgs;

    private static void RequestRelayout(UITooltipItem ui, object[] args)
    {
        try
        {
            if (ui == null || args == null || args.Length < 3) return;
            s_relayouting = true;
            try
            {
                ui.UpdateLayout((Vector3)args[0], (Vector2)args[1], args[2] as RectTransform);
            }
            finally { s_relayouting = false; }
        }
        catch { s_relayouting = false; }
    }

    private static bool HasBracket(string text)
        => s_kgGradeRegex.IsMatch(text) || s_kgGradeOnlyRegex.IsMatch(text);

    // ── Regex patterns (exact — load-bearing) ─────────────────────────

    // KG tier+grade bracket  "[<color=…>5</color><color=…>A</color>]"
    // Group 1 = tier number  Group 2 = grade colour hex  Group 3 = grade letter
    private static readonly Regex s_kgGradeRegex = new(
        @"\[(?:<color=[^>]+>)?(\d+)(?:</color>)?<color=([^>]+)>([SABCF])</color>\]",
        RegexOptions.Compiled);

    // KG grade-only bracket (unique/set/implicit)  "[<color=…>A</color>]"
    private static readonly Regex s_kgGradeOnlyRegex = new(
        @"\[<color=([^>]+)>([SABCF])</color>\]",
        RegexOptions.Compiled);

    // ANY KG bracket, tiered or grade-only, in line order — hybrid lines
    // stack several ("[F] [1S] +48 Armor"), and SPEC's "grade per stat
    // (S·S)" needs every one. Group 1 = optional tier digits,
    // Group 2 = grade colour, Group 3 = letter.
    private static readonly Regex s_kgAnyBracketRegex = new(
        @"\[(?:(?:<color=[^>]+>)?(\d+)(?:</color>)?)?<color=([^>]+)>([SABCF])</color>\]",
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

    // ── Patch ─────────────────────────────────────────────────────────

    [HarmonyPatch(typeof(UITooltipItem), "UpdateLayout")]
    internal static class Patch_UpdateLayout
    {
        private static void Postfix(UITooltipItem __instance, object[] __args)
        {
            if (s_relayouting) return;                  // our own re-measure call
            if (!Prefs.EnableTooltips.Value) return;
            try
            {
                s_lastTooltip = __instance;             // for the Alt-path re-measure
                s_lastArgs    = __args;

                TextMeshProUGUI[] allTMPs =
                    UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>();
                if (allTMPs == null) return;

                // Prune stale originals while we're here
                PruneOriginals();

                int composed = 0;

                // ── Pass 1: collect grade colour per parent instance ID so
                //    standalone Range-only TMPs (set/unique siblings) can
                //    inherit the correct grade colour in deep view.
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

                        // Our own composed output — never re-ingest it.
                        if (text.Contains(Marker)) continue;

                        // FilterRuleTooltip's lane — a user-named filter
                        // rule containing "Tier:"/"Range:" must not drag
                        // the 'requires' TMP into the composer.
                        try { if (tmp.gameObject.name == "requires") continue; } catch { }

                        bool hasTier  = text.Contains("Tier:");
                        bool hasRange = text.Contains("Range:");
                        // Composer must ALSO wake on bracket-only TMPs:
                        // with EHG's tier-info display OFF there are no
                        // Tier:/Range: lines at all. '[' is the cheap
                        // pre-gate before the regex runs.
                        if (!hasTier && !hasRange && text.IndexOf('[') < 0) continue;

                        Match gm          = s_kgGradeRegex.Match(text);
                        bool  isTierGrade = gm.Success;
                        if (!isTierGrade) gm = s_kgGradeOnlyRegex.Match(text);
                        bool hasKgGrade = gm.Success;
                        if (!hasTier && !hasRange && !hasKgGrade) continue;

                        bool multiLine = text.IndexOf('\n') >= 0;

                        // ── Unbracketed multi-line affix block ────────────
                        // Hybrid / sealed multi-stat affixes come through a
                        // game formatter the injector doesn't reach: no
                        // bracket, no roll data — but the tier is right
                        // there in the text. Synthesize the chip, kill the
                        // essay. (Without this they were misclassified as
                        // standalone widgets and tinted whole-block blue.)
                        if (!hasKgGrade && multiLine)
                        {
                            s_originals[tmp.GetInstanceID()] = (tmp, text);
                            tmp.text = ComposeUnbracketed(text);
                            composed++;
                            continue;
                        }

                        // ── EHG standalone Tier TMP (single-line widget) ──
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

                        // ── Standalone Range-only TMP (single-line widget,
                        //    unique/set/legendary item sections) ──────────
                        // "Ranges off" means off EVERYWHERE. SetActive
                        // hiding lost a tug-of-war (our re-measure call /
                        // EHG's refresh re-activated the widget every
                        // pass) — so the TEXT goes empty instead: the same
                        // mechanism that collapses the affix essay
                        // collapses this row, and EHG can re-activate the
                        // widget all it wants, there's nothing in it.
                        if (hasRange && !hasKgGrade && !hasTier)
                        {
                            s_originals[tmp.GetInstanceID()] = (tmp, text);

                            if (!DeepRange)
                            {
                                tmp.text = Marker;
                                composed++;
                                continue;
                            }

                            string stripped = s_colorTagRegex.Replace(text, "");
                            stripped = s_kgExtraDataRegex.Replace(stripped, "").Trim();

                            string inheritedColor = null;
                            Transform parent = tmp.transform.parent;
                            if (parent != null)
                                parentGradeColor.TryGetValue(parent.GetInstanceID(), out inheritedColor);
                            if (inheritedColor == null && parent?.parent != null)
                                parentGradeColor.TryGetValue(parent.parent.GetInstanceID(), out inheritedColor);

                            tmp.text = (inheritedColor != null
                                ? $"<color={inheritedColor}>{stripped}</color>"
                                : stripped) + Marker;
                            composed++;
                            continue;
                        }

                        // ── KG affix TMP → THE CLEAN LINE ─────────────────
                        if (!hasKgGrade) continue;

                        // Fresh EHG-written text (bracket present, no
                        // marker) = the original. Store it, then compose.
                        s_originals[tmp.GetInstanceID()] = (tmp, text);
                        tmp.text = Compose(text);
                        composed++;
                    }
                    catch { }
                }

                // ── Lean law: we shrank texts AFTER the game measured the
                //    essay — re-measure once so the blank rows collapse.
                if (composed > 0)
                    RequestRelayout(__instance, __args);
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

    // ── The composer (bracketed affix TMPs) ───────────────────────────
    // Deterministic: always transforms ORIGINAL EHG text (bracket intact),
    // never its own output. Deep view = Alt held or per-detail pins.
    // Output carries the Marker so later passes skip it.
    private static string Compose(string original)
    {
        bool deepTier  = DeepTier;
        bool deepRange = DeepRange;

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
        bool sealedPending = false;

        foreach (string line in lines)
        {
            // KG main affix line — compose the clean line. Hybrid lines
            // stack several brackets; every grade survives (S·S).
            MatchCollection brackets = s_kgAnyBracketRegex.Matches(line);
            if (brackets.Count > 0)
            {
                int tier = 0;
                var grades = new List<(string color, string letter)>();
                foreach (Match b in brackets)
                {
                    if (tier == 0 && b.Groups[1].Success &&
                        int.TryParse(b.Groups[1].Value, out int t))
                        tier = t;
                    grades.Add((b.Groups[2].Value, b.Groups[3].Value));
                }
                string tierHex = tier > 0 ? Colors.TierColor(tier) : null;

                string clean = s_colorTagRegex.Replace(line, "");
                clean = s_kgBracketStripRegex.Replace(clean, "");
                clean = s_kgExtraDataRegex.Replace(clean, "");
                clean = clean.Trim();

                outLines.Add(ComposeCleanLine(clean, tier, tierHex, grades, sealedPending));
                sealedPending = false;
                continue;
            }

            // EHG sealed header — folds into a dim "Sealed" signal element
            // on the next affix line; deep view restores the full header.
            if (line.IndexOf("SEALED AFFIX", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (deepTier)
                    outLines.Add($"<color={Dim}>{s_colorTagRegex.Replace(line, "").Trim()}</color>");
                else
                    sealedPending = true;
                continue;
            }

            // EHG Tier line — folded into the clean line. The signal
            // already broadcasts the tier, so even in deep view the number
            // NEVER repeats. Only the annotation earns a row.
            if (s_tierRegex.IsMatch(line))
            {
                if (deepTier)
                {
                    string note = TierAnnotation(line);
                    if (note != null) outLines.Add(note);
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

            // Everything else (flavor text) — untouched
            outLines.Add(line);
        }

        return string.Join("\n", outLines) + Marker;
    }

    // ── The composer (UNBRACKETED multi-line blocks) ──────────────────
    // Hybrid / sealed multi-stat affixes: no roll data → no grade chip.
    // Tier synthesized from the block's own "Tier:" line.
    private static string ComposeUnbracketed(string original)
    {
        bool deepTier  = DeepTier;
        bool deepRange = DeepRange;

        string[] lines = original.Split('\n');

        int tier = 0;
        foreach (string line in lines)
        {
            Match tm = s_tierRegex.Match(line);
            if (tm.Success && int.TryParse(tm.Groups[1].Value, out int t)) { tier = t; break; }
        }
        string tierHex = tier > 0 ? Colors.TierColor(tier) : null;

        bool sealedFound = false;
        var nameLines = new List<string>();
        var deepLines = new List<string>();

        foreach (string line in lines)
        {
            if (line.IndexOf("SEALED AFFIX", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (deepTier)
                    deepLines.Add($"<color={Dim}>{s_colorTagRegex.Replace(line, "").Trim()}</color>");
                else
                    sealedFound = true;
                continue;
            }
            if (s_tierRegex.IsMatch(line))
            {
                if (deepTier)
                {
                    string note = TierAnnotation(line);
                    if (note != null) deepLines.Add(note);
                }
                continue;
            }
            if (line.StartsWith("Range:", StringComparison.OrdinalIgnoreCase))
            {
                if (deepRange)
                {
                    string stripped = s_colorTagRegex.Replace(line, "").Trim();
                    deepLines.Add($"<color={tierHex ?? "#FFFFFF"}>{stripped}</color>");
                }
                continue;
            }
            string name = s_colorTagRegex.Replace(line, "").Trim();
            if (name.Length > 0) nameLines.Add(name);
        }

        var outLines = new List<string>(nameLines.Count + deepLines.Count);
        var noGrades = new List<(string color, string letter)>();
        for (int i = 0; i < nameLines.Count; i++)
        {
            if (i == 0)
            {
                // First stat line carries the synthesized signal
                outLines.Add(ComposeCleanLine(nameLines[0], tier, tierHex, noGrades, sealedFound));
            }
            else
            {
                string name = nameLines[i];
                if (Prefs.NameColorMode.Value == AffixNameColorMode.TierColor &&
                    Prefs.TooltipTierColors.Value && tierHex != null)
                    name = $"<color={tierHex}>{name}</color>";
                outLines.Add(name);
            }
        }
        outLines.AddRange(deepLines);

        // Everything suppressed (e.g. a lone Range widget restored via the
        // Alt path with ranges off again) → empty-but-marked is correct.
        if (outLines.Count == 0) return Marker;
        return string.Join("\n", outLines) + Marker;
    }

    // "Tier: 5 (max craftable)" → dim "max craftable" · "Tier: 4" → null
    private static string TierAnnotation(string line)
    {
        string stripped = s_colorTagRegex.Replace(line, "").Trim();
        Match tm = s_tierRegex.Match(stripped);
        if (!tm.Success) return null;
        string tail = stripped.Substring(tm.Index + tm.Length).Trim();
        if (tail.StartsWith("(") && tail.EndsWith(")") && tail.Length > 2)
            tail = tail.Substring(1, tail.Length - 2).Trim();
        return tail.Length > 0 ? $"<color={Dim}>{tail}</color>" : null;
    }

    // One affix, one line. Layout per Prefs.Layout; every part honors its
    // own kill-switch (TierColors / RankColors / ShowGradeLetters / name mode).
    private static string ComposeCleanLine(string cleanName, int tier,
        string tierHex, List<(string color, string letter)> grades, bool sealedAffix)
    {
        bool tintTier = Prefs.TooltipTierColors.Value;
        bool tintRank = Prefs.TooltipRankColors.Value;
        bool badges   = Prefs.Style.Value == SignalStyle.Badge;

        // Badge style: TMP's <mark> tag draws a colored quad — and in this
        // game's TMP the quad renders ON TOP of its own enclosed glyphs
        // (in-game verified 2026-06-11: dark ink vanished under 80%-alpha
        // plates; light ink survived). So the label look is built the only
        // way the engine allows: TRANSLUCENT plate (40%) + bright ink that
        // reads through the overlay. Tight chips (no padding spaces) keep
        // long affix lines from clipping at the tooltip edge.
        const string Ink = "#FFF6E6";

        string sealedPart = sealedAffix
            ? (badges
                ? $"<mark={Dim}66><color={Ink}>Sealed</color></mark>"
                : $"<color={Dim}>Sealed</color>")
            : null;

        string tierPart = null;
        if (tier > 0)
        {
            if (tintTier && tierHex != null)
                tierPart = badges
                    ? $"<mark={tierHex}66><color={Ink}>Tier {tier}</color></mark>"
                    : $"<color={tierHex}>Tier {tier}</color>";
            else
                tierPart = $"Tier {tier}";   // colors off → no chip, plain text
        }

        string gradePart = null;
        if (Prefs.ShowGradeLetters.Value && grades.Count > 0)
        {
            var letters = new List<string>(grades.Count);
            foreach (var (color, letter) in grades)
            {
                if (tintRank)
                    letters.Add(badges
                        ? $"<mark={color}66><color={Ink}>{letter}</color></mark>"
                        : $"<color={color}>{letter}</color>");
                else
                    letters.Add(letter);
            }
            gradePart = string.Join(
                badges && tintRank ? " " : $"<color={Dim}>·</color>", letters);
        }

        // Name colour: tier colour by default (the WoW retina read);
        // untiered (unique/set/implicit) names borrow the grade colour, as in v2.
        string name = cleanName;
        if (Prefs.NameColorMode.Value == AffixNameColorMode.TierColor && tintTier)
        {
            string nameHex = tierHex ?? (grades.Count > 0 ? grades[0].color : null);
            if (nameHex != null)
                name = $"<color={nameHex}>{cleanName}</color>";
        }

        // Chips separate themselves visually; plain text needs the dot.
        string signal = JoinSignal(
            badges ? " "
                   : (Prefs.Layout.Value == TooltipLayout.BadgeLeft
                        ? $"<color={Dim}>·</color>" : " "),
            sealedPart, tierPart, gradePart);

        if (signal == null) return name;

        switch (Prefs.Layout.Value)
        {
            case TooltipLayout.SignalRight:
                // <pos> moves the caret absolutely — a long name would be
                // overdrawn (char count is an approximation of rendered
                // width; threshold is deliberately conservative). Long
                // names degrade gracefully to Trailing.
                if (cleanName.Length <= 30)
                    return $"{name}<pos=68%>{signal}";
                return $"{name} <color={Dim}>—</color> {signal}";

            case TooltipLayout.Trailing:
                return $"{name} <color={Dim}>—</color> {signal}";

            default:   // BadgeLeft — Andrew's pick
                return $"{signal}  {name}";
        }
    }

    private static string JoinSignal(string sep, params string[] parts)
    {
        string result = null;
        foreach (string p in parts)
        {
            if (p == null) continue;
            result = result == null ? p : result + sep + p;
        }
        return result;
    }
}
