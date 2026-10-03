using System.Collections.Generic;
using KingdomAccess.Game;
using KingdomAccess.Localization;
using KingdomAccess.Speech;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Features;

/// <summary>
/// Announces the payable object selected by the game (the real interaction point):
/// name, price, action, or lock reason.
/// </summary>
internal static class HoverAnnouncer
{
    // Debounce: the object must stay selected for a moment before being announced,
    // and an object left then found again right away is not announced again.
    private const float SettleDelay = 0.05f;
    private const float RepeatSuppression = 4f;

    private static Payable _last;
    private static string _lastMessage = "";
    private static Payable _candidate;
    private static float _candidateSince;
    private static Payable _lastSpokenTarget;
    private static string _lastSpokenMessage = "";
    private static float _lastSpokenAt = -100f;

    public static void Reset()
    {
        _last = null; _lastMessage = "";
        _candidate = null; _candidateSince = 0f;
        _lastSpokenTarget = null; _lastSpokenMessage = ""; _lastSpokenAt = -100f;
    }

    public static void Tick(Player player, AccessSettings s, float now)
    {
        Payable target = Selected(player);
        if (target == null)
        {
            _candidate = null;
            _last = null;
            _lastMessage = "";
            return;
        }

        if (target != _candidate)
        {
            _candidate = target;
            _candidateSince = now;
        }
        if (now - _candidateSince < SettleDelay) return;

        string message = DescribeCore(player, target);
        if (message == null) return;

        bool sameAsLastSpoken = target == _lastSpokenTarget && message == _lastSpokenMessage;
        if (target != _last && sameAsLastSpoken && now - _lastSpokenAt < RepeatSuppression)
        {
            _last = target;
            _lastMessage = message;
            return;
        }

        if (target != _last || message != _lastMessage)
        {
            // A new object cuts the previous announcement; an update of the same object waits.
            bool isNew = target != _last;
            SpeechOut.Say(message, isNew);
            if (isNew && s.PaySound && CanPayHere(player, target)) GameAudio.Chime();
            _lastMessage = message;
            _lastSpokenMessage = message;
            _lastSpokenTarget = target;
            _lastSpokenAt = now;
        }
        _last = target;
    }

    /// <summary>Object selected by the game for interaction, or null.</summary>
    public static Payable Selected(Player player)
    {
        try
        {
            Component sel = player.selectedPayable;
            if (sel == null) return null;
            var p = sel.GetComponent<Payable>();
            if (p == null || GameState.IsPlayerOrSteed(player, p.gameObject)) return null;
            return p;
        }
        catch { return null; }
    }

    /// <summary>Object selected by the game, otherwise the nearest available payable.</summary>
    public static Payable SelectedOrClosest(Player player, float range)
    {
        var sel = Selected(player);
        if (sel != null) return sel;

        var payables = GameState.Payables;
        if (payables == null) return null;
        float x = GameState.PlayerX(player);
        Payable best = null;
        float bestDist = range;
        for (int i = 0; i < payables.Length; i++)
        {
            var p = payables[i];
            if (!GameState.IsAvailable(p)) continue;
            float d = Mathf.Abs(p.transform.position.x - x);
            if (d >= bestDist) continue;
            var go = p.gameObject;
            if (GameState.IsPlayerOrSteed(player, go) || go.GetComponent<Player>() != null) continue;
            best = p;
            bestDist = d;
        }
        return best;
    }

    /// <summary>Analysis of a payable: identity, lock, price and action key.</summary>
    public struct Analysis
    {
        public ObjInfo Info;
        public string Name;
        public string LockText;
        public int Price;
        public CurrencyType Currency;
        public string ActionKey;
    }

