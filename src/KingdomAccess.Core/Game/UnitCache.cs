using System;
using System.Collections.Generic;
using UnityEngine;

namespace KingdomAccess.Game;

public enum UnitKind
{
    Archer, Worker, Knight, Ninja, Berserker, Peasant, Farmer, Pikeman, Beggar,
    Enemy, Castle, BeggarCamp, Wall, Portal, Ballista, Catapult
}

/// <summary>
/// Registry of active units and buildings, refreshed by polling.
/// Enemies are refreshed four times per second (enemy alerts need fresh data); every other
/// kind is refreshed about once per second, one kind per frame, to spread the cost.
/// No Harmony patches are used here on purpose: patching unit Awake methods conflicts with
/// other mods that patch the same methods (two mods patching them can crash the game at startup).
/// </summary>
internal static class UnitCache
{
    private sealed class Entry
    {
        public UnitKind Kind;
        public Func<List<Component>> FindAll;
        public List<Component> Items = new();
    }

    private const float EnemyInterval = 0.25f;
    private const float FullCycle = 1f;

    private static readonly List<Entry> Entries = new();
    private static readonly Dictionary<UnitKind, Entry> ByKind = new();
    private static IModLog _log;
    private static int _roundRobin;
    private static float _nextEnemyScan, _nextStep;

    private static void Add<T>(UnitKind kind) where T : Component
    {
        var e = new Entry
        {
            Kind = kind,
            FindAll = () =>
            {
                var list = new List<Component>();
                foreach (var c in UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None))
                    if (c != null) list.Add(c);
                return list;
            }
        };
        Entries.Add(e);
        ByKind[kind] = e;
    }

    internal static void Initialize(IModLog log)
    {
        _log = log;
        Add<Archer>(UnitKind.Archer);
        Add<Worker>(UnitKind.Worker);
        Add<Knight>(UnitKind.Knight);
        Add<Ninja>(UnitKind.Ninja);
        Add<Berserker>(UnitKind.Berserker);
        Add<Peasant>(UnitKind.Peasant);
        Add<Farmer>(UnitKind.Farmer);
        Add<Pikeman>(UnitKind.Pikeman);
        Add<Beggar>(UnitKind.Beggar);
        Add<Enemy>(UnitKind.Enemy);
        Add<Castle>(UnitKind.Castle);
        Add<BeggarCamp>(UnitKind.BeggarCamp);
        Add<Wall>(UnitKind.Wall);
        Add<Portal>(UnitKind.Portal);
        Add<Ballista>(UnitKind.Ballista);
        Add<Catapult>(UnitKind.Catapult);
    }

    /// <summary>Refreshes everything at once (used when an island is loaded).</summary>
    internal static void ColdBoot()
    {
        int count = 0;
        foreach (var e in Entries)
        {
            Refresh(e);
            count += e.Items.Count;
        }
        _log?.Info($"[Cache] {count} objects tracked after island load.");
    }

    /// <summary>Called every frame: incremental refresh.</summary>
    internal static void Tick(float now)
    {
        if (Entries.Count == 0) return;
        if (now >= _nextEnemyScan)
        {
            _nextEnemyScan = now + EnemyInterval;
            Refresh(ByKind[UnitKind.Enemy]);
        }
        if (now >= _nextStep)
        {
            _nextStep = now + FullCycle / Entries.Count;
            _roundRobin = (_roundRobin + 1) % Entries.Count;
            var e = Entries[_roundRobin];
            if (e.Kind != UnitKind.Enemy) Refresh(e);
        }
    }

    private static void Refresh(Entry e)
    {
        try { e.Items = e.FindAll(); }
        catch (Exception ex) { _log?.Warn($"[Cache] Could not scan {e.Kind}: {ex.Message}"); }
    }

    public static int Count(UnitKind kind)
    {
        var items = ByKind[kind].Items;
        items.RemoveAll(c => c == null);
        return items.Count;
    }

    public static IEnumerable<T> All<T>(UnitKind kind) where T : Component
    {
        var items = ByKind[kind].Items;
        items.RemoveAll(c => c == null);
        foreach (var c in items.ToArray())
        {
            var t = c.TryCast<T>();
            if (t != null) yield return t;
        }
    }
}
