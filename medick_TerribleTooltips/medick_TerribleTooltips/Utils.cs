// ================================================================
//  Utils.cs  —  medick_Terrible_Tooltips
//
//  Helper methods for injecting settings into the game's UI panel.
//  Ported from KG / war3i4i's Utils.cs (credit: KG / war3i4i).
// ================================================================

using System.Reflection;
using Il2Cpp;
using Il2CppLE.UI.Controls;
using Il2CppTMPro;
using UnityEngine.Localization.Components;
using AccessTools = HarmonyLib.AccessTools;

namespace medick_Terrible_Tooltips;

public static class Utils
{
    // ── IL2CPP List indexer workaround ────────────────────────────────
    // IL2CPP wraps System.Collections.Generic.List<T> and the normal []
    // indexer isn't reliable. Use reflection to call get_Item instead.
    private static readonly Dictionary<Type, MethodInfo> _cachedMethods = new();

    public static T get<T>(this Il2CppSystem.Collections.Generic.List<T> list, int index)
    {
        Type type = typeof(T);
        if (_cachedMethods.TryGetValue(type, out MethodInfo method))
            return (T)method.Invoke(list, new object[] { index });
        method = AccessTools.Method(
            typeof(Il2CppSystem.Collections.Generic.List<T>),
            "get_Item", new[] { typeof(int) });
        _cachedMethods[type] = method;
        return (T)method.Invoke(list, new object[] { index });
    }

    // ── Settings panel helpers ────────────────────────────────────────
    // Creates (or finds) a named category header in the settings panel.
    // Returns the sibling index AFTER the last item already in this category,
    // so new items always append to the bottom of the group.
    private static int CreateCategoryIfNeeded(SettingsPanelTabNavigable settings,
                                              string category)
    {
        Transform root = settings.transform.GetChild(0).GetChild(0);
        string catName = $"ModsCategory - {category}";
        Transform existing = root.Find(catName);

        if (!existing)
        {
            Transform headerInterface = root.Find("Header - Interface");
            if (!headerInterface) return 0;

            Transform newCat = UnityEngine.Object.Instantiate(
                headerInterface, headerInterface.parent);
            newCat.name = catName;
            TMP_Text label = newCat.GetChild(0).GetChild(0).GetComponent<TMP_Text>();
            label.text  = category;
            label.color = Color.white;
            UnityEngine.Object.DestroyImmediate(
                newCat.GetChild(0).GetChild(0).GetComponent<LocalizeStringEvent>());
            newCat.transform.SetSiblingIndex(headerInterface.GetSiblingIndex());
        }

        // Walk forward from the category header, counting how many mod-added
        // children already exist so new items append at the end of this group.
        Transform cat = root.Find(catName);
        int headerIdx = cat.GetSiblingIndex();
        int insertAt  = headerIdx;

        for (int i = headerIdx + 1; i < root.childCount; i++)
        {
            Transform sib = root.GetChild(i);
            // Stop when we hit another category header or a non-mod element
            if (sib.name.StartsWith("ModsCategory - ") && sib.name != catName) break;
            if (sib.name.StartsWith("Header - ")) break;
            insertAt = i;
        }

        return insertAt;
    }

