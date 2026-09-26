using System.Collections.Generic;
using System.Text.RegularExpressions;
using KingdomAccess.Game;
using KingdomAccess.Localization;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Features;

/// <summary>
/// Tutorial: a ghost shows an action (key pictogram) and a place on screen,
/// often without text. Each new hint is announced (text translated by the game if any,
/// otherwise the expected action) with where the ghost is. The TutorialHint shortcut repeats it.
/// </summary>
internal static class TutorialNarrator
{
    private static float _nextScan;
    private static Ghost _ghost;
    private static string _lastHintName;
    private static string _lastText;

    internal static IModLog Log;

    public static void Reset() { _ghost = null; _lastHintName = null; _lastText = null; }

    /// <summary>The tutorial ghost, for the scanner and auto-walk.</summary>
    public static Ghost CurrentGhost => _ghost != null && _ghost.gameObject.activeInHierarchy ? _ghost : null;

    public static void Tick(Player player, AccessSettings s, float now)
    {
        if (!s.AnnounceScreenText || now < _nextScan) return;
        _nextScan = now + 0.5f;

        Ghost ghost = null;
        try
        {
            foreach (var g in Object.FindObjectsByType<Ghost>(FindObjectsSortMode.None))
                if (g != null && g.gameObject.activeInHierarchy && !g.IsPlayerTwoGhost) { ghost = g; break; }
        }
        catch { }
        _ghost = ghost;
        if (ghost == null) { _lastHintName = null; return; }

        var data = HintData(ghost);
        string name = data != null ? data.name : null;
        if (name == null || name == _lastHintName) return;
        _lastHintName = name;
        _lastText = Describe(player, ghost, data);
        try { Log?.Info($"[Tutoriel] indice {name}, action {data.glyphAction} -> {_lastText}"); } catch { }
        SpeechOut.Say(_lastText, false);
    }

    /// <summary>Repeats the current hint with the current position of the ghost.</summary>
    public static void Repeat(Player player)
    {
        var ghost = CurrentGhost;
        var data = ghost != null ? HintData(ghost) : null;
        if (data == null) { SpeechOut.Say(Loc.T("tutorial.none")); return; }
        SpeechOut.Say(Describe(player, ghost, data));
    }

    private static GhostHintData HintData(Ghost g)
    {
        try
        {
            var data = g.CurrentHintData;
            if (data != null) return data;
            var trig = g._currentTrigger;
            return trig != null ? trig.hintData : null;
        }
        catch { return null; }
    }

    private static string Describe(Player player, Ghost ghost, GhostHintData data)
    {
        var parts = new List<string> { Loc.T("tutorial.prefix") };

        // Game texts attached to the hint (translated into the game language).
        var texts = new List<string>();
        AddLoc(texts, data.PlayerPresentHintlocIdHint);
        AddLoc(texts, data.PlayerBeckonHintlocIdHint);
        AddLoc(texts, data.PlayerNotPresentHintlocIdHint);
        try
        {
            var beckon = ghost._beckonText;
            if (beckon != null && beckon.gameObject.activeInHierarchy) AddText(texts, beckon.Text);
        }
        catch { }
        if (texts.Count > 0) parts.Add(string.Join(". ", texts));

        // Expected action (pictogram shown next to the ghost).
        string action = null;
        try { action = Loc.TryT("glyph." + data.glyphAction.ToString().ToLowerInvariant()); } catch { }
        if (action == null) action = Loc.TryT("tutorialhint." + Key(data.name));
        if (action != null) parts.Add(action);

        // Where the ghost is.
        if (player != null)
        {
            float dx = ghost.transform.position.x - GameState.PlayerX(player);
            parts.Add(Mathf.Abs(dx) < 2f ? Loc.T("tutorial.ghost_here") : Loc.T("tutorial.ghost_at", Directions.DistanceSide(dx)));
        }
        return string.Join(". ", parts);
    }

    private static void AddLoc(List<string> list, FilteredLocID id)
    {
        try
        {
            string key = id.locId;
            if (string.IsNullOrEmpty(key)) return;
            string val = global::Language.Get(key);
            if (val != key) AddText(list, val);
        }
        catch { }
    }

    private static readonly Regex Tags = new("<.*?>", RegexOptions.Compiled);

    private static void AddText(List<string> list, string t)
    {
        if (string.IsNullOrWhiteSpace(t)) return;
        t = Tags.Replace(t, "").Replace('\n', ' ').Trim();
        if (t.Length < 2 || t.StartsWith("#") || list.Contains(t)) return;
        list.Add(t);
    }

    private static string Key(string s) => Regex.Replace((s ?? "").ToLowerInvariant(), "[^a-z0-9]", "");
}
