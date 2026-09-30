using System.Collections.Generic;
using System.Text.RegularExpressions;
using KingdomAccess.Localization;
using UnityEngine;

namespace KingdomAccess.Game;

/// <summary>Object categories, used for the name, the proposed action and the radar.</summary>
public enum ObjKind
{
    Unknown, Castle, Wall, Tower, Shop, Tree, Beggar, BeggarCamp, Hermit, Merchant, Chest,
    GemChest, GemGuard, Banker, Portal, Teleporter, Statue, CrownStatue, Steed, Boat, Wharf,
    Farmhouse, Farmland, Workshop, Bush, Lighthouse, Horn, Shield, Bomb, Dog, Quarry, Mine,
    Bakery, Stable, Forge, Dojo, Ballista, CitizenHouse, Banner, Bell, Upgrade, Puzzle, CaveBomb, CaveNest,
    Oracle, Shipyard, Border
}

public struct ObjInfo
{
    public ObjKind Kind;
    public string Name;
}

/// <summary>
/// Gives game objects a readable, translated name. Game components come first
/// (reliable, independent of internal names), then the cleaned object name.
/// </summary>
internal static class ObjectNames
{
    public static ObjInfo Identify(Component c)
    {
        var go = c.gameObject;

        // Shops: the exact type is given by ShopTag.
        var tag = go.GetComponent<ShopTag>();
        if (tag != null)
        {
            string shop = ShopName(tag.type);
            if (shop != null) return Info(ObjKind.Shop, shop);
        }

        var dlc = DlcObjects.Identify(go);
        if (dlc.HasValue) return dlc.Value;

        // Olympus objects are recognised by component name: touching an Olympus type in another
        // world (where it was never loaded) can crash the IL2CPP runtime.
        var names = ComponentNames(go);
        if (names.Contains("Oracle")) return Named(ObjKind.Oracle);
        if (names.Contains("Shipyard")) return Named(ObjKind.Shipyard);
        if (names.Contains("PayableBorder")) return Named(ObjKind.Border);

        if (go.GetComponent<Castle>() != null) return Named(ObjKind.Castle);
        if (go.GetComponent<Wall>() != null) return Named(ObjKind.Wall);
        if (go.GetComponent<Tower>() != null) return Named(ObjKind.Tower);
        if (go.GetComponent<PayableTree>() != null) return Named(ObjKind.Tree);
        if (go.GetComponent<BeggarCamp>() != null) return Named(ObjKind.BeggarCamp);
        if (go.GetComponent<Beggar>() != null) return Named(ObjKind.Beggar);
        if (go.GetComponent<Hermit>() != null) return Named(ObjKind.Hermit);
        if (go.GetComponent<Merchant>() != null) return Named(ObjKind.Merchant);
        if (go.GetComponent<PayableGemChest>() != null) return Named(ObjKind.GemChest);
        if (go.GetComponent<PayableGemGuard>() != null) return Named(ObjKind.GemGuard);
        if (go.GetComponent<Banker>() != null) return Named(ObjKind.Banker);
        if (go.GetComponent<Chest>() != null) return Named(ObjKind.Chest);
        var portal = go.GetComponent<Portal>();
        if (portal != null) return Info(ObjKind.Portal, PortalName(portal));
        if (go.GetComponent<PayableTeleporter>() != null) return Named(ObjKind.Teleporter);
        if (go.GetComponent<PayableCrownStatue>() != null || go.GetComponent<UnlockNewRulerStatue>() != null) return Named(ObjKind.CrownStatue);
        if (go.GetComponent<Statue>() != null) return Info(ObjKind.Statue, StatueName(go.GetComponent<Statue>()));
        if (go.GetComponent<SteedSpawn>() != null) return Info(ObjKind.Steed, SteedName(go.GetComponent<SteedSpawn>()));
        // A mount standing in the world, waiting to be bought or ridden (e.g. the Olympus griffin).
        var steed = go.GetComponent<Steed>();
        if (steed != null) return Info(ObjKind.Steed, Loc.T("steed.format", SteedTypeName(steed)));
        if (go.GetComponent<Boat>() != null || go.GetComponent<PayableBoat>() != null || go.GetComponent<BoatSailPosition>() != null)
            return Info(ObjKind.Boat, BoatName(c, go));
        if (go.GetComponent<Wharf>() != null) return Named(ObjKind.Wharf);
        if (go.GetComponent<Farmhouse>() != null) return Named(ObjKind.Farmhouse);
        if (go.GetComponent<Farmland>() != null) return Named(ObjKind.Farmland);
        if (go.GetComponent<PayableWorkshop>() != null) return Named(ObjKind.Workshop);
        if (go.GetComponent<PayableBush>() != null) return Named(ObjKind.Bush);
        if (go.GetComponent<Lighthouse>() != null) return Named(ObjKind.Lighthouse);
        if (go.GetComponent<PayableHorn>() != null) return Named(ObjKind.Horn);
        if (go.GetComponent<PayableShield>() != null) return Named(ObjKind.Shield);
        if (go.GetComponent<Bomb>() != null) return Info(ObjKind.CaveBomb, Loc.T("obj.cavebomb"));
        if (go.GetComponent<PayableBombPurchase>() != null) return Named(ObjKind.Bomb);
        if (go.GetComponent<Dog>() != null) return Named(ObjKind.Dog);
        if (go.GetComponent<Ballista>() != null) return Named(ObjKind.Ballista);

        return FromName(go.name);
    }

