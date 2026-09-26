using System.Collections.Generic;

namespace KingdomAccess;

/// <summary>Logger provided by the mod loader (BepInEx, MelonLoader...).</summary>
public interface IModLog
{
    void Info(string message);
    void Warn(string message);
    void Error(string message);
}

/// <summary>
/// All mod settings. The loader adapter fills them from its own configuration system
/// (BepInEx: BepInEx\config\kingdom.access.cfg). Defaults are the values below.
/// </summary>
public sealed class AccessSettings
{
    // ----- General -----
    public bool Enabled = true;
    /// <summary>Forced language code ("fr", "en"...). Empty = follow the game language.</summary>
    public string LanguageOverride = "";
    /// <summary>Use the Windows voice (SAPI) when no screen reader is running.</summary>
    public bool SapiFallback = true;
    /// <summary>Number of messages kept in the speech history.</summary>
    public int HistorySize = 50;

    // ----- Announcements -----
    /// <summary>Announce the payable object selected by the game (the interaction point).</summary>
    public bool AnnounceHover = true;
    /// <summary>Range used by the "details of the object in front of you" report.</summary>
    public float HoverRange = 18f;
    public bool AnnounceCastleZone = true;
    public bool AnnounceCampZone = true;
    /// <summary>Read texts that appear on screen (tutorial hints, notifications, summaries).</summary>
    public bool AnnounceScreenText = true;
    /// <summary>Announce when an ability (item of power, ruler, mount) is ready again.</summary>
    public bool AnnounceAbilities = true;

    // ----- Radar and scanner -----
    public float RadarRange = 60f;
    /// <summary>Scanner range (0 = whole island).</summary>
    public float ScannerRange = 0f;
    /// <summary>Radar and scanner only list what the player has already explored.</summary>
    public bool ScanExploredOnly = true;
    /// <summary>Margin added on both sides of the explored area.</summary>
    public float ExploreMargin = 25f;
    public bool HideDestroyedPortals = false;

    // ----- Alerts and sounds -----
    public bool EnemyAlert = true;
    /// <summary>First enemy alert distance; the next ones are at half and a quarter of it.</summary>
    public float EnemyAlertRange = 20f;
    public bool CrownAlert = true;
    public bool AnnounceDayPhases = true;
    public bool SoundsEnabled = true;

    // ----- Menus -----
    public bool MenuNarration = true;
    /// <summary>Write every selected menu element to the log (development aid).</summary>
    public bool LogUI = false;

    // ----- Keys -----
    public readonly AccessKeys Keys = new();
}

/// <summary>
/// Keyboard shortcuts. Each one has a name (config key), a default and a description.
/// Defaults avoid the game's own keys: player 1 uses WASD, the arrows and Shift; player 2 uses
/// G H J K L I and Right Shift (G and J also start split-screen co-op).
/// </summary>
public sealed class AccessKeys
{
    public sealed class Definition
    {
        public string Name, Default, Description;
    }

    private static Definition D(string name, string def, string desc) => new() { Name = name, Default = def, Description = desc };

    /// <summary>All shortcuts, in the order used by the config file and the help list.</summary>
    public static readonly Definition[] Definitions =
    {
        D("Help", "F1", "List all shortcuts (browse with Page Up / Page Down)."),
        D("TutorialHint", "Shift+F1", "Repeat the current tutorial hint."),
        D("RepeatLast", "F11", "Repeat the last message."),
        D("PreviousMessage", "Shift+F11", "Previous message in the history."),
        D("NextMessage", "Ctrl+F11", "Next message in the history."),
        D("ReadScreen", "F2", "Read the current screen (window text and selected element; blazon summary in the blazon editor)."),
        D("CaptureScreenText", "F3", "Put every visible text in a list (browse with Page Up / Page Down)."),
        D("Wallet", "O", "Coins and gems (O for 'or', gold)."),
        D("Time", "T", "Day, season, time of day, hours until night or dawn."),
        D("Mount", "M", "Current mount and whether it is tired."),
        D("Abilities", "R", "Relic (item of power), ruler and mount abilities, with what they do."),
        D("Compass", "C", "Facing direction, time of day, danger, nearest wall."),
        D("Census", "P", "Population: troops by type (browse with Page Up / Page Down)."),
        D("Radar", "V", "Nearest interesting objects on each side (browse with Page Up / Page Down)."),
        D("TargetDetails", "Shift+V", "Details of the object in front of you (price, level, distance)."),
        D("NextCategory", "Home", "Scanner: next category."),
        D("PreviousCategory", "Shift+Home", "Scanner: previous category."),
        D("RepeatItem", "Ctrl+Home", "Reread the selected item with its current distance."),
        D("PreviousItem", "PageUp", "Previous item of the last list (closer in the scanner)."),
        D("NextItem", "PageDown", "Next item of the last list (farther in the scanner)."),
        D("WalkToItem", "End", "Walk to the selected item; press again to stop."),
        D("RunToItem", "Shift+End", "Run to the selected item; press again to stop."),
        D("RunToBase", "B", "Run to the castle or base camp."),
        D("RunToCrown", "Shift+C", "Run to your lost crown."),
        D("RunBehindLeftWall", "Ctrl+LeftArrow", "Run just behind (inside) the farthest wall on the left."),
        D("RunBehindRightWall", "Ctrl+RightArrow", "Run just behind (inside) the farthest wall on the right."),
        D("DumpTarget", "Shift+F3", "Development: write the object in front of you to the log."),
        D("DumpIsland", "Ctrl+Shift+F3", "Development: write every object of the island to the log."),
    };

    private readonly Dictionary<string, KeyBinding> _map = new();

    public AccessKeys()
    {
        foreach (var d in Definitions) _map[d.Name] = KeyBinding.Parse(d.Default, KeyBinding.None);
    }

    /// <summary>Sets a shortcut from its config text (invalid text keeps the default).</summary>
    public void Set(string name, string text, IModLog log)
    {
        var def = System.Array.Find(Definitions, d => d.Name == name);
        if (def == null) return;
        _map[name] = KeyBinding.Parse(text, KeyBinding.Parse(def.Default, KeyBinding.None), log);
    }

    public KeyBinding this[string name] => _map.TryGetValue(name, out var k) ? k : KeyBinding.None;
}

/// <summary>Services provided to the core by the loader adapter.</summary>
public sealed class ModContext
{
    public IModLog Log;
    public AccessSettings Settings;

    /// <summary>Mod folder (contains Lang\, Sounds\, Tolk.dll, nvdaControllerClient64.dll).</summary>
    public string ModDirectory;
}
