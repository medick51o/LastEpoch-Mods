using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(medick_A_Terrible_Button.NexusButtonMod),
    medick_A_Terrible_Button.BuildInfo.Name,
    medick_A_Terrible_Button.BuildInfo.Version,
    medick_A_Terrible_Button.BuildInfo.Author)]
[assembly: MelonGame("Eleventh Hour Games", "Last Epoch")]

namespace medick_A_Terrible_Button
{
    internal static class BuildInfo
    {
        public const string Name         = "medick_A_Terrible_Button";
        public const string OfficialName = "A Terrible Button: The Nexus Button";
        public const string Version      = "0.1.0";   // bump in lockstep with <Version> in the csproj
        public const string Author       = "medick";
    }

    // PROBE 1 (see SPEC.md): a plain button on the left-panel crest spot.
    // A click asks the game's own Nexus command to open the menu and logs
    // what happened; it never clicks a destination. Styling comes after the
    // probe tells us which path works and what the native button is made of.
    public class NexusButtonMod : MelonMod
    {
        // Spot from Andrew's mock-up: centred on the left-panel crest, which at
        // 1920x1080 is 360px in from the left edge and 32px down from the top.
        // Pinned to the TOP-LEFT corner and scaled by screen HEIGHT only, so it
        // stays on the crest at 16:9, 21:9 and 32:9 — assuming the game pins its
        // left panel the same way (probe logs the real frame to verify).
        // Measured 2026-10-08 (3440x1369, panel open): ACTIVITIES centre 489px = 386 units in.
        static readonly Vector2 CrestOffset = new Vector2(386f, -32f);

        // Design values — set from the design council synthesis (see SPEC.md).
        static readonly Vector2 NativeSize = new Vector2(124f, 36f);   // council: native 165x48 aspect
        static readonly string[] NativeChildrenToHide = { "Image_IconBackground", "Image_PromptIcon", "Nexus Questmark" };

        static GameObject _canvas;
        static Action _onClick;   // Il2Cpp listeners must stay referenced or clicks go dead

