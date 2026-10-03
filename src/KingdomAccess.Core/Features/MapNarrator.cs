using System.Collections.Generic;
using System.Text.RegularExpressions;
using KingdomAccess.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Features;

/// <summary>
/// "Map and timeline" screen: this screen handles its selection itself (outside the
/// standard UI). Follows the highlighted island and reign and announces them with their content.
///
/// Call of Olympus has its own map (MapTimelineMenuGreece) with two views: the world view, where
/// each island is a button (oracle, god temples, god quests, Mount Olympus), and the single-island
/// view, opened with Enter, where left and right scroll through the islands. Both are handled
/// here; the generic menu narrator leaves those island buttons to this class.
/// </summary>
internal static class MapNarrator
{
    private static MapTimelineMenu _menu;
    private static MapTimelineMenuGreece _greece;
    private static float _nextLookup;
    private static int _lastLand = -1, _lastReign = -1;
    private static bool _wasOpen;
    private static string _lastGreekView;
    private static GameObject _lastGreekButton;
    private static GameObject _lastLoggedSelection;
    internal static IModLog Log { set => _log = value; }
    private static IModLog _log;

    /// <summary>Development aid: writes each element selected on the Olympus map to the log.</summary>
    private static void LogSelection()
    {
        try
        {
            var go = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (go == null || go == _lastLoggedSelection) return;
            _lastLoggedSelection = go;
            string path = go.name;
            for (var t = go.transform.parent; t != null; t = t.parent) path = t.name + "/" + path;
            _log?.Info($"[Map] Olympus map selection: {path} (view {_greece._openWorldMapState}, focused land {_greece.focusedLand})");
        }
        catch { }
    }

    public static void Tick(float now)
    {
        if (now >= _nextLookup && (_menu == null || !_menu.isActiveAndEnabled))
        {
            _nextLookup = now + 1f;
            FindMenu();
        }
        if (_greece == null) GreekMapOpen = false;
        if (_menu == null) return;

        try
        {
            if (_greece != null) TickGreece();
            else TickClassic();
        }
        catch { _menu = null; _greece = null; }
    }

    /// <summary>
    /// Finds the active map. The Olympus map type is only touched once an instance of it exists
    /// (touching a type that is not loaded in the current world can crash the game).
    /// </summary>
    private static void FindMenu()
    {
        _menu = null; _greece = null;
        try
        {
            foreach (var m in Object.FindObjectsByType<MapTimelineMenu>(FindObjectsSortMode.None))
            {
                if (m == null || !m.isActiveAndEnabled) continue;
                _menu = m;
                if (m.GetIl2CppType().Name == "MapTimelineMenuGreece") _greece = m.TryCast<MapTimelineMenuGreece>();
                break;
            }
        }
        catch { }
    }

    // ---------- Classic map ----------

