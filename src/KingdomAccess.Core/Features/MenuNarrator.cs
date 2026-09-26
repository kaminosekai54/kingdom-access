using System.Collections.Generic;
using System.Text;
using KingdomAccess.Game;
using KingdomAccess.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using SpeechOut = KingdomAccess.Speech.Speech;
using UIButton = UnityEngine.UI.Button;
using UISelectable = UnityEngine.UI.Selectable;
using UISlider = UnityEngine.UI.Slider;
using UIText = UnityEngine.UI.Text;
using UIToggle = UnityEngine.UI.Toggle;

namespace KingdomAccess.Features;

/// <summary>
/// Menu narration (Unity UI): reads the selected element (text, type, state,
/// position "2 of 5"), the text of a window that opens, and value changes.
/// If a menu is open with nothing selected, an arrow or Tab selects the first
/// element so the keyboard works.
/// </summary>
internal static class MenuNarrator
{
    private static GameObject _last;
    private static Transform _lastPanel;
    private static bool? _lastToggle;
    private static float _lastSlider = float.NaN;
    private static float _nextValueSpeak;

    public static void Tick(AccessSettings s, IModLog log, float now)
    {
        if (!s.MenuNarration) return;

        EventSystem es;
        try { es = EventSystem.current; } catch { return; }
        if (es == null) return;

        GameObject go = es.currentSelectedGameObject;
        if (go == null || !go.activeInHierarchy)
        {
            _last = null;
            TryAutoSelect(es);
            return;
        }

        if (go != _last)
        {
            _last = go;
            string text = Describe(go, out Transform panel);
            if (panel != _lastPanel)
            {
                _lastPanel = panel;
                string header = PanelText(panel, go);
                if (!string.IsNullOrEmpty(header)) text = header + ". " + text;
            }
            SpeechOut.Say(text, true);
            if (s.LogUI) log.Info($"[UI] {Path(go)} -> {text}");
            RememberValues(go);
            return;
        }

        // Same element: announce value changes (checkbox, slider).
        var toggle = go.GetComponent<UIToggle>();
        if (toggle != null && _lastToggle.HasValue && toggle.isOn != _lastToggle.Value)
        {
            _lastToggle = toggle.isOn;
            SpeechOut.Say(Loc.T(toggle.isOn ? "ui.checked" : "ui.unchecked"), true);
        }
        var slider = go.GetComponent<UISlider>();
        if (slider != null && !float.IsNaN(_lastSlider) && Mathf.Abs(slider.value - _lastSlider) > 0.0001f && now >= _nextValueSpeak)
        {
            _lastSlider = slider.value;
            _nextValueSpeak = now + 0.1f;
            SpeechOut.Say(SliderValue(slider), true);
        }
    }

    private static void RememberValues(GameObject go)
    {
        var toggle = go.GetComponent<UIToggle>();
        _lastToggle = toggle != null ? toggle.isOn : null;
        var slider = go.GetComponent<UISlider>();
        _lastSlider = slider != null ? slider.value : float.NaN;
    }

    /// <summary>Full text of an element: label, type, state, position.</summary>
    private static string Describe(GameObject go, out Transform panel)
    {
        var parts = new List<string>();
        string label = Label(go);
        string friendly = Loc.TryT("uiname." + Key(go.name));
        // Label reduced to numbers (e.g. "0, 30, 1"): the known name is more useful.
        if (friendly != null && NumbersOnly.IsMatch(label ?? "")) label = null;
        if (string.IsNullOrEmpty(label)) label = FriendlyName(go);
        if (!string.IsNullOrEmpty(label)) parts.Add(label);

        // Text next to a single button (e.g. "Difficulty: Normal" next to the map).
        string context = SoloGroupText(go);
        if (!string.IsNullOrEmpty(context)) parts.Add(context);

        string blazon = BlazonContext(go);
        if (blazon != null) parts.Add(blazon);

        var toggle = go.GetComponent<UIToggle>();
        var slider = go.GetComponent<UISlider>();
        if (toggle != null) parts.Add(Loc.T("ui.checkbox") + ", " + Loc.T(toggle.isOn ? "ui.checked" : "ui.unchecked"));
        else if (slider != null) parts.Add(Loc.T("ui.slider") + ", " + SliderValue(slider));

        var sel = go.GetComponent<UISelectable>();
        if (sel != null && !sel.IsInteractable()) parts.Add(Loc.T("ui.disabled"));

        panel = FindPanel(go.transform);
        string pos = Position(go);
        if (pos != null) parts.Add(pos);

        if (parts.Count == 0) parts.Add(ObjectNames.CleanName(go.name));
        return string.Join(", ", parts);
    }

    private static string SliderValue(UISlider s)
    {
        float range = s.maxValue - s.minValue;
        if (s.wholeNumbers || range > 20f) return Mathf.RoundToInt(s.value).ToString();
        return Loc.T("ui.percent", Mathf.RoundToInt(s.normalizedValue * 100f));
    }

