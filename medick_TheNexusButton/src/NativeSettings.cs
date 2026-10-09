using System;
using System.Collections.Generic;
using System.Globalization;
using Il2Cpp;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace medick_A_Terrible_Button;

// Adapted from Terrible Tooltips: native clones, fresh events, named last,
// idempotent rebinding, and session-long delegate pins for IL2CPP listeners.
internal static class NativeSettings
{
    const string CategoryPrefix = "ModsCategory - ";
    const string HeaderAnchor = "Header - Interface";
    static readonly string[] ToggleTemplates = { "Toogle - Minion Health Bars", "Toggle - Minion Health Bars" };
    static readonly List<Delegate> _keepAlive = new();
    static bool _degradedWarned;

    internal static void WarnDegradedOnce(string detail)
    {
        if (_degradedWarned) return;
        _degradedWarned = true;
        MelonLogger.Warning($"settings hierarchy changed ({detail}) — A Terrible Button settings UI unavailable; " +
            "configure via UserData/medick_A_Terrible_Button.cfg");
    }

    static Transform Root(SettingsPanelTabNavigable settings)
        => settings.transform.GetChild(0).GetChild(0);

    static Transform FindToggleTemplate(Transform root)
    {
        foreach (string name in ToggleTemplates)
        {
            var row = root.Find(name);
            if (row != null) return row;
        }
        return null;
    }

    static Transform FindSliderTemplate(Transform root)
    {
        Transform first = null;
        for (int i = 0; i < root.childCount; i++)
        {
            var row = root.GetChild(i);
            if (row.name.StartsWith("NB - ") || row.name.StartsWith(CategoryPrefix)) continue;
            if (row.GetComponentInChildren<Slider>(true) == null) continue;
            if (first == null) first = row;
            if (row.name.IndexOf("Scale", StringComparison.OrdinalIgnoreCase) >= 0) return row;
        }
        return first;
    }

    internal static bool TemplatesAvailable(SettingsPanelTabNavigable settings)
    {
        try
        {
            var root = Root(settings);
            var toggle = FindToggleTemplate(root);
            if (toggle == null || (toggle.GetComponentInChildren<Toggle>(true) == null &&
                toggle.GetComponent<Button>() == null))
                throw new InvalidOperationException("reset row template missing");
            if (FindSliderTemplate(root) == null) throw new InvalidOperationException("slider row template missing");
            if (root.Find(HeaderAnchor) == null) throw new InvalidOperationException("category header template missing");
            return true;
        }
        catch (Exception ex)
        {
            RemoveSection(settings, "A Terrible Button");
            WarnDegradedOnce(ex.Message);
            return false;
        }
    }

    static void StripLocalization(Transform row)
    {
        foreach (var loc in row.GetComponentsInChildren<LocalizeStringEvent>(true))
            UnityEngine.Object.DestroyImmediate(loc);
    }

    static int CreateCategoryIfNeeded(SettingsPanelTabNavigable settings, string category)
    {
        var root = Root(settings);
        string name = CategoryPrefix + category;
        var cat = root.Find(name);
        if (cat == null)
        {
            Transform clone = null;
            try
            {
                var template = root.Find(HeaderAnchor);
                clone = UnityEngine.Object.Instantiate(template, template.parent);
                StripLocalization(clone);
                var label = clone.GetComponentInChildren<TMP_Text>(true);
                if (label == null) throw new InvalidOperationException("category label missing");
                label.text = category;
                label.color = Color.white;
                clone.SetSiblingIndex(template.GetSiblingIndex());
                clone.name = name;
                cat = clone;
            }
            catch
            {
                if (clone != null) UnityEngine.Object.DestroyImmediate(clone.gameObject);
                throw;
            }
        }

        int insertAt = cat.GetSiblingIndex();
        for (int i = insertAt + 1; i < root.childCount; i++)
        {
            var sibling = root.GetChild(i);
            if (sibling.name.StartsWith(CategoryPrefix) || sibling.name.StartsWith("Header - ")) break;
            insertAt = i;
        }
        return insertAt;
    }

