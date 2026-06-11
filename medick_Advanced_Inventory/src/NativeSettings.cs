using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace medick_Terrible_Inventory
{
    // Injection helpers for the game's native settings screen.
    // Technique by KG / war3i4i (LastEpochImprovements) — clone the game's
    // own option rows, strip their localization, rewire their widgets.
    //
    // Hardened per the family playbook (proven in fog_OF_war):
    //  • ONE latched degradation warning per session; fog... mod features
    //    keep working from MelonPreferences.cfg if the UI can't build.
    //  • Clones named LAST + destroyed in catch — half-built rows are never
    //    findable and mistaken for success.
    //  • Creators are idempotent: existing rows are REBOUND, not duplicated.
    internal static class NativeSettings
    {
        const string CategoryPrefix = "ModsCategory - ";
        const string ToggleTemplate = "Toogle - Minion Health Bars";   // [sic] — the game's own typo
        const string HeaderAnchor   = "Header - Interface";

        static bool _degradedWarned;
        static readonly List<Delegate> _keepAlive = new();

        static void WarnDegradedOnce(string detail)
        {
            if (_degradedWarned) return;
            _degradedWarned = true;
            MelonLogger.Warning(
                $"settings hierarchy changed ({detail}) — {BuildInfo.DisplayName} settings UI unavailable; " +
                "configure via UserData/MelonPreferences.cfg");
        }

        static Transform Root(SettingsPanelTabNavigable settings) =>
            settings.transform.GetChild(0).GetChild(0);

        public static bool TemplatesAvailable(SettingsPanelTabNavigable settings)
        {
            try
            {
                Transform root = Root(settings);
                if (!root.Find(ToggleTemplate)) { WarnDegradedOnce($"template '{ToggleTemplate}' missing"); return false; }
                if (!root.Find(HeaderAnchor))   { WarnDegradedOnce($"anchor '{HeaderAnchor}' missing");     return false; }
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

        // Honest boolean toggle row (taxonomy class: real control).
        // Returns the live Toggle (or null) so callers can sync if needed.
        public static Toggle CreateToggle(SettingsPanelTabNavigable settings,
            string category, string rowName, string title, string description,
            bool initial, Action<bool> onChanged)
        {
            try
            {
                Transform root = Root(settings);

                Transform existing = root.Find(rowName);
                if (existing) return RebindToggle(existing, initial, onChanged);

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
                    foreach (var loc in row.GetComponentsInChildren<LocalizeStringEvent>(true))
                        UnityEngine.Object.DestroyImmediate(loc);

                    TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
                    if (texts.Length > 0)
                        texts[0].text = string.IsNullOrEmpty(description)
                            ? title
                            : $"{title}\n<size=62%><color=#AAAAAA>{description}</color></size>";

                    row.name = rowName;   // name LAST
                    return RebindToggle(row, initial, onChanged);
                }
                catch (Exception ex)
                {
                    if (row) UnityEngine.Object.DestroyImmediate(row.gameObject);
                    MelonLogger.Warning($"settings toggle '{title}' failed: {ex.Message}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"settings toggle '{title}' failed: {ex.Message}");
                return null;
            }
        }

        static Toggle RebindToggle(Transform row, bool initial, Action<bool> onChanged)
        {
            Toggle toggle = row.GetComponentInChildren<Toggle>(true);
            if (toggle == null) return null;
            toggle.onValueChanged.RemoveAllListeners();
            try { toggle.SetIsOnWithoutNotify(initial); }
            catch { toggle.isOn = initial; }   // safe: no listeners attached yet
            var listener = new Action<bool>(_ => onChanged(toggle.isOn));
            _keepAlive.Add(listener);
            toggle.onValueChanged.AddListener(listener);
            return toggle;
        }
    }
}