        public override void OnInitializeMelon()
        {
            Prefs.Init();
            MelonLogger.Msg($"{BuildInfo.OfficialName} v{BuildInfo.Version} ready");
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            bool gameplay = IsGameplay(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            try
            {
                if (gameplay && _canvas == null) Build();
                if (_canvas != null) _canvas.SetActive(gameplay);
                if (gameplay) TryUseNativeLook();
            }
            catch (Exception e) { MelonLogger.Warning($"button setup failed: {e.Message}"); }
        }

        static bool IsGameplay(string scene)
        {
            string l = (scene ?? "").ToLower();
            return l.Length > 0 && !l.Contains("loading") && !l.Contains("menu") && !l.Contains("boot")
                && !l.Contains("splash") && !l.Contains("character") && !l.Contains("login")
                && !l.Contains("persistentui");
        }

        static void Build()
        {
            _canvas = new GameObject("medick_NexusButtonCanvas");
            UnityEngine.Object.DontDestroyOnLoad(_canvas);
            var canvas = _canvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = _canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;   // height-only: ultrawide-safe
            _canvas.AddComponent<GraphicRaycaster>();

            var go = new GameObject("medick_NexusButton");
            go.transform.SetParent(_canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);   // top-left corner
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = CrestOffset;
            rt.sizeDelta = new Vector2(100f, 26f);   // mock-up size at 1920 wide
            var img = go.AddComponent<Image>();
            img.color = new Color(0.20f, 0.08f, 0.30f, 0.92f);
            BlockWorldClicks(go);
            var btn = go.AddComponent<Button>();
            _onClick = OnClick;
            btn.onClick.AddListener(_onClick);

            var label = new GameObject("Label");
            label.transform.SetParent(go.transform, false);
            var lrt = label.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text = "THE NEXUS";
            tmp.fontSize = 11f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            MelonLogger.Msg("probe: Nexus button placed");
        }

        // ── Native look (Andrew: "use the same exact assets") ─────────────
        // Clone the game's own THE NEXUS button (sprite ButtonLarge_Default_Purple,
        // font LastEpochAlt-SemiboldB, SpriteSwap states) the way Terrible
        // Inventory clones its buttons. The Monolith panel sits in the panel
        // pool in memory, so the donor usually exists away from the Monolith
        // too; until it is found the plain placeholder stays.
        static bool _native;
        static int _nativeFailures;
        static bool _nativeGaveUp;
        static float _nextNativeTry;

        // Optional N hotkey (off by default). Never fires while chat or any text field
        // has focus, so typing an "n" can't open the Nexus.
        public override void OnUpdate()
        {
            try
            {
                if (Prefs.HotkeyEnabled == null || !Prefs.HotkeyEnabled.Value) return;
                if (_canvas == null || !_canvas.activeSelf) return;
                if (!Input.GetKeyDown(KeyCode.N)) return;
                if (TypingSomewhere()) return;
                OnClick();
            }
            catch { }
        }

        static bool TypingSomewhere()
        {
            try { if (UIBase.instance != null && UIBase.instance.ChatOpen) return true; } catch { }
            try
            {
                var sel = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
                if (sel != null && (sel.GetComponent<TMP_InputField>() != null || sel.GetComponent<InputField>() != null)) return true;
            }
            catch { }
            return false;
        }

        public override void OnLateUpdate()
        {
            if (_canvas == null || !_canvas.activeSelf) return;
            if (!_native && !_nativeGaveUp && Time.unscaledTime >= _nextNativeTry)
            {
                _nextNativeTry = Time.unscaledTime + 2f;  // a scene scan every 2s at most, only until found
                TryUseNativeLook();
            }
            FollowCrest();
            ApplyShowButton();
        }

        static void ApplyShowButton()
        {
            try
            {
                bool show = Prefs.ShowButton == null || Prefs.ShowButton.Value;
                var b = CurrentButton();
                if (b != null && b.gameObject.activeSelf != show) b.gameObject.SetActive(show);
            }
            catch { }
        }

        // ── Placement (Andrew: centred over ACTIVITIES, flush on the crest) ──
        // While a left-side panel is open the button sits exactly on the panel's
        // own crest (EterranSidePanelTemplate/border-top-decoration, sprite
        // top_ornament_Eterra_Active) at the panel's own scale, so it follows
        // the game's UI Scale and any aspect. With no panel open it stays where
        // the crest last was (CrestOffset until a panel has been seen).
        static RectTransform _crest;
        static float _nextCrestSearch;
        static bool _haveLast;
        static Vector2 _lastFromTopLeft;   // crest centre in screen-height units from the top-left
        static float _lastScale = 1f;
        static Vector2 _lastSize;          // tab-matched box size, kept while no panel is open
        static float _lastFont;
        static Transform _tabPanel;        // panel the cached tab belongs to
        static RectTransform _tab;
        static TextMeshProUGUI _tabLabel;

        // Middle tab of the panel that owns this crest (crest path:
        // <panel>/Background/EterranSidePanelTemplate/border-top-decoration).
        // Cached per panel; the tab row lives under a child named "Tab UI Controller".
        static RectTransform MiddleTabOf(RectTransform crest)
        {
            var panel = crest.parent?.parent?.parent;
            if (panel == null) return null;
            if (panel == _tabPanel && _tab != null) return _tab;
            _tabPanel = panel; _tab = null; _tabLabel = null;
            var row = FindChildDeep(panel, "Tab UI Controller", 5);
            if (row == null) return null;
            // Only tabs actually on screen with a label. The Activities panel carries 5
            // tab objects, 2 of them hidden, so a plain middle pick landed on FACTIONS
            // (Andrew 21:03). ACTIVITIES wins by name whenever it exists.
            var tabs = new List<Transform>();
            Transform activities = null;
            for (int i = 0; i < row.childCount; i++)
            {
                var t = row.GetChild(i);
                if (!t.gameObject.activeInHierarchy) continue;
                var lbl = t.GetComponentInChildren<TextMeshProUGUI>(false);
                var trt = t.GetComponent<RectTransform>();
                if (lbl == null || string.IsNullOrWhiteSpace(lbl.text) || trt == null || trt.rect.width < 1f) continue;
                tabs.Add(t);
                if (lbl.text.Trim().ToUpper() == "ACTIVITIES") activities = t;
            }
            if (tabs.Count == 0) return null;
            var mid = activities ?? tabs[tabs.Count / 2];
            _tab = mid.GetComponent<RectTransform>();
            _tabLabel = mid.GetComponentInChildren<TextMeshProUGUI>(true);
            MelonLogger.Msg($"tab: matching '{PathOf(mid)}' ({tabs.Count} tabs, label '{_tabLabel?.text}' size {_tabLabel?.fontSize})");
            return _tab;
        }

        static Transform FindChildDeep(Transform root, string name, int depth)
        {
            if (root == null || depth < 0) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                var c = root.GetChild(i);
                if (c.name == name) return c;
                var hit = FindChildDeep(c, name, depth - 1);
                if (hit != null) return hit;
            }
            return null;
        }

