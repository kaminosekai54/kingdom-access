using System.Collections.Generic;
using KingdomAccess.Game;
using KingdomAccess.Localization;
using KingdomAccess.Speech;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Features;

/// <summary>
/// Bomb expedition to the Greed cave, beyond the cliff portal:
/// announces each stage (journey, entrance, crossing, boss, detonation, exit), warns when
/// the player crosses the portal, and limits the scanner and radar to that area while there.
/// </summary>
internal static class CaveNarrator
{
    private static float _nextCheck;
    private static readonly Dictionary<System.IntPtr, CliffPortalState.State> States = new();
    private static bool? _beyond;
    private static float _cliffX, _castleX;
    private static bool _hasCliff;

    public static void Reset() { States.Clear(); _beyond = null; _hasCliff = false; }

    /// <summary>True if the player is beyond the cliff portal (cave side).</summary>
    public static bool PlayerBeyondCliff => _beyond == true;

    /// <summary>True if x is on the same side of the cliff portal as the player.</summary>
    public static bool InPlayerZone(float x)
    {
        if (!_hasCliff || _beyond != true) return true;
        return _cliffX < _castleX ? x < _cliffX + 2f : x > _cliffX - 2f;
    }

    public static void Tick(Player player, float now)
    {
        if (now < _nextCheck) return;
        _nextCheck = now + 0.5f;

        UpdateZone(player);

        try
        {
            foreach (var cps in Object.FindObjectsByType<CliffPortalState>(FindObjectsSortMode.None))
            {
                if (cps == null) continue;
                var st = cps._state;
                if (!States.TryGetValue(cps.Pointer, out var before) || before != st) LogStage(player, st.ToString());
                if (States.TryGetValue(cps.Pointer, out before) && before != st)
                {
                    string text = Loc.TryT("cave.state." + st.ToString().ToLowerInvariant());
                    if (text != null) SpeechOut.Say(text, true);
                }
                States[cps.Pointer] = st;
            }
        }
        catch { }
    }

    private static void UpdateZone(Player player)
    {
        _hasCliff = false;
        float px = GameState.PlayerX(player);
        Castle castle = null;
        foreach (var c in UnitCache.All<Castle>(UnitKind.Castle)) { castle = c; break; }
        if (castle == null) return;
        _castleX = castle.transform.position.x;

        // Cliff portal on the player's side of the island.
        Portal cliff = null;
        foreach (var p in UnitCache.All<Portal>(UnitKind.Portal))
        {
            try { if (p.type != Portal.Type.Cliff) continue; } catch { continue; }
            float x = p.transform.position.x;
            if ((x < _castleX) != (px < _castleX)) continue;
            cliff = p;
            break;
        }
        if (cliff == null) { _beyond = null; return; }
        _hasCliff = true;
        _cliffX = cliff.transform.position.x;

        bool beyond = _cliffX < _castleX ? px < _cliffX : px > _cliffX;
        if (_beyond.HasValue && _beyond.Value != beyond)
            SpeechOut.Say(Loc.T(beyond ? "cave.enter_zone" : "cave.leave_zone"), true);
        _beyond = beyond;
    }

    // ---------- Bomb beacon ----------

    internal static IModLog Log { set => _log = value; }
    private static IModLog _log;

    /// <summary>Expedition stages during which the bomb is inside the cave.</summary>
    private static readonly System.Collections.Generic.HashSet<string> CaveStages = new()
    {
        "CaveEntrance", "GoingInsideCave", "TraversingCave", "FightingBoss", "WaitingForLight"
    };

    private static float? _targetX;
    private static float _nextBeat, _nextTarget;
    private static bool _reachedSaid;

    private static bool InStage(System.Func<string, bool> test)
    {
        foreach (var st in States.Values)
            if (test(st.ToString())) return true;
        return false;
    }

    /// <summary>The expedition bomb (nearest to x), or null.</summary>
    private static Bomb FindBomb(float x)
    {
        Bomb best = null;
        float bestD = float.MaxValue;
        try
        {
            foreach (var b in Object.FindObjectsByType<Bomb>(FindObjectsSortMode.None))
            {
                if (b == null || !b.gameObject.activeInHierarchy) continue;
                float d = Mathf.Abs(b.transform.position.x - x);
                if (d < bestD) { bestD = d; best = b; }
            }
        }
        catch { }
        return best;
    }

    /// <summary>
    /// True when the bomb waits for the player to light its fuse (the game's "waiting for light"
    /// stage): paying then lights it.
    /// </summary>
    public static bool BombAtDetonation(GameObject bombGo) => InStage(s => s == "WaitingForLight");

    /// <summary>
    /// Inside the cave, a heartbeat guides the player to the bomb, where they have to act: faster
    /// and louder when closer, in the ear of the side where it is. Called every frame.
    /// </summary>
    public static void BeaconTick(Player player, float now)
    {
        if (now >= _nextTarget)
        {
            _nextTarget = now + 0.25f;
            _targetX = null;
            if (InStage(s => CaveStages.Contains(s)))
            {
                var bomb = FindBomb(GameState.PlayerX(player));
                if (bomb != null) _targetX = bomb.transform.position.x;
            }
        }
        if (_targetX == null) { _reachedSaid = false; return; }

        float dx = _targetX.Value - GameState.PlayerX(player);
        float d = Mathf.Abs(dx);
        if (d < 2f && !_reachedSaid) { _reachedSaid = true; SpeechOut.Say(Loc.T("cave.bomb_here"), false); }
        else if (d > 5f) _reachedSaid = false;

        if (now < _nextBeat) return;
        float closeness = Mathf.Clamp01(1f - d / 60f);
        _nextBeat = now + Mathf.Lerp(1.6f, 0.35f, closeness);
        float pan = d < 1.5f ? 0f : Mathf.Sign(dx) * Mathf.Clamp01(d / 12f);
        GameAudio.Heartbeat(Mathf.Lerp(0.25f, 1f, closeness), pan);
    }

    /// <summary>Diagnostics: positions and the game's raw bomb points at each expedition stage.</summary>
    private static void LogStage(Player player, string stage)
    {
        try
        {
            float px = GameState.PlayerX(player);
            var bomb = FindBomb(px);
            string bombX = bomb != null ? bomb.transform.position.x.ToString("0.0") : "none";
            var sb = new System.Text.StringBuilder();
            foreach (var bp in Object.FindObjectsByType<BombablePortal>(FindObjectsSortMode.None))
                if (bp != null)
                    sb.Append($" | portal x={bp.transform.position.x:0.0} wait={bp.bombWaitPosition:0.0} enter={bp.bombEnterPosition:0.0} detonate={bp.bombDetonatesPosition:0.0}");
            _log?.Info($"[Cave] stage {stage}: player x={px:0.0}, bomb x={bombX}, cliff x={(_hasCliff ? _cliffX.ToString("0.0") : "?")}, beyond cliff={_beyond}{sb}");
        }
        catch { }
    }

    /// <summary>Bomb details: where the bomb is now, then where the cliff portal is, from the player.</summary>
    public static string BombDetails(Player player, GameObject bombGo)
    {
        try
        {
            if (player == null) return null;
            float px = GameState.PlayerX(player);
            var parts = new System.Collections.Generic.List<string>
            {
                Loc.T("cave.point.bomb", Directions.DistanceSide(bombGo.transform.position.x - px))
            };
            if (_hasCliff) parts.Add(Loc.T("cave.point.portal", Directions.DistanceSide(_cliffX - px)));
            return string.Join(", ", parts);
        }
        catch { return null; }
    }
}
