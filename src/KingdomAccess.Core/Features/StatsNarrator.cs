using System.Collections.Generic;
using System.Linq;
using KingdomAccess.Localization;
using UnityEngine;

namespace KingdomAccess.Features;

/// <summary>
/// End-of-island summary (statistics panel): read in full when it appears, then
/// browsable line by line with the list keys. Waits for the animated count to finish.
/// </summary>
internal static class StatsNarrator
{
    private const float SettleTime = 2.5f;

    private static float _nextLookup;
    private static StatsPanel[] _panels = new StatsPanel[0];
    private static StatsPanel _shown;
    private static float _shownSince;
    private static bool _announced;

    public static void Tick(float now)
    {
        if (now >= _nextLookup)
        {
            _nextLookup = now + 1f;
            try
            {
                var found = Object.FindObjectsByType<StatsPanel>(FindObjectsSortMode.None);
                _panels = new StatsPanel[found.Length];
                for (int i = 0; i < found.Length; i++) _panels[i] = found[i];
            }
            catch { }
        }

        StatsPanel visible = null;
        foreach (var p in _panels)
        {
            try
            {
                if (p != null && p.isActiveAndEnabled && p.canvasGroup != null && p.canvasGroup.alpha > 0.5f) { visible = p; break; }
            }
            catch { }
        }

        if (visible == null) { _shown = null; _announced = false; return; }
        if (_shown == null || visible.Pointer != _shown.Pointer)
        {
            _shown = visible;
            _shownSince = now;
            _announced = false;
        }
        if (!_announced && now - _shownSince >= SettleTime)
        {
            _announced = true;
            Announce(visible);
        }
    }

    private static void Announce(StatsPanel p)
    {
        var entries = new List<NavEntry>();
        try
        {
            var labels = p.labelsInOrder;
            var values = p.statsInOrder;
            int n = Mathf.Max(labels?.Length ?? 0, values?.Length ?? 0);
            for (int i = 0; i < n; i++)
            {
                string label = null, value = null;
                try { var l = labels != null && i < labels.Length ? labels[i] : null; if (l != null && l.gameObject.activeInHierarchy) label = l.Text; } catch { }
                try { var v = values != null && i < values.Length ? values[i] : null; if (v != null && v.gameObject.activeInHierarchy) value = v.text; } catch { }
                label = label?.Trim(); value = value?.Trim();
                if (string.IsNullOrEmpty(label) && string.IsNullOrEmpty(value)) continue;
                string line = string.IsNullOrEmpty(label) ? value : string.IsNullOrEmpty(value) ? label : $"{label} : {value}";
                entries.Add(new NavEntry { Text = line, Name = line });
            }
        }
        catch { }
        if (entries.Count == 0) return;
        ListNav.ShowStatic(entries, Loc.T("stats.title") + ". " + string.Join(". ", entries.Select(e => e.Text)) + ". " + Loc.T("stats.help", AccessMod.KeyName("PreviousItem"), AccessMod.KeyName("NextItem")));
    }
}
