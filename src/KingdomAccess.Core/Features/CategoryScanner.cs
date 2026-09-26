using System.Collections.Generic;
using System.Linq;
using KingdomAccess.Game;
using KingdomAccess.Localization;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Features;

public enum ScanCategory
{
    Buildable, Upgradable, Walls, Towers, Trees, Camps, Troops, Shops, Buildings, Mounts, Statues,
    Characters, Treasure, Travel, Puzzles, Cave, Enemies, Others
}

/// <summary>
/// Scanner by categories. Next / previous category (the list wraps around,
/// empty categories are skipped). Previous / next item: closer / farther.
/// </summary>
internal static class CategoryScanner
{
    private sealed class Item
    {
        public Component Obj;
        public string Name;
        public float X;
        public bool IsPayable;
    }

    private static readonly ScanCategory[] Order = (ScanCategory[])System.Enum.GetValues(typeof(ScanCategory));
    private static int _categoryIndex = -1;
    private static Component _selected;

    public static Component Selected => _selected != null ? _selected : null;
    public static string SelectedName { get; private set; }

    public static void Reset()
    {
        _selected = null;
        SelectedName = null;
    }

    public static void ChangeCategory(Player player, int step, AccessSettings s)
    {
        ListNav.UseScanner();
        for (int tries = 0; tries < Order.Length; tries++)
        {
            _categoryIndex = ((_categoryIndex + step) % Order.Length + Order.Length) % Order.Length;
            var items = Build(player, Order[_categoryIndex], s);
            if (items.Count == 0) continue;

            _selected = items[0].Obj;
            SelectedName = items[0].Name;
            SpeechOut.Say($"{CategoryName(Order[_categoryIndex])}, {items.Count}. {Describe(player, items, 0)}");
            return;
        }
        _selected = null;
        SpeechOut.Say(Loc.T("scan.nothing"));
    }

    public static void ChangeItem(Player player, int step, AccessSettings s)
    {
        if (_categoryIndex < 0) { ChangeCategory(player, 1, s); return; }

        var items = Build(player, Order[_categoryIndex], s);
        if (items.Count == 0)
        {
            SpeechOut.Say(Loc.T("scan.empty", CategoryName(Order[_categoryIndex])));
            _selected = null;
            return;
        }

        int current = _selected == null ? -1 : items.FindIndex(i => i.Obj == _selected);
        int next = current < 0 ? 0 : ((current + step) % items.Count + items.Count) % items.Count;
        _selected = items[next].Obj;
        SelectedName = items[next].Name;
        SpeechOut.Say(Describe(player, items, next));
    }

    public static void RepeatSelected(Player player, AccessSettings s)
    {
        if (_selected == null || _categoryIndex < 0) { SpeechOut.Say(Loc.T("scan.no_selection")); return; }
        var items = Build(player, Order[_categoryIndex], s);
        int i = items.FindIndex(it => it.Obj == _selected);
        if (i < 0) { SpeechOut.Say(Loc.T("scan.gone")); _selected = null; return; }
        SpeechOut.Say(Describe(player, items, i));
    }

    private static string Describe(Player player, List<Item> items, int index)
    {
        var it = items[index];
        string label = it.Name;
        if (it.IsPayable)
        {
            var p = it.Obj.GetComponent<Payable>();
            if (p != null) label = HoverAnnouncer.Describe(player, p) ?? it.Name;
        }
        float dx = it.X - GameState.PlayerX(player);
        string where = Mathf.Abs(dx) < 1.5f ? Loc.T("dir.here") : Directions.DistanceSide(dx);
        return $"{label}, {where}, {Loc.T("scan.position", index + 1, items.Count)}";
    }

    public static string CategoryName(ScanCategory c) => Loc.T("cat." + c.ToString().ToLowerInvariant());

    private static readonly UnitKind[] TroopKinds =
    {
        UnitKind.Archer, UnitKind.Worker, UnitKind.Farmer, UnitKind.Knight, UnitKind.Pikeman,
        UnitKind.Ninja, UnitKind.Berserker, UnitKind.Peasant
    };