    public static Analysis Analyze(Player player, Payable p)
    {
        var a = new Analysis { Info = ObjectNames.Identify(p), Currency = CurrencyType.Coins };
        a.Name = string.IsNullOrEmpty(a.Info.Name) ? ObjectNames.CleanName(p.gameObject.name) : a.Info.Name;

        a.LockText = LockText(player, p);
        if (a.Info.Kind == ObjKind.Tree && Zones.TreeProtectsCamp(p.transform.position.x))
            a.LockText = Loc.T("warn.tree_camp");

        try { a.Price = p.Price; a.Currency = p.Currency; } catch { }
        a.ActionKey = ActionKey(a.Info.Kind, p, a.Price, a.Currency);
        return a;
    }

    /// <summary>Full sentence for a payable.</summary>
    public static string Describe(Player player, Payable p)
    {
        string text = DescribeCore(player, p);
        // Expedition bomb: where the cave entrance and the detonation point are.
        if (text != null && p.GetComponent<Bomb>() != null)
        {
            string extra = CaveNarrator.BombDetails(player, p.gameObject);
            if (!string.IsNullOrEmpty(extra)) text += ", " + extra;
        }
        return text;
    }

    /// <summary>
    /// Full sentence for a payable, always in the same order: name and level, price, action
    /// (with the target level for an upgrade), then why it cannot be paid right now or how many
    /// coins are missing. Example: "Wall, level 2, 6 coins, upgrade to level 3, 2 coins missing".
    /// </summary>
    internal static string DescribeCore(Player player, Payable p)
    {
        var a = Analyze(player, p);
        var parts = new List<string>();

        int level = LevelOf(p.gameObject);
        parts.Add(level > 0 ? Loc.T("hover.name_level", a.Name, level) : a.Name);

        bool freeAction = a.Info.Kind is ObjKind.GemChest or ObjKind.GemGuard or ObjKind.Banker;
        if (a.Price > 0 || freeAction) parts.Add(Price(a.Price, a.Currency));

        if (a.ActionKey != null)
            parts.Add(a.ActionKey == "act.upgrade" && level > 0 ? Loc.T("act.upgrade_to", level + 1) : Loc.T(a.ActionKey));

        if (!string.IsNullOrEmpty(a.LockText)) parts.Add(a.LockText);
        else if (!GameState.CanPayNow(p))
        {
            // The game's "can pay" answer is only meaningful when the player stands at the object,
            // and it is wrong for the boat (refused although sailing works): precise reasons are
            // always given, the generic "unavailable right now" only in front of the object.
            string reason = DlcObjects.WhyUnavailable(p.gameObject, player) ?? Missing(player, a);
            bool atObject = Selected(player) == p || Mathf.Abs(p.transform.position.x - GameState.PlayerX(player)) < 3f;
            if (reason == null && atObject && a.Info.Kind != ObjKind.Boat) reason = Loc.T("state.unavailable_now");
            if (reason != null) parts.Add(reason);
        }
        else
        {
            string missing = Missing(player, a);
            if (missing != null) parts.Add(missing);
        }
        return string.Join(", ", parts);
    }

    /// <summary>Current level of a wall, tower or castle (1 = first level), or 0 if not applicable.</summary>
    public static int LevelOf(GameObject go)
    {
        try
        {
            var wall = go.GetComponent<Wall>();
            if (wall != null) return wall.level;
            var tower = go.GetComponent<Tower>();
            if (tower != null) return tower.level;
            var castle = go.GetComponent<Castle>();
            if (castle != null) return (int)castle.level + 1;
        }
        catch { }
        return 0;
    }

    /// <summary>True if the player can pay this object right now (not locked, coins enough).</summary>
    private static bool CanPayHere(Player player, Payable p)
    {
        try
        {
            var a = Analyze(player, p);
            if (!string.IsNullOrEmpty(a.LockText) || a.Price <= 0) return false;
            if (a.Info.Kind != ObjKind.Boat && !GameState.CanPayNow(p)) return false;
            return Missing(player, a) == null;
        }
        catch { return false; }
    }

