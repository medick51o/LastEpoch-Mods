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
//  • Scan tooltip descendants plus explicitly referenced content panels
//    (including detached comparison panels), not every TMP in the scene.
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

    // Standalone Range widgets the game keeps REWRITING after we empty
    // them (the rewrite happens inside/after UpdateLayout, where our
    // re-entrancy latch blinds the postfix). Enforced every LateUpdate —
    // the proven v2 pattern: s_tierColorCache wins the exact same war
    // over tier colours by having the last word before render.
    private static readonly Dictionary<int, TextMeshProUGUI> s_suppressedRanges = new();

    private static bool s_altHeld;
    private static bool s_relayouting;   // re-entrancy latch for our own UpdateLayout call
    private static bool? s_masterPreviouslyEnabled;

    // ── Scan gate (v3.0.1 — speedscalzone's "mouseover stutter") ──────
    // UpdateLayout is a POSITIONING call: a ground tooltip tracks its
    // label, so while the player walks the game re-lays-out the tooltip
    // EVERY FRAME — and v3.0.0 answered every one with a full-scene
    // FindObjectsOfType<TextMeshProUGUI> plus regexes over every hit.
    // That scan is now run only when the tooltip's CONTENT may have
    // changed: a SetAs*Tooltip call (dirty flag), the native range
    // switch flipping (the game re-renders), a tracked TMP losing our
    // marker (the game rewrote it), or a slow fallback tick for paths
    // nobody has mapped. Positioning-only frames cost nothing.
    private static int   s_dirtyUntilFrame = -1;
    private static int   s_lastScanFrame = -1;
    private static float s_lastScanTime = -1f;
    private const  int   DirtyFrameWindow = 5;
    private const  float FallbackScanInterval = 0.5f;   // seconds

    internal static void MarkDirty()
        => s_dirtyUntilFrame = Time.frameCount + DirtyFrameWindow;

    // GLM/Astra 2026-09-10: style and layout changes must rebuild an open tooltip now.
    internal static void ReRenderNow()
        => ReRenderFromOriginals();

    internal enum ScanTrigger { None, Dirty, MarkerLoss, Fallback }

    private static ScanTrigger ShouldScan()
    {
        int frame = Time.frameCount;
        if (frame == s_lastScanFrame) return ScanTrigger.None; // never twice in one frame
        if (frame <= s_dirtyUntilFrame) return ScanTrigger.Dirty;
        float now = Time.unscaledTime;
        if (s_lastScanTime < 0f || now - s_lastScanTime >= FallbackScanInterval) return ScanTrigger.Fallback;

        // Cheap per-frame check: did the game overwrite something we composed?
        foreach (var kv in s_originals)
        {
            TextMeshProUGUI tmp = kv.Value.tmp;
            try
            {
                if (tmp == null || !tmp.gameObject.activeInHierarchy) continue;
                if (!(tmp.text ?? "").Contains(Marker)) return ScanTrigger.MarkerLoss;
            }
            catch { }
        }
        return ScanTrigger.None;
    }

    private static bool DeepTier  => s_altHeld || Prefs.AlwaysShowTierDetails.Value;
    private static bool DeepRange => s_altHeld || Prefs.AlwaysShowRanges.Value;

    // ── The native range switch (the puppeteer, found via ApiProbe) ───
    // TooltipItemManager.showRangesInsteadOfDescriptionEnabled is the
    // STATIC flag the game's TooltipMode system reads — with it true the
    // tooltip runs in DefaultModRanges mode and RE-RENDERS the range rows
    // continuously (why SetActive and text-emptying both lost the war —
    // and why earlier models never cracked this). Driving the flag itself
    // ends the war: ranges off = the game never draws them; Alt/pin = the
    // game draws them natively. Runtime-only — the player's persisted
    // setting is never written; original value restored when the master
    // toggle goes off.
    private static bool? s_nativeRangesOriginal;
    private static bool s_nativeRangesWasEnabled;

    private static void DriveNativeRangeSwitch()
    {
        try
        {
            bool cur = TooltipItemManager.showRangesInsteadOfDescriptionEnabled;
            bool enabled = Prefs.EnableTooltips.Value;

            // Astra 2026-09-10: relinquish this native setting once on master-off.
            if (!enabled)
            {
                if (s_nativeRangesWasEnabled && s_nativeRangesOriginal != null &&
                    cur != s_nativeRangesOriginal.Value)
                    TooltipItemManager.showRangesInsteadOfDescriptionEnabled = s_nativeRangesOriginal.Value;
                s_nativeRangesOriginal = null;
                s_nativeRangesWasEnabled = false;
                return;
            }

            if (!s_nativeRangesWasEnabled || s_nativeRangesOriginal == null)
                s_nativeRangesOriginal = cur;
            s_nativeRangesWasEnabled = true;

            bool want = DeepRange;   // Alt held or the Always Show Ranges pin
            if (cur != want)
            {
                TooltipItemManager.showRangesInsteadOfDescriptionEnabled = want;
                MarkDirty();   // the game re-renders on this flip — fresh EHG text incoming
            }
        }
        catch (Exception ex) { Dbg.Log("native range switch failed: " + ex.Message); }
    }

    // ── Called from TerribleTooltipsMod.OnLateUpdate() ────────────────
    public static void OnLateUpdate()
    {
        TooltipPerf.Tick();
        DriveNativeRangeSwitch();

        // GLM BLOCKER 2026-09-10: master-off must restore marked text and release caches.
        try
        {
            bool enabled = Prefs.EnableTooltips.Value;
            if (s_masterPreviouslyEnabled == true && !enabled)
                RestoreVanillaOnMasterOff();
            s_masterPreviouslyEnabled = enabled;
        }
        catch { }

        // Alt deep view: state change → re-compose every cached TMP live.
        // Master toggle gates the re-render (a v2 fleet law: master OFF
        // means the mod touches nothing).
        try
        {
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            if (alt != s_altHeld)
            {
                s_altHeld = alt;
                MarkDirty();   // Alt can make the game rewrite TMPs after the cached originals pass
                if (Prefs.EnableTooltips.Value)
                    ReRenderFromOriginals();
            }
        }
        catch { }

        // Active-tooltip catch-up: keep evaluating the full scan gate even
        // when a static inventory tooltip receives no more UpdateLayout calls.
        try
        {
            if (Prefs.EnableTooltips.Value && HasActiveTooltip())
            {
                ScanTrigger trigger = ShouldScan();
                if (trigger != ScanTrigger.None)
                    RunScan(s_lastTooltip, s_lastArgs, trigger);
            }
        }
        catch { }

        // ── Per-frame range enforcement (last word before render) ─────
        try
        {
            if (Prefs.EnableTooltips.Value && !DeepRange && s_suppressedRanges.Count > 0)
            {
                var drop = new List<int>();
                foreach (var kv in s_suppressedRanges)
                {
                    TextMeshProUGUI tmp = kv.Value;
                    if (tmp == null) { drop.Add(kv.Key); continue; }
                    try
                    {
                        if (!tmp.gameObject.activeInHierarchy) { drop.Add(kv.Key); continue; }
                        string cur = tmp.text ?? "";
                        if (cur == Marker) continue;             // still ours
                        if (cur.IndexOf("Range:", StringComparison.OrdinalIgnoreCase) >= 0 &&
                            cur.IndexOf('\n') < 0)
                            tmp.text = Marker;                   // rewritten — re-empty
                        else
                            drop.Add(kv.Key);                    // repurposed — not ours, let go
                    }
                    catch { drop.Add(kv.Key); }
                }
                foreach (int k in drop) s_suppressedRanges.Remove(k);
            }
        }
        catch { }

        if (!Prefs.EnableTooltips.Value || s_tierColorCache.Count == 0) return;
        s_tierColorCache.RemoveAll(p => p.tmp == null || !p.tmp.gameObject.activeInHierarchy);
        foreach (var (tmp, color) in s_tierColorCache)
            tmp.color = color;
    }

    private static void RestoreVanillaOnMasterOff()
    {
        int restored = 0;
        foreach (var kv in s_originals)
        {
            var (tmp, original) = kv.Value;
            try
            {
                if (tmp != null && (tmp.text ?? "").Contains(Marker))
                {
                    tmp.text = original;
                    restored++;
                }
            }
            catch { }
        }

        s_originals.Clear();
        s_suppressedRanges.Clear();
        s_tierColorCache.Clear();

        RelayoutTooltips();
        Dbg.Log($"master off — restored {restored} TMPs");
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
            RelayoutTooltips();
        }
    }

    // UpdateLayout needs its original positioning arguments — the postfix
    // captures them (typed object[] via Harmony __args) and we replay them
    // verbatim for the re-measure.
    private static UITooltipItem s_lastTooltip;
    private static object[]      s_lastArgs;
    private static readonly Dictionary<int, (UITooltipItem ui, object[] args)> s_tooltips = new();
    private static float s_lastFullSceneTime = -1f;

    private static bool HasActiveTooltip()
    {
        if (s_lastTooltip != null && s_lastTooltip.tooltipActive) return true;
        foreach (var entry in s_tooltips.Values)
            if (entry.ui != null && entry.ui.tooltipActive) return true;
        return false;
    }

    private static void RememberTooltip(UITooltipItem ui, object[] args)
    {
        if (ui != null) s_tooltips[ui.GetInstanceID()] = (ui, args);
    }

    // The installed UITooltipItem bindings own content/compareContent and
    // blessing comparison panels. Serialized references do NOT prove ancestry:
    // include detached panels explicitly, and retain other observed instances
    // even when their UpdateLayout was rejected by the one-scan-per-frame gate.
    private static TextMeshProUGUI[] CollectTooltipTMPs()
    {
        var roots = new List<Transform>();
        var dead = new List<int>();
        foreach (var pair in s_tooltips)
        {
            var ui = pair.Value.ui;
            if (ui == null || !ui.tooltipActive) { dead.Add(pair.Key); continue; }
            AddRoot(ui.transform);
            AddRoot(ui.content?.transform);
            // MUTATION: comparison panel omitted
            AddRoot(ui.blessingContent?.transform);
            AddRoot(ui.blessingCompareContent?.transform);
            AddRoot(ui.resonanceContent?.transform);
        }
        foreach (int id in dead) s_tooltips.Remove(id);

        // Bindings expose references, not the serialized prefab hierarchy.
        // Verify the actual affix anchors instead of assuming every panel is
        // parented as expected. A foreign/reparented anchor uses the bounded
        // compatibility path below; never silently omit a comparison affix.
        foreach (var entry in s_tooltips.Values)
        {
            var ui = entry.ui;
            RequireTMP(ui.implicitText);
            RequireTMP(ui.compareImplicitText);
            RequireTMP(ui.blessingImplicitTMP);
            RequireTMP(ui.blessingCompareImplicitTMP);
            foreach (var group in new[] { ui.uniqueAffixesText, ui.prefixesText, ui.suffixesText,
                                         ui.compareUniqueAffixesText, ui.comparePrefixes, ui.compareSuffixes })
                if (group != null)
                    foreach (var affix in group)
                        if (affix != null) RequireTMP(affix._text);
            foreach (var affix in new[] { ui.sealedAffix, ui.sealedPrimordialAffix, ui.sealedCorruptedAffix,
                                         ui.compareSealedAffix, ui.compareSealedPrimordialAffix, ui.compareSealedCorruptedAffix })
                if (affix != null) RequireTMP(affix._text);
        }

        var tmps = new List<TextMeshProUGUI>();
        foreach (Transform root in roots)
        {
            var descendants = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (TooltipPerf.Enabled) TooltipPerf.TMPs(descendants.Length);
            foreach (TextMeshProUGUI tmp in descendants)
                tmps.Add(tmp);
        }
        // Range colour looks up parent AND grandparent keys. Their entire
        // descendant sets must be in scope, or an external sibling could carry
        // the grade. Validate this at runtime rather than guessing prefab shape.
        foreach (var tmp in tmps)
        {
            if (tmp == null || !tmp.gameObject.activeInHierarchy) continue;
            string text = tmp.text ?? "";
            if (text.Contains(Marker) && s_originals.TryGetValue(tmp.GetInstanceID(), out var saved))
                text = saved.original;
            if (!text.Contains("Range:") || text.Contains("Tier:") || HasBracket(text)) continue;
            RequireRoot(tmp.transform.parent);
            RequireRoot(tmp.transform.parent?.parent);
        }
        return tmps.ToArray();

        void RequireTMP(TextMeshProUGUI tmp)
        {
            if (tmp != null && tmp.gameObject.activeInHierarchy) RequireRoot(tmp.transform);
        }

        void RequireRoot(Transform transform)
        {
            if (transform == null) return;
            foreach (Transform root in roots)
                if (transform == root || transform.IsChildOf(root)) return;
            throw new InvalidOperationException("affix or range sibling lies outside tooltip panel roots");
        }

        void AddRoot(Transform root)
        {
            if (root == null) return;
            foreach (Transform existing in roots)
                if (root == existing || root.IsChildOf(existing)) return;
            roots.RemoveAll(existing => existing.IsChildOf(root));
            roots.Add(root);
        }
    }

    private static void RelayoutTooltips()
    {
        foreach (var entry in s_tooltips.Values)
            if (entry.ui != null && entry.ui.tooltipActive)
                RequestRelayout(entry.ui, entry.args);
    }

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
            s_lastTooltip = __instance;                 // for the Alt-path re-measure
            s_lastArgs    = __args;
            RememberTooltip(__instance, __args);
            ScanTrigger trigger = ShouldScan();
            if (trigger == ScanTrigger.None) return;
            RunScan(__instance, __args, trigger);
        }
    }

    // The scan + compose pass. Runs from the UpdateLayout postfix (gated)
    // and from the LateUpdate dirty catch-up.
    private static void RunScan(UITooltipItem __instance, object[] __args, ScanTrigger trigger)
    {
        s_lastScanFrame = Time.frameCount;
        s_lastScanTime = Time.unscaledTime;
        if (TooltipPerf.Enabled) TooltipPerf.Scan(trigger);
        try
        {
            RememberTooltip(__instance, __args);
            TextMeshProUGUI[] allTMPs;
            try
            {
                allTMPs = CollectTooltipTMPs();
                if (TooltipPerf.Enabled) TooltipPerf.Scoped();
            }
            catch (Exception ex)
            {
                // A destroyed/reparenting native hierarchy must not silently
                // drop comparison text. Compatibility fallback is bounded even
                // during dirty bursts; an empty *valid* scope never triggers it.
                if (TooltipPerf.Enabled) TooltipPerf.ScanError();
                float now = Time.unscaledTime;
                if (s_lastFullSceneTime >= 0f && now - s_lastFullSceneTime < FallbackScanInterval) return;
                s_lastFullSceneTime = now; // latch before enumeration, including throws
                if (TooltipPerf.Enabled) TooltipPerf.FullScene();
                if (TooltipPerf.Enabled)
                    Dbg.Log("tooltip scope unavailable; bounded full-scene fallback: " + ex.Message);
                allTMPs = UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>();
                if (TooltipPerf.Enabled && allTMPs != null) TooltipPerf.TMPs(allTMPs.Length);
            }
            if (allTMPs == null) return;

            TrackFormatterHealth(allTMPs);

            // Prune stale originals while we're here
            PruneOriginals();

            int composed = 0;

            // ── Pass 1: collect grade colour per parent instance ID so
            //    standalone Range-only TMPs (set/unique siblings) can
            //    inherit the correct grade colour in deep view.
            var parentGradeColor = new Dictionary<int, string>();
            foreach (TextMeshProUGUI tmp in allTMPs)
            {
                if (tmp == null || !tmp.gameObject.activeInHierarchy) continue;
                string t = tmp?.text;
                if (t != null && t.Contains(Marker) && s_originals.TryGetValue(tmp.GetInstanceID(), out var saved))
                    t = saved.original; // later range rewrites still inherit a composed sibling's grade
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
                    if (tmp == null || !tmp.gameObject.activeInHierarchy) continue;
                    string text = tmp?.text;
                    if (string.IsNullOrEmpty(text)) continue;

                    // Our own composed output — never re-ingest it.
                    if (text.Contains(Marker)) continue;

                    // GroundLabels' output — a single-affix ground bracket
                    // matches the grade regex, and composing it mangles the
                    // label permanently (its 3-ZWSP marker survives inside
                    // our 4-ZWSP one, so its own double-process guard then
                    // skips the repair). Ground labels are not this
                    // composer's territory.
                    if (text.Contains(GroundLabels.Marker)) continue;

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
                            s_suppressedRanges[tmp.GetInstanceID()] = tmp;
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
                            ? $"<size=90%><color={inheritedColor}>{stripped}</color></size>"
                            : $"<size=90%>{stripped}</size>") + Marker;
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

            // Only after a completed pass, BEFORE native relayout can rewrite
            // successfully composed text. Marked text still needs its original
            // for Alt/master-off; active markerless replacements no longer do.
            RetireStaleOriginals();

            // ── Lean law: we shrank texts AFTER the game measured the
            //    essay — re-measure once so the blank rows collapse.
            if (composed > 0)
                RelayoutTooltips();
        }
        catch (Exception ex)
        {
            if (TooltipPerf.Enabled) TooltipPerf.ScanError();
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
    private static int s_emptyBracketScans;
    private static bool s_affixHookWarned;

    // GLM 2026-09-10: make a dead affix formatter hook visible instead of silently vanilla.
    private static void TrackFormatterHealth(TextMeshProUGUI[] allTMPs)
    {
        if (!Prefs.EnableTooltips.Value) return;

        bool found = false;
        bool expected = false;
        foreach (TextMeshProUGUI tmp in allTMPs)
        {
            if (tmp == null || !tmp.gameObject.activeInHierarchy) continue;
            string text = tmp?.text;
            if (string.IsNullOrEmpty(text)) continue;
            // A synthesized Tier chip or suppressed Range is NOT evidence of
            // an injected bracket. Inspect its saved source, not our marker.
            if (text.Contains(Marker))
            {
                if (!s_originals.TryGetValue(tmp.GetInstanceID(), out var saved)) continue;
                text = saved.original;
            }
            if (text.Contains(GroundLabels.Marker) || tmp.gameObject.name == "requires") continue;
            if (HasBracket(text))
            {
                found = true;
                break;
            }
            // Shards/lore/range-only rows do not establish that an affix hook
            // should have run. Only native tier-bearing affix text is a negative
            // sample; unique-only failures without tiers remain inconclusive.
            if (s_tierRegex.IsMatch(text)) expected = true;
        }

        if (found || !expected)
        {
            s_emptyBracketScans = 0;
            return;
        }

        s_emptyBracketScans++;
        if (s_emptyBracketScans >= 20 && !s_affixHookWarned)
        {
            s_affixHookWarned = true;
            MelonLogger.Warning(
                "no affix brackets seen in 20 tier-bearing tooltip scans — the affix formatter hook may be dead after a game update; tier synthesis alone does not verify the hook");
        }
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

    private static void RetireStaleOriginals()
    {
        List<int> stale = null;
        foreach (var kv in s_originals)
        {
            try
            {
                TextMeshProUGUI tmp = kv.Value.tmp;
                if (tmp == null || !tmp.gameObject.activeInHierarchy ||
                    (tmp.text ?? "").Contains(Marker)) continue;
                (stale ??= new List<int>()).Add(kv.Key);
            }
            catch { } // Unknown ownership: keep restoration data, not a blind delete.
        }
        if (stale == null) return;
        foreach (int id in stale)
        {
            s_originals.Remove(id);
            // Do not let the enforcement pass re-hide a future Range rewrite
            // after its restoration data was retired. The next scan recaptures it.
            s_suppressedRanges.Remove(id);
        }
        if (TooltipPerf.Enabled) TooltipPerf.StaleRetired(stale.Count);
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
        string lastTierHex = null;   // continuation lines of a multi-stat affix wear its first-line colour
        int lastTier = 0;

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
                lastTierHex   = tierHex;
                lastTier      = tier;
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
                    outLines.Add($"<size=90%><color={tmpGradeColor}>{stripped}</color></size>");
                }
                continue;   // suppressed
            }

            // Continuation stat line of a multi-stat affix (weaver idol
            // "+17 Health / +2 Health Regen", hybrids) — the bracket sits
            // on line one only; the rest wear the same tier colour so the
            // affix reads as ONE thing. Flavor text on non-affix TMPs has
            // no bracket above it, so lastTierHex is null and it passes
            // through untouched, as before.
            if (lastTierHex != null && line.Trim().Length > 0 &&
                Prefs.NameColorMode.Value == AffixNameColorMode.GreaterAffix)
            {
                string continuation = s_colorTagRegex.Replace(line, "").Trim();
                if (Prefs.TooltipTierColors.Value && (lastTier == 6 || lastTier == 7))
                    continuation = $"<color={GreaterAffixHex()}>{continuation}</color>";
                outLines.Add(continuation);
                continue;
            }
            if (lastTierHex != null && line.Trim().Length > 0 &&
                Prefs.NameColorMode.Value == AffixNameColorMode.TierColor &&
                Prefs.TooltipTierColors.Value)
            {
                outLines.Add($"<color={lastTierHex}>{s_colorTagRegex.Replace(line, "").Trim()}</color>");
                continue;
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
                    deepLines.Add($"<size=90%><color={tierHex ?? "#FFFFFF"}>{stripped}</color></size>");
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
                else if (Prefs.NameColorMode.Value == AffixNameColorMode.GreaterAffix &&
                         Prefs.TooltipTierColors.Value && (tier == 6 || tier == 7))
                    name = $"<color={GreaterAffixHex()}>{name}</color>";
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
        return tail.Length > 0 ? $"<size=90%><color={Dim}>{tail}</color></size>" : null;
    }

    private static bool s_greaterAffixTintWarned;

    private static string GreaterAffixHex()
    {
        string value = Prefs.GreaterAffixTint.Value;
        if (Regex.IsMatch(value ?? "", @"^#[0-9A-Fa-f]{6}([0-9A-Fa-f]{2})?$"))
            return value;

        if (!s_greaterAffixTintWarned)
        {
            s_greaterAffixTintWarned = true;
            MelonLogger.Warning(
                $"invalid GreaterAffixTint '{value}'; using {Colors.GreaterAffixTintDefault}");
        }
        return Colors.GreaterAffixTintDefault;
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
                : $"<size=90%><color={Dim}>Sealed</color></size>")
            : null;

        string tierPart = null;
        if (tier > 0)
        {
            string tierWord = Prefs.TierWord.Value == TierWordStyle.Compact
                ? $"T{tier}"
                : $"Tier {tier}";
            if (tintTier && tierHex != null)
                tierPart = badges
                    ? $"<mark={tierHex}66><color={Ink}>{tierWord}</color></mark>"
                    : $"<color={tierHex}>{tierWord}</color>";
            else
                tierPart = tierWord;   // colors off → no chip, plain text
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
        else if (Prefs.NameColorMode.Value == AffixNameColorMode.GreaterAffix &&
                 tintTier && (tier == 6 || tier == 7))
        {
            name = $"<color={GreaterAffixHex()}>{cleanName}</color>";
        }

        // Chips separate themselves visually; plain text uses the configured divider.
        string signal;
        if (badges)
        {
            signal = JoinSignal(" ", sealedPart, tierPart, gradePart);
        }
        else
        {
            string signalSeparator = Prefs.Layout.Value == TooltipLayout.BadgeLeft
                ? $"<color={Dim}>·</color>"
                : " ";
            string dividerGlyph = Prefs.UnitSeparator.Value == UnitSeparatorStyle.Dot ? "·" : "|";
            string divider = Prefs.DividerStyle.Value == DividerStyle.Strip
                ? $" <color=#00000000>{dividerGlyph}</color> "
                : $" <color={Dim}>{dividerGlyph}</color> ";
            string unit = null;
            if (tierPart != null && gradePart != null)
                unit = $"<link=\"ttu\">{tierPart}</link><link=\"ttd\">{divider}</link><link=\"ttu\">{gradePart}</link>";
            else if (tierPart != null || gradePart != null)
                unit = $"<link=\"ttu\">{tierPart ?? gradePart}</link>";
            signal = JoinSignal(signalSeparator, sealedPart, unit);
        }

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

// Beta telemetry stays in this file to avoid widening the source write set.
// Disabled: preference guards only; no clock reads, counters, formatting or I/O.
// Callers guard before invoking event methods. The first event starts the window,
// including events before the first LateUpdate; enabling never loses that sample.
internal static class TooltipPerf
{
    internal static bool Enabled => Prefs.DebugLog != null && Prefs.DebugLog.Value;
    private static bool s_running;
    private static float s_started;
    private static long s_scans, s_fullScene, s_scoped, s_tmps, s_dirty, s_markerLoss, s_fallback;
    private static long s_ruleStart, s_ruleReplacement, s_ruleAttempts, s_ruleMatch, s_ruleNoTarget;
    private static long s_ruleGiveUp, s_ruleInjected, s_ruleReuse, s_staleRetired, s_scanErrors;

    private static void Begin()
    {
        if (s_running) return;
        s_running = true;
        s_started = Time.unscaledTime;
    }

    internal static void Scan(TooltipRecolor.ScanTrigger trigger)
    {
        Begin();
        s_scans++;
        if (trigger == TooltipRecolor.ScanTrigger.Dirty) s_dirty++;
        else if (trigger == TooltipRecolor.ScanTrigger.MarkerLoss) s_markerLoss++;
        else if (trigger == TooltipRecolor.ScanTrigger.Fallback) s_fallback++;
    }
    internal static void FullScene() { Begin(); s_fullScene++; }
    internal static void Scoped() { Begin(); s_scoped++; }
    internal static void TMPs(int count) { Begin(); s_tmps += count; }
    internal static void ScanError() { Begin(); s_scanErrors++; }
    internal static void RuleStart() { Begin(); s_ruleStart++; }
    internal static void RuleReplacement() { Begin(); s_ruleReplacement++; }
    internal static void RuleAttempt() { Begin(); s_ruleAttempts++; }
    internal static void RuleMatch() { Begin(); s_ruleMatch++; }
    internal static void RuleNoTarget() { Begin(); s_ruleNoTarget++; }
    internal static void RuleGiveUp() { Begin(); s_ruleGiveUp++; }
    internal static void RuleInjected() { Begin(); s_ruleInjected++; }
    internal static void RuleReuse() { Begin(); s_ruleReuse++; }
    internal static void StaleRetired(int count) { Begin(); s_staleRetired += count; }

    internal static void Tick()
    {
        if (!Enabled)
        {
            if (s_running) { Reset(); s_running = false; }
            return;
        }
        Begin();
        float now = Time.unscaledTime;
        float elapsed = now - s_started;
        if (elapsed < 5f) return;
        // Advance/reset before logging so a logger failure cannot cause a flood.
        string summary = FormattableString.Invariant(
            $"[perf] {elapsed:0.0}s: scans={s_scans} (scoped={s_scoped}, fullScene={s_fullScene}, tmps={s_tmps}, trigger: dirty={s_dirty}/markerLoss={s_markerLoss}/fallback={s_fallback}) ruleStart={s_ruleStart} ruleReplacement={s_ruleReplacement} ruleAttempts={s_ruleAttempts} ruleMatch={s_ruleMatch} ruleNoTarget={s_ruleNoTarget} ruleGiveUp={s_ruleGiveUp} ruleInjected={s_ruleInjected} ruleReuse={s_ruleReuse} staleRetired={s_staleRetired} scanErrors={s_scanErrors}");
        Reset();
        s_started = now;
        try { MelonLogger.Msg(summary); } catch { }
    }

    private static void Reset()
    {
        s_scans = s_fullScene = s_scoped = s_tmps = s_dirty = s_markerLoss = s_fallback = 0;
        s_ruleStart = s_ruleReplacement = s_ruleAttempts = s_ruleMatch = s_ruleNoTarget = 0;
        s_ruleGiveUp = s_ruleInjected = s_ruleReuse = s_staleRetired = s_scanErrors = 0;
    }
}
