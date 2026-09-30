using System;
using HarmonyLib;
using KingdomAccess.Features;
using KingdomAccess.Game;
using KingdomAccess.Localization;
using KingdomAccess.Speech;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess;

/// <summary>
/// Entry point of the core. The loader adapter calls <see cref="Initialize"/> once, then
/// <see cref="Tick"/> every frame. Everything else is driven from here.
/// </summary>
public static class AccessMod
{
    public const string Version = "0.8.0";

    private static ModContext _ctx;
    private static bool _initialized;
    private static Player _currentPlayer;
    private static bool _welcomeDone;

    /// <summary>True once initialized and enabled in the configuration.</summary>
    public static bool Active => _initialized && _ctx.Settings.Enabled;

    public static void Initialize(ModContext ctx, Harmony harmony)
    {
        _ctx = ctx;
        var log = ctx.Log;
        log.Info($"KingdomAccess {Version}: starting.");

        Loc.Initialize(ctx.ModDirectory, ctx.Settings.LanguageOverride, log);
        SpeechOut.Configure(ctx.Settings.HistorySize);
        ScreenReader.Initialize(ctx.ModDirectory, ctx.Settings.SapiFallback, log);
        Sounds.Initialize(ctx.ModDirectory, ctx.Settings.SoundsEnabled);
        GameState.SetLog(log);
        BiomeSelectPatches.Log = log;
        TutorialNarrator.Log = log;
        CrownWatcher.Initialize(log);
        GameInputBlocker.Initialize(log);
        UnitCache.Initialize(log);
        DlcObjects.Initialize(log);
        ApplyAttributePatches(harmony, log);

        _initialized = true;
        log.Info("KingdomAccess: ready.");
    }

    public static void Shutdown() => ScreenReader.Shutdown();

    // ---------- API for companion mods ----------

    /// <summary>
    /// Set by another mod while it owns the keyboard (its own menu is open): the accessibility
    /// mod then ignores its shortcuts and the player does not move.
    /// </summary>
    public static bool ExternalKeyCapture { get; set; }

    /// <summary>Object chosen in the last list read (scanner, radar, census), or null.</summary>
    public static Component SelectedTarget => ListNav.SelectedTarget;

    /// <summary>Name of the object chosen in the last list read.</summary>
    public static string SelectedName => ListNav.SelectedName;

    /// <summary>Spoken description of the game time (same as the Time shortcut).</summary>
    public static string DescribeTime() => DayClock.Describe();

    /// <summary>"24 left": distance and side, in the mod language.</summary>
    public static string DistanceSide(float dx) => Directions.DistanceSide(dx);

    /// <summary>
    /// Spoken name of a shortcut for messages (e.g. "Control C"): the gamepad buttons when a pad
    /// is connected and the shortcut has some, otherwise the keyboard key.
    /// </summary>
    public static string KeyName(string shortcut)
    {
        if (_ctx == null) return "";
        var s = _ctx.Settings;
        var pad = s.Keys.Pad(shortcut);
        if (s.GamepadEnabled && Gamepad.Connected && pad.Button != PadButton.None) return HelpList.Spoken(pad);
        return HelpList.Spoken(s.Keys[shortcut]);
    }

    // ---------- Setup ----------

    /// <summary>Applies every [HarmonyPatch] class of this assembly, one by one (a failure is logged, not fatal).</summary>
    private static void ApplyAttributePatches(Harmony harmony, IModLog log)
    {
        foreach (var type in AccessTools.GetTypesFromAssembly(typeof(AccessMod).Assembly))
        {
            if (type.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0) continue;
            try
            {
                harmony.CreateClassProcessor(type).Patch();
                log.Info($"[Patch] {type.Name} applied.");
            }
            catch (Exception ex) { log.Warn($"[Patch] {type.Name} not applied: {ex.Message}"); }
        }
    }

    // ---------- Per-frame update ----------

    public static void Tick()
    {
        if (!_initialized) return;
        var s = _ctx.Settings;
        try
        {
            float now = Time.unscaledTime;
            Loc.Tick(now);

            if (!_welcomeDone && now > 4f)
            {
                _welcomeDone = true;
                SpeechOut.Say(Loc.T("mod.loaded", Version, HelpList.Spoken(s.Keys["Help"])), false);
            }

            if (!s.Enabled) return;

            // Gamepad: read it, and block the game's own pad input while a mod layer is held.
            if (s.GamepadEnabled) Gamepad.Poll();
            GameInputBlocker.Tick(s);

            // Menus and screens that exist outside of gameplay.
            HandleGlobalKeys(s);
            MenuNarrator.Tick(s, _ctx.Log, now);
            if (s.MenuNarration)
            {
                MapNarrator.Tick(now);
                NewGameNarrator.Tick(now);
                StatsNarrator.Tick(now);
            }
            ScreenText.Tick(s, now);

            var player = GameState.Player;
            if (player == null) { _currentPlayer = null; AutoWalk.Stop(); return; }

            if (player != _currentPlayer)
            {
                _currentPlayer = player;
                OnNewLevel(now);
            }

            // Gameplay.
            UnitCache.Tick(now);
            Exploration.Tick(player);
            if (!ExternalKeyCapture) HandleGameKeys(player, s);
            AutoWalk.Tick(player, now);
            if (s.AnnounceHover) HoverAnnouncer.Tick(player, s, now);
            if (s.AnnouncePassing) PassingAnnouncer.Tick(player, s, now);
            Zones.Tick(player, s, now, Time.unscaledDeltaTime);
            DayClock.Tick(s, _ctx.Log, now);
            EnemyAlert.Tick(player, s, now);
            Abilities.Tick(player, s, now);
            TutorialNarrator.Tick(player, s, now);
            DlcObjects.Tick(now);
            if (s.CrownAlert) CrownWatcher.Tick(player, now);
            CaveNarrator.Tick(player, now);
        }
        catch (Exception ex)
        {
            _ctx.Log.Error($"[Tick] {ex}");
        }
    }

