using KingdomAccess.Game;
using KingdomAccess.Localization;
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
    private const float SettleDelay = 0.2f;
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
            SpeechOut.Say(message, false);
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

    private static string DescribeCore(Player player, Payable p)
    {
        var a = Analyze(player, p);
        if (!string.IsNullOrEmpty(a.LockText)) return $"{a.Name}, {a.LockText}";
        if (!GameState.CanPayNow(p))
            return $"{a.Name}, {DlcObjects.WhyUnavailable(p.gameObject, player) ?? Loc.T("state.unavailable_now")}";

        string action = a.ActionKey == null ? null : Loc.T(a.ActionKey);
        bool freeAction = a.Info.Kind is ObjKind.GemChest or ObjKind.GemGuard or ObjKind.Banker;
        if (a.Price <= 0 && !freeAction)
            return action == null ? a.Name : $"{a.Name}, {action}";

        string cost = Price(a.Price, a.Currency);
        return action == null ? $"{a.Name}, {cost}" : $"{a.Name}, {cost}, {action}";
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
            case ObjKind.CaveBomb: return "act.push";
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
