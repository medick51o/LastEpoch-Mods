// ================================================================
//  NativeSettings.cs — injection helpers for the game's settings screen.
//
//  Technique by KG / war3i4i (LastEpochImprovements) — clone the game's
//  own option rows, strip their localization, rewire their widgets.
//  Hardened per the family playbook (fog_OF_war lineage):
//   • ONE latched degradation warning per session; the mod keeps working
//     and prefs stay editable via UserData/medick_Terrible_Tooltips.cfg.
//   • Clones named LAST + destroyed in catch — a half-built row can never
//     be found later and mistaken for success.
//   • Creators are idempotent: existing rows are REBOUND, not duplicated.
//   • InfoRow: an honest non-interactive display row (legend/reference) —
//     the checkbox visual is removed instead of apologizing for it
//     ("this box is not clickable" dies here).
// ================================================================

using Il2CppLE.UI.Controls;
using UnityEngine.Localization.Components;

namespace medick_Terrible_Tooltips;

internal static class NativeSettings
{
    const string CategoryPrefix   = "ModsCategory - ";
    const string HeaderAnchor     = "Header - Interface";
    const string DropdownTemplate = "Dropdown - Language Selection";
    // The game's own typo, first — plus the spelling EHG would fix it to.
    static readonly string[] ToggleTemplates =
    {
        "Toogle - Minion Health Bars",
        "Toggle - Minion Health Bars",
    };

    static bool _degradedWarned;
    // Pins listener delegates so IL2CPP never GCs one out from under a live
    // widget. Grows by one set per settings-panel Awake — bounded by how
    // many times a player opens settings per session; accepted, not pruned
    // (pruning risks collecting a delegate a surviving widget still calls).
    static readonly List<Delegate> _keepAlive = new();

    public static void WarnDegradedOnce(string detail)
    {
        if (_degradedWarned) return;
        _degradedWarned = true;
        MelonLogger.Warning(
            $"settings hierarchy changed ({detail}) — Terrible Tooltips settings UI unavailable; " +
            "configure via UserData/medick_Terrible_Tooltips.cfg");
    }

    static Transform Root(SettingsPanelTabNavigable settings) =>
        settings.transform.GetChild(0).GetChild(0);

    static Transform FindToggleTemplate(Transform root)
    {
        foreach (string name in ToggleTemplates)
        {
            Transform t = root.Find(name);
            if (t) return t;
        }
        return null;
    }

    public static bool TemplatesAvailable(SettingsPanelTabNavigable settings)
    {
        try
        {
            Transform root = Root(settings);
            if (FindToggleTemplate(root) == null) { WarnDegradedOnce("toggle row template missing");           return false; }
            if (!root.Find(DropdownTemplate))     { WarnDegradedOnce($"template '{DropdownTemplate}' missing"); return false; }
            if (!root.Find(HeaderAnchor))         { WarnDegradedOnce($"anchor '{HeaderAnchor}' missing");       return false; }
            return true;
        }
        catch (Exception ex)
        {
            WarnDegradedOnce("settings root path unreachable: " + ex.Message);
            return false;
        }
    }

    static int CreateCategoryIfNeeded(SettingsPanelTabNavigable settings, string category)
    {
        Transform root = Root(settings);
        string catName = CategoryPrefix + category;

        if (!root.Find(catName))
        {
            Transform headerInterface = root.Find(HeaderAnchor);
            if (!headerInterface)
            {
                WarnDegradedOnce($"anchor '{HeaderAnchor}' missing");
                return -1;
            }

            Transform newCat = null;
            try
            {
                newCat = UnityEngine.Object.Instantiate(headerInterface, headerInterface.parent);
                TMP_Text label = newCat.GetChild(0).GetChild(0).GetComponent<TMP_Text>();
                label.text  = category;
                label.color = Color.white;
                UnityEngine.Object.DestroyImmediate(
                    newCat.GetChild(0).GetChild(0).GetComponent<LocalizeStringEvent>());
                newCat.SetSiblingIndex(headerInterface.GetSiblingIndex());
                newCat.name = catName;   // name LAST
            }
            catch (Exception ex)
            {
                if (newCat) UnityEngine.Object.DestroyImmediate(newCat.gameObject);
                WarnDegradedOnce("category header build failed: " + ex.Message);
                return -1;
            }
        }

        Transform cat = root.Find(catName);
        int headerIdx = cat.GetSiblingIndex();
        int insertAt  = headerIdx;
        for (int i = headerIdx + 1; i < root.childCount; i++)
        {
            Transform sib = root.GetChild(i);
            if (sib.name.StartsWith(CategoryPrefix) && sib.name != catName) break;
            if (sib.name.StartsWith("Header - ")) break;
            insertAt = i;
        }
        return insertAt;
    }