    static Transform BuildRowBase(SettingsPanelTabNavigable settings, string category,
        string rowName, Transform template)
    {
        var existing = Root(settings).Find(rowName);
        if (existing != null) return existing;
        if (template == null) throw new InvalidOperationException("row template missing");
        int index = CreateCategoryIfNeeded(settings, category);
        var row = UnityEngine.Object.Instantiate(template, template.parent);
        try
        {
            row.SetSiblingIndex(index + 1);
            StripLocalization(row);
            return row; // Widget setup must succeed before the caller gives it its final name.
        }
        catch
        {
            UnityEngine.Object.DestroyImmediate(row.gameObject);
            throw;
        }
    }

    internal sealed class SliderRow
    {
        readonly Slider _slider;
        readonly SliderInput _input;
        readonly List<TMP_Text> _valueTexts;

        internal SliderRow(Slider slider, SliderInput input, List<TMP_Text> valueTexts)
        {
            _slider = slider;
            _input = input;
            _valueTexts = valueTexts;
        }

        internal void SetValue(float value)
        {
            if (_slider == null) return;
            _slider.SetValueWithoutNotify(value);
            if (_input != null) _input.SetValueWithoutNotify(_slider.value);
            string text = _slider.value.ToString("0", CultureInfo.InvariantCulture);
            foreach (var label in _valueTexts)
                if (label != null) label.text = text;
        }
    }

