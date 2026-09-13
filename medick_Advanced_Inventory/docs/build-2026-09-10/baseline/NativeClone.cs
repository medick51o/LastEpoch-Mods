using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace medick_Terrible_Inventory
{
    // The EHG-proud factory: every visual element this mod shows is a CLONE
    // of the game's own Sort button — native 9-slice sprite, native font and
    // material, native hover/press transitions. Nothing hand-painted.
    // (v1 drew flat-color rectangles with a scavenged font; see SPEC.md.)
    internal static class NativeClone
    {
        // Il2Cpp delegates passed to AddListener must stay referenced from
        // managed code or the GC collects them and clicks go dead.
        static readonly List<Delegate> _keepAlive = new();

        public static GameObject Button(GameObject template, Transform parent, string name, Action onClick)
        {
            GameObject go = UnityEngine.Object.Instantiate(template, parent);
            try
            {
                // Strip the donor's behaviour so it can't fight our wiring.
                var sort = go.GetComponent<SortInventoryButton>();
                if (sort != null) UnityEngine.Object.DestroyImmediate(sort);

                // Kill localization bindings or the game rewrites our labels.
                foreach (var loc in go.GetComponentsInChildren<UnityEngine.Localization.Components.LocalizeStringEvent>(true))
                    UnityEngine.Object.DestroyImmediate(loc);

                var btn = go.GetComponent<Button>();
                if (btn != null)
                {
                    // Fresh event object: clears runtime AND serialized
                    // persistent listeners (RemoveAllListeners only clears
                    // runtime ones — a game update wiring the donor via
                    // persistent calls would survive onto our clones).
                    btn.onClick = new Button.ButtonClickedEvent();
                    var action = new Action(onClick);
                    _keepAlive.Add(action);
                    btn.onClick.AddListener(action);
                }

                // Native-look diagnostics for the in-game validation pass:
                // the 9-slice assumption lives or dies on this sprite.
                if (Prefs.DebugLog != null && Prefs.DebugLog.Value)
                {
                    try
                    {
                        var img = go.transform.childCount > 0
                            ? go.transform.GetChild(0).GetComponent<Image>()
                            : go.GetComponent<Image>();
                        if (img != null)
                            Dbg.Log($"clone '{name}': sprite={(img.sprite != null ? img.sprite.name : "none")} " +
                                    $"type={img.type} border={(img.sprite != null ? img.sprite.border.ToString() : "?")}");
                    }
                    catch { }
                }

                go.name = name;   // name LAST — a half-built clone is never findable as ours
                return go;
            }
            catch
            {
                // Destroyed in catch — never leak a live donor-named clone
                // into the footer/column (it would duplicate every rebuild).
                try { if (go) UnityEngine.Object.DestroyImmediate(go); } catch { }
                throw;            // callers own the single-log degradation
            }
        }

        // The Sort button's child(1) is its dots glyph — it overlaps custom
        // label text ("the S and the symbol with the dots are overlapping").
        public static void HideIcon(GameObject btn)
        {
            try
            {
                if (btn.transform.childCount > 1)
                    btn.transform.GetChild(1).gameObject.SetActive(false);
            }
            catch { }
        }

        public static TMP_Text Label(GameObject btn)
        {
            try { return btn.GetComponentInChildren<TMP_Text>(true); }
            catch { return null; }
        }

        // Single-line auto-sizing label (footer buttons). Pinned single-line:
        // the donor's wrap setting must not break "STASH ALL" onto two lines.
        public static void SetLabel(GameObject btn, string text, float minSize, float maxSize)
        {
            var tmp = Label(btn);
            if (tmp == null) return;
            tmp.text = text;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = minSize;
            tmp.fontSizeMax = maxSize;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Truncate;
        }

        // Rich-text label stretched over the whole button (teleport column).
        // baseSize > 0 pins a deterministic font size (the donor's auto-size
        // leaves whatever it last computed for its own string); autoMin/Max
        // > 0 enables shrink-to-fit for long destination names.
        public static void SetRichLabel(GameObject btn, string richText,
            float baseSize = 0f, float autoMin = 0f, float autoMax = 0f)
        {
            var tmp = Label(btn);
            if (tmp == null) return;
            tmp.text = richText;
            if (baseSize > 0f) tmp.fontSize = baseSize;
            if (autoMin > 0f && autoMax > 0f)
            {
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = autoMin;
                tmp.fontSizeMax = autoMax;
            }
            else
            {
                tmp.enableAutoSizing = false;
            }
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Truncate;
            try
            {
                var rt = tmp.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(6f, 2f);
                rt.offsetMax = new Vector2(-4f, -2f);
            }
            catch { }
        }

        // Slim faction-identity accent bar on the button's left edge —
        // colour coding without painting over the native art.
        public static void AddAccentBar(GameObject btn, Color color)
        {
            var bar = new GameObject("medick_Accent");
            bar.transform.SetParent(btn.transform, false);
            var rt = bar.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.offsetMin = new Vector2(3f, 4f);
            rt.offsetMax = new Vector2(7f, -4f);   // 4px wide strip, inset from the frame
            var img = bar.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        // Sized for free-form parents (master tab) — no layout group there.
        public static void SetRect(GameObject go, Vector2 anchoredPos, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;
        }

        // Sized for VerticalLayoutGroup parents (the teleport column).
        public static void SetLayoutSize(GameObject go, float width, float height)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.preferredWidth  = width;
            le.preferredHeight = height;
            le.flexibleWidth   = 0f;
            le.flexibleHeight  = 0f;
        }
    }
}