    // Adds a boolean toggle row to the settings panel.
    // description is shown as a subtitle/hint on the right side of the row.
    public static void CreateNewOption_Toggle(
        this SettingsPanelTabNavigable settings,
        string category, string name,
        MelonPreferences_Entry<bool> option,
        Action<bool> callback,
        string description = "")
    {
        try
        {
            Transform root = settings.transform.GetChild(0).GetChild(0);
            Transform template = root.Find("Toogle - Minion Health Bars");
            if (!template) return;

            int orderIndex = CreateCategoryIfNeeded(settings, category);
            Transform newBtn = UnityEngine.Object.Instantiate(template, template.parent);
            newBtn.name = name;
            newBtn.SetSiblingIndex(orderIndex + 1);

            Toggle toggle = newBtn.GetComponentInChildren<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveAllListeners();
                toggle.isOn = option.Value;
                toggle.onValueChanged.AddListener(
                    new Action<bool>(_ => callback(toggle.isOn)));
            }

            foreach (var loc in newBtn.GetComponentsInChildren<LocalizeStringEvent>())
                UnityEngine.Object.DestroyImmediate(loc);

            TMP_Text[] texts = newBtn.GetComponentsInChildren<TMP_Text>();
            if (texts.Length > 0)
                texts[0].text = string.IsNullOrEmpty(description)
                    ? name
                    : $"{name}\n<size=62%><color=#AAAAAA>{description}</color></size>";
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[Terrible Tooltips] Toggle '{name}' failed: {ex.Message}");
        }
    }

    // Adds a clickable button row to the settings panel.
    // callback fires when the player clicks it.
    public static void CreateNewOption_Button(
        this SettingsPanelTabNavigable settings,
        string category, string name, string description,
        Action callback)
    {
        try
        {
            Transform root = settings.transform.GetChild(0).GetChild(0);
            Transform template = root.Find("Toogle - Minion Health Bars");
            if (!template) return;

            int orderIndex = CreateCategoryIfNeeded(settings, category);
            Transform newBtn = UnityEngine.Object.Instantiate(template, template.parent);
            newBtn.name = name;
            newBtn.SetSiblingIndex(orderIndex + 1);

            // Disable the checkbox visual — this row acts as a pure button
            Toggle toggle = newBtn.GetComponentInChildren<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveAllListeners();
                toggle.isOn = false;
                // Re-wire: any value change → fire callback and reset visual
                toggle.onValueChanged.AddListener(new Action<bool>(_ =>
                {
                    toggle.isOn = false;
                    callback?.Invoke();
                }));
            }

            // If the row has a root Button component, wire that too
            Button btn = newBtn.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(new Action(() => callback?.Invoke()));
            }

            foreach (var loc in newBtn.GetComponentsInChildren<LocalizeStringEvent>())
                UnityEngine.Object.DestroyImmediate(loc);

            TMP_Text[] texts = newBtn.GetComponentsInChildren<TMP_Text>();
            if (texts.Length > 0)
                texts[0].text = string.IsNullOrEmpty(description)
                    ? name
                    : $"{name}\n<size=62%><color=#AAAAAA>{description}</color></size>";
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[Terrible Tooltips] Button '{name}' failed: {ex.Message}");
        }
    }

    // Stores a dropdown template GameObject so it can be reused
    public static GameObject CopyFrom_Dropdown;

    // Adds an enum dropdown row to the settings panel.
    public static void CreateNewOption_EnumDropdown<T>(
        this SettingsPanelTabNavigable settings,
        string category, string name, string description,
        MelonPreferences_Entry<T> option,
        Action<int> callback) where T : Enum
    {
        try
        {
            Transform root = settings.transform.GetChild(0).GetChild(0);
            Transform template = root.Find("Dropdown - Language Selection");
            if (!template) return;

            int orderIndex = CreateCategoryIfNeeded(settings, category);
            Transform newDrop = UnityEngine.Object.Instantiate(template, template.parent);

            UnityEngine.Object.DestroyImmediate(
                newDrop.GetComponent<LocalizationSettingsPanelUI>());
            UnityEngine.Object.DestroyImmediate(
                newDrop.GetComponent<LootFilterSettingsPanelUI>());

            newDrop.name = name;
            newDrop.SetSiblingIndex(orderIndex + 1);

            TMP_Text nameLabel = newDrop.GetChild(0).GetComponent<TMP_Text>();
            nameLabel.text = name;
            UnityEngine.Object.DestroyImmediate(
                newDrop.GetChild(0).GetComponent<LocalizeStringEvent>());

            TMP_Text descLabel = newDrop.GetChild(1).GetComponent<TMP_Text>();
            descLabel.text = description;
            UnityEngine.Object.DestroyImmediate(
                newDrop.GetChild(1).GetComponent<LocalizeStringEvent>());

            if (!CopyFrom_Dropdown)
            {
                GameObject disabled = new("disabled.copydropdown")
                    { hideFlags = HideFlags.HideAndDontSave };
                disabled.SetActive(false);
                CopyFrom_Dropdown = UnityEngine.Object.Instantiate(
                    newDrop.gameObject, disabled.transform);
            }

            ColoredIconDropdown dropdown =
                newDrop.GetChild(3).GetComponent<ColoredIconDropdown>();
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.ClearOptions();

            Il2CppSystem.Collections.Generic.List<string> opts = new();
            foreach (string enumName in Enum.GetNames(typeof(T)))
                opts.Add(enumName.Replace("_", " "));
            dropdown.AddOptions(opts);
            dropdown.value = (int)(object)option.Value;
            dropdown.onValueChanged.AddListener(
                new Action<int>(_ => callback(dropdown.value)));
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[Terrible Tooltips] Dropdown '{name}' failed: {ex.Message}");
        }
    }
}