    private static List<Item> Build(Player player, ScanCategory cat, AccessSettings s)
    {
        var items = new Dictionary<int, Item>();
        float px = GameState.PlayerX(player);

        void AddItem(Component c, string name, bool payable)
        {
            if (c == null) return;
            var go = c.gameObject;
            if (go == null || !go.activeInHierarchy || GameState.IsPlayerOrSteed(player, go)) return;
            float x = c.transform.position.x;
            // Beyond the cliff portal, only objects in that area matter.
            if (!CaveNarrator.InPlayerZone(x)) return;
            if (s.ScannerRange > 0f && Mathf.Abs(x - px) > s.ScannerRange) return;
            // Troops and enemies move around: no exploration filter for them.
            // The castle is always listed: it is the main landmark when arriving on an island.
            bool mobile = cat == ScanCategory.Troops || cat == ScanCategory.Enemies || c.GetComponent<Castle>() != null;
            if (!mobile && s.ScanExploredOnly && !Exploration.IsExplored(x, s.ExploreMargin)) return;
            int id = go.GetInstanceID();
            if (items.ContainsKey(id)) return;
            items[id] = new Item { Obj = c, Name = name, X = x, IsPayable = payable };
        }

        var payables = GameState.Payables;
        if (payables != null && cat != ScanCategory.Troops && cat != ScanCategory.Enemies)
        {
            for (int i = 0; i < payables.Length; i++)
            {
                var p = payables[i];
                if (!GameState.IsAvailable(p)) continue;

                if (cat == ScanCategory.Buildable || cat == ScanCategory.Upgradable)
                {
                    var a = HoverAnnouncer.Analyze(player, p);
                    if (!string.IsNullOrEmpty(a.LockText) || a.Price <= 0 || !GameState.CanPayNow(p)) continue;
                    string wanted = cat == ScanCategory.Buildable ? "act.build" : "act.upgrade";
                    if (a.ActionKey != wanted || a.Info.Kind == ObjKind.Unknown) continue;
                    AddItem(p, a.Name, true);
                    continue;
                }

                var info = ObjectNames.Identify(p);
                if (CategoryOf(info.Kind) != cat) continue;

                // Building spot not built yet (farm, wall, tower...): listed only if it
                // can be built now, and announced as such. Only for real buildings:
                // a puzzle or a character is never "to build".
                if (IsBuildingKind(info.Kind))
                {
                    var a = HoverAnnouncer.Analyze(player, p);
                    if (a.ActionKey == "act.build")
                    {
                        if (!string.IsNullOrEmpty(a.LockText) || !GameState.CanPayNow(p)) continue;
                        AddItem(p, info.Name + ", " + Loc.T("state.to_build"), true);
                        continue;
                    }
                }
                AddItem(p, info.Name, true);
            }
        }

        switch (cat)
        {
            case ScanCategory.Treasure:
                // Chests are not payable objects: look them up directly.
                foreach (var chest in Object.FindObjectsByType<Chest>(FindObjectsSortMode.None))
                {
                    if (chest == null || !chest.gameObject.activeInHierarchy) continue;
                    string content = "";
                    try { if (chest.currencyAmount > 0) content = ", " + HoverAnnouncer.Price(chest.currencyAmount, chest.currencyType); } catch { }
                    AddItem(chest, Loc.T("obj.chest") + content, false);
                }
                break;
            case ScanCategory.Cave:
                // Greed nests of the cave (they spawn the enemies) and the cliff portal.
                foreach (var nest in Object.FindObjectsByType<CaveEnemySpawner>(FindObjectsSortMode.None))
                    if (nest != null) AddItem(nest, Loc.T("obj.cavenest"), false);
                foreach (var p in UnitCache.All<Portal>(UnitKind.Portal))
                {
                    bool cliff = false;
                    try { cliff = p.type == Portal.Type.Cliff; } catch { }
                    if (cliff) AddItem(p, ObjectNames.PortalName(p), false);
                }
                break;
            case ScanCategory.Characters:
                var ghost = TutorialNarrator.CurrentGhost;
                if (ghost != null) AddItem(ghost, Loc.T("tutorial.ghost"), false);
                break;
            case ScanCategory.Travel:
                // The built boat is not necessarily a payable object: look it up directly.
                foreach (var boat in Object.FindObjectsByType<Boat>(FindObjectsSortMode.None))
                {
                    if (boat == null || !boat.gameObject.activeInHierarchy) continue;
                    if (boat.gameObject.name.ToLowerInvariant().Contains("wreck")) continue; // wreck not repaired yet
                    var pay = boat.GetComponent<Payable>();
                    AddItem(boat, Loc.T("obj.boat"), pay != null && GameState.IsAvailable(pay));
                }
                break;
            case ScanCategory.Walls:
                foreach (var w in UnitCache.All<Wall>(UnitKind.Wall))
                    AddItem(w, Loc.T("obj.wall"), GameState.IsAvailable(w.GetComponent<Payable>()));
                break;
            case ScanCategory.Camps:
                foreach (var c in UnitCache.All<BeggarCamp>(UnitKind.BeggarCamp)) AddItem(c, Loc.T("obj.beggarcamp"), false);
                foreach (var b in UnitCache.All<Beggar>(UnitKind.Beggar))
                    AddItem(b, Loc.T("obj.beggar"), GameState.IsAvailable(b.GetComponent<Payable>()));
                break;
            case ScanCategory.Troops:
                foreach (var kind in TroopKinds)
                {
                    string name = Loc.T("unitname." + kind.ToString().ToLowerInvariant());
                    foreach (var u in UnitCache.All<Component>(kind))
                    {
                        // Units that carry coins (mostly knights) announce them.
                        int coins = GameState.CoinsOf(u);
                        AddItem(u, coins > 0 ? $"{name}, {HoverAnnouncer.Price(coins, CurrencyType.Coins)}" : name, false);
                    }
                }
                break;
            case ScanCategory.Buildings:
                foreach (var c in UnitCache.All<Castle>(UnitKind.Castle))
                    AddItem(c, Loc.T("obj.castle"), GameState.IsAvailable(c.GetComponent<Payable>()));
                break;
            case ScanCategory.Puzzles:
                foreach (var c in DlcObjects.All(Time.unscaledTime))
                {
                    var info = DlcObjects.Identify(c.gameObject);
                    if (info.HasValue) AddItem(c, info.Value.Name, GameState.IsAvailable(c.GetComponent<Payable>()));
                }
                break;
            case ScanCategory.Enemies:
                foreach (var e in UnitCache.All<Enemy>(UnitKind.Enemy)) AddItem(e, EnemyNames.Name(e), false);
                foreach (var p in UnitCache.All<Portal>(UnitKind.Portal))
                {
                    if (s.HideDestroyedPortals && ObjectNames.IsPortalDown(p)) continue;
                    AddItem(p, ObjectNames.PortalName(p), false);
                }
                break;
        }

        return items.Values.OrderBy(it => Mathf.Abs(it.X - px)).ToList();
    }

