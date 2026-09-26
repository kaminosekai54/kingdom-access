using HarmonyLib;
using KingdomAccess.Game;
using KingdomAccess.Localization;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Features;

/// <summary>
/// Auto-walk / auto-run to a selected item or to a position (behind a wall).
/// Ends on: arrival, target gone, opposite direction pressed, the shortcut pressed again, or being stuck.
/// </summary>
internal static class AutoWalk
{
    private const float ArriveDistance = 0.8f;
    private const float StuckTimeout = 4f;

    private static Component _target;
    private static bool _hasFixedX;
    private static float _fixedX;
    private static string _targetName;
    private static bool _run;
    private static bool _sprintStarted;
    private static float _lastProgressTime;
    private static float _bestDistance;

    public static bool Active => _target != null || _hasFixedX;

    /// <summary>Direction to inject (-1 left, 1 right, 0 none).</summary>
    internal static int Direction { get; private set; }
    internal static bool Run => _run;

    /// <summary>
    /// True right after starting: the shortcut key (e.g. the arrow of Ctrl+Right) is still
    /// held and must not be taken as the player taking over. Cleared as soon as no
    /// direction is pressed.
    /// </summary>
    internal static bool IgnoreUserInput { get; set; }

    /// <summary>Walk / run to the item of the last list read.</summary>
    public static void Toggle(Player player, bool run, float now)
    {
        if (Active) { Stop("walk.stopped"); return; }

        var target = ListNav.SelectedTarget;
        if (target == null) { SpeechOut.Say(Loc.T("walk.no_target", AccessMod.KeyName("NextCategory"), AccessMod.KeyName("PreviousItem"), AccessMod.KeyName("NextItem"))); return; }

        Start(player, target, false, 0f, ListNav.SelectedName ?? Loc.T("obj.unknown"), run, now);
    }

    /// <summary>
    /// Run just behind the farthest wall on the chosen side
    /// (kingdom side, in safety).
    /// </summary>
    public static void RunBehindOuterWall(Player player, bool right, float now)
    {
        Wall outer = null;
        foreach (var w in UnitCache.All<Wall>(UnitKind.Wall))
        {
            if (!w.gameObject.activeInHierarchy) continue;
            float wx = w.transform.position.x;
            if (outer == null || (right ? wx > outer.transform.position.x : wx < outer.transform.position.x)) outer = w;
        }
        if (outer == null) { SpeechOut.Say(Loc.T("walk.no_wall")); return; }

        // The kingdom is on the castle side: stop 2 units inside the wall.
        float wallX = outer.transform.position.x;
        float inside = right ? -2f : 2f;
        string name = Loc.T(right ? "walk.wall_right" : "walk.wall_left");
        Stop();
        Start(player, null, true, wallX + inside, name, true, now);
    }

    /// <summary>Run to the lost crown (on the ground or carried away).</summary>
    public static void RunToCrown(Player player, float now)
    {
        var crown = CrownWatcher.Lost;
        if (crown == null) { SpeechOut.Say(Loc.T("crown.none")); return; }
        Stop();
        Start(player, crown, false, 0f, Loc.T("crown.name"), true, now);
    }

    /// <summary>Run to the castle (or campfire), the main landmark of the island.</summary>
    public static void RunToCastle(Player player, float now)
    {
        Castle best = null;
        float px = GameState.PlayerX(player), bestD = float.MaxValue;
        foreach (var c in UnitCache.All<Castle>(UnitKind.Castle))
        {
            float d = Mathf.Abs(c.transform.position.x - px);
            if (d < bestD) { bestD = d; best = c; }
        }
        if (best == null) { SpeechOut.Say(Loc.T("walk.no_castle")); return; }
        Stop();
        Start(player, best, false, 0f, Loc.T("obj.castle"), true, now);
    }

    private static void Start(Player player, Component target, bool fixedX, float x, string name, bool run, float now)
    {
        IgnoreUserInput = true;
        _target = target;
        _hasFixedX = fixedX;
        _fixedX = x;
        _targetName = name;
        _run = run;
        _sprintStarted = false;
        _bestDistance = Mathf.Abs(TargetX() - GameState.PlayerX(player));
        _lastProgressTime = now;

        if (IsArrived(player))
        {
            _target = null; _hasFixedX = false;
            SpeechOut.Say(Loc.T("walk.already_here", _targetName));
            return;
        }
        float dx = TargetX() - GameState.PlayerX(player);
        SpeechOut.Say(Loc.T(run ? "walk.start_run" : "walk.start", _targetName) + ", " + Directions.DistanceSide(dx));
    }

