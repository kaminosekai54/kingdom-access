using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using KingdomAccess.Localization;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Game;

/// <summary>
/// Puzzles and special objects of the Norse Lands and Olympus DLCs (Thor, Heimdall,
/// Hel, Loki, Cerberus and Chariot pillars; Hephaestus forge, Artemis cage, gates of Olympus,
/// serpent weak points...). Identified by component type name, state read from the object.
/// </summary>
internal static class DlcObjects
{
    // Known component types (game class names).
    private static readonly string[] KnownTypes =
    {
        "ThorPuzzleController", "ThorPuzzlePillar", "ThorPuzzleStatue", "ThorPuzzleHammer", "ThorItem",
        "HeimdallPuzzleController", "HeimdallPuzzlePillar", "HeimdallPuzzleHorn", "HeimdalItem",
        "HelPuzzleController", "HelPuzzlePillar", "HelPuzzleItem",
        "LokiPuzzle", "LokiPuzzleStaff", "LokiStaff",
        "CerberusPuzzleController", "CerberusPuzzlePillar",
        "ChariotPuzzleController", "ChariotPuzzlePillar",
        "HephaestusForge", "HephaestusAnvil", "HephaestusHammer",
        "ArtemisCage", "ArtemisClearing", "MtOlympusGates", "MtOlympusStatue",
        "WorldEatingSerpentWeakPoint", "WorldEatingSerpentPortal", "ChangeItemOfPowerShop",
        "SerpentLevelTrojanHorse", "AthenaFarmhouse"
    };
    private static readonly HashSet<string> Known = new(KnownTypes);

    private static readonly List<Component> Found = new();
    private static float _nextScan;
    private static IModLog _log;
    private static readonly HashSet<string> LoggedTypes = new();

    internal static void Initialize(IModLog log)
    {
        _log = log;
        // No patch on OnSolved: patching this virtual method (base and overrides) crashes the
        // game when a puzzle is solved. Puzzle states are polled instead (see Tick).
    }

    private static readonly Dictionary<IntPtr, int> ControllerStates = new();
    private static float _nextStateCheck;

    public static void ResetStates() => ControllerStates.Clear();

    /// <summary>Announces puzzle state changes (solved, horn taken...).</summary>
    public static void Tick(float now)
    {
        if (now < _nextStateCheck) return;
        _nextStateCheck = now + 1f;
        try
        {
            foreach (var comp in All(now))
            {
                string tn = null;
                try { tn = comp.GetIl2CppType().Name; } catch { }
                if (tn == null || !tn.EndsWith("PuzzleController")) continue;
                var pc = comp.TryCast<PuzzleController>();
                if (pc == null) continue;
                int state = pc.State;
                if (ControllerStates.TryGetValue(pc.Pointer, out int before) && before != state)
                {
                    var info = Identify(pc.gameObject);
                    string name = info?.Name ?? Loc.T("cat.puzzles");
                    _log?.Info($"[DLC] {pc.GetIl2CppType().Name}: state {before} -> {state}");
                    if (before == 0 && state != 0) SpeechOut.Say(Loc.T("puzzle.solved", name), false);
                    else SpeechOut.Say(name, false);
                }
                ControllerStates[pc.Pointer] = state;
            }
        }
        catch { }
    }

