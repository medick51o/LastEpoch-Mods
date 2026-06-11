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
    // Fragility, accepted and guarded (see SPEC.md): template names and the
    // GetChild(0).GetChild(0) root path are game-version-dependent. Every
    // entry point is wrapped; on failure we log one warning and the mod
    // degrades to cfg-file configuration while fog control keeps working.
    //
    // All creators are idempotent: a row that already exists in the target
    // hierarchy is rebound, not duplicated.
    internal static class NativeSettings
    {
        const string CategoryPrefix = "ModsCategory - ";

        static Transform Root(SettingsPanelTabNavigable settings) =>
            settings.transform.GetChild(0).GetChild(0);

        // Creates (or finds) a named category header. Returns the sibling
        // index AFTER the last item already in this category, so new items
        // append to the bottom of the group.
        static int CreateCategoryIfNeeded(SettingsPanelTabNavigable settings, string category)
        {
            Transform root = Root(settings);
            string catName = CategoryPrefix + category;
            Transform existing = root.Find(catName);

            if (!existing)
            {
                Transform headerInterface = root.Find("Header - Interface");
                if (!headerInterface) return 0;

                Transform newCat = UnityEngine.Object.Instantiate(headerInterface, headerInterface.parent);
                newCat.name = catName;
                TMP_Text label = newCat.GetChild(0).GetChild(0).GetComponent<TMP_Text>();
                label.text  = category;
                label.color = Color.white;
                UnityEngine.Object.DestroyImmediate(
                    newCat.GetChild(0).GetChild(0).GetComponent<LocalizeStringEvent>());
                newCat.transform.SetSiblingIndex(headerInterface.GetSiblingIndex());
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

        // Clickable row (cloned toggle row with the checkbox repurposed as a
        // click target). Used for the level legend.
        public static void CreateButton(SettingsPanelTabNavigable settings,
            string category, string rowName, string title, string description, Action onClick)
        {
            try
            {
                Transform root = Root(settings);
                if (root.Find(rowName)) return;          // idempotent

                Transform template = root.Find("Toogle - Minion Health Bars");
                if (!template)
                {
                    MelonLogger.Warning($"settings template missing — '{title}' row skipped");
                    return;
                }

                int orderIndex = CreateCategoryIfNeeded(settings, category);
                Transform row = UnityEngine.Object.Instantiate(template, template.parent);
                row.name = rowName;
                row.SetSiblingIndex(orderIndex + 1);

                Toggle toggle = row.GetComponentInChildren<Toggle>();
                if (toggle != null)
                {
                    toggle.onValueChanged.RemoveAllListeners();
                    toggle.isOn = false;
                    toggle.onValueChanged.AddListener(new Action<bool>(_ =>
                    {
                        toggle.isOn = false;             // pure button: snap back
                        onClick?.Invoke();
                    }));
                }
                Button btn = row.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(new Action(() => onClick?.Invoke()));
                }

                foreach (var loc in row.GetComponentsInChildren<LocalizeStringEvent>())
                    UnityEngine.Object.DestroyImmediate(loc);

                TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>();
                if (texts.Length > 0)
                    texts[0].text = string.IsNullOrEmpty(description)
                        ? title
                        : $"{title}\n<size=62%><color=#AAAAAA>{description}</color></size>";
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"settings row '{title}' failed: {ex.Message}");
            }
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
                if (existing)                            // idempotent: rebind
                {
                    var dd = existing.GetChild(3).GetComponent<ColoredIconDropdown>();
                    if (dd != null)
                    {
                        dd.onValueChanged.RemoveAllListeners();
                        dd.value = (int)(object)option.Value;
                        dd.onValueChanged.AddListener(new Action<int>(_ => onChanged(dd.value)));
                    }
                    return dd;
                }

                Transform template = root.Find("Dropdown - Language Selection");
                if (!template)
                {
                    MelonLogger.Warning($"settings dropdown template missing — '{title}' skipped");
                    return null;
                }

                int orderIndex = CreateCategoryIfNeeded(settings, category);
                Transform row = UnityEngine.Object.Instantiate(template, template.parent);
                UnityEngine.Object.DestroyImmediate(row.GetComponent<LocalizationSettingsPanelUI>());
                UnityEngine.Object.DestroyImmediate(row.GetComponent<LootFilterSettingsPanelUI>());
                row.name = rowName;
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
                return dropdown;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"settings dropdown '{title}' failed: {ex.Message}");
                return null;
            }
        }
    }
}