    // ---------- Names by type ----------

    private static ObjInfo Named(ObjKind kind) => Info(kind, Loc.T("obj." + kind.ToString().ToLowerInvariant()));
    private static ObjInfo Info(ObjKind kind, string name) => new() { Kind = kind, Name = name };

    public static string ShopName(PayableShop.ShopType type)
    {
        switch (type)
        {
            case PayableShop.ShopType.Bow: return Loc.T("shop.bow");
            case PayableShop.ShopType.Hammer: return Loc.T("shop.hammer");
            case PayableShop.ShopType.Scythe: return Loc.T("shop.scythe");
            case PayableShop.ShopType.PikeLeft:
            case PayableShop.ShopType.PikeRight: return Loc.T("shop.pike");
            case PayableShop.ShopType.ShieldShopLeft:
            case PayableShop.ShopType.ShieldShopRight: return Loc.T("shop.shield");
            case PayableShop.ShopType.Forge: return Loc.T("shop.forge");
            case PayableShop.ShopType.NinjaLeft:
            case PayableShop.ShopType.NinjaRight: return Loc.T("shop.ninja");
            case PayableShop.ShopType.WorkshopLeft:
            case PayableShop.ShopType.WorkshopRight: return Loc.T("shop.workshop");
            default: return null;
        }
    }

    /// <summary>Il2Cpp type names of every component of an object (no type is touched).</summary>
    private static HashSet<string> ComponentNames(GameObject go)
    {
        var set = new HashSet<string>();
        try
        {
            foreach (var comp in go.GetComponents<Component>())
                if (comp != null) set.Add(comp.GetIl2CppType().Name);
        }
        catch { }
        return set;
    }

    /// <summary>
    /// Distinct names for the boat parts: wreck, hull being built, set-sail point, the boat itself.
    /// </summary>
    private static string BoatName(Component c, GameObject go)
    {
        try
        {
            if (go.name.ToLowerInvariant().Contains("wreck")) return Loc.T("boat.wreck");
            if (go.GetComponent<BoatSailPosition>() != null)
            {
                var p = go.GetComponent<Payable>();
                return Loc.T(p != null && p.Price >= 10 ? "boat.sail" : "boat.build");
            }
        }
        catch { }
        return Loc.T("obj.boat");
    }

    private static string StatueName(Statue s)
    {
        string deity = s.deity.ToString().ToLowerInvariant();
        return Loc.TryT("statue." + deity) ?? Loc.T("obj.statue");
    }

    /// <summary>Portal name with its state (open, destroyed, rebuilding...).</summary>
    public static string PortalName(Portal portal)
    {
        string state;
        try { state = portal.state.ToString().ToLowerInvariant(); }
        catch { return Loc.T("obj.portal"); }
        // The big cliff portal leads to the Greed cave: its own name and states.
        bool cliff = false;
        try { cliff = portal.type == Portal.Type.Cliff; } catch { }
        if (cliff) return Loc.TryT("portal.cliff." + state) ?? Loc.T("portal.cliff");
        return Loc.TryT("portal." + state) ?? Loc.T("obj.portal");
    }

