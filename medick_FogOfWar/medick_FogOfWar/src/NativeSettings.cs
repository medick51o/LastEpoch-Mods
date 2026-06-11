using System;
using Il2Cpp;
using Il2CppLE.UI.Controls;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace medick_FogOfWar
{
    // Injection helpers for the game's native settings screen.
    //
    // Technique by KG / war3i4i (LastEpochImprovements) — clone the game's
    // own option rows ("Toogle - Minion Health Bars", "Dropdown - Language
    // Selection"), strip their localization bindings, rewire their widgets.
    // Credit where it's due: this approach is his.
    //
    // Hardening contract (this file is the template the larger Terrible mods
    // inherit — see SPEC.md):
    //  • Degradation is ONE latched warning per session, then silence; fog
    //    control keeps working from the cfg file.
    //  • Clones are named LAST and destroyed in catch blocks, so a half-built
    //    row or header can never be found later and mistaken for success.
    //  • All creators are idempotent: an existing row is REBOUND (listeners
    //    rewired to the new delegates), never duplicated; a broken leftover
    //    row is destroyed and rebuilt.
    internal static class NativeSettings
    {
        const string CategoryPrefix = "ModsCategory - ";
        const string ToggleTemplate   = "Toogle - Minion Health Bars";   // [sic] — the game's own typo
        const string DropdownTemplate = "Dropdown - Language Selection";
        const string HeaderAnchor     = "Header - Interface";

        static bool _degradedWarned;

        static void WarnDegradedOnce(string detail)
        {
            if (_degradedWarned) return;
            _degradedWarned = true;
            MelonLogger.Warning(
                $"settings hierarchy changed ({detail}) — {BuildInfo.DisplayName} settings UI unavailable; " +
                "fog control still works via UserData/medick_The_fogOFwar.cfg");
        }

        static Transform Root(SettingsPanelTabNavigable settings) =>
            settings.transform.GetChild(0).GetChild(0);

        // Probe everything the creators depend on, before building anything.
        // One missing piece → one latched warning → caller skips injection.
        public static bool TemplatesAvailable(SettingsPanelTabNavigable settings)
        {
            try
            {
                Transform root = Root(settings);
                if (!root.Find(ToggleTemplate))   { WarnDegradedOnce($"template '{ToggleTemplate}' missing");   return false; }
                if (!root.Find(DropdownTemplate)) { WarnDegradedOnce($"template '{DropdownTemplate}' missing"); return false; }
                if (!root.Find(HeaderAnchor))     { WarnDegradedOnce($"anchor '{HeaderAnchor}' missing");       return false; }
                return true;
            }
            catch (Exception ex)
            {
                WarnDegradedOnce("settings root path unreachable: " + ex.Message);
                return false;
            }
        }

        // Creates (or finds) a named category header. Returns the sibling
        // index AFTER the last item already in this category, or -1 when the
        // header cannot exist (caller must skip row creation).
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
                    newCat.name = catName;   // name LAST — a half-built clone is never findable as ours
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

        // Clickable row (cloned toggle row). Returns the row's Toggle so the
        // caller can reflect selection state radio-style; null on failure.
        public static Toggle CreateButton(SettingsPanelTabNavigable settings,
            string category, string rowName, string title, string description, Action onClick)
        {
            try
            {
                Transform root = Root(settings);

                Transform existing = root.Find(rowName);
                if (existing) return RebindButton(existing, onClick);   // idempotent: rebind

                Transform template = root.Find(ToggleTemplate);
                if (!template)
                {
                    WarnDegradedOnce($"template '{ToggleTemplate}' missing");
                    return null;
                }
                int orderIndex = CreateCategoryIfNeeded(settings, category);
                if (orderIndex < 0) return null;

                Transform row = null;
                try
                {
                    row = UnityEngine.Object.Instantiate(template, template.parent);
                    row.SetSiblingIndex(orderIndex + 1);
                    foreach (var loc in row.GetComponentsInChildren<LocalizeStringEvent>())
                        UnityEngine.Object.DestroyImmediate(loc);

                    TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>();
                    if (texts.Length > 0)
                        texts[0].text = string.IsNullOrEmpty(description)
                            ? title
                            : $"{title}\n<size=62%><color=#AAAAAA>{description}</color></size>";

                    row.name = rowName;   // name LAST
                    return RebindButton(row, onClick);
                }
                catch (Exception ex)
                {
                    if (row) UnityEngine.Object.DestroyImmediate(row.gameObject);
                    MelonLogger.Warning($"settings row '{title}' failed: {ex.Message}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"settings row '{title}' failed: {ex.Message}");
                return null;
            }
        }

        static Toggle RebindButton(Transform row, Action onClick)
        {
            Toggle toggle = row.GetComponentInChildren<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveAllListeners();
                // One invoke per user edge. The caller owns the checked state
                // (synced via SetIsOnWithoutNotify); writing isOn inside this
                // listener would re-enter onValueChanged and double-fire.
                toggle.onValueChanged.AddListener(new Action<bool>(_ => onClick?.Invoke()));
            }
            Button btn = row.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(new Action(() => onClick?.Invoke()));
            }
            return toggle;
        }

        // Enum dropdown row. Returns the live dropdown (or null) so the
        // caller can sync it when the value changes elsewhere.
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
                            dd.onValueChanged.AddListener(new Action<int>(_ => onChanged(dd.value)));
                            return dd;
                        }
                    }
                    catch { }
                    // Broken leftover (hierarchy drift mid-session): rebuild.
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
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
                    dropdown.onValueChanged.RemoveAllListeners();
                    dropdown.ClearOptions();
                    Il2CppSystem.Collections.Generic.List<string> opts = new();
                    foreach (string enumName in Enum.GetNames(typeof(T)))
                        opts.Add(enumName.Replace("_", " "));
                    dropdown.AddOptions(opts);
                    dropdown.value = (int)(object)option.Value;
                    dropdown.onValueChanged.AddListener(new Action<int>(_ => onChanged(dropdown.value)));

                    row.name = rowName;   // name LAST
                    return dropdown;
                }
                catch (Exception ex)
                {
                    if (row) UnityEngine.Object.DestroyImmediate(row.gameObject);
                    MelonLogger.Warning($"settings dropdown '{title}' failed: {ex.Message}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"settings dropdown '{title}' failed: {ex.Message}");
                return null;
            }
        }
    }
}
