using Coatsink.Common;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace KingdomAccess.Game;

/// <summary>Central access to game objects, with light caching.</summary>
internal static class GameState
{
    private static EnemyManager _enemyManager;
    private static float _nextEnemyManagerLookup;

    public static Managers Managers
    {
        get
        {
            try { return SingletonMonoBehaviour<Managers>.Inst; }
            catch { return null; }
        }
    }

    /// <summary>Player 1, or null outside of gameplay (menus, loading).</summary>
    public static Player Player
    {
        get
        {
            try
            {
                var kingdom = Managers?.kingdom;
                return kingdom == null ? null : kingdom.GetPlayer(0);
            }
            catch { return null; }
        }
    }

    public static Director Director
    {
        get
        {
            try { return Managers?.director; }
            catch { return null; }
        }
    }

    public static Il2CppReferenceArray<Payable> Payables
    {
        get
        {
            try
            {
                var payables = Managers?.payables;
                return payables == null ? null : payables.AllPayables;
            }
            catch { return null; }
        }
    }

    public static EnemyManager EnemyManager
    {
        get
        {
            if (_enemyManager == null && Time.unscaledTime >= _nextEnemyManagerLookup)
            {
                _nextEnemyManagerLookup = Time.unscaledTime + 2f;
                _enemyManager = Object.FindFirstObjectByType<EnemyManager>();
            }
            return _enemyManager;
        }
    }

    public static float PlayerX(Player player) => player.transform.position.x;

    /// <summary>Coins carried by a unit (knight, archer, peasant...), or 0.</summary>
    public static int CoinsOf(Component unit)
    {
        if (unit == null) return 0;
        try
        {
            var knight = unit.GetComponent<Knight>();
            if (knight != null && knight.Wallet != null) return knight.Wallet.GetCurrency(CurrencyType.Coins);
            var character = unit.GetComponent<Character>();
            if (character != null && character.wallet != null) return character.wallet.GetCurrency(CurrencyType.Coins);
        }
        catch { }
        return 0;
    }

    /// <summary>
    /// True if the payable really exists for the player: active object, enabled component,
    /// payment not blocked. Objects that depend on an unlock (boat, boat bell,
    /// shield shops) are also hidden while the game refuses their payment.
    /// The others stay listed, with "unavailable right now" (see CanPayNow).
    /// </summary>
    public static bool IsAvailable(Payable p)
    {
        if (p == null) return false;
        try
        {
            if (!p.isActiveAndEnabled) return false;
            if (p.forceBlockPayment) return false;
            if (!IsUnlockGated(p.gameObject)) return true;
            // Boat already built ("set sail" step, high price): always listed,
            // even if the departure is refused for now (the reason is then announced).
            if (IsBoatPart(p.gameObject) && p.Price >= 10 && !p.gameObject.name.ToLowerInvariant().Contains("wreck")) return true;
            return CanPayNow(p);
        }
        catch { return true; }
    }

    private static bool IsBoatPart(GameObject go) =>
        go.GetComponent<BoatSailPosition>() != null || go.GetComponent<PayableBoat>() != null || go.GetComponent<Boat>() != null;

    /// <summary>Objects that only exist for the player once unlocked.</summary>
    private static bool IsUnlockGated(GameObject go) =>
        go.GetComponent<PayableShield>() != null
        || go.GetComponent<BoatSailPosition>() != null
        || go.GetComponent<PayableBoat>() != null
        || go.GetComponent<Boat>() != null
        || go.GetComponent<BoatSummoningBell>() != null;

    /// <summary>
    /// True if the game accepts the payment right now (CanPay). Always true if CanPay
    /// is not reliable on this island (dependency on distance detected).
    /// </summary>
    public static bool CanPayNow(Payable p)
    {
        if (p == null) return false;
        try
        {
            if (!CanPayIsReliable()) return true;
            var player = Player;
            return player == null || p.CanPay(player);
        }
        catch { return true; }
    }

    // CanPay could depend on the player distance or coins: then it would hide
    // everything far away. Check it over the whole island and ignore it if needed.
    private static bool _canPayReliable = true;
    private static float _nextCanPayCheck;
    private static IModLog _log;
    private static bool? _loggedReliability;

    internal static void SetLog(IModLog log) => _log = log;

    public static void ResetAvailability() { _nextCanPayCheck = 0f; _loggedReliability = null; }

    private static bool CanPayIsReliable()
    {
        float now = Time.unscaledTime;
        if (now < _nextCanPayCheck) return _canPayReliable;
        _nextCanPayCheck = now + 3f;

        var player = Player;
        var payables = Payables;
        if (player == null || payables == null) return _canPayReliable;

        float px = PlayerX(player);
        int active = 0, yes = 0, far = 0, farYes = 0;
        for (int i = 0; i < payables.Length; i++)
        {
            var p = payables[i];
            if (p == null || !p.isActiveAndEnabled) continue;
            active++;
            bool isFar = Mathf.Abs(p.transform.position.x - px) > 25f;
            if (isFar) far++;
            bool ok;
            try { ok = p.CanPay(player); } catch { ok = true; }
            if (ok) { yes++; if (isFar) farYes++; }
        }

        // Reliable if some objects are accepted, including far away ones.
        _canPayReliable = active > 0 && yes > 0 && (far < 5 || farYes > 0);
        if (_loggedReliability != _canPayReliable)
        {
            _loggedReliability = _canPayReliable;
            _log?.Info($"[Availability] CanPay {(_canPayReliable ? "used" : "ignored")}: active={active} accepted={yes} far={far} far accepted={farYes}");
        }
        return _canPayReliable;
    }

    /// <summary>True if the object is the player or their mount (ignored in announcements).</summary>
    public static bool IsPlayerOrSteed(Player player, GameObject go)
    {
        if (go == null) return true;
        if (go == player.gameObject) return true;
        var steed = player.steed;
        return steed != null && go == steed.gameObject;
    }
}