    /// <summary>A new island (new player object) was loaded: reset every per-island state.</summary>
    private static void OnNewLevel(float now)
    {
        _ctx.Log.Info("New island detected.");
        UnitCache.ColdBoot();
        GameState.ResetAvailability();
        Exploration.Reset();
        Exploration.SeedFromKingdom();
        HoverAnnouncer.Reset();
        ListNav.Reset();
        AutoWalk.Stop();
        Zones.Reset(now);
        DayClock.Reset();
        EnemyAlert.Reset();
        Abilities.Reset();
        TutorialNarrator.Reset();
        DlcObjects.ResetStates();
        CrownWatcher.Reset();
        CaveNarrator.Reset();
        PassingAnnouncer.Reset();
        GameInputBlocker.Release();
    }

    // ---------- Shortcuts ----------

    /// <summary>True if the shortcut was pressed on the keyboard or on the gamepad this frame.</summary>
    private static bool Hit(AccessKeys k, string name) =>
        k[name].Pressed() || (_ctx.Settings.GamepadEnabled && k.Pad(name).Pressed());

    /// <summary>Shortcuts that work everywhere, including menus and outside of gameplay.</summary>
    private static void HandleGlobalKeys(AccessSettings s)
    {
        var k = s.Keys;
        var p = GameState.Player;

        if (Hit(k, "RepeatLast")) SpeechOut.RepeatLast();
        if (Hit(k, "PreviousMessage")) SpeechOut.Previous();
        if (Hit(k, "NextMessage")) SpeechOut.Next();
        if (Hit(k, "Help")) HelpList.Show(s);
        if (Hit(k, "TutorialHint")) TutorialNarrator.Repeat(p);
        if (ExternalKeyCapture) return;

        if (Hit(k, "ReadScreen")) MenuNarrator.ReadScreen();
        if (Hit(k, "CaptureScreenText")) ScreenText.Capture();

        // List navigation also works outside of gameplay (island summary, captured texts, help).
        if (Hit(k, "RepeatItem")) ListNav.Repeat(p, s);
        if (Hit(k, "PreviousItem")) ListNav.Move(p, -1, s);
        if (Hit(k, "NextItem")) ListNav.Move(p, 1, s);
    }

    /// <summary>Shortcuts that only make sense in gameplay.</summary>
    private static void HandleGameKeys(Player player, AccessSettings s)
    {
        var k = s.Keys;
        float now = Time.unscaledTime;

        if (Hit(k, "Wallet")) Reports.Wallet(player);
        if (Hit(k, "Time")) Reports.World();
        if (Hit(k, "Mount")) Abilities.MountReport(player);
        if (Hit(k, "Abilities")) Abilities.Report(player);
        if (Hit(k, "Compass")) Reports.Compass(player);
        if (Hit(k, "Census")) Census.Show(player);
        if (Hit(k, "Radar")) Radar.Pulse(player, s);
        if (Hit(k, "TargetDetails")) Reports.TargetDetails(player, s);

        if (Hit(k, "NextCategory")) CategoryScanner.ChangeCategory(player, 1, s);
        if (Hit(k, "PreviousCategory")) CategoryScanner.ChangeCategory(player, -1, s);
        if (Hit(k, "WalkToItem")) AutoWalk.Toggle(player, false, now);
        if (Hit(k, "RunToItem")) AutoWalk.Toggle(player, true, now);
        if (Hit(k, "RunToBase")) AutoWalk.RunToCastle(player, now);
        if (Hit(k, "RunToCrown")) AutoWalk.RunToCrown(player, now);
        if (Hit(k, "RunBehindLeftWall")) AutoWalk.RunBehindOuterWall(player, false, now);
        if (Hit(k, "RunBehindRightWall")) AutoWalk.RunBehindOuterWall(player, true, now);

        if (Hit(k, "DumpTarget")) Reports.DumpTarget(player, s, _ctx.Log);
        if (Hit(k, "DumpIsland")) Reports.DumpIsland(player, _ctx.Log);
    }
}