    // Boolean toggle row, adapted from Terrible Tooltips' NativeSettings.CreateToggle.
    internal static Toggle CreateToggle(SettingsPanelTabNavigable settings,
        string category, string rowName, string title, string description,
        bool initial, Action<bool> onChanged)
    {
        try
        {
            var existing = Root(settings).Find(rowName);
            if (existing != null) return existing.GetComponentInChildren<Toggle>(true);
            Transform row = BuildRowBase(settings, category, rowName, FindToggleTemplate(Root(settings)));
            if (row == null) return null;
            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length > 0)
                texts[0].text = string.IsNullOrEmpty(description)
                    ? title
                    : $"{title}\n<size=62%><color=#AAAAAA>{description}</color></size>";
            Toggle toggle = row.GetComponentInChildren<Toggle>(true);
            if (toggle == null)
            {
                WarnDegradedOnce($"toggle widget missing in row '{row.name}'");
                try { UnityEngine.Object.DestroyImmediate(row.gameObject); } catch { }
                return null;
            }
            toggle.onValueChanged = new Toggle.ToggleEvent();
            try { toggle.SetIsOnWithoutNotify(initial); }
            catch { toggle.isOn = initial; }
            var listener = new Action<bool>(_ => onChanged(toggle.isOn));
            _keepAlive.Add(listener);
            toggle.onValueChanged.AddListener(listener);
            row.name = rowName;   // name LAST, after the widget is wired
            return toggle;
        }
        catch (Exception ex)
        {
            WarnDegradedOnce($"toggle '{title}' failed: {ex.Message}");
            return null;
        }
    }

    internal static SliderRow CreateSlider(SettingsPanelTabNavigable settings, string category,
        string rowName, string title, float min, float max, float initial, Action<float> onChanged)
    {
        Transform row = null;
        try
        {
            row = BuildRowBase(settings, category, rowName, FindSliderTemplate(Root(settings)));
            StripLocalization(row);
            var slider = row.GetComponentInChildren<Slider>(true);
            if (slider == null) throw new InvalidOperationException("slider widget missing");
            slider.onValueChanged = new Slider.SliderEvent();

            var valueTexts = new List<TMP_Text>();
            foreach (var updater in row.GetComponentsInChildren<UpdateLabelWithSliderValue>(true))
            {
                if (updater.textMesh != null) valueTexts.Add(updater.textMesh);
                UnityEngine.Object.DestroyImmediate(updater);
            }
            var input = row.GetComponentInChildren<SliderInput>(true);
            if (input != null)
            {
                // Keep the native number input, but detach all donor settings callbacks.
                input._Changed_k__BackingField = new SliderInput.FloatChangeEvent();
                input.ChangedAndPointerLeft = null;
                input.MinValue = min;
                input.MaxValue = max;
            }

            TMP_Text titleLabel = null;
            foreach (var text in row.GetComponentsInChildren<TMP_Text>(true))
            {
                bool isValue = valueTexts.Contains(text) || text.GetComponentInParent<TMP_InputField>(true) != null ||
                    text.name.IndexOf("Value", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    float.TryParse((text.text ?? "").Trim().TrimEnd('%'), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out _);
                if (isValue)
                {
                    if (!valueTexts.Contains(text)) valueTexts.Add(text);
                }
                else if (titleLabel == null) titleLabel = text;
                else text.text = ""; // Drop donor descriptions and units.
            }
            if (titleLabel == null) throw new InvalidOperationException("slider title label missing");
            titleLabel.text = title;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = true;
            var result = new SliderRow(slider, input, valueTexts);
            result.SetValue(initial);
            var listener = new Action<float>(_ =>
            {
                result.SetValue(slider.value);
                onChanged(slider.value);
            });
            _keepAlive.Add(listener);
            slider.onValueChanged.AddListener(listener);
            if (input != null)
            {
                // Numeric edits may notify SliderInput.Changed without firing the slider event.
                var inputListener = new Action<float>(_ =>
                {
                    result.SetValue(input.Value);
                    onChanged(slider.value);
                });
                _keepAlive.Add(inputListener);
                input.Changed.AddListener(inputListener);
            }
            row.name = rowName;
            return result;
        }
        catch
        {
            if (row != null) UnityEngine.Object.DestroyImmediate(row.gameObject);
            throw;
        }
    }

    internal static void CreateButton(SettingsPanelTabNavigable settings, string category,
        string rowName, string title, Action onClick)
    {
        Transform row = null;
        try
        {
            row = BuildRowBase(settings, category, rowName, FindToggleTemplate(Root(settings)));
            StripLocalization(row);
            var texts = row.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length == 0) throw new InvalidOperationException("reset title label missing");
            texts[0].text = title;
            for (int i = 1; i < texts.Length; i++) texts[i].text = "";
            var toggle = row.GetComponentInChildren<Toggle>(true);
            var button = row.GetComponent<Button>();
            if (button != null) button.onClick = new Button.ButtonClickedEvent();
            if (toggle != null)
            {
                toggle.onValueChanged = new Toggle.ToggleEvent();
                toggle.SetIsOnWithoutNotify(false);
                var listener = new Action<bool>(_ =>
                {
                    toggle.SetIsOnWithoutNotify(false);
                    onClick();
                });
                _keepAlive.Add(listener);
                toggle.onValueChanged.AddListener(listener);
            }
            if (button != null)
            {
                var listener = new Action(onClick);
                _keepAlive.Add(listener);
                button.onClick.AddListener(listener);
            }
            if (toggle == null && button == null) throw new InvalidOperationException("reset widget missing");
            row.name = rowName;
        }
        catch
        {
            if (row != null) UnityEngine.Object.DestroyImmediate(row.gameObject);
            throw;
        }
    }

    internal static void RemoveSection(SettingsPanelTabNavigable settings, string category)
    {
        try
        {
            var root = Root(settings);
            foreach (string name in new[] { "NB - Offset X", "NB - Offset Y", "NB - Reset Position", CategoryPrefix + category })
            {
                var row = root.Find(name);
                if (row != null) UnityEngine.Object.DestroyImmediate(row.gameObject);
            }
        }
        catch { } // The root itself may be unavailable; the button remains independent.
    }
}
