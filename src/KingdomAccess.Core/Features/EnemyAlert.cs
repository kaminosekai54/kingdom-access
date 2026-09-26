using KingdomAccess.Game;
using KingdomAccess.Localization;
using KingdomAccess.Speech;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Features;

/// <summary>
/// Enemy alert in three levels per side: range, half, quarter (20, 10, 5 by default).
/// The side is the world side (left or right of the screen), never the player's facing:
/// the sound comes from the ear the enemy is coming from. Each level has its own sound,
/// more urgent as the enemy gets closer.
/// </summary>
internal static class EnemyAlert
{
    private const float CheckInterval = 0.25f;
    private const float Hysteresis = 2f;
    private const float MinRepeat = 1.5f;

    private struct SideState
    {
        public int Level;          // 0 = none, 1 = range, 2 = half, 3 = quarter
        public float LastAlert;
    }

    private static SideState _left, _right;
    private static float _nextCheck;

    public static void Reset()
    {
        _left = new SideState { LastAlert = -100f };
        _right = new SideState { LastAlert = -100f };
    }

    public static void Tick(Player player, AccessSettings s, float now)
    {
        if (!s.EnemyAlert || now < _nextCheck) return;
        _nextCheck = now + CheckInterval;

        float x = GameState.PlayerX(player);
        float range = Mathf.Max(2f, s.EnemyAlertRange);
        int leftCount = 0, rightCount = 0;
        float leftClosest = float.MaxValue, rightClosest = float.MaxValue;

        foreach (var e in UnitCache.All<Enemy>(UnitKind.Enemy))
        {
            if (!e.gameObject.activeInHierarchy) continue;
            float dx = e.transform.position.x - x;
            float d = Mathf.Abs(dx);
            if (d > range + Hysteresis) continue;
            if (dx < 0) { leftCount++; leftClosest = Mathf.Min(leftClosest, d); }
            else { rightCount++; rightClosest = Mathf.Min(rightClosest, d); }
        }

        Update(ref _left, leftCount, leftClosest, false, range, now);
        Update(ref _right, rightCount, rightClosest, true, range, now);
    }

    /// <summary>Level for a distance (0 to 3), with a margin to avoid flickering.</summary>
    private static int LevelFor(float d, float range, int current)
    {
        float[] thresholds = { range, range / 2f, range / 4f };
        int level = 0;
        for (int i = 0; i < thresholds.Length; i++)
        {
            // To stay in a level already reached, accept a small margin beyond it.
            float limit = thresholds[i] + (current > i ? Hysteresis : 0f);
            if (d <= limit) level = i + 1;
        }
        return level;
    }

    private static void Update(ref SideState st, int count, float closest, bool right, float range, float now)
    {
        int level = count == 0 ? 0 : LevelFor(closest, range, st.Level);
        if (level > st.Level && now - st.LastAlert >= (st.Level == 0 ? 0f : MinRepeat * 0.3f))
        {
            string side = right ? "right" : "left";
            Sounds.Play($"enemy_{side}_{level}");
            string where = Directions.DistanceSide(right ? closest : -closest);
            string key = level == 1
                ? (count == 1 ? "alert.enemy.one" : "alert.enemy.many")
                : "alert.enemy.closer";
            SpeechOut.Say(Loc.T(key, count, where), true);
            st.LastAlert = now;
        }
        st.Level = level;
    }
}