    /// <summary>Every visible text of the element, without duplicates.</summary>
    private static string Label(GameObject go)
    {
        var seen = new HashSet<string>();
        var list = new List<string>();
        void Add(string t)
        {
            if (string.IsNullOrWhiteSpace(t)) return;
            t = Clean(t);
            if (t.Length == 0 || IsNoise(t)) return;
            // Texts the game left in English (e.g. "Randomise"): translated if known.
            t = Loc.TryT("uitext." + Key(t)) ?? t;
            if (!seen.Add(t)) return;
            list.Add(t);
        }

        foreach (var tmp in go.GetComponentsInChildren<TMP_Text>(false)) Add(tmp.text);
        foreach (var txt in go.GetComponentsInChildren<UIText>(false)) Add(txt.text);
        foreach (var tm in go.GetComponentsInChildren<TextMesh>(false)) Add(tm.text);
        if (list.Count > 5) list.RemoveRange(5, list.Count - 5);
        return string.Join(", ", list);
    }

    private static string Clean(string t)
    {
        var sb = new StringBuilder(t.Length);
        bool inTag = false;
        foreach (char c in t)
        {
            if (c == '<') { inTag = true; continue; }
            if (c == '>') { inTag = false; continue; }
            if (!inTag) sb.Append(c == '\n' || c == '\r' ? ' ' : c);
        }
        return sb.ToString().Trim();
    }

    /// <summary>Translation key from an object name: lowercase, letters and digits only.</summary>
    private static string Key(string name)
    {
        var sb = new StringBuilder();
        foreach (char c in (name ?? "").Replace("(Clone)", "").ToLowerInvariant())
            if (char.IsLetterOrDigit(c)) sb.Append(c);
        return sb.ToString();
    }

    /// <summary>
    /// Name of an element without text (icon, arrow): translation of the object name (uiname.*),
    /// preceded by that of its group (uigroup.*), for example "Background, next".
    /// </summary>
    private static string FriendlyName(GameObject go)
    {
        string own = Loc.TryT("uiname." + Key(go.name));
        var parent = go.transform.parent;
        string group = parent != null ? Loc.TryT("uigroup." + Key(parent.name)) : null;
        if (own != null && group != null) return group + ", " + own;
        if (own != null) return own;
        if (group != null) return group + ", " + ObjectNames.CleanName(go.name);
        return ObjectNames.CleanName(go.name);
    }

    /// <summary>Current blazon value for the editor buttons (background, emblem, colours, crown).</summary>
    private static string BlazonContext(GameObject go)
    {
        BlazonEditor editor = null;
        try { editor = go.GetComponentInParent<BlazonEditor>(); } catch { }
        if (editor == null) return null;
        var parent = go.transform.parent;
        string group = parent != null ? Key(parent.name) : "";
        string name = Key(go.name);
        try
        {
            if (group == "backgroundbuttons") return Patches.Blazon.SpriteName(editor._backgroundPreview);
            if (group == "emblembuttons") return Patches.Blazon.SpriteName(editor._emblemPreview);
            if (group == "crownselectbuttons") return Patches.Blazon.Crown(editor);
            if (name == "emblemcolourbutton") return Patches.Blazon.ColorOf(editor.emblemColour);
            if (name == "primarycolourbutton") return Patches.Blazon.ColorOf(editor.primaryColour);
            if (name == "secondarycolourbutton") return Patches.Blazon.ColorOf(editor.secondaryColour);
        }
        catch { }
        return null;
    }

    /// <summary>Reads the screen again (window text and selected element, or blazon summary).</summary>
    public static void ReadScreen()
    {
        try
        {
            var editor = Object.FindFirstObjectByType<BlazonEditor>();
            if (editor != null && editor.isActiveAndEnabled)
            {
                SpeechOut.Say(Patches.Blazon.Summary(editor), true);
                return;
            }
        }
        catch { }

        EventSystem es = null;
        try { es = EventSystem.current; } catch { }
        var go = es != null ? es.currentSelectedGameObject : null;
        if (go == null) { SpeechOut.Say(Loc.T("ui.nothing"), true); return; }
        string text = Describe(go, out Transform panel);
        string header = PanelText(panel, go);
        SpeechOut.Say(string.IsNullOrEmpty(header) ? text : header + ". " + text, true);
    }

