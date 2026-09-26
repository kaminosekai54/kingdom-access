using System.Collections.Generic;
using System.Linq;
using KingdomAccess.Game;
using KingdomAccess.Localization;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Features;

/// <summary>Item of a browsable list.</summary>
internal sealed class NavEntry
{
    /// <summary>Fixed text (e.g. "8 archers"). If null, the text is Name + current distance.</summary>
    public string Text;
    public string Name;
    /// <summary>Object the walk shortcut goes to (may be null).</summary>
    public Component Target;
}

/// <summary>
/// Active list for the previous / next / repeat item shortcuts and auto-walk.
/// The last list opened (scanner, radar, census, help, summary, screen texts) stays active.
/// </summary>
internal static class ListNav
{
    private enum Mode { None, Scanner, Static }

    private static Mode _mode = Mode.None;
    private static List<NavEntry> _entries = new();
    private static int _index = -1;

    public static void Reset()
    {
        _mode = Mode.None;
        _entries = new List<NavEntry>();
        _index = -1;
        CategoryScanner.Reset();
    }

    public static void UseScanner() => _mode = Mode.Scanner;

    /// <summary>Opens a fixed list: speaks the summary; "next item" then reads the first item.</summary>
    public static void ShowStatic(List<NavEntry> entries, string summary)
    {
        _mode = Mode.Static;
        _entries = entries;
        _index = -1;
        SpeechOut.Say(summary);
    }

    public static void Move(Player player, int step, AccessSettings s)
    {
        if (_mode == Mode.Scanner || _mode == Mode.None)
        {
            if (player == null) return;
            CategoryScanner.ChangeItem(player, step, s);
            _mode = Mode.Scanner;
            return;
        }

        Prune();
        if (_entries.Count == 0) { SpeechOut.Say(Loc.T("scan.no_selection")); return; }
        _index = _index < 0 ? (step > 0 ? 0 : _entries.Count - 1)
                            : ((_index + step) % _entries.Count + _entries.Count) % _entries.Count;
        SpeechOut.Say(Read(player, _index));
    }

    public static void Repeat(Player player, AccessSettings s)
    {
        if (_mode == Mode.Scanner) { if (player != null) CategoryScanner.RepeatSelected(player, s); return; }
        Prune();
        if (_index < 0 || _index >= _entries.Count) { SpeechOut.Say(Loc.T("scan.no_selection")); return; }
        SpeechOut.Say(Read(player, _index));
    }

    public static Component SelectedTarget =>
        _mode == Mode.Scanner ? CategoryScanner.Selected
        : (_index >= 0 && _index < _entries.Count ? _entries[_index].Target : null);

    public static string SelectedName =>
        _mode == Mode.Scanner ? CategoryScanner.SelectedName
        : (_index >= 0 && _index < _entries.Count ? _entries[_index].Name : null);

    private static void Prune()
    {
        // Remove items whose target is gone (fixed texts stay).
        _entries.RemoveAll(e => e.Text == null && (e.Target == null || !e.Target.gameObject.activeInHierarchy));
        if (_index >= _entries.Count) _index = _entries.Count - 1;
    }

    private static string Read(Player player, int i)
    {
        var e = _entries[i];
        string text = e.Text ?? e.Name;
        if (e.Text == null && e.Target != null && player != null)
        {
            float dx = e.Target.transform.position.x - GameState.PlayerX(player);
            text += ", " + (Mathf.Abs(dx) < 1.5f ? Loc.T("dir.here") : Directions.DistanceSide(dx));
        }
        return $"{text}, {Loc.T("scan.position", i + 1, _entries.Count)}";
    }
}

/// <summary>Radar: the nearest object of each kind on each side.</summary>
internal static class Radar
{
    private static readonly HashSet<ObjKind> Ignored = new()
    {
        ObjKind.Unknown, ObjKind.Tree, ObjKind.Bush, ObjKind.Wall, ObjKind.Farmland, ObjKind.Upgrade
    };