        internal static void FollowCrest()
        {
            try
            {
                if (_canvas == null) return;
                var btn = CurrentButton();
                if (btn == null) return;

                if (Time.unscaledTime >= _nextCrestSearch)
                {
                    // Re-pick 4x/s so a panel opened ON TOP takes over (Andrew 20:42: Settings +
                    // Monolith stacked, button stayed on the Settings crest behind). Cheap: walks
                    // only the Left Panel Stack's children, never the whole scene.
                    _nextCrestSearch = Time.unscaledTime + 0.25f;
                    var picked = FindTopCrest();
                    if (picked != _crest && picked != null)
                        MelonLogger.Msg($"crest: following '{PathOf(picked)}'");
                    _crest = picked;
                }

                float h = Screen.height;
                var ourCanvas = _canvas.GetComponent<Canvas>();
                float sf = ourCanvas != null && ourCanvas.scaleFactor > 0f ? ourCanvas.scaleFactor : 1f;
                var ourLabel = MainLabel(btn);

                // Andrew 20:44: "the purple box should have the same box size of the activities
                // panel and should rest right above it and the nexus text should be relatively the
                // same size". So with a tabbed panel on top: copy the middle tab's box and label
                // size, and sit directly above that tab (ACTIVITIES on the Monolith panel).
                var tab = (_crest != null && _crest.gameObject.activeInHierarchy) ? MiddleTabOf(_crest) : null;
                if (tab != null && tab.gameObject.activeInHierarchy)
                {
                    var c = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>(4);
                    tab.GetWorldCorners(c);
                    float wPx = c[2].x - c[0].x, hPx = c[2].y - c[0].y;
                    btn.localScale = Vector3.one;
                    // Andrew 21:06: "shrink the purple box vertically just a bit" — it clipped
                    // the ACTIVITIES tab art. Height x0.78, top edge kept, so the bottom lifts.
                    const float HeightFactor = 0.74f;   // 21:08: 5% shorter again
                    float top = c[2].y + 9f * sf + hPx;   // 21:08: up a sliver (+3 units)
                    float bh = hPx * HeightFactor;
                    btn.sizeDelta = new Vector2(wPx / sf, bh / sf);
                    btn.position = new Vector3((c[0].x + c[2].x) / 2f, top - bh / 2f, 0f);
                    if (ourLabel != null && _tabLabel != null)
                    {
                        ourLabel.enableAutoSizing = false;
                        ourLabel.fontSize = _tabLabel.fontSize * (_tabLabel.transform.lossyScale.y / Mathf.Max(0.0001f, ourLabel.transform.lossyScale.y));
                        _lastFont = ourLabel.fontSize;
                    }
                    _lastFromTopLeft = new Vector2(btn.position.x / h, (h - btn.position.y) / h);
                    _lastSize = btn.sizeDelta;
                    _lastScale = 1f;
                    _haveLast = true;
                }
                else if (_crest != null && _crest.gameObject.activeInHierarchy)
                {
                    var c = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>(4);
                    _crest.GetWorldCorners(c);
                    var centre = new Vector3((c[0].x + c[2].x) / 2f, (c[0].y + c[2].y) / 2f, 0f);
                    var theirs = _crest.GetComponentInParent<Canvas>()?.rootCanvas;
                    var ours = _canvas.GetComponent<Canvas>();
                    float scale = (theirs != null && ours != null && ours.scaleFactor > 0f) ? theirs.scaleFactor / ours.scaleFactor : 1f;
                    btn.position = centre;
                    btn.localScale = Vector3.one * scale;
                    _lastFromTopLeft = new Vector2(centre.x / h, (h - centre.y) / h);
                    _lastScale = scale;
                    _haveLast = true;
                }
                else if (_haveLast)
                {
                    btn.position = new Vector3(_lastFromTopLeft.x * h, h - _lastFromTopLeft.y * h, 0f);
                    btn.localScale = Vector3.one * _lastScale;
                    if (_lastSize.x > 0f) btn.sizeDelta = _lastSize;
                    if (_lastFont > 0f && ourLabel != null) { ourLabel.enableAutoSizing = false; ourLabel.fontSize = _lastFont; }
                }
                else
                {
                    // Re-establish the baseline each time, including before any crest was seen.
                    btn.anchoredPosition = CrestOffset;
                    btn.localScale = Vector3.one;
                }

                // Keep the default spot fully on screen: the crest ornament's centre sits at
                // the very top edge, which put half the button off-screen (Andrew 20:36).
                // Applied to the baseline only, so the Y slider can still move it anywhere.
                {
                    float halfH = btn.rect.height * 0.5f * btn.lossyScale.y;
                    float margin = 2f * btn.lossyScale.y;
                    float maxCentreY = Screen.height - halfH - margin;
                    if (btn.position.y > maxCentreY)
                        btn.position = new Vector3(btn.position.x, maxCentreY, btn.position.z);
                }

                // Store only the unnudged crest above. Anchored units already include our
                // canvas scale; multiply by the button scale to match the game's UI scale.
                btn.anchoredPosition += new Vector2(Prefs.OffsetX.Value * btn.localScale.x,
                    Prefs.OffsetY.Value * btn.localScale.y);
            }
            catch { }
        }

