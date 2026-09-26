using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace KingdomAccess.BepInExAdapter;

/// <summary>
/// BepInEx 6 IL2CPP entry point. Reads BepInEx\config\kingdom.access.cfg (created with the
/// defaults on first launch, every setting and shortcut documented), then starts the core.
/// </summary>
[BepInPlugin(Guid, "Kingdom Access", AccessMod.Version)]
public class Plugin : BasePlugin
{
    public const string Guid = "kingdom.access";

    public override void Load()
    {
        var log = new BepInExLog(Log);
        var ctx = new ModContext
        {
            Log = log,
            Settings = BindSettings(Config, log),
            ModDirectory = Path.GetDirectoryName(typeof(Plugin).Assembly.Location)
        };

        AccessMod.Initialize(ctx, new Harmony(Guid));
        AddComponent<UpdateDriver>();
    }

    public override bool Unload()
    {
        AccessMod.Shutdown();
        return true;
    }

    private static AccessSettings BindSettings(ConfigFile cfg, IModLog log)
    {
        var s = new AccessSettings();
        var d = new AccessSettings();

        const string general = "1. General";
        s.Enabled = cfg.Bind(general, "Enabled", d.Enabled, "Turn the accessibility mod on or off.").Value;
        s.LanguageOverride = cfg.Bind(general, "Language", d.LanguageOverride,
            "Force the mod language (a file name from the Lang folder: fr, en...). Empty = follow the game language.").Value;
        s.SapiFallback = cfg.Bind(general, "SapiFallback", d.SapiFallback,
            "Use the Windows voice (SAPI) when no screen reader is running.").Value;
        s.HistorySize = cfg.Bind(general, "HistorySize", d.HistorySize, "Number of messages kept in the speech history.").Value;

        const string ann = "2. Announcements";
        s.AnnounceHover = cfg.Bind(ann, "AnnounceHover", d.AnnounceHover,
            "Announce the object selected by the game (the point where you can pay).").Value;
        s.HoverRange = cfg.Bind(ann, "HoverRange", d.HoverRange,
            "Range of the 'details of the object in front of you' shortcut.").Value;
        s.AnnounceCastleZone = cfg.Bind(ann, "AnnounceCastleZone", d.AnnounceCastleZone,
            "Announce entering and leaving the kingdom (between the outermost walls).").Value;
        s.AnnounceCampZone = cfg.Bind(ann, "AnnounceCampZone", d.AnnounceCampZone,
            "Announce entering and leaving vagrant camps.").Value;
        s.AnnounceScreenText = cfg.Bind(ann, "AnnounceScreenText", d.AnnounceScreenText,
            "Read texts that appear on screen (tutorial hints, notifications) and the tutorial ghost's hints.").Value;
        s.AnnounceAbilities = cfg.Bind(ann, "AnnounceAbilities", d.AnnounceAbilities,
            "Announce when an ability is ready again (item of power, ruler, mount).").Value;

        const string scan = "3. Radar and scanner";
        s.RadarRange = cfg.Bind(scan, "RadarRange", d.RadarRange, "Radar range.").Value;
        s.ScannerRange = cfg.Bind(scan, "ScannerRange", d.ScannerRange, "Scanner range. 0 = whole island.").Value;
        s.ScanExploredOnly = cfg.Bind(scan, "ScanExploredOnly", d.ScanExploredOnly,
            "Radar and scanner only list what you have already explored (troops, enemies and the castle are always listed).").Value;
        s.ExploreMargin = cfg.Bind(scan, "ExploreMargin", d.ExploreMargin, "Margin added on both sides of the explored area.").Value;
        s.HideDestroyedPortals = cfg.Bind(scan, "HideDestroyedPortals", d.HideDestroyedPortals,
            "Hide destroyed portals (otherwise they are listed as destroyed).").Value;

        const string alerts = "4. Alerts and sounds";
        s.EnemyAlert = cfg.Bind(alerts, "EnemyAlert", d.EnemyAlert, "Sound and speech alert when enemies approach.").Value;
        s.EnemyAlertRange = cfg.Bind(alerts, "EnemyAlertRange", d.EnemyAlertRange,
            "Distance of the first enemy alert; the next ones are at half and a quarter of it.").Value;
        s.CrownAlert = cfg.Bind(alerts, "CrownAlert", d.CrownAlert, "Alert when your crown is on the ground or carried away.").Value;
        s.AnnounceDayPhases = cfg.Bind(alerts, "AnnounceDayPhases", d.AnnounceDayPhases,
            "Sound and speech at dawn, day, evening and night.").Value;
        s.SoundsEnabled = cfg.Bind(alerts, "SoundsEnabled", d.SoundsEnabled,
            "Mod sounds (Sounds folder; replace a WAV file with your own, keeping its name).").Value;

        const string menus = "5. Menus";
        s.MenuNarration = cfg.Bind(menus, "MenuNarration", d.MenuNarration, "Read the game menus.").Value;
        s.LogUI = cfg.Bind(menus, "LogUI", d.LogUI, "Write every selected menu element to the log (development aid).").Value;

        const string keys = "6. Keys";
        foreach (var def in AccessKeys.Definitions)
        {
            string text = cfg.Bind(keys, def.Name, def.Default,
                def.Description + " Examples: O, Shift+V, Ctrl+F11, PageDown. Empty = disabled.").Value;
            s.Keys.Set(def.Name, text, log);
        }
        return s;
    }
}

/// <summary>Calls the core every frame.</summary>
public class UpdateDriver : MonoBehaviour
{
    public UpdateDriver(IntPtr ptr) : base(ptr) { }

    private void Update() => AccessMod.Tick();
}

internal sealed class BepInExLog : IModLog
{
    private readonly ManualLogSource _log;
    public BepInExLog(ManualLogSource log) => _log = log;
    public void Info(string message) => _log.LogInfo(message);
    public void Warn(string message) => _log.LogWarning(message);
    public void Error(string message) => _log.LogError(message);
}