    public static void Pulse(Player player, AccessSettings s)
    {
        float x = GameState.PlayerX(player);
        var nearest = new Dictionary<(string, bool), (float d, Component c)>();

        void Consider(Component c, string name)
        {
            float dx = c.transform.position.x - x;
            float d = Mathf.Abs(dx);
            if (d > s.RadarRange || d < 3f) return;
            if (!CaveNarrator.InPlayerZone(c.transform.position.x)) return;
            if (s.ScanExploredOnly && c.GetComponent<Castle>() == null && !Exploration.IsExplored(c.transform.position.x, s.ExploreMargin)) return;
            var key = (name, dx > 0);
            if (!nearest.TryGetValue(key, out var prev) || d < prev.d) nearest[key] = (d, c);
        }

        var payables = GameState.Payables;
        if (payables != null)
        {
            for (int i = 0; i < payables.Length; i++)
            {
                var p = payables[i];
                if (!GameState.IsAvailable(p) || GameState.IsPlayerOrSteed(player, p.gameObject)) continue;
                var info = ObjectNames.Identify(p);
                if (Ignored.Contains(info.Kind) || string.IsNullOrEmpty(info.Name)) continue;
                Consider(p, info.Name);
            }
        }
        foreach (var portal in UnitCache.All<Portal>(UnitKind.Portal))
            if (!ObjectNames.IsPortalDown(portal)) Consider(portal, ObjectNames.PortalName(portal));
        try
        {
            foreach (var nest in Object.FindObjectsByType<CaveEnemySpawner>(FindObjectsSortMode.None))
                if (nest != null && nest.gameObject.activeInHierarchy) Consider(nest, Loc.T("obj.cavenest"));
        }
        catch { }

        if (nearest.Count == 0) { ListNav.ShowStatic(new List<NavEntry>(), Loc.T("radar.none")); return; }

        var sorted = nearest.OrderBy(kv => kv.Value.d).ToList();
        var entries = sorted.Select(kv => new NavEntry { Name = kv.Key.Item1, Target = kv.Value.c }).ToList();
        string summary = string.Join(", ", sorted.Select(kv =>
            $"{kv.Key.Item1} {Directions.DistanceSide(kv.Key.Item2 ? kv.Value.d : -kv.Value.d)}"));
        ListNav.ShowStatic(entries, summary);
    }
}

/// <summary>Census of the troops, then browsable with the list keys.</summary>
internal static class Census
{
    private static readonly (UnitKind kind, string countKey, string nameKey)[] Order =
    {
        (UnitKind.Archer, "unit.archer", "unitname.archer"),
        (UnitKind.Worker, "unit.worker", "unitname.worker"),
        (UnitKind.Farmer, "unit.farmer", "unitname.farmer"),
        (UnitKind.Knight, "unit.knight", "unitname.knight"),
        (UnitKind.Pikeman, "unit.pikeman", "unitname.pikeman"),
        (UnitKind.Ninja, "unit.ninja", "unitname.ninja"),
        (UnitKind.Berserker, "unit.berserker", "unitname.berserker"),
        (UnitKind.Peasant, "unit.peasant", "unitname.peasant"),
        (UnitKind.Beggar, "unit.beggar", "obj.beggar")
    };

    public static void Show(Player player)
    {
        float x = GameState.PlayerX(player);
        var entries = new List<NavEntry>();
        int total = 0;
        foreach (var (kind, countKey, nameKey) in Order)
        {
            int n = UnitCache.Count(kind);
            if (n == 0) continue;
            total += kind == UnitKind.Beggar ? 0 : n;

            Component nearest = null;
            float best = float.MaxValue;
            int coins = 0;
            foreach (var u in UnitCache.All<Component>(kind))
            {
                float d = Mathf.Abs(u.transform.position.x - x);
                if (d < best) { best = d; nearest = u; }
                coins += GameState.CoinsOf(u);
            }
            string text = Loc.T(countKey, n);
            if (coins > 0) text += ", " + Loc.T("census.coins", HoverAnnouncer.Price(coins, CurrencyType.Coins));
            entries.Add(new NavEntry { Text = text, Name = Loc.T(nameKey), Target = nearest });
        }

        if (entries.Count == 0) { ListNav.ShowStatic(entries, Loc.T("report.no_units")); return; }
        string summary = Loc.T("census.total", total) + ". " + string.Join(", ", entries.Select(e => e.Text));
        ListNav.ShowStatic(entries, summary);
    }
}