    /// <summary>"2 coins missing" when the wallet cannot pay the price, otherwise null.</summary>
    private static string Missing(Player player, Analysis a)
    {
        try
        {
            if (a.Price <= 0) return null;
            int have = player.wallet.GetCurrency(a.Currency);
            if (have >= a.Price) return null;
            return Loc.T("hover.missing", Price(a.Price - have, a.Currency));
        }
        catch { return null; }
    }

    public static string Price(int amount, CurrencyType currency)
    {
        bool gems = currency == CurrencyType.Gems;
        string key = gems ? (amount == 1 ? "currency.gem.one" : "currency.gem.many")
                          : (amount == 1 ? "currency.coin.one" : "currency.coin.many");
        return Loc.T(key, amount);
    }

    private static string LockText(Player player, Payable p)
    {
        try
        {
            if (!p.IsLocked(player, out LockIndicator.LockReason reason)) return null;
            return reason switch
            {
                LockIndicator.LockReason.NotLocked => null,
                LockIndicator.LockReason.StoneTechRequired => Loc.T("lock.stone"),
                LockIndicator.LockReason.IronTechRequired => Loc.T("lock.iron"),
                LockIndicator.LockReason.HermitLocked => Loc.T("lock.hermit"),
                LockIndicator.LockReason.NoUpgrade => Loc.T("lock.max"),
                LockIndicator.LockReason.Base => Loc.T("lock.base"),
                // Every other game reason (passenger required, Trojan horse, serpent...).
                _ => Loc.TryT("lock." + reason.ToString().ToLowerInvariant()) ?? Loc.T("lock.generic")
            };
        }
        catch { return null; }
    }

    private static string ActionKey(ObjKind kind, Payable p, int price, CurrencyType currency)
    {
        var go = p.gameObject;
        switch (kind)
        {
            case ObjKind.Castle:
            {
                var castle = go.GetComponent<Castle>();
                return castle != null && castle.level == Castle.Level.Castle1 ? "act.build" : "act.upgrade";
            }
            case ObjKind.Wall:
            {
                var wall = go.GetComponent<Wall>();
                return wall != null && wall.level > 0 ? "act.upgrade" : "act.build";
            }
            case ObjKind.Tower:
            {
                var tower = go.GetComponent<Tower>();
                return tower != null && tower.level > 0 ? "act.upgrade" : "act.build";
            }
            case ObjKind.Farmhouse: return price >= 3 ? "act.upgrade" : "act.build";
            case ObjKind.Shop:
            case ObjKind.Workshop:
            case ObjKind.Bomb: return "act.buy";
            case ObjKind.Tree: return "act.chop";
            case ObjKind.Bush: return "act.clear";
            case ObjKind.Beggar:
            case ObjKind.BeggarCamp:
            case ObjKind.Hermit: return "act.hire";
            case ObjKind.Merchant: return "act.invest";
            case ObjKind.Chest: return "act.open";
            case ObjKind.GemChest:
            case ObjKind.Banker: return "act.deposit";
            case ObjKind.GemGuard: return "act.withdraw";
            case ObjKind.Portal: return "act.destroy";
            case ObjKind.Teleporter: return "act.teleport";
            case ObjKind.Statue:
            case ObjKind.CrownStatue:
            case ObjKind.Puzzle: return currency == CurrencyType.Gems ? "act.pay" : "act.activate";
            case ObjKind.Steed: return "act.ride";
            case ObjKind.CaveBomb: return CaveNarrator.BombAtDetonation(p.gameObject) ? "act.detonate" : "act.push"; case ObjKind.Oracle: return "act.consult"; case ObjKind.Shipyard: return "act.build_ship"; case ObjKind.Border: return "act.activate";
            case ObjKind.Banner: return "act.expedition";
            case ObjKind.Bell: return "act.call";
            case ObjKind.Boat:
                if (go.name.ToLowerInvariant().Contains("wreck")) return "act.repair";
                if (price <= 3) return "act.boat_parts";
                return price < 10 ? "act.repair" : "act.sail";
            case ObjKind.Unknown: return null;
            default: return "act.build";
        }
    }
}