    private static void TickClassic()
    {
        var st = _menu.CurrentState;
        bool open = _menu.isActiveAndEnabled
                    && (st == MapTimelineMenu.State.ShowingMapOnly || st == MapTimelineMenu.State.ShowingWithTimeline);
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
            SpeechOut.Say(DescribeLand(land, null), true);
        }
        if (reign != _lastReign)
        {
            bool first = _lastReign < 0;
            _lastReign = reign;
            if (!first) SpeechOut.Say(DescribeReign(reign), true);
        }
    }

    // ---------- Olympus map ----------

    private const float ViewSettle = 0.6f;
    private const float LandSettle = 0.15f;
    private static string _pendingView;
    private static float _pendingViewSince;
    private static int _pendingLand = -1;
    private static float _pendingLandSince;

    /// <summary>True while the Olympus map is open (on-screen text reading pauses meanwhile).</summary>
    public static bool GreekMapOpen { get; private set; }

    /// <summary>True while the classic map or the Olympus map is open.</summary>
    public static bool AnyMapOpen => GreekMapOpen || _wasOpen;

    /// <summary>
    /// Olympus map. In the island view, left and right change island, and the game briefly goes
    /// back through the world view at each change: view changes are only announced once they
    /// last, and the island is announced whenever the focused island changes, in either view.
    /// </summary>
    private static void TickGreece()
    {
        float now = Time.unscaledTime;
        if (!_greece.isActiveAndEnabled)
        {
            GreekMapOpen = false;
            _wasOpen = false; _lastLand = -1; _lastGreekView = null; _lastGreekButton = null;
            _pendingView = null; _pendingLand = -1;
            return;
        }
        GreekMapOpen = true;
        LogSelection();

        // View: announce it only when it stays the same for a moment.
        string view = _greece._openWorldMapState.ToString();
        if (view != _pendingView) { _pendingView = view; _pendingViewSince = now; }
        else if (view != _lastGreekView && now - _pendingViewSince >= ViewSettle)
        {
            bool first = _lastGreekView == null;
            _lastGreekView = view;
            _log?.Info($"[Map] Olympus map view: {view}, focused land {_greece.focusedLand}");
            // The first time the map opens, the island announcement follows the view announcement.
            SpeechOut.Say(Loc.T(view == "ShowingWorld" ? "map.greece.world" : "map.greece.island"), !first);
        }

        // Island: the focused island, or in the world view the selected island button.
        int land = _greece.focusedLand;
        var go = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (view == "ShowingWorld" && go != null && go != _lastGreekButton && IsGreekLandButton(go))
        {
            _lastGreekButton = go;
            int fromButton = LandForGreekButton(go.GetComponent<UIMainMapLand>());
            if (fromButton >= 0) land = fromButton;
        }
        if (land != _pendingLand) { _pendingLand = land; _pendingLandSince = now; return; }
        if (land == _lastLand || now - _pendingLandSince < LandSettle) return;
        _lastLand = land;
        var button = GreekButtonForLand(land);
        string text = button != null ? DescribeGreekButton(button) : DescribeLand(land, null);
        SpeechOut.Say(text, _lastGreekView != null);
    }

    /// <summary>True for the island buttons of the Olympus world map (announced by this class).</summary>
    public static bool IsGreekLandButton(GameObject go)
    {
        if (go == null || _greece == null) return false;
        try
        {
            foreach (var c in go.GetComponents<Component>())
                if (c != null && c.GetIl2CppType().Name == "UIMainMapLand") return true;
        }
        catch { }
        return false;
    }

    private static string DescribeGreekButton(GameObject go)
    {
        var parts = new List<string> { GreekIslandName(go.name) };
        try
        {
            var land = go.GetComponent<UIMainMapLand>();
            if (land != null)
            {
                if (!land.IsUnlocked)
                {
                    parts.Add(Loc.T("map.locked"));
                    string quest = land._lockedByQuest.ToString();
                    if (quest != "None") parts.Add(Loc.T("map.locked_by", Loc.TryT("quest." + quest.ToLowerInvariant()) ?? quest));
                }
                int landIndex = LandForGreekButton(land);
                if (landIndex >= 0)
                {
                    string extra = DescribeLandContent(landIndex);
                    if (!string.IsNullOrEmpty(extra)) parts.Add(extra);
                }
            }
        }
        catch { }
        return string.Join(", ", parts);
    }

    /// <summary>"Land Button God Athena" -> translated island name (greekland.godathena).</summary>
    private static string GreekIslandName(string objectName)
    {
        string key = Regex.Replace(Regex.Replace(objectName ?? "", "(?i)land ?button", ""), "[^A-Za-z0-9]", "").ToLowerInvariant();
        return Loc.TryT("greekland." + key) ?? Loc.T("map.island");
    }

    /// <summary>Campaign land index shown by a world-map button (reverse of LAND_TO_MAP_LAND).</summary>
    private static int LandForGreekButton(UIMainMapLand button)
    {
        try
        {
            var lands = _greece._mainMap._mainMapLands;
            int mapIndex = -1;
            for (int i = 0; i < lands.Length; i++)
                if (lands[i] != null && lands[i].Pointer == button.Pointer) { mapIndex = i; break; }
            if (mapIndex < 0) return -1;
            foreach (var kv in _greece.LAND_TO_MAP_LAND)
                if (kv.Value == mapIndex) return kv.Key;
        }
        catch { }
        return -1;
    }

    /// <summary>World-map button that shows a campaign land, or null.</summary>
    private static GameObject GreekButtonForLand(int land)
    {
        try
        {
            if (_greece.LAND_TO_MAP_LAND.TryGetValue(land, out int mapIndex))
            {
                var lands = _greece._mainMap._mainMapLands;
                if (mapIndex >= 0 && mapIndex < lands.Length && lands[mapIndex] != null) return lands[mapIndex].gameObject;
            }
        }
        catch { }
        return null;
    }

    // ---------- Shared descriptions ----------

    private static string DescribeLand(int index, GameObject greekButton)
    {
        var parts = new List<string>();
        if (greekButton != null) parts.Add(GreekIslandName(greekButton.name));
        int count = 0;
        try { count = _menu.lands.Count; } catch { }
        if (greekButton == null) parts.Add(Loc.T("map.island.of", index + 1, count));
        string content = DescribeLandContent(index);
        parts.Add(string.IsNullOrEmpty(content) ? Loc.T("map.no_icons") : content);
        return string.Join(", ", parts);
    }

    /// <summary>Ship and map icons of a land (statues, mounts, hermits, gems, portals...).</summary>
    private static string DescribeLandContent(int index)
    {
        var parts = new List<string>();
        try
        {
            int count = _menu.lands.Count;
            if (index < 0 || index >= count) return "";
            var land = _menu.lands[index];
            if (land._ship != null && land._ship.activeInHierarchy) parts.Add(Loc.T("map.ship_here"));
            foreach (var icon in land.GetComponentsInChildren<UIMapIcon>(false))
            {
                if (icon == null || icon.icon == null || !icon.icon.enabled) continue;
                string name = Loc.TryT("mapicon." + icon.iconType.ToString().ToLowerInvariant()) ?? icon.iconType.ToString();
                string txt = icon.text != null && icon.text.gameObject.activeInHierarchy ? icon.text.text?.Trim() : null;
                parts.Add(string.IsNullOrEmpty(txt) ? name : $"{name} {txt}");
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
