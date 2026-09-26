using System.Collections.Generic;
using KingdomAccess.Game;
using KingdomAccess.Localization;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Features;

/// <summary>
/// Kingdom zones: castle (between the two outermost walls) and vagrant camps
/// (between the trees closest to each camp). Announces entering and leaving,
/// the direction of the base camp on arrival, and knows whether a tree protects a camp.
/// </summary>
internal static class Zones
{
    private const float RecomputeInterval = 5f;
    private const float AnnounceCooldown = 0.5f;

    private static float _castleMin = float.MaxValue, _castleMax = float.MinValue;
    private static readonly List<Vector2> Camps = new();
    private static float _nextRecompute;
    private static float _cooldown;
    private static bool? _inCastle, _inCamp;
    private static bool _orientationDone;
    private static float _levelStartTime;

    /// <summary>Called when a new island (new player object) is detected.</summary>
    public static void Reset(float now)
    {
        _castleMin = float.MaxValue; _castleMax = float.MinValue;
        Camps.Clear();
        _inCastle = null; _inCamp = null;
        _orientationDone = false;
        _levelStartTime = now;
        _nextRecompute = 0f;
    }

    public static void Tick(Player player, AccessSettings s, float now, float dt)
    {
        if (_cooldown > 0f) _cooldown -= dt;
        if (now >= _nextRecompute)
        {
            _nextRecompute = now + RecomputeInterval;
            Recompute();
        }

        float x = GameState.PlayerX(player);

        if (!_orientationDone && now - _levelStartTime > 3f)
            AnnounceBaseDirection(player, x);

        if (s.AnnounceCastleZone && _castleMin <= _castleMax)
        {
            bool inside = x >= _castleMin && x <= _castleMax;
            if (_inCastle.HasValue && inside != _inCastle.Value && _cooldown <= 0f)
            {
                SpeechOut.Say(Loc.T(inside ? "zone.castle.enter" : "zone.castle.leave"), false);
                _cooldown = AnnounceCooldown;
            }
            _inCastle = inside;
        }

        if (s.AnnounceCampZone)
        {
            bool inCamp = false;
            foreach (var c in Camps)
                if (x >= c.x && x <= c.y) { inCamp = true; break; }
            if (_inCamp.HasValue && inCamp != _inCamp.Value && _cooldown <= 0f)
            {
                SpeechOut.Say(Loc.T(inCamp ? "zone.camp.enter" : "zone.camp.leave"), false);
                _cooldown = AnnounceCooldown;
            }
            _inCamp = inCamp;
        }
    }

    /// <summary>True if this tree is the boundary tree of a camp: cutting it destroys the camp.</summary>
    public static bool TreeProtectsCamp(float treeX)
    {
        foreach (var c in Camps)
            if (Mathf.Abs(treeX - c.x) < 0.1f || Mathf.Abs(treeX - c.y) < 0.1f) return true;
        return false;
    }

    private static void AnnounceBaseDirection(Player player, float x)
    {
        Castle closest = null;
        float best = float.MaxValue;
        foreach (var castle in UnitCache.All<Castle>(UnitKind.Castle))
        {
            float d = Mathf.Abs(castle.transform.position.x - x);
            if (d < best) { best = d; closest = castle; }
        }
        if (closest == null) return;
        _orientationDone = true;

        float dx = closest.transform.position.x - x;
        if (Mathf.Abs(dx) < 3f)
            SpeechOut.Say(Loc.T("zone.base.here"), false);
        else
            SpeechOut.Say(Loc.T("zone.base.dir", Directions.Side(dx), Mathf.RoundToInt(Mathf.Abs(dx))), false);
    }

    private static void Recompute()
    {
        float min = float.MaxValue, max = float.MinValue;
        foreach (var wall in UnitCache.All<Wall>(UnitKind.Wall))
        {
            float wx = wall.transform.position.x;
            if (wx < min) min = wx;
            if (wx > max) max = wx;
        }
        if (min > max)
        {
            // No wall yet: 20-unit zone around the campfire.
            foreach (var castle in UnitCache.All<Castle>(UnitKind.Castle))
            {
                float cx = castle.transform.position.x;
                min = Mathf.Min(min, cx - 20f);
                max = Mathf.Max(max, cx + 20f);
            }
        }
        _castleMin = min; _castleMax = max;

        Camps.Clear();
        var payables = GameState.Payables;
        foreach (var camp in UnitCache.All<BeggarCamp>(UnitKind.BeggarCamp))
        {
            float cx = camp.transform.position.x;
            float left = cx - 15f, right = cx + 15f;
            float bestLeft = float.MaxValue, bestRight = float.MaxValue;
            if (payables != null)
            {
                for (int i = 0; i < payables.Length; i++)
                {
                    var p = payables[i];
                    if (p == null || p.GetComponent<PayableTree>() == null) continue;
                    float tx = p.transform.position.x;
                    if (tx < cx && cx - tx < bestLeft) { bestLeft = cx - tx; left = tx; }
                    if (tx > cx && tx - cx < bestRight) { bestRight = tx - cx; right = tx; }
                }
            }
            Camps.Add(new Vector2(left, right));
        }
    }
}

/// <summary>Wording of directions and distances.</summary>
internal static class Directions
{
    public static string Side(float dx) => Loc.T(dx < 0 ? "dir.left" : "dir.right");

    /// <summary>"24 left" / "24 à gauche".</summary>
    public static string DistanceSide(float dx) =>
        Loc.T("dir.distance", Mathf.RoundToInt(Mathf.Abs(dx)), Side(dx));
}
