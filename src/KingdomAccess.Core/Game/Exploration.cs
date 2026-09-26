using UnityEngine;

namespace KingdomAccess.Game;

/// <summary>
/// Explored area of the island: from the leftmost to the rightmost point reached by
/// the player, widened by a margin (what is visible on screen). Kingdom buildings
/// (walls, castle) count as explored, which covers reloading a saved game.
/// </summary>
internal static class Exploration
{
    private static float _min = float.MaxValue, _max = float.MinValue;

    public static void Reset()
    {
        _min = float.MaxValue;
        _max = float.MinValue;
    }

    public static void Tick(Player player)
    {
        float x = GameState.PlayerX(player);
        if (x < _min) _min = x;
        if (x > _max) _max = x;
    }

    /// <summary>Adds the kingdom buildings already present when the island loads.</summary>
    public static void SeedFromKingdom()
    {
        foreach (var w in UnitCache.All<Wall>(UnitKind.Wall)) Include(w.transform.position.x);
        foreach (var c in UnitCache.All<Castle>(UnitKind.Castle)) Include(c.transform.position.x);
    }

    private static void Include(float x)
    {
        if (x < _min) _min = x;
        if (x > _max) _max = x;
    }

    /// <summary>True if x is inside the explored area (margin included).</summary>
    public static bool IsExplored(float x, float margin)
    {
        if (_min > _max) return true;
        return x >= _min - margin && x <= _max + margin;
    }
}