    /// <summary>True if the portal is destroyed or crumbled (no longer a threat).</summary>
    public static bool IsPortalDown(Portal portal)
    {
        try
        {
            var s = portal.state;
            return s == Portal.State.Destroyed || s == Portal.State.Crumbled || s == Portal.State.Passable;
        }
        catch { return false; }
    }

    private static string SteedName(SteedSpawn spawn)
    {
        try
        {
            var steeds = spawn.steeds;
            if (steeds != null && steeds.Length > 0 && steeds[0] != null)
                return Loc.T("steed.format", SteedTypeName(steeds[0]));
        }
        catch { }
        return Loc.T("obj.steed");
    }

    public static string SteedTypeName(Steed steed)
    {
        string type = steed.steedType.ToString();
        // P1Griffin, P2Griffin -> griffin ; Reindeer_Norselands -> reindeer
        string key = type.ToLowerInvariant().Replace("_norselands", "");
        if (key.StartsWith("p1") || key.StartsWith("p2")) key = key.Substring(2);
        return Loc.TryT("steed." + key) ?? SplitWords(type.Replace("P1", "").Replace("P2", "").Replace("_", " "));
    }

    // ---------- Fallback: cleaned Unity object name ----------

    private static readonly Regex Paren = new(@"\s*\(.*?\)", RegexOptions.Compiled);
    private static readonly Regex PNumber = new(@"\sP\d+", RegexOptions.Compiled);
    private static readonly Regex DigitsDash = new(@"[\d-]", RegexOptions.Compiled);
    private static readonly Regex TrailingUpper = new(@"\s[A-Z]$", RegexOptions.Compiled);
    private static readonly Regex Camel = new("([a-z])([A-Z])", RegexOptions.Compiled);
    private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex Biome = new(@"(?i)\b(dead ?lands|deadlands|norselands|shogun|olympus|dynasty|viking|challenge|bamboo|jade|norse|greece|europe|dire|plague)\b", RegexOptions.Compiled);

    // Internal name keywords -> category (from most specific to most general).
    // "wall" and "tower" come before "wreck": a wall ruin is not a boat.
    private static readonly (string word, ObjKind kind)[] Keywords =
    {
        ("quarry", ObjKind.Quarry), ("mine", ObjKind.Mine), ("bakery", ObjKind.Bakery),
        ("stable", ObjKind.Stable), ("forge", ObjKind.Forge), ("dojo", ObjKind.Dojo),
        ("citizen house", ObjKind.CitizenHouse), ("lighthouse", ObjKind.Lighthouse),
        ("banner", ObjKind.Banner), ("bbb", ObjKind.Banner), ("bell", ObjKind.Bell),
        ("ballista", ObjKind.Ballista), ("horn", ObjKind.Horn),
        ("wall", ObjKind.Wall), ("tower", ObjKind.Tower), ("castle", ObjKind.Castle),
        ("wharf", ObjKind.Wharf), ("boat", ObjKind.Boat), ("ship", ObjKind.Boat),
        ("portal", ObjKind.Portal), ("teleporter", ObjKind.Teleporter), ("statue", ObjKind.Statue),
        ("hermit", ObjKind.Hermit), ("beggar", ObjKind.Beggar), ("merchant", ObjKind.Merchant),
        ("chest", ObjKind.Chest), ("tree", ObjKind.Tree), ("bush", ObjKind.Bush),
        ("farm", ObjKind.Farmhouse), ("scaffold", ObjKind.Upgrade)
    };

    public static ObjInfo FromName(string raw)
    {
        string clean = CleanName(raw);
        string lower = clean.ToLowerInvariant();
        foreach (var (word, kind) in Keywords)
            if (lower.Contains(word)) return Named(kind);
        return Info(ObjKind.Unknown, clean);
    }

    public static string CleanName(string original)
    {
        if (string.IsNullOrEmpty(original)) return "";
        string t = original.Replace("(Clone)", "").Replace("_", " ");
        t = Paren.Replace(t, "");
        t = PNumber.Replace(t, "");
        t = DigitsDash.Replace(t, "");
        t = TrailingUpper.Replace(t, "");
        t = Camel.Replace(t, "$1 $2");
        t = Biome.Replace(t, "");
        return Spaces.Replace(t, " ").Trim();
    }

    private static string SplitWords(string s) => Camel.Replace(s ?? "", "$1 $2");
}