    // ── Shared row factory (clone + strip + title/desc) ──────────────
    static Transform BuildRowBase(SettingsPanelTabNavigable settings, string category,
        string rowName, string title, string description, out bool existed)
    {
        Transform root = Root(settings);

        Transform existing = root.Find(rowName);
        if (existing) { existed = true; return existing; }
        existed = false;

        Transform template = FindToggleTemplate(root);
        if (!template) { WarnDegradedOnce("toggle row template missing"); return null; }

        int orderIndex = CreateCategoryIfNeeded(settings, category);
        if (orderIndex < 0) return null;

        Transform row = null;
        try
        {
            row = UnityEngine.Object.Instantiate(template, template.parent);
            row.SetSiblingIndex(orderIndex + 1);
            foreach (var loc in row.GetComponentsInChildren<LocalizeStringEvent>(true))
                UnityEngine.Object.DestroyImmediate(loc);

            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length > 0)
                texts[0].text = string.IsNullOrEmpty(description)
                    ? title
                    : $"{title}\n<size=62%><color=#AAAAAA>{description}</color></size>";

            row.name = rowName;   // name LAST
            return row;
        }
        catch (Exception ex)
        {
            if (row) UnityEngine.Object.DestroyImmediate(row.gameObject);
            WarnDegradedOnce($"row '{title}' build failed: {ex.Message}");
            return null;
        }
    }

    // ── Honest boolean toggle ─────────────────────────────────────────
    public static Toggle CreateToggle(SettingsPanelTabNavigable settings,
        string category, string rowName, string title, string description,
        bool initial, Action<bool> onChanged)
    {
        try
        {
            Transform row = BuildRowBase(settings, category, rowName, title, description, out _);
            if (row == null) return null;

            Toggle toggle = row.GetComponentInChildren<Toggle>(true);
            if (toggle == null)
            {
                WarnDegradedOnce($"toggle widget missing in row '{row.name}'");
                try { UnityEngine.Object.DestroyImmediate(row.gameObject); } catch { }
                return null;
            }
            // Fresh event object: clears runtime AND serialized persistent
            // listeners a game update might wire into the donor row.
            toggle.onValueChanged = new Toggle.ToggleEvent();
            try { toggle.SetIsOnWithoutNotify(initial); }
            catch { toggle.isOn = initial; }   // safe: no listeners attached yet
            var listener = new Action<bool>(_ => onChanged(toggle.isOn));
            _keepAlive.Add(listener);
            toggle.onValueChanged.AddListener(listener);
            return toggle;
        }
        catch (Exception ex)
        {
            WarnDegradedOnce($"toggle '{title}' failed: {ex.Message}");
            return null;
        }
    }

    // ── Clickable button row (Aaron's House) ──────────────────────────
    public static void CreateButton(SettingsPanelTabNavigable settings,
        string category, string rowName, string title, string description, Action onClick)
    {
        try
        {
            Transform row = BuildRowBase(settings, category, rowName, title, description, out _);
            if (row == null) return;

            Toggle toggle = row.GetComponentInChildren<Toggle>(true);
            if (toggle != null)
            {
                toggle.onValueChanged = new Toggle.ToggleEvent();
                // Pure button: start unchecked and snap back after every
                // press, so the donor checkbox never latches a meaningless
                // state (v1 parity — the rebuild briefly dropped this).
                try { toggle.SetIsOnWithoutNotify(false); }
                catch { toggle.isOn = false; }   // safe: no listeners yet
                var listener = new Action<bool>(_ =>
                {
                    try { toggle.SetIsOnWithoutNotify(false); } catch { }
                    onClick?.Invoke();
                });
                _keepAlive.Add(listener);
                toggle.onValueChanged.AddListener(listener);
            }
            Button btn = row.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick = new Button.ButtonClickedEvent();
                var click = new Action(() => onClick?.Invoke());
                _keepAlive.Add(click);
                btn.onClick.AddListener(click);
            }
        }
        catch (Exception ex)
        {
            WarnDegradedOnce($"button '{title}' failed: {ex.Message}");
        }
    }

    // ── Honest information row (legend / reference) ───────────────────
    // The checkbox visual is REMOVED — the row simply looks like what it
    // is. No interaction, no apology.
    public static void CreateInfoRow(SettingsPanelTabNavigable settings,
        string category, string rowName, string title, string description)
    {
        try
        {
            Transform row = BuildRowBase(settings, category, rowName, title, description, out bool existed);
            if (row == null || existed) return;

            try
            {
                Toggle toggle = row.GetComponentInChildren<Toggle>(true);
                if (toggle != null)
                {
                    try { if (toggle.targetGraphic != null) toggle.targetGraphic.gameObject.SetActive(false); } catch { }
                    try { if (toggle.graphic       != null) toggle.graphic.gameObject.SetActive(false);       } catch { }
                    UnityEngine.Object.DestroyImmediate(toggle);
                }
                Button btn = row.GetComponent<Button>();
                if (btn != null) UnityEngine.Object.DestroyImmediate(btn);
            }
            catch { }   // worst case: a dead toggle remains — v1 behavior
        }
        catch (Exception ex)
        {
            WarnDegradedOnce($"info row '{title}' failed: {ex.Message}");
        }
    }

    // ── Enum dropdown ─────────────────────────────────────────────────
    public static ColoredIconDropdown CreateEnumDropdown<T>(SettingsPanelTabNavigable settings,
        string category, string rowName, string title, string description,
        MelonPreferences_Entry<T> option, Action<int> onChanged) where T : Enum
    {
        try
        {
            Transform root = Root(settings);

            Transform existing = root.Find(rowName);
            if (existing)
            {
                try
                {
                    var dd = existing.GetChild(3).GetComponent<ColoredIconDropdown>();
                    if (dd != null)                          // idempotent: rebind
                    {
                        dd.onValueChanged.RemoveAllListeners();
                        dd.value = (int)(object)option.Value;
                        var rebound = new Action<int>(_ => onChanged(dd.value));
                        _keepAlive.Add(rebound);
                        dd.onValueChanged.AddListener(rebound);
                        return dd;
                    }
                }
                catch { }
                UnityEngine.Object.DestroyImmediate(existing.gameObject);   // broken leftover: rebuild
            }

            Transform template = root.Find(DropdownTemplate);
            if (!template)
            {
                WarnDegradedOnce($"template '{DropdownTemplate}' missing");
                return null;
            }
            int orderIndex = CreateCategoryIfNeeded(settings, category);
            if (orderIndex < 0) return null;

            Transform row = null;
            try
            {
                row = UnityEngine.Object.Instantiate(template, template.parent);
                UnityEngine.Object.DestroyImmediate(row.GetComponent<LocalizationSettingsPanelUI>());
                UnityEngine.Object.DestroyImmediate(row.GetComponent<LootFilterSettingsPanelUI>());
                row.SetSiblingIndex(orderIndex + 1);

                TMP_Text nameLabel = row.GetChild(0).GetComponent<TMP_Text>();
                nameLabel.text = title;
                UnityEngine.Object.DestroyImmediate(row.GetChild(0).GetComponent<LocalizeStringEvent>());

                TMP_Text descLabel = row.GetChild(1).GetComponent<TMP_Text>();
                descLabel.text = description;
                UnityEngine.Object.DestroyImmediate(row.GetChild(1).GetComponent<LocalizeStringEvent>());

                ColoredIconDropdown dropdown = row.GetChild(3).GetComponent<ColoredIconDropdown>();
                // RemoveAllListeners (not a fresh event object like the
                // toggles get): ColoredIconDropdown may carry serialized
                // listeners tied to its own visuals; the donor's settings
                // handlers die with the components destroyed above. This
                // asymmetry is deliberate and shipped-proven since v1.
                dropdown.onValueChanged.RemoveAllListeners();
                dropdown.ClearOptions();
                Il2CppSystem.Collections.Generic.List<string> opts = new();
                foreach (string enumName in Enum.GetNames(typeof(T)))
                    opts.Add(enumName.Replace("_", " "));
                dropdown.AddOptions(opts);
                dropdown.value = (int)(object)option.Value;
                var listener = new Action<int>(_ => onChanged(dropdown.value));
                _keepAlive.Add(listener);
                dropdown.onValueChanged.AddListener(listener);

                row.name = rowName;   // name LAST
                return dropdown;
            }
            catch (Exception ex)
            {
                if (row) UnityEngine.Object.DestroyImmediate(row.gameObject);
                WarnDegradedOnce($"dropdown '{title}' build failed: {ex.Message}");
                return null;
            }
        }
        catch (Exception ex)
        {
            WarnDegradedOnce($"dropdown '{title}' failed: {ex.Message}");
            return null;
        }
    }
}
