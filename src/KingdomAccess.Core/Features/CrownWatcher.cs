using KingdomAccess.Game;
using KingdomAccess.Localization;
using KingdomAccess.Speech;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Features;

/// <summary>
/// Lost crown: when the crown is on the ground or carried away by a Greed, announce it at once
/// with its position (alert sound on the right side), then regularly until it is
/// recovered. The RunToCrown shortcut runs to it. (A Greed carrying the crown away = end of the reign.)
/// </summary>
internal static class CrownWatcher
{
    private const float ScanInterval = 0.3f;
    private const float RepeatGround = 3f;
    private const float RepeatStolen = 1.5f;

    private static float _nextScan, _nextRepeat;
    private static Crown _lost;
    private static bool _wasStolen;
    private static IModLog _log;

    public static void Initialize(IModLog log) => _log = log;

    public static void Reset() { _lost = null; _wasStolen = false; }

    /// <summary>The lost crown, if any.</summary>
    public static Crown Lost => _lost != null && _lost.gameObject.activeInHierarchy ? _lost : null;

    public static void Tick(Player player, float now)
    {
        if (now < _nextScan) return;
        _nextScan = now + ScanInterval;

        Crown found = null;
        bool stolen = false;
        float px = GameState.PlayerX(player);
        try
        {
            foreach (var c in Object.FindObjectsByType<Crown>(FindObjectsSortMode.None))
            {
                if (c == null || !c.gameObject.activeInHierarchy) continue;
                bool foe = false;
                try { foe = c.heldByFoe; } catch { }
                // The worn crown follows the player: only report it when it is somewhere else.
                float d = Mathf.Abs(c.transform.position.x - px);
                if (!foe && d < 1.5f) continue;
                found = c;
                stolen = foe;
                break;
            }
        }
        catch { }

        if (found == null)
        {
            if (_lost != null) SpeechOut.Say(Loc.T("crown.recovered"), true);
            _lost = null;
            _wasStolen = false;
            return;
        }

        bool isNew = _lost == null || _lost.Pointer != found.Pointer || stolen != _wasStolen;
        if (isNew) _log?.Info($"[Crown] {(stolen ? "carried by a Greed" : "on the ground")} at x={found.transform.position.x:0.0}");
        _lost = found;
        _wasStolen = stolen;

        if (!isNew && now < _nextRepeat) return;
        _nextRepeat = now + (stolen ? RepeatStolen : RepeatGround);

        float dx = found.transform.position.x - px;
        Sounds.Play(dx < 0 ? "enemy_left_3" : "enemy_right_3");
        string where = Mathf.Abs(dx) < 1.5f ? Loc.T("dir.here") : Directions.DistanceSide(dx);
        SpeechOut.Say(Loc.T(stolen ? "crown.stolen" : "crown.dropped", where, AccessMod.KeyName("RunToCrown")), true);
    }
}