        static RectTransform CurrentButton()
        {
            var t = _canvas.transform.Find(_native ? "medick_NexusButton_Native" : "medick_NexusButton");
            return t != null ? t.GetComponent<RectTransform>() : null;
        }

        static Transform _leftStack;

        // The top-most open left-side panel's crest: Left Panel Stack children are
        // walked from the highest sibling (drawn on top) down.
        static RectTransform FindTopCrest()
        {
            if (_leftStack == null)
            {
                var gui = GameObject.Find("GUI");
                _leftStack = gui != null ? gui.transform.Find("Panel System/Panel Stacks/Left Panel Stack") : null;
                if (_leftStack == null) return null;
            }
            for (int i = _leftStack.childCount - 1; i >= 0; i--)
            {
                var panel = _leftStack.GetChild(i);
                if (panel == null || !panel.gameObject.activeInHierarchy) continue;
                var crest = panel.Find("Background/EterranSidePanelTemplate/border-top-decoration");
                if (crest != null && crest.gameObject.activeInHierarchy)
                    return crest.GetComponent<RectTransform>();
            }
            return null;
        }

        static void TryUseNativeLook()
        {
            if (_native || _canvas == null) return;
            GameObject go = null;
            try
            {
                Button donor = null;
                foreach (var b in UnityEngine.Object.FindObjectsOfType<Button>(true))
                    if (b != null && b.gameObject.name == "The Nexus" && PathOf(b.transform).Contains("Monolith Timeline Panel"))
                    { donor = b; break; }
                if (donor == null) return;

                var placeholder = _canvas.transform.Find("medick_NexusButton");
                go = UnityEngine.Object.Instantiate(donor.gameObject, _canvas.transform, false);
                go.name = "medick_NexusButton_Native";
                go.SetActive(true);

                // Kill localization bindings or the game rewrites our label.
                foreach (var c in go.GetComponentsInChildren<Component>(true))
                {
                    if (c == null) continue;
                    string type = c.GetIl2CppType().FullName ?? "";
                    if (type.Contains("Localization")) UnityEngine.Object.DestroyImmediate(c);
                }
                // The donor sits in a layout group; its auto-sizers forced 160 wide (log 20:23).
                foreach (var c in go.GetComponents<Component>())
                {
                    if (c == null) continue;
                    string type = c.GetIl2CppType().Name ?? "";
                    if (type == "ContentSizeFitter" || type == "LayoutElement" || type.EndsWith("LayoutGroup"))
                        UnityEngine.Object.DestroyImmediate(c);
                }
                go.transform.localScale = Vector3.one;
                // Andrew 21:25: on controller the copy showed "LB" — the donor's prompt
                // script re-enables its prompt icon. Destroy the extra children outright and
                // strip every game script, keeping only plain Unity UI (Button/Image/TMP...).
                foreach (var child in NativeChildrenToHide)
                {
                    var t = go.transform.Find(child);
                    if (t != null) UnityEngine.Object.DestroyImmediate(t.gameObject);
                }
                foreach (var c in go.GetComponentsInChildren<Component>(true))
                {
                    if (c == null) continue;
                    string full = c.GetIl2CppType().FullName ?? "";
                    if (full.StartsWith("UnityEngine.") || full.StartsWith("TMPro.")) continue;
                    try { UnityEngine.Object.DestroyImmediate(c); } catch { }
                }
                foreach (var t in go.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    string txt = (t.text ?? "").Trim();
                    if (txt.Length > 0 && txt.Length <= 3 && t.gameObject != go) t.gameObject.SetActive(false);   // any stray prompt glyph text
                }

                // The donor's label leaves room for its icon slot on the left; with the
                // icon hidden the text sat off-centre (Andrew 20:36). Stretch + centre it.
                var label = MainLabel(go.transform);
                if (label != null)
                {
                    var lrt = label.rectTransform;
                    lrt.anchorMin = Vector2.zero;
                    lrt.anchorMax = Vector2.one;
                    lrt.offsetMin = new Vector2(6f, 0f);
                    lrt.offsetMax = new Vector2(-6f, 0f);
                    label.alignment = TextAlignmentOptions.Center;
                    label.enableAutoSizing = true;
                    label.fontSizeMin = 10f;
                    label.fontSizeMax = 16f;
                }

                BlockWorldClicks(go);
                var btn = go.GetComponent<Button>();
                btn.onClick = new Button.ButtonClickedEvent();   // drop the donor's own wiring
                btn.onClick.AddListener(_onClick);

                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = NativeSize;
                rt.anchoredPosition = CrestOffset;

                if (placeholder != null) placeholder.gameObject.SetActive(false);
                _native = true;
                MelonLogger.Msg($"native look: cloned '{PathOf(donor.transform)}' at {NativeSize.x}x{NativeSize.y}");
            }
            catch (Exception e)
            {
                // Grok review: latching _native=true here left CurrentButton() pointing at a
                // clone that never finished, so placement stopped. Undo the half-built clone,
                // keep the plain button working, and retry slowly (give up after 3 tries).
                if (go != null) { try { UnityEngine.Object.Destroy(go); } catch { } }
                var ph = _canvas.transform.Find("medick_NexusButton");
                if (ph != null) ph.gameObject.SetActive(true);
                _nativeFailures++;
                _nextNativeTry = Time.unscaledTime + 10f;
                MelonLogger.Warning($"native look failed ({_nativeFailures}/3), keeping the plain button: {e.Message}");
                if (_nativeFailures >= 3) _nativeGaveUp = true;
            }
        }

