using System.Collections.Generic;
using System.Reflection;
using KingdomAccess.Game;
using KingdomAccess.Localization;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Features;

/// <summary>Spoken reports triggered by shortcuts.</summary>
internal static class Reports
{
    /// <summary>Compass: facing direction, time of day, danger, nearest wall.</summary>
    public static void Compass(Player player)
    {
        var parts = new List<string>();
        try
        {
            bool left = player.mover.GetDirection() == Side.Left;
            parts.Add(Loc.T("report.facing", Loc.T(left ? "dir.left" : "dir.right")));
        }
        catch { }

        var phase = DayClock.Phase();
        if (phase.HasValue) parts.Add(DayClock.PhaseName(phase.Value));

        try
        {
            var em = GameState.EnemyManager;
            if (em != null) parts.Add(Loc.T(em.IsDangerous ? "report.danger" : "report.safe"));
        }
        catch { }

        float x = GameState.PlayerX(player);
        Wall nearest = null;
        float best = float.MaxValue;
        foreach (var wall in UnitCache.All<Wall>(UnitKind.Wall))
        {
            float d = Mathf.Abs(wall.transform.position.x - x);
            if (d < best) { best = d; nearest = wall; }
        }
        if (nearest != null)
            parts.Add(Loc.T("report.nearest_wall", Directions.DistanceSide(nearest.transform.position.x - x)));

        SpeechOut.Say(string.Join(", ", parts));
    }

    /// <summary>Wallet content.</summary>
    public static void Wallet(Player player)
    {
        try
        {
            var w = player.wallet;
            SpeechOut.Say($"{HoverAnnouncer.Price(w.Coins, CurrencyType.Coins)}, {HoverAnnouncer.Price(w.Gems, CurrencyType.Gems)}");
        }
        catch { SpeechOut.Say(Loc.T("report.unavailable")); }
    }

    /// <summary>Time: day, season, time of day, hour, time until night or dawn.</summary>
    public static void World() => SpeechOut.Say(DayClock.Describe());

    /// <summary>Mount and whether it is tired.</summary>
    public static void Mount(Player player)
    {
        var steed = player.steed;
        if (steed == null) { SpeechOut.Say(Loc.T("report.no_mount")); return; }
        string name = ObjectNames.SteedTypeName(steed);
        SpeechOut.Say($"{name}, {Loc.T(steed.IsTired ? "mount.tired" : "mount.ready")}");
    }

    /// <summary>Details of the object in front of you (price, level, distance).</summary>
    public static void TargetDetails(Player player, AccessSettings s)
    {
        var p = HoverAnnouncer.SelectedOrClosest(player, s.HoverRange);
        if (p == null) { SpeechOut.Say(Loc.T("report.no_target")); return; }

        string text = HoverAnnouncer.Describe(player, p);
        var go = p.gameObject;
        int level = -1;
        var wall = go.GetComponent<Wall>();
        if (wall != null) level = wall.level;
        var castle = go.GetComponent<Castle>();
        if (castle != null) level = (int)castle.level + 1;
        var tower = go.GetComponent<Tower>();
        if (tower != null) level = tower.level;
        if (level >= 0) text += ", " + Loc.T("report.level", level);

        text += ", " + Directions.DistanceSide(p.transform.position.x - GameState.PlayerX(player));
        SpeechOut.Say(text);
    }

    /// <summary>Development: writes the fields of the object in front of you to the log.</summary>
    public static void DumpTarget(Player player, AccessSettings s, IModLog log)
    {
        var p = HoverAnnouncer.SelectedOrClosest(player, s.HoverRange);
        if (p == null) { SpeechOut.Say(Loc.T("report.no_target")); return; }

        var go = p.gameObject;
        log.Info($"[Dump] Object '{go.name}' at x={p.transform.position.x:0.0}");
        foreach (var comp in go.GetComponents<Component>())
            if (comp != null) log.Info($"[Dump]   Composant : {comp.GetIl2CppType().FullName}");
        foreach (var prop in p.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (prop.GetIndexParameters().Length > 0) continue;
            try { log.Info($"[Dump]   {prop.Name} = {prop.GetValue(p)}"); } catch { }
        }
        SpeechOut.Say(Loc.T("report.dumped", ObjectNames.CleanName(go.name)));
    }

    /// <summary>
    /// Development: writes every object of the island with its classification to the log,
    /// to fix identification mistakes from real data.
    /// </summary>
    public static void DumpIsland(Player player, IModLog log)
    {
        float px = GameState.PlayerX(player);
        log.Info($"[Island] ===== Island dump, player at x={px:0.0} =====");
        var payables = GameState.Payables;
        int n = 0;
        if (payables != null)
        {
            for (int i = 0; i < payables.Length; i++)
            {
                var p = payables[i];
                if (p == null) continue;
                n++;
                var go = p.gameObject;
                var info = ObjectNames.Identify(p);
                var comps = new List<string>();
                foreach (var c in go.GetComponents<Component>())
                    if (c != null) comps.Add(c.GetIl2CppType().Name);
                bool enabled = false, blocked = false, canPay = false;
                int price = 0;
                try { enabled = p.isActiveAndEnabled; blocked = p.forceBlockPayment; price = p.Price; canPay = p.CanPay(player); } catch { }
                log.Info($"[Island] x={p.transform.position.x,8:0.0} | '{go.name}' | {info.Kind} '{info.Name}' | active={go.activeInHierarchy} enabled={enabled} blocked={blocked} canPay={canPay} available={GameState.IsAvailable(p)} price={price} | {string.Join(",", comps)}");
            }
        }
        foreach (var portal in UnitCache.All<Portal>(UnitKind.Portal))
            log.Info($"[Island] PORTAL x={portal.transform.position.x,8:0.0} | '{portal.gameObject.name}' | state={SafeState(portal)}");
        foreach (var e in UnitCache.All<Enemy>(UnitKind.Enemy))
            log.Info($"[Island] ENEMY x={e.transform.position.x,8:0.0} | '{e.gameObject.name}' | type={SafeType(e)}");
        log.Info($"[Island] ===== {n} payables =====");
        SpeechOut.Say(Loc.T("report.island_dumped", n));
    }

    private static string SafeState(Portal p) { try { return p.state.ToString(); } catch { return "?"; } }
    private static string SafeType(Enemy e) { try { return e.Type.ToString(); } catch { return "?"; } }
}
