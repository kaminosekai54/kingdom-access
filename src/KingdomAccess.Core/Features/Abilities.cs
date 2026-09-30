using System.Collections.Generic;
using KingdomAccess.Game;
using KingdomAccess.Localization;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Features;

/// <summary>
/// Special abilities: item of power (Norse Lands and Olympus DLCs), ruler ability,
/// mount ability. The Abilities shortcut gives their state; each ability coming back is announced.
/// </summary>
internal static class Abilities
{
    private static bool? _itemReady, _rulerReady, _steedReady;
    private static float _nextCheck;

    public static void Reset() { _itemReady = _rulerReady = _steedReady = null; }

    /// <summary>Abilities shortcut: state of every ability, with what it does.</summary>
    public static void Report(Player player)
    {
        var parts = new List<string>();
        string item = ItemStatus(player, out _);
        if (item != null)
        {
            parts.Add(item);
            string desc = ItemDescription(player);
            if (desc != null) parts.Add(desc);
        }
        else parts.Add(Loc.T("ability.no_relic"));
        string ruler = RulerStatus(player, out _);
        if (ruler != null)
        {
            parts.Add(ruler);
            // Dead Lands monarchs have their own ability; in the Norse Lands and Olympus the
            // ruler ability is the relic, already described above.
            string rdesc = RulerDescription(player);
            if (rdesc != null) parts.Add(rdesc);
        }

        var steed = player.steed;
        if (steed != null)
        {
            string status = SteedStatus(player, out _);
            parts.Add(Loc.T("ability.mount_is", ObjectNames.SteedTypeName(steed)) + (status != null ? ", " + status : ""));
            string desc = SteedDescription(steed);
            parts.Add(desc ?? Loc.T("steeddesc.unknown"));
        }
        SpeechOut.Say(parts.Count > 0 ? string.Join(". ", parts) : Loc.T("ability.none"));
    }

    /// <summary>
    /// Mount shortcut: name, tired or ready, ability state, and what the mount does.
    /// </summary>
    public static void MountReport(Player player)
    {
        var steed = player.steed;
        if (steed == null) { SpeechOut.Say(Loc.T("report.no_mount")); return; }
        var parts = new List<string>
        {
            $"{ObjectNames.SteedTypeName(steed)}, {Loc.T(steed.IsTired ? "mount.tired" : "mount.ready")}"
        };
        string status = SteedStatus(player, out _);
        if (status != null) parts.Add(status);
        parts.Add(SteedDescription(steed) ?? Loc.T("steeddesc.unknown"));
        string trigger = TriggerNote(steed);
        if (trigger != null) parts.Add(trigger);
        SpeechOut.Say(string.Join(". ", parts));
    }

    /// <summary>
    /// How to trigger the mount ability, only when the game says the ability is manual
    /// (automatic ones, like the stag luring deer, need no key).
    /// </summary>
    private static string TriggerNote(Steed steed)
    {
        try
        {
            var ability = steed.GetComponentInChildren<SteedAbility>(true);
            if (ability == null || ability.IsAutomaticAbility) return null;
            return Loc.T("steed.trigger");
        }
        catch { return null; }
    }

    /// <summary>
    /// What the monarch's own ability does (Dead Lands monarchs), from the ability's class name
    /// (MiriamRulerAbility...). Relic-based abilities return null: the relic is described instead.
    /// </summary>
    private static string RulerDescription(Player player)
    {
        try
        {
            var ability = player.GetComponentInChildren<RulerAbility>(true);
            if (ability == null) return null;
            string type = ability.GetIl2CppType().Name;
            if (type == "ItemBasedRulerAbility") return null;
            return Loc.TryT("rulerdesc." + type.ToLowerInvariant());
        }
        catch { return null; }
    }

    private static string ItemDescription(Player player)
    {
        try
        {
            var type = player.equippedItemOfPower;
            if (type == ItemOfPower.ItemType.None) return null;
            return Loc.TryT("itemdesc." + type.ToString().ToLowerInvariant());
        }
        catch { return null; }
    }

    /// <summary>What the mount does (same key as its name: steeddesc.griffin...).</summary>
    public static string SteedDescription(Steed steed)
    {
        try
        {
            string key = steed.steedType.ToString().ToLowerInvariant().Replace("_norselands", "");
            if (key.StartsWith("p1") || key.StartsWith("p2")) key = key.Substring(2);
            return Loc.TryT("steeddesc." + key);
        }
        catch { return null; }
    }

    /// <summary>Announces an ability coming back (from "recharging" to "ready").</summary>
    public static void Tick(Player player, AccessSettings s, float now)
    {
        if (!s.AnnounceAbilities || now < _nextCheck) return;
        _nextCheck = now + 0.5f;

        ItemStatus(player, out bool? item);
        Notify(ref _itemReady, item, "ability.item_ready", ItemName(player));
        RulerStatus(player, out bool? ruler);
        Notify(ref _rulerReady, ruler, "ability.ruler_ready", null);
        SteedStatus(player, out bool? steed);
        Notify(ref _steedReady, steed, "ability.steed_ready", null);
    }

    private static void Notify(ref bool? last, bool? now, string key, string name)
    {
        if (now == true && last == false) SpeechOut.Say(Loc.T(key, name), false);
        last = now;
    }

    private static string ItemName(Player player)
    {
        try
        {
            var type = player.equippedItemOfPower;
            if (type == ItemOfPower.ItemType.None) return null;
            return Loc.TryT("item." + type.ToString().ToLowerInvariant()) ?? type.ToString();
        }
        catch { return null; }
    }

    private static string ItemStatus(Player player, out bool? ready)
    {
        ready = null;
        string name = ItemName(player);
        if (name == null) return null;
        try
        {
            var item = player.GetComponentInChildren<ItemOfPower>(true);
            if (item == null) return Loc.T("ability.item", name, Loc.T("ability.ready"));
            float left = item._nextActivationTime - Time.time;
            ready = left <= 0f;
            return Loc.T("ability.item", name, Remaining(left));
        }
        catch { return Loc.T("ability.item", name, Loc.T("ability.ready")); }
    }

    private static string RulerStatus(Player player, out bool? ready)
    {
        ready = null;
        try
        {
            var ability = player.GetComponentInChildren<RulerAbility>(true);
            if (ability == null || !ability.enabled) return null;
            float left = ability.nextActivationTime - Time.time;
            ready = left <= 0f;
            return Loc.T("ability.ruler", Remaining(left));
        }
        catch { return null; }
    }

    private static string SteedStatus(Player player, out bool? ready)
    {
        ready = null;
        try
        {
            var steed = player.steed;
            if (steed == null) return null;
            var ability = steed.GetComponentInChildren<SteedAbility>(true);
            if (ability == null || !ability.enabled || ability.IsAutomaticAbility) return null;
            ready = ability.IsAbilityReady;
            if (ability.IsAbilityInProgress) return Loc.T("ability.steed", Loc.T("ability.in_progress"));
            float left = ability._nextActivationTime - Time.time;
            return Loc.T("ability.steed", ready == true ? Loc.T("ability.ready") : Remaining(left));
        }
        catch { return null; }
    }

    private static string Remaining(float seconds)
    {
        if (seconds <= 0f) return Loc.T("ability.ready");
        return Loc.T("ability.cooldown", Mathf.CeilToInt(seconds));
    }
}