        // Andrew 21:12: clicking the button also walked the character toward it. The game
        // only treats the mouse as "over UI" when a UIMouseListener is under it (its own
        // tabs carry one), so give our button the game's own blocker.
        static void BlockWorldClicks(GameObject go)
        {
            // Plain UIMouseListener only: it flips underMouse on pointer enter/exit over
            // THIS rect. (UIMouseBlockerAdapter blocked the whole screen — 21:14.)
            try
            {
                if (go.GetComponent<UIMouseListener>() == null) go.AddComponent<UIMouseListener>();
            }
            catch (Exception e) { MelonLogger.Warning($"click blocker unavailable: {e.Message}"); }
        }

        // The button's real label (the longest active text), never a hidden prompt glyph.
        static TextMeshProUGUI MainLabel(Transform root)
        {
            TextMeshProUGUI best = null;
            foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t != null && t.gameObject.activeSelf && (best == null || (t.text ?? "").Length > (best.text ?? "").Length)) best = t;
            return best;
        }

        static void OnClick()
        {
            bool diag = Prefs.DebugLog != null && Prefs.DebugLog.Value;
            if (diag) { MelonLogger.Msg("probe: ---- Nexus button clicked ----"); LogManagers("before"); }
            try
            {
                var result = Il2CppLE.Dev.Console.Commands.PanelSystemCommands.Nexus();
                if (diag) MelonLogger.Msg($"probe: PanelSystemCommands.Nexus() returned: {DescribeResult(result)}");
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"probe: PanelSystemCommands.Nexus() threw {e.GetType().Name}: {e.Message}");
            }
            if (diag) MelonCoroutines.Start(AfterClick());   // scene-wide diagnostics only when DebugLog is on
        }

        static System.Collections.IEnumerator AfterClick()
        {
            // Give the panel a few frames to build before reading the scene.
            for (int i = 0; i < 10; i++) yield return null;
            LogManagers("after");
            DumpNexusButtonRecipe();
            LogLayout();
            LogWhatIsUnderTheButton();
        }

        static string DescribeResult(object result)
        {
            if (result == null) return "null";
            try { return result.ToString(); } catch (Exception e) { return $"<ToString failed: {e.Message}>"; }
        }

        static void LogManagers(string when)
        {
            try
            {
                var managers = UnityEngine.Object.FindObjectsOfType<MonolithTimelinePanelManager>(true);
                int active = 0;
                foreach (var m in managers) if (m != null && m.gameObject.activeInHierarchy) active++;
                MelonLogger.Msg($"probe: [{when}] MonolithTimelinePanelManager instances={managers.Length} active={active} " +
                                $"scene='{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}'");
            }
            catch (Exception e) { MelonLogger.Msg($"probe: [{when}] manager scan failed: {e.Message}"); }
        }

        // Writes down everything needed to rebuild the native THE NEXUS button
        // later (sprite, font, colours, glow), wherever it exists right now.
        static void DumpNexusButtonRecipe()
        {
            try
            {
                int found = 0;
                foreach (var t in UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>(true))
                {
                    string txt = t != null ? (t.text ?? "") : "";
                    if (!txt.ToLower().Contains("nexus")) continue;
                    found++;
                    MelonLogger.Msg($"probe: text '{txt}' at {PathOf(t.transform)} active={t.gameObject.activeInHierarchy} " +
                                    $"font={t.font?.name} size={t.fontSize} color={t.color} style={t.fontStyle}");
                    var b = t.GetComponentInParent<Button>(true);
                    if (b != null)
                    {
                        MelonLogger.Msg($"probe:   button at {PathOf(b.transform)} transition={b.transition}");
                        foreach (var im in b.GetComponentsInChildren<Image>(true))
                            MelonLogger.Msg($"probe:     image '{im.gameObject.name}' sprite={im.sprite?.name} type={im.type} " +
                                            $"color={im.color} mat={im.material?.name} size={im.rectTransform.rect.size}");
                    }
                }
                MelonLogger.Msg($"probe: Nexus texts found: {found}");
            }
            catch (Exception e) { MelonLogger.Msg($"probe: recipe dump failed: {e.Message}"); }
        }

        // Ultrawide check: where our button sits vs. the screen, and where any
        // open left-side panel frame sits, in screen pixels.
        static void LogLayout()
        {
            try
            {
                MelonLogger.Msg($"probe: screen {Screen.width}x{Screen.height} (aspect {(float)Screen.width / Screen.height:0.000})");
                var ours = _canvas?.transform.Find("medick_NexusButton")?.GetComponent<RectTransform>();
                if (ours != null) MelonLogger.Msg($"probe: our button centre (screen px) = {ours.position}");
                foreach (var p in UnityEngine.Object.FindObjectsOfType<Il2CppLE.UI.PanelSystem.MonolithPanel>(true))
                {
                    var prt = p.GetComponent<RectTransform>();
                    if (prt == null) continue;
                    var c = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>(4);
                    prt.GetWorldCorners(c);
                    MelonLogger.Msg($"probe: MonolithPanel '{p.gameObject.name}' active={p.gameObject.activeInHierarchy} " +
                                    $"corners BL={c[0]} TR={c[2]}");
                }
            }
            catch (Exception e) { MelonLogger.Msg($"probe: layout log failed: {e.Message}"); }
        }

        // Placement instrument (Andrew: "right above in the middle of activities
        // covering that crest ... flush ... part of the whole UI"): list every
        // game UI graphic under our button's centre, so the button can attach
        // to the real crest object instead of screen maths.
        static void LogWhatIsUnderTheButton()
        {
            try
            {
                Transform ours = _canvas.transform.Find(_native ? "medick_NexusButton_Native" : "medick_NexusButton");
                if (ours == null) return;
                Vector2 centre = ours.position;
                int n = 0;
                foreach (var g in UnityEngine.Object.FindObjectsOfType<Graphic>())
                {
                    if (g == null || !g.gameObject.activeInHierarchy || g.transform.IsChildOf(_canvas.transform)) continue;
                    var rt = g.rectTransform;
                    var canvas = g.canvas;
                    Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
                    if (!RectTransformUtility.RectangleContainsScreenPoint(rt, centre, cam)) continue;
                    if (++n > 25) break;
                    var img = g.TryCast<Image>();
                    MelonLogger.Msg($"probe: under button: {PathOf(g.transform)} sprite={img?.sprite?.name} size={rt.rect.size}");
                }
                MelonLogger.Msg($"probe: under button: {n} graphic(s) at {centre}");
                foreach (var t in UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>())
                {
                    if (t == null || !t.gameObject.activeInHierarchy) continue;
                    string txt = (t.text ?? "").Trim().ToUpper();
                    if (txt != "ACTIVITIES" && txt != "MONOLITH" && txt != "FACTIONS") continue;
                    var c = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>(4);
                    t.rectTransform.GetWorldCorners(c);
                    MelonLogger.Msg($"probe: tab '{txt}' at {PathOf(t.transform)} centre=({(c[0].x + c[2].x) / 2:0},{(c[0].y + c[2].y) / 2:0}) top={c[2].y:0}");
                }
            }
            catch (Exception e) { MelonLogger.Msg($"probe: under-button scan failed: {e.Message}"); }
        }

        static string PathOf(Transform t)
        {
            var parts = new List<string>();
            for (var cur = t; cur != null && parts.Count < 8; cur = cur.parent) parts.Insert(0, cur.name);
            return string.Join("/", parts);
        }
    }
}
