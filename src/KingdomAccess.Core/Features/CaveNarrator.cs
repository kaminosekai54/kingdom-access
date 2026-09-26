using System.Collections.Generic;
using KingdomAccess.Game;
using KingdomAccess.Localization;
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
                if (States.TryGetValue(cps.Pointer, out var before) && before != st)
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

    /// <summary>Bomb details: key points of the expedition, from the player.</summary>
    public static string BombDetails(Player player, GameObject bombGo)
    {
        try
        {
            var bomb = bombGo.GetComponent<Bomb>();
            if (bomb == null || player == null) return null;
            float px = GameState.PlayerX(player);
            BombablePortal best = null;
            float bestD = float.MaxValue;
            foreach (var bp in Object.FindObjectsByType<BombablePortal>(FindObjectsSortMode.None))
            {
                if (bp == null) continue;
                float d = Mathf.Abs(bp.transform.position.x - bombGo.transform.position.x);
                if (d < bestD) { bestD = d; best = bp; }
            }
            if (best == null) return null;
            float bx = best.transform.position.x;
            // Absolute position, or relative to the portal if the value is small and the portal far away.
            float Abs(float v) => Mathf.Abs(v) < 60f && Mathf.Abs(bx) > 100f ? bx + v : v;
            var parts = new List<string>
            {
                Loc.T("cave.point.enter", Directions.DistanceSide(Abs(best.bombEnterPosition) - px)),
                Loc.T("cave.point.detonate", Directions.DistanceSide(Abs(best.bombDetonatesPosition) - px))
            };
            return string.Join(", ", parts);
        }
        catch { return null; }
    }
}
