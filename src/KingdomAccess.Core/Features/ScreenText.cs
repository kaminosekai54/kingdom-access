using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using KingdomAccess.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using SpeechOut = KingdomAccess.Speech.Speech;
using UIText = UnityEngine.UI.Text;

namespace KingdomAccess.Features;

/// <summary>
/// Texts that appear on screen outside of menus: ghost hints (tutorial), information
/// bubbles, notifications, arrival messages. Each new visible text is read once.
/// The capture shortcut puts every visible text in a browsable list.
/// </summary>
internal static class ScreenText
{
    private const float ScanInterval = 0.5f;
    private const float RepeatAfter = 60f;

    private static readonly Regex HasLetter = new(@"\p{L}", RegexOptions.Compiled);
    private static readonly Dictionary<string, float> LastSpoken = new();
    private static HashSet<string> _visibleBefore = new();
    private static float _nextScan;

    private struct Found
    {
        public string Text;
        public float Y, X;
        public bool IsUi;
    }

    public static void Tick(AccessSettings s, float now)
    {
        if (!s.AnnounceScreenText || now < _nextScan) return;
        _nextScan = now + ScanInterval;

        // UI texts are left to the menu narrator when an element has the focus.
        bool menuFocused = false;
        try { var es = EventSystem.current; menuFocused = es != null && es.currentSelectedGameObject != null; } catch { }

        var visibleNow = new HashSet<string>();
        foreach (var f in Collect(!menuFocused))
        {
            visibleNow.Add(f.Text);
            if (_visibleBefore.Contains(f.Text)) continue;
            if (LastSpoken.TryGetValue(f.Text, out float t) && now - t < RepeatAfter) continue;
            LastSpoken[f.Text] = now;
            SpeechOut.Say(f.Text, false);
        }
        _visibleBefore = visibleNow;
    }

    /// <summary>Every visible text, top to bottom, in a browsable list.</summary>
    public static void Capture()
    {
        var all = Collect(true);
        all.Sort((a, b) => a.Y != b.Y ? b.Y.CompareTo(a.Y) : a.X.CompareTo(b.X));
        var entries = new List<NavEntry>();
        var seen = new HashSet<string>();
        foreach (var f in all)
            if (seen.Add(f.Text)) entries.Add(new NavEntry { Text = f.Text, Name = f.Text });
        if (entries.Count == 0) { ListNav.ShowStatic(entries, Loc.T("screen.none")); return; }
        ListNav.ShowStatic(entries, Loc.T("screen.captured", entries.Count, AccessMod.KeyName("PreviousItem"), AccessMod.KeyName("NextItem")) + " " + entries[0].Text);
    }

    private static List<Found> Collect(bool includeUi)
    {
        var list = new List<Found>();
        try
        {
            // Texts in the world (ghost hints, information bubbles).
            foreach (var tm in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
            {
                if (tm == null || !tm.gameObject.activeInHierarchy || tm.color.a < 0.1f) continue;
                var r = tm.GetComponent<MeshRenderer>();
                if (r != null && !r.enabled) continue;
                Add(list, tm.text, tm.transform.position, false);
            }
            foreach (var tmp in Object.FindObjectsByType<TextMeshPro>(FindObjectsSortMode.None))
            {
                if (tmp == null || !tmp.gameObject.activeInHierarchy || !tmp.enabled || tmp.alpha < 0.1f) continue;
                Add(list, tmp.text, tmp.transform.position, false);
            }
            if (!includeUi) return list;

            // Visible UI texts (windows, notifications, summaries).
            foreach (var t in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
            {
                if (t == null || !t.gameObject.activeInHierarchy || !t.enabled || IsUnderStats(t.transform)) continue;
                if (t.canvasRenderer != null && t.canvasRenderer.GetInheritedAlpha() < 0.1f) continue;
                Add(list, t.text, t.transform.position, true);
            }
            foreach (var t in Object.FindObjectsByType<UIText>(FindObjectsSortMode.None))
            {
                if (t == null || !t.gameObject.activeInHierarchy || !t.enabled || IsUnderStats(t.transform)) continue;
                if (t.canvasRenderer != null && t.canvasRenderer.GetInheritedAlpha() < 0.1f) continue;
                Add(list, t.text, t.transform.position, true);
            }
        }
        catch { }
        return list;
    }

    private static bool IsUnderStats(Transform t)
    {
        try { return t.GetComponentInParent<StatsPanel>() != null; } catch { return false; }
    }

    private static void Add(List<Found> list, string raw, Vector3 pos, bool ui)
    {
        string text = Clean(raw);
        if (text.Length < 3 || !HasLetter.IsMatch(text)) return;
        list.Add(new Found { Text = text, X = pos.x, Y = pos.y, IsUi = ui });
    }

    private static string Clean(string t)
    {
        if (string.IsNullOrEmpty(t)) return "";
        var sb = new StringBuilder(t.Length);
        bool tag = false;
        foreach (char c in t)
        {
            if (c == '<') { tag = true; continue; }
            if (c == '>') { tag = false; continue; }
            if (!tag) sb.Append(c == '\n' || c == '\r' ? ' ' : c);
        }
        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }
}
