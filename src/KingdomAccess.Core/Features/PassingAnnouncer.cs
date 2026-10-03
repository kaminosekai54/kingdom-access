using System.Collections.Generic;
using KingdomAccess.Game;
using KingdomAccess.Localization;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Features;

/// <summary>
/// While galloping the game selects nothing, so the usual hover announcement stays silent.
/// This announces every useful object the player rides past: payables (castle, shops, merchant,
/// mounts, statues, puzzles, walls...), forest-edge trees, portals, chests, Greed nests, the
/// boat and non-payable puzzle objects. It checks the whole stretch covered since the last
/// frame, so nothing is skipped at full speed. Each object is announced at most once every
/// few seconds.
/// </summary>
internal static class PassingAnnouncer
{
    private sealed class Candidate
    {
        public Component Obj;
        public string Name;
        public bool IsPayable;
    }

    private const float Margin = 0.8f;
    private const float RepeatAfter = 8f;
    private const float RefreshEvery = 1f;

    private static readonly List<Candidate> Candidates = new();
    private static readonly Dictionary<System.IntPtr, float> LastSaid = new();
    private static float _nextRefresh;
    private static float? _lastX;

    public static void Reset() { Candidates.Clear(); LastSaid.Clear(); _lastX = null; _nextRefresh = 0f; }

    public static void Tick(Player player, AccessSettings s, float now)
    {
        float x = GameState.PlayerX(player);
        if (!IsRunning(player)) { _lastX = x; return; }
        if (now >= _nextRefresh) { _nextRefresh = now + RefreshEvery; Refresh(player); }

        float from = _lastX ?? x;
        _lastX = x;
        float min = Mathf.Min(from, x) - Margin, max = Mathf.Max(from, x) + Margin;

        // Nearest first, in the direction of travel.
        Candidate best = null;
        float bestD = float.MaxValue;
        foreach (var c in Candidates)
        {
            if (c.Obj == null) continue;
            float cx = c.Obj.transform.position.x;
            if (cx < min || cx > max) continue;
            if (LastSaid.TryGetValue(c.Obj.Pointer, out float t) && now - t < RepeatAfter) continue;
            float d = Mathf.Abs(cx - x);
            if (d < bestD) { bestD = d; best = c; }
        }
        if (best == null) return;
        LastSaid[best.Obj.Pointer] = now;

        string text = best.Name;
        if (best.IsPayable)
        {
            var p = best.Obj.TryCast<Payable>();
            string full = p != null ? HoverAnnouncer.DescribeCore(player, p) : null;
            if (!string.IsNullOrEmpty(full)) text = full;
        }
        SpeechOut.Say(text, true); // each new object passed cuts the previous one
    }

    private static bool IsRunning(Player player)
    {
        try { return player.isRunning || player.actionState == Player.ActionState.Run; }
        catch { return false; }
    }

    /// <summary>Rebuilds the list of useful objects (positions barely change, once per second is enough).</summary>
    private static void Refresh(Player player)
    {
        Candidates.Clear();
        var seen = new HashSet<System.IntPtr>();
        void Add(Component c, string name, bool payable)
        {
            if (c == null || !c.gameObject.activeInHierarchy || !seen.Add(c.gameObject.Pointer)) return;
            if (!CaveNarrator.InPlayerZone(c.transform.position.x)) return;
            Candidates.Add(new Candidate { Obj = c, Name = name, IsPayable = payable });
        }

        var payables = GameState.Payables;
        if (payables != null)
        {
            for (int i = 0; i < payables.Length; i++)
            {
                var p = payables[i];
                if (p == null || GameState.IsPlayerOrSteed(player, p.gameObject) || !GameState.IsAvailable(p)) continue;
                var info = ObjectNames.Identify(p);
                switch (info.Kind)
                {
                    case ObjKind.Bush:
                    case ObjKind.Farmland:
                    case ObjKind.Unknown:
                        continue;
                    case ObjKind.Tree:
                        if (!CategoryScanner.IsCuttable(p)) continue;
                        break;
                }
                Add(p, info.Name, true);
            }
        }

        foreach (var portal in UnitCache.All<Portal>(UnitKind.Portal))
            Add(portal, ObjectNames.PortalName(portal), false);
        try
        {
            foreach (var chest in Object.FindObjectsByType<Chest>(FindObjectsSortMode.None))
                Add(chest, Loc.T("obj.chest"), false);
            foreach (var boat in Object.FindObjectsByType<Boat>(FindObjectsSortMode.None))
                if (!boat.gameObject.name.ToLowerInvariant().Contains("wreck")) Add(boat, ObjectNames.Identify(boat).Name, false);
            foreach (var nest in Object.FindObjectsByType<CaveEnemySpawner>(FindObjectsSortMode.None))
                Add(nest, Loc.T("obj.cavenest"), false);
        }
        catch { }
        foreach (var d in DlcObjects.All(Time.unscaledTime))
        {
            string type = null;
            try { type = d.GetIl2CppType().Name; } catch { }
            if (type == null || type.EndsWith("Controller")) continue; // invisible puzzle logic
            var info = DlcObjects.Identify(d.gameObject);
            if (info.HasValue) Add(d, info.Value.Name, d.GetComponent<Payable>() != null);
        }
    }
}