    private static readonly System.Text.RegularExpressions.Regex Noise =
        new(@"\(r\d+\)|^\d+(\.\d+)+$|^[ivxlcdm]+\.?$|^kingdom two crowns \d", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>Decorative texts not to read (version number, roman numerals...).</summary>
    private static bool IsNoise(string t) => Noise.IsMatch(t);

    private static readonly System.Text.RegularExpressions.Regex NumbersOnly =
        new(@"^[\d\s\.,:;/-]*$", System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>Number of active selectable elements under a transform.</summary>
    private static int SelectableCount(Transform t)
    {
        try { return t.GetComponentsInChildren<UISelectable>(false).Length; }
        catch { return 0; }
    }

    /// <summary>
    /// If the parent of the element contains only it as a selectable element, its other
    /// texts describe it (e.g. "Difficulty: Normal" in the map frame).
    /// </summary>
    private static string SoloGroupText(GameObject go)
    {
        var parent = go.transform.parent;
        if (parent == null || SelectableCount(parent) != 1) return null;
        var list = new List<string>();
        var seen = new HashSet<string>();
        foreach (var tmp in parent.GetComponentsInChildren<TMP_Text>(false))
        {
            if (tmp.transform.IsChildOf(go.transform)) continue;
            string t = Clean(tmp.text);
            if (t.Length == 0 || IsNoise(t) || NumbersOnly.IsMatch(t) || !seen.Add(t)) continue;
            list.Add(t);
            if (list.Count >= 4) break;
        }
        foreach (var txt in parent.GetComponentsInChildren<UIText>(false))
        {
            if (list.Count >= 4 || txt.transform.IsChildOf(go.transform)) continue;
            string t = Clean(txt.text);
            if (t.Length == 0 || IsNoise(t) || NumbersOnly.IsMatch(t) || !seen.Add(t)) continue;
            list.Add(t);
        }
        return list.Count == 0 ? null : string.Join(" ", list);
    }

    /// <summary>
    /// True if this text is the label of a single button (it is read with that button,
    /// not as the window title).
    /// </summary>
    private static bool IsSoloButtonLabel(Transform text, Transform panel)
    {
        var cur = text.parent;
        while (cur != null && cur != panel)
        {
            if (SelectableCount(cur) == 1) return true;
            cur = cur.parent;
        }
        return false;
    }

    /// <summary>Position among sibling selectable elements: "2 of 5".</summary>
    private static string Position(GameObject go)
    {
        var parent = go.transform.parent;
        if (parent == null) return null;
        int index = -1, count = 0;
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i).gameObject;
            if (!child.activeInHierarchy) continue;
            var sel = child.GetComponent<UISelectable>();
            if (sel == null) continue;
            if (child == go) index = count;
            count++;
        }
        if (count < 2 || index < 0) return null;
        return Loc.T("scan.position", index + 1, count);
    }

    /// <summary>Panel (window, menu) containing the element: first parent with a CanvasGroup.</summary>
    private static Transform FindPanel(Transform t)
    {
        var cur = t.parent;
        while (cur != null)
        {
            if (cur.GetComponent<CanvasGroup>() != null) return cur;
            cur = cur.parent;
        }
        return t.root;
    }

    /// <summary>Panel texts outside buttons (title, window message).</summary>
    private static string PanelText(Transform panel, GameObject selected)
    {
        if (panel == null) return null;
        var list = new List<string>();
        var seen = new HashSet<string>();
        foreach (var tmp in panel.GetComponentsInChildren<TMP_Text>(false))
        {
            if (tmp.GetComponentInParent<UISelectable>() != null || IsSoloButtonLabel(tmp.transform, panel)) continue;
            string t = Clean(tmp.text);
            if (t.Length == 0 || IsNoise(t) || NumbersOnly.IsMatch(t) || !seen.Add(t)) continue;
            list.Add(t);
            if (list.Count >= 3) break;
        }
        foreach (var txt in panel.GetComponentsInChildren<UIText>(false))
        {
            if (list.Count >= 3) break;
            if (txt.GetComponentInParent<UISelectable>() != null || IsSoloButtonLabel(txt.transform, panel)) continue;
            string t = Clean(txt.text);
            if (t.Length == 0 || IsNoise(t) || NumbersOnly.IsMatch(t) || !seen.Add(t)) continue;
            list.Add(t);
        }
        string joined = string.Join(". ", list);
        return joined.Length > 300 ? joined.Substring(0, 300) : joined;
    }

    /// <summary>
    /// Menu open without selection (often after a click): an arrow or Tab selects
    /// the first usable element. Only when paused or out of gameplay, not to disturb the game.
    /// </summary>
    private static void TryAutoSelect(EventSystem es)
    {
        bool navKey = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow)
                      || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow)
                      || Input.GetKeyDown(KeyCode.Tab);
        if (!navKey) return;
        if (AccessMod.ExternalKeyCapture) return;
        if (GameState.Player != null && Time.timeScale > 0f) return;

        try
        {
            foreach (var sel in UISelectable.allSelectablesArray)
            {
                if (sel == null || !sel.gameObject.activeInHierarchy || !sel.IsInteractable()) continue;
                es.SetSelectedGameObject(sel.gameObject);
                return;
            }
        }
        catch { }
    }

    private static string Path(GameObject go)
    {
        var names = new List<string>();
        var t = go.transform;
        while (t != null && names.Count < 6) { names.Insert(0, t.name); t = t.parent; }
        return string.Join("/", names);
    }
}