    /// <summary>Kinds that can exist as an empty spot, to be built.</summary>
    private static bool IsBuildingKind(ObjKind k) => k is ObjKind.Wall or ObjKind.Tower or ObjKind.Farmhouse
        or ObjKind.Farmland or ObjKind.Wharf or ObjKind.Lighthouse or ObjKind.Quarry or ObjKind.Mine
        or ObjKind.Bakery or ObjKind.Stable or ObjKind.Dojo or ObjKind.CitizenHouse or ObjKind.Ballista
        or ObjKind.Horn or ObjKind.Workshop or ObjKind.Forge or ObjKind.Upgrade;

    private static ScanCategory? CategoryOf(ObjKind k) => k switch
    {
        ObjKind.Wall or ObjKind.Horn => ScanCategory.Walls,
        ObjKind.Tower or ObjKind.Ballista => ScanCategory.Towers,
        ObjKind.Tree => ScanCategory.Trees,
        ObjKind.BeggarCamp or ObjKind.Beggar => ScanCategory.Camps,
        ObjKind.Shop or ObjKind.Workshop or ObjKind.Bomb or ObjKind.Forge or ObjKind.Shield => ScanCategory.Shops,
        ObjKind.Castle or ObjKind.Farmhouse or ObjKind.Farmland or ObjKind.Lighthouse
            or ObjKind.Quarry or ObjKind.Mine or ObjKind.Bakery or ObjKind.Stable or ObjKind.Dojo
            or ObjKind.CitizenHouse or ObjKind.Upgrade or ObjKind.Bell or ObjKind.Banner => ScanCategory.Buildings,
        ObjKind.Steed => ScanCategory.Mounts,
        ObjKind.Statue or ObjKind.CrownStatue => ScanCategory.Statues,
        ObjKind.Hermit or ObjKind.Merchant or ObjKind.Banker or ObjKind.Dog => ScanCategory.Characters,
        ObjKind.Chest or ObjKind.GemChest or ObjKind.GemGuard => ScanCategory.Treasure,
        ObjKind.Boat or ObjKind.Wharf or ObjKind.Teleporter => ScanCategory.Travel,
        // Portals come from the unit cache (with their state), not from the payable list.
        ObjKind.Portal => null,
        ObjKind.Puzzle => ScanCategory.Puzzles,
        ObjKind.CaveBomb or ObjKind.CaveNest => ScanCategory.Cave,
        _ => ScanCategory.Others
    };
}

/// <summary>Enemy names by type (the game EnemyType).</summary>
internal static class EnemyNames
{
    public static string Name(Enemy e)
    {
        try
        {
            string key = "enemy." + e.Type.ToString().ToLowerInvariant();
            string t = Loc.TryT(key);
            if (t != null) return t;
        }
        catch { }
        return Loc.T("obj.enemy");
    }
}
