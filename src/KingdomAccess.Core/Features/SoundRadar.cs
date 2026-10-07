using System;
using System.Collections.Generic;
using KingdomAccess.Game;
using KingdomAccess.Localization;
using KingdomAccess.Speech;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Features;

/// <summary>
/// Sound radar: plays the earcon of every useful object within range, one after the other with
/// a short delay, nearest first. The volume drops with the distance, and each sound is played
/// only in the ear of the side where the object is (whatever the monarch's facing). The range
/// comes from the configuration and can be switched in game between 30, 50 and 100.
/// Also hosts a tiny scheduler for delayed sounds and the sound legend.
/// </summary>
internal static class SoundRadar
{
    private static readonly float[] Steps = { 30f, 50f, 100f };
    private static float _delay = 0.45f;
    private const int MaxItems = 20;

    private static float _range = 30f;
    private static readonly List<(float at, Action play)> Scheduled = new();

    public static void Initialize(AccessSettings s)
    {
        _range = s.SoundRadarRange > 0 ? s.SoundRadarRange : 30f;
        _delay = Mathf.Clamp(s.SoundRadarDelay, 0.1f, 3f);
    }

    /// <summary>Runs a sound later (in seconds). Used by the radar and the hover chime.</summary>
    public static void Later(float delay, Action play) => Scheduled.Add((Time.unscaledTime + delay, play));

    public static void Tick(float now)
    {
        for (int i = Scheduled.Count - 1; i >= 0; i--)
        {
            if (Scheduled[i].at > now) continue;
            var a = Scheduled[i].play;
            Scheduled.RemoveAt(i);
            try { a(); } catch { }
        }
    }

    public static void Stop() => Scheduled.Clear();

    /// <summary>Switches the range: 30, 50, 100, then 30 again.</summary>
    public static void CycleRange()
    {
        int i = Array.FindIndex(Steps, v => v >= _range - 0.5f);
        _range = Steps[(i < 0 ? 0 : i + 1) % Steps.Length];
        SpeechOut.Say(Loc.T("soundradar.range", Mathf.RoundToInt(_range)));
    }

    private sealed class Item
    {
        public Component Obj;
        public string Key;
        public Steed Steed;
        public float Dx;
    }

    /// <summary>Plays the earcons of the objects within range, nearest first.</summary>
    public static void Scan(Player player)
    {
        Stop();
        float px = GameState.PlayerX(player);
        var items = Collect(player, px);
        items.Sort((a, b) => Math.Abs(a.Dx).CompareTo(Math.Abs(b.Dx)));
        if (items.Count == 0) { SpeechOut.Say(Loc.T("soundradar.none", Mathf.RoundToInt(_range))); return; }

        int n = Math.Min(items.Count, MaxItems);
        for (int i = 0; i < n; i++)
        {
            var it = items[i];
            float d = Math.Abs(it.Dx);
            float volume = Mathf.Lerp(1f, 0.15f, Mathf.Clamp01(d / _range));
            float pan = d < 1f ? 0f : Math.Sign(it.Dx);
            Later(i * _delay, () =>
            {
                if (it.Steed != null) Earcons.PlayMount(it.Steed, volume, pan);
                else Earcons.Play(it.Key, volume, pan);
            });
        }
    }

    private static List<Item> Collect(Player player, float px)
    {
        var list = new List<Item>();
        var seen = new HashSet<IntPtr>();
        void Add(Component c, ObjKind kind, string actionKey)
        {
            if (c == null || !c.gameObject.activeInHierarchy || !seen.Add(c.gameObject.Pointer)) return;
            float dx = c.transform.position.x - px;
            if (Math.Abs(dx) > _range || !CaveNarrator.InPlayerZone(c.transform.position.x)) return;
            Steed steed = null;
            if (kind == ObjKind.Steed) { try { steed = c.GetComponentInChildren<Steed>(true); } catch { } }
            list.Add(new Item { Obj = c, Key = Earcons.KeyFor(c, kind, actionKey), Steed = steed, Dx = dx });
        }

        var payables = GameState.Payables;
        if (payables != null)
        {
            for (int i = 0; i < payables.Length; i++)
            {
                var p = payables[i];
                if (p == null || GameState.IsPlayerOrSteed(player, p.gameObject) || !GameState.IsAvailable(p)) continue;
                var a = HoverAnnouncer.Analyze(player, p);
                switch (a.Info.Kind)
                {
                    case ObjKind.Bush: case ObjKind.Farmland: case ObjKind.Unknown: continue;
                    case ObjKind.Tree: if (!CategoryScanner.IsCuttable(p)) continue; break;
                }
                Add(p, a.Info.Kind, a.ActionKey);
            }
        }
        foreach (var portal in UnitCache.All<Portal>(UnitKind.Portal))
            if (!ObjectNames.IsPortalDown(portal)) Add(portal, ObjKind.Portal, null);
        foreach (var e in UnitCache.All<Component>(UnitKind.Enemy)) Add(e, ObjKind.Portal, null);
        try
        {
            foreach (var chest in UnityEngine.Object.FindObjectsByType<Chest>(FindObjectsSortMode.None)) Add(chest, ObjKind.Chest, null);
            foreach (var nest in UnityEngine.Object.FindObjectsByType<CaveEnemySpawner>(FindObjectsSortMode.None)) Add(nest, ObjKind.CaveNest, null);
        }
        catch { }
        foreach (var d in DlcObjects.All(Time.unscaledTime))
        {
            string type = null;
            try { type = d.GetIl2CppType().Name; } catch { }
            if (type == null || type.EndsWith("Controller")) continue;
            Add(d, ObjKind.Puzzle, null);
        }
        return list;
    }

    /// <summary>Sound legend: every sound with its name, browsable with the list shortcuts.</summary>
    public static void ShowLegend()
    {
        var entries = new List<NavEntry>();
        foreach (var k in Earcons.Legend)
        {
            string key = k;
            string name = LegendName(key);
            entries.Add(new NavEntry { Text = name, Name = name, OnRead = () => Earcons.Play(key, 0.9f, 0f) });
        }
        ListNav.ShowStatic(entries, Loc.T("soundradar.legend", entries.Count, AccessMod.KeyName("PreviousItem"), AccessMod.KeyName("NextItem")));
    }

    /// <summary>"Wall, to build", "Bow shop"...</summary>
    private static string LegendName(string key)
    {
        string state = null, family = key;
        if (key.EndsWith("_build")) { state = Loc.T("earcon.state.build"); family = key.Substring(0, key.Length - 6); }
        else if (key.EndsWith("_upgrade")) { state = Loc.T("earcon.state.upgrade"); family = key.Substring(0, key.Length - 8); }
        string name = family.StartsWith("shop_") ? Loc.T("shop." + family.Substring(5)) : (Loc.TryT("earcon." + family) ?? family);
        return state == null ? name : $"{name}, {state}";
    }
}
