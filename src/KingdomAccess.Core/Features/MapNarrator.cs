using System.Collections.Generic;
using KingdomAccess.Localization;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Features;

/// <summary>
/// "Map and timeline" screen: this screen handles its selection itself (outside the
/// standard UI). Follow the highlighted island and reign and announce them with their content.
/// </summary>
internal static class MapNarrator
{
    private static MapTimelineMenu _menu;
    private static float _nextLookup;
    private static int _lastLand = -1, _lastReign = -1;
    private static bool _wasOpen;

    public static void Tick(float now)
    {
        if (_menu == null && now >= _nextLookup)
        {
            _nextLookup = now + 1f;
            try { _menu = Object.FindFirstObjectByType<MapTimelineMenu>(); } catch { }
        }
        if (_menu == null) return;

        bool open;
        try
        {
            var st = _menu.CurrentState;
            open = _menu.isActiveAndEnabled
                   && (st == MapTimelineMenu.State.ShowingMapOnly || st == MapTimelineMenu.State.ShowingWithTimeline);
        }
        catch { _menu = null; return; }

        if (!open)
        {
            _wasOpen = false;
            _lastLand = _lastReign = -1;
            return;
        }

        int land = _menu.focusedLand, reign = _menu.focusedReign;
        if (!_wasOpen)
        {
            _wasOpen = true;
            SpeechOut.Say(Loc.T("map.open"), false);
        }
        if (land != _lastLand)
        {
            _lastLand = land;
            SpeechOut.Say(DescribeLand(land), true);
        }
        if (reign != _lastReign)
        {
            bool first = _lastReign < 0;
            _lastReign = reign;
            if (!first) SpeechOut.Say(DescribeReign(reign), true);
        }
    }

    private static string DescribeLand(int index)
    {
        var parts = new List<string>();
        int count = 0;
        try { count = _menu.lands.Count; } catch { }
        parts.Add(Loc.T("map.island.of", index + 1, count));

        try
        {
            if (index >= 0 && index < count)
            {
                var land = _menu.lands[index];
                if (land._ship != null && land._ship.activeInHierarchy) parts.Add(Loc.T("map.ship_here"));
                var icons = new List<string>();
                foreach (var icon in land.GetComponentsInChildren<UIMapIcon>(false))
                {
                    if (icon == null || icon.icon == null || !icon.icon.enabled) continue;
                    string name = Loc.TryT("mapicon." + icon.iconType.ToString().ToLowerInvariant()) ?? icon.iconType.ToString();
                    string txt = icon.text != null && icon.text.gameObject.activeInHierarchy ? icon.text.text?.Trim() : null;
                    icons.Add(string.IsNullOrEmpty(txt) ? name : $"{name} {txt}");
                }
                parts.Add(icons.Count == 0 ? Loc.T("map.no_icons") : string.Join(", ", icons));
            }
        }
        catch { }
        return string.Join(", ", parts);
    }

    private static string DescribeReign(int index)
    {
        var parts = new List<string>();
        int count = 0;
        try { count = _menu.reigns.Count; } catch { }
        parts.Add(Loc.T("map.reign.of", index + 1, count));
        try
        {
            if (index >= 0 && index < count)
            {
                var r = _menu.reigns[index];
                if (r.dayCountText != null) parts.Add(Loc.T("map.reign.days", r.dayCountText.text?.Trim()));
                if (r.curRuler != null && r.curRuler.activeInHierarchy) parts.Add(Loc.T("map.reign.current"));
            }
        }
        catch { }
        return string.Join(", ", parts);
    }
}