    private static float TargetX() => _target != null ? _target.transform.position.x : _fixedX;

    public static void Stop(string key = null)
    {
        bool was = Active;
        _target = null;
        _hasFixedX = false;
        Direction = 0;
        if (was && key != null) SpeechOut.Say(Loc.T(key, _targetName));
    }

    /// <summary>Called every frame: updates the direction and detects the end.</summary>
    public static void Tick(Player player, float now)
    {
        if (!Active) { Direction = 0; return; }

        if (!_hasFixedX && (_target == null || !_target.gameObject.activeInHierarchy))
        {
            Stop("walk.gone");
            return;
        }

        if (IsArrived(player))
        {
            Stop("walk.arrived");
            return;
        }

        float dx = TargetX() - GameState.PlayerX(player);
        Direction = dx < 0 ? -1 : 1;

        float d = Mathf.Abs(dx);
        if (d < _bestDistance - 0.3f)
        {
            _bestDistance = d;
            _lastProgressTime = now;
        }
        else if (now - _lastProgressTime > StuckTimeout)
        {
            Stop("walk.stuck");
        }
    }

    private static bool IsArrived(Player player)
    {
        if (_target != null)
        {
            // For a payable object: arrived as soon as the game selects it (interaction point).
            var p = _target.GetComponent<Payable>();
            if (p != null && HoverAnnouncer.Selected(player) == p) return true;
        }
        return Mathf.Abs(TargetX() - GameState.PlayerX(player)) < ArriveDistance;
    }

    internal static bool ConsumeSprintStart()
    {
        if (!_run || _sprintStarted) return false;
        _sprintStarted = true;
        return true;
    }
}

/// <summary>
/// Injects the auto-walk direction into the player input processing.
/// The player pressing the opposite direction stops auto-walk; the same direction does not.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.UpdateActionState))]
internal static class PlayerInputPatch
{
    private static void Prefix(Player __instance, ref int __0, ref bool __1, ref bool __2, ref bool __3, ref bool __4)
    {
        if (AccessMod.ExternalKeyCapture)
        {
            // Another mod owns the keyboard (its menu is open): the arrows do not move the player.
            __0 = 0; __1 = false; __3 = false; __4 = false;
            return;
        }
        if (!AutoWalk.Active) return;
        var p1 = GameState.Player;
        if (p1 == null || __instance == null || __instance.Pointer != p1.Pointer) return;

        if (__0 == 0) AutoWalk.IgnoreUserInput = false;
        else if (!AutoWalk.IgnoreUserInput && AutoWalk.Direction != 0 && System.Math.Sign(__0) != AutoWalk.Direction)
        {
            AutoWalk.Stop("walk.cancelled");
            return;
        }

        __0 = AutoWalk.Direction;
        if (AutoWalk.Run)
        {
            if (AutoWalk.ConsumeSprintStart()) { __1 = true; __4 = true; }
            __2 = false;
            __3 = true;
        }
    }

    private static float _nextGallopTry;
    private static int _gallopTries;

    /// <summary>
    /// Sprint flags alone are not always enough: if the player is not running,
    /// ask the game to gallop, then force the "run" state as a last resort.
    /// </summary>
    private static void Postfix(Player __instance)
    {
        if (!AutoWalk.Active || !AutoWalk.Run || AutoWalk.Direction == 0) { _gallopTries = 0; return; }
        var p1 = GameState.Player;
        if (p1 == null || __instance == null || __instance.Pointer != p1.Pointer) return;

        try
        {
            if (__instance.isRunning || __instance.actionState == Player.ActionState.Run) { _gallopTries = 0; return; }
            var steed = __instance.steed;
            if (steed != null && steed.IsTired) return;

            float now = Time.unscaledTime;
            if (now < _nextGallopTry) return;
            _nextGallopTry = now + 0.4f;

            if (_gallopTries < 2) __instance.TryToGallop(AutoWalk.Direction);
            else __instance.SetActionState(Player.ActionState.Run, AutoWalk.Direction, false);
            _gallopTries++;
        }
        catch { }
    }
}