    /// <summary>Name and state of a DLC object, or null if the object is not one.</summary>
    public static ObjInfo? Identify(GameObject go)
    {
        if (go == null) return null;
        try
        {
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) continue;
                string type = c.GetIl2CppType().Name;
                if (!Known.Contains(type)) continue;
                string name = Loc.TryT("dlc." + type.ToLowerInvariant()) ?? Split(type);
                if (type == "HelPuzzlePillar" && go.name.ToLowerInvariant().Contains("statue")) name = Loc.T("dlc.helbust");
                string state = StateOf(c, type);
                if (!string.IsNullOrEmpty(state)) name += ", " + state;
                string extra = Details(c, type);
                if (!string.IsNullOrEmpty(extra)) name += ", " + extra;
                return new ObjInfo { Kind = ObjKind.Puzzle, Name = name };
            }
        }
        catch { }
        return null;
    }

    // ---------- Heimdall puzzle ----------
    // Each pillar only accepts its gem at a given time of day (dawn, day, evening,
    // night). The horn can be taken once the pillars are solved.

    private static string PhaseWhen(DayPhase p) => Loc.T("phase.when." + p.ToString().ToLowerInvariant());

    private static DayPhase? CurrentPhase()
    {
        try
        {
            var d = GameState.Director;
            return d == null ? null : d.CurrentTimesOfDay.GetDayPhase(d.currentTime);
        }
        catch { return null; }
    }

    /// <summary>Details added to the name (e.g. activation time of a pillar).</summary>
    private static string Details(Component c, string type)
    {
        try
        {
            if (type == "HeimdallPuzzlePillar")
            {
                var pillar = c.TryCast<HeimdallPuzzlePillar>();
                if (pillar == null || pillar.state != HeimdallPuzzlePillar.HeimdallPuzzlePillarState.WaitingForGem) return null;
                var when = pillar._activationDayPhase;
                bool now = CurrentPhase() == when;
                return Loc.T(now ? "puzzle.active_now" : "puzzle.active_at", PhaseWhen(when)) + ", " + Loc.T("puzzle.mount_note");
            }
            if (type == "ThorPuzzlePillar") return ThorPillarDetails(c.TryCast<ThorPuzzlePillar>());
            if (type == "HelPuzzlePillar") return HelDetails(c.TryCast<HelPuzzlePillar>());
            if (type == "LokiPuzzle")
            {
                // Loki puzzle: the crown must fall into the hands of the statue.
                var loki = c.TryCast<LokiPuzzle>();
                if (loki == null) return null;
                var st = loki.puzzleState;
                if (st == LokiPuzzle.PuzzleState.UnSolved || st == LokiPuzzle.PuzzleState.None)
                    return Loc.T("puzzle.loki_howto", Mathf.RoundToInt(loki.crownActivateDistance));
                if (st == LokiPuzzle.PuzzleState.PlayerTransformed) return Loc.T("puzzle.loki_transformed");
                if (st == LokiPuzzle.PuzzleState.StaffSpawned) return Loc.T("puzzle.loki_staff");
                return null;
            }
            if (type == "ThorPuzzleStatue")
            {
                var statue = c.TryCast<ThorPuzzleStatue>();
                if (statue == null) return null;
                return Loc.T(statue.IsInteractable ? "puzzle.active" : "puzzle.inactive") + ", " + ThorSummary();
            }
        }
        catch { }
        return null;
    }

    // ---------- Hel puzzle ----------
    // Four sconces (2 coins each) and a bust of Hel (more expensive). Once paid, each
    // sconce waits for the bow of an archer (with a shield) and the bust for a knight's weapons;
    // the reward is Hel's trophy, picked up for one coin.

    private static string HelDetails(HelPuzzlePillar p)
    {
        if (p == null) return null;
        var parts = new List<string>();
        try
        {
            if (p.state == HelPuzzlePillar.HelPuzzlePillarState.WaitingForPayment)
                parts.Add(Loc.T(p.itemNeeded == HelPuzzlePillar.ExpectedItem.Bow ? "puzzle.hel_then_bow" : "puzzle.hel_then_axe"));
            else if (p.state == HelPuzzlePillar.HelPuzzlePillarState.WaitingForUnitItem)
            {
                parts.Add(Loc.T(p.itemNeeded == HelPuzzlePillar.ExpectedItem.Bow ? "puzzle.hel_need_bow" : "puzzle.hel_need_axe"));
                if (p.assignedArcher != null || p.assignedKnight != null) parts.Add(Loc.T("puzzle.hel_unit_coming"));
            }
        }
        catch { }
        return parts.Count == 0 ? null : string.Join(", ", parts);
    }

    // ---------- Thor puzzle ----------
    // Seven pillars each carry a rune; paying a coin switches to the next rune.
    // When all seven runes are right, pay the statue to call the lightning.

    private static readonly HashSet<string> LoggedRunes = new();

    /// <summary>Name of the displayed rune (from the game image), logged once.</summary>
    private static string RuneName(ThorPuzzlePillar.PillarState st)
    {
        try
        {
            string raw = st.Sign != null ? st.Sign.name : null;
            if (string.IsNullOrEmpty(raw)) return null;
            if (LoggedRunes.Add(raw)) _log?.Info($"[DLC] Symbole de pilier de Thor : {raw}");
            return Loc.TryT("rune." + System.Text.RegularExpressions.Regex.Replace(raw.ToLowerInvariant(), "[^a-z0-9]", "")) ?? null;
        }
        catch { return null; }
    }

    private static string ThorPillarDetails(ThorPuzzlePillar pillar)
    {
        if (pillar == null) return null;
        var parts = new List<string>();
        try
        {
            // Pillar number (the objects are named pillar1 to pillar7).
            var m = System.Text.RegularExpressions.Regex.Match(pillar.gameObject.name, @"(\d+)");
            if (m.Success) parts.Add(Loc.T("puzzle.pillar_number", m.Groups[1].Value));

            var st = pillar.currentPillarState;
            var list = pillar.pillarStates;
            int idx = pillar.GetStateIndex();
            int count = list != null ? list.Count : 0;
            string rune = st != null ? RuneName(st) : null;
            parts.Add(rune != null ? Loc.T("puzzle.rune_named", rune, idx + 1, count) : Loc.T("puzzle.rune", idx + 1, count));
            if (st != null) parts.Add(Loc.T(st.IsCorrect ? "puzzle.correct" : "puzzle.incorrect"));
            if (!pillar.IsInteractable) parts.Add(Loc.T("puzzle.inactive"));
        }
        catch { }
        return string.Join(", ", parts);
    }

    /// <summary>"3 correct runes out of 7".</summary>
    private static string ThorSummary()
    {
        int ok = 0, total = 0;
        try
        {
            foreach (var c in OfType("ThorPuzzlePillar"))
            {
                var p = c.TryCast<ThorPuzzlePillar>();
                if (p == null) continue;
                total++;
                var st = p.currentPillarState;
                if (st != null && st.IsCorrect) ok++;
            }
        }
        catch { }
        return Loc.T("puzzle.thor_summary", ok, total);
    }

    /// <summary>
    /// True if the player rides Heimdall's mount (DayNight type, which changes with day
    /// and night). The pillars seem to require it; the exact game condition cannot be read.
    /// </summary>
    private static bool RidesHeimdallMount(Player player)
    {
        try { var s = player.steed; return s != null && s.steedType == SteedType.DayNight; }
        catch { return true; }
    }

    /// <summary>
    /// Why a DLC object refuses payment right now, or null if unknown.
    /// </summary>
    public static string WhyUnavailable(GameObject go, Player player)
    {
        try
        {
            // Look up by type name first: a DLC type is only touched if it is present.
            var pc = FindComponent(go, "HeimdallPuzzlePillar");
            var pillar = pc != null ? pc.TryCast<HeimdallPuzzlePillar>() : null;
            if (pillar != null)
            {
                if (pillar.state != HeimdallPuzzlePillar.HeimdallPuzzlePillarState.WaitingForGem) return Loc.T("puzzle.pillar_done");
                var when = pillar._activationDayPhase;
                if (CurrentPhase() != when) return Loc.T("puzzle.wait_phase", PhaseWhen(when));
                if (player != null && player.wallet.GetCurrency(CurrencyType.Gems) < 1) return Loc.T("puzzle.need_gem");
                if (player != null && !RidesHeimdallMount(player)) return Loc.T("puzzle.need_heimdall_mount");
                return null;
            }
            var hc = FindComponent(go, "HeimdallPuzzleHorn");
            var horn = hc != null ? hc.TryCast<HeimdallPuzzleHorn>() : null;
            if (horn != null) return horn.HornPickedUp ? Loc.T("puzzle.horn_taken") : Loc.T("puzzle.horn_wait");

            var tpc = FindComponent(go, "ThorPuzzlePillar");
            var thorPillar = tpc != null ? tpc.TryCast<ThorPuzzlePillar>() : null;
            if (thorPillar != null)
                return thorPillar.IsInteractable ? null : Loc.T("puzzle.thor_pillar_inactive");
            var tsc = FindComponent(go, "ThorPuzzleStatue");
            var thorStatue = tsc != null ? tsc.TryCast<ThorPuzzleStatue>() : null;
            if (thorStatue != null)
                return thorStatue.IsInteractable ? Loc.T("puzzle.thor_statue_wait", ThorSummary()) : Loc.T("puzzle.inactive");
        }
        catch { }
        return null;
    }

    /// <summary>Time of day changed: announce the pillars that become active.</summary>
    public static void OnPhaseChange(DayPhase phase)
    {
        try
        {
            int n = 0;
            foreach (var c in OfType("HeimdallPuzzlePillar"))
            {
                var p = c.TryCast<HeimdallPuzzlePillar>();
                if (p != null && p._activationDayPhase == phase
                    && p.state == HeimdallPuzzlePillar.HeimdallPuzzlePillarState.WaitingForGem) n++;
            }
            if (n > 0) SpeechOut.Say(Loc.T(n == 1 ? "puzzle.pillar_awake.one" : "puzzle.pillar_awake.many", n), false);
        }
        catch { }
    }

    /// <summary>
    /// Every DLC object on the island (refreshed every 3 seconds).
    /// Never ask the game for "every object of type X" with a DLC type: if that type is
    /// not loaded in this world (e.g. an Olympus type in the Norse Lands), touching it can
    /// crash the game. Walk the components and recognise them by their type name.
    /// </summary>
    public static List<Component> All(float now)
    {
        if (now < _nextScan) { Found.RemoveAll(c => c == null); return Found; }
        _nextScan = now + 3f;
        Found.Clear();
        // Puzzles and relics only exist in the Norse Lands (3) and Olympus (5).
        int biome = -1;
        try { biome = BiomeHolder.Inst.BiomeIndex; } catch { }
        if (biome != 3 && biome != 5) return Found;
        try
        {
            foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb == null || !mb.gameObject.activeInHierarchy) continue;
                string name;
                try { name = mb.GetIl2CppType().Name; } catch { continue; }
                if (Known.Contains(name)) Found.Add(mb);
            }
        }
        catch { }
        return Found;
    }

    /// <summary>Component of an object by its type name, without touching the type itself.</summary>
    private static Component FindComponent(GameObject go, string typeName)
    {
        try
        {
            foreach (var c in go.GetComponents<Component>())
                if (c != null && c.GetIl2CppType().Name == typeName) return c;
        }
        catch { }
        return null;
    }

    /// <summary>DLC objects of a given type among those found on the island.</summary>
    private static IEnumerable<Component> OfType(string typeName)
    {
        foreach (var c in All(Time.unscaledTime))
        {
            string n = null;
            try { n = c.GetIl2CppType().Name; } catch { }
            if (n == typeName) yield return c;
        }
    }

    private static readonly Dictionary<string, Type> TypeCache = new();

    /// <summary>
    /// Game interop type by name, searched in Assembly-CSharp only.
    /// (Searching every assembly can crash the IL2CPP runtime.)
    /// </summary>
    private static Type GameType(string name)
    {
        if (TypeCache.TryGetValue(name, out var t)) return t;
        try { t = typeof(Player).Assembly.GetType(name, false); } catch { t = null; }
        TypeCache[name] = t;
        return t;
    }

    /// <summary>
    /// State read by reflection on the object (enum property whose name contains "state"),
    /// translated through state.* when known. The chosen property is logged once.
    /// </summary>
    private static string StateOf(Component c, string type)
    {
        try
        {
            object target = null;
            // The object of the exact type is needed to see its properties.
            var method = typeof(Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase).GetMethod("TryCast");
            var exact = GameType(type);
            if (exact != null && method != null) target = method.MakeGenericMethod(exact).Invoke(c, null);
            if (target == null) return null;

            foreach (var p in target.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!p.PropertyType.IsEnum) continue;
                if (p.Name.IndexOf("state", StringComparison.OrdinalIgnoreCase) < 0) continue;
                object v = p.GetValue(target);
                if (v == null) continue;
                string raw = v.ToString();
                if (LoggedTypes.Add(type)) _log?.Info($"[DLC] {type}: state read from {p.Name} = {raw}");
                return Loc.TryT("state." + raw.ToLowerInvariant()) ?? Split(raw).ToLowerInvariant();
            }
        }
        catch { }
        return null;
    }

    private static string Split(string s) => Regex.Replace(s ?? "", "([a-z])([A-Z])", "$1 $2");
}
