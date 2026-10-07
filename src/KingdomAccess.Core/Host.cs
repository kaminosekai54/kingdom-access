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
    /// <summary>While galloping, announce the useful objects the player rides past.</summary>
    public bool AnnouncePassing = true;
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
    /// <summary>Inside the Greed cave, a heartbeat guides the player to the bomb's detonation point.</summary>
    public bool CaveBeacon = true;
    /// <summary>A short chime when the object in front of the player can be paid right now.</summary>
    public bool PaySound = true;
    /// <summary>Play the earcon of the object selected by the game (castle, wall, tower, mount...).</summary>
    public bool HoverSounds = true;
    /// <summary>Default range of the sound radar (switched in game between 30, 50 and 100).</summary>
    public float SoundRadarRange = 30f;
    /// <summary>Seconds between two sounds of the sound radar.</summary>
    public float SoundRadarDelay = 0.45f;

    // ----- Menus -----
    public bool MenuNarration = true;
    /// <summary>Write every selected menu element to the log (development aid).</summary>
    public bool LogUI = false;

    // ----- Keys and gamepad -----
    public readonly AccessKeys Keys = new();
    /// <summary>Read the gamepad (XInput) for the mod shortcuts.</summary>
    public bool GamepadEnabled = true;
    /// <summary>While a layer button is held, the game ignores the pad (no coin dropped by mistake).</summary>
    public bool GamepadBlockGame = true;
    /// <summary>Buttons that open a layer of mod shortcuts while held.</summary>
    public PadButton[] GamepadLayerButtons = { PadButton.LB, PadButton.RB };
}

/// <summary>
/// Keyboard and gamepad shortcuts. Each one has a name (config key), defaults and a description.
/// Keyboard defaults avoid the game's own keys: player 1 uses WASD, the arrows and Shift (Left
/// Shift also triggers mount abilities, so no default uses Shift); player 2 uses G H J K L I and
/// Right Shift (G and J also start split-screen co-op).
/// Gamepad defaults use two layers: hold LB for navigation, hold RB for reports.
/// </summary>
public sealed class AccessKeys
{
    public sealed class Definition
    {
        public string Name, Default, Pad, Description;
        /// <summary>Previous keyboard default, replaced automatically in existing config files.</summary>
        public string OldDefault;
    }

    private static Definition D(string name, string key, string pad, string desc, string old = null) =>
        new() { Name = name, Default = key, Pad = pad, Description = desc, OldDefault = old };

    /// <summary>All shortcuts, in the order used by the config file and the help list.</summary>
    public static readonly Definition[] Definitions =
    {
        D("Help", "F1", "RB+Back", "List all shortcuts (browse with the previous / next item shortcuts)."),
        D("TutorialHint", "F4", "RB+LS", "Repeat the current tutorial hint.", "Shift+F1"),
        D("RepeatLast", "F11", "LB+Back", "Repeat the last message."),
        D("PreviousMessage", "F9", "RB+LT", "Previous message in the history.", "Shift+F11"),
        D("NextMessage", "F10", "RB+RT", "Next message in the history.", "Ctrl+F11"),
        D("ReadScreen", "F2", "LB+RS", "Read the current screen (window text and selected element; blazon summary in the blazon editor)."),
        D("CaptureScreenText", "F3", "LB+LS", "Put every visible text in a list."),
        D("Wallet", "O", "RB+A", "Coins and gems (O for 'or', gold)."),
        D("Time", "T", "RB+X", "Day, season, time of day, hours until night or dawn."),
        D("Mount", "M", "RB+B", "Current mount, its ability and what it does."),
        D("Abilities", "R", "RB+Y", "Relic (item of power), ruler and mount abilities, with what they do."),
        D("Compass", "C", "RB+DpadUp", "Facing direction, time of day, danger, nearest wall."),
        D("Census", "P", "RB+DpadDown", "Population: troops by type."),
        D("Radar", "V", "LB+Y", "Nearest interesting objects on each side."),
        D("TargetDetails", "X", "LB+B", "Details of the object in front of you (price, level, distance).", "Shift+V"),
        D("SoundRadar", "N", "LB+Start", "Sound radar: the sound of each object around you, nearest first, on its side."),
        D("SoundRadarRange", "Ctrl+N", "RB+Start", "Sound radar range: 30, 50 or 100."),
        D("SoundLegend", "Alt+N", "", "Sound legend: listen to every object sound with its name."),
        D("NextCategory", "Home", "LB+DpadDown", "Scanner: next category."),
        D("PreviousCategory", "Ctrl+Home", "LB+DpadUp", "Scanner: previous category.", "Shift+Home"),
        D("RepeatItem", "E", "LB+X", "Reread the selected item with its current distance.", "Ctrl+Home"),
        D("PreviousItem", "PageUp", "LB+DpadLeft", "Previous item of the last list (closer in the scanner)."),
        D("NextItem", "PageDown", "LB+DpadRight", "Next item of the last list (farther in the scanner)."),
        D("WalkToItem", "End", "LB+A", "Walk to the selected item; press again to stop."),
        D("RunToItem", "Ctrl+End", "LB+RT", "Run to the selected item; press again to stop.", "Shift+End"),
        D("RunToBase", "B", "LB+LT", "Run to the castle or base camp."),
        D("RunToCrown", "Ctrl+C", "RB+RS", "Run to your lost crown.", "Shift+C"),
        D("RunBehindLeftWall", "Ctrl+LeftArrow", "RB+DpadLeft", "Run just behind (inside) the farthest wall on the left."),
        D("RunBehindRightWall", "Ctrl+RightArrow", "RB+DpadRight", "Run just behind (inside) the farthest wall on the right."),
        D("DumpTarget", "Ctrl+F3", "", "Development: write the object in front of you to the log.", "Shift+F3"),
        D("DumpIsland", "Alt+F3", "", "Development: write every object of the island to the log.", "Ctrl+Shift+F3"),
    };

    private readonly Dictionary<string, KeyBinding> _map = new();
    private readonly Dictionary<string, PadBinding> _pad = new();

    public AccessKeys()
    {
        foreach (var d in Definitions)
        {
            _map[d.Name] = KeyBinding.Parse(d.Default, KeyBinding.None);
            _pad[d.Name] = PadBinding.Parse(d.Pad, PadBinding.None);
        }
    }

    /// <summary>Sets a keyboard shortcut from its config text (invalid text keeps the default).</summary>
    public void Set(string name, string text, IModLog log)
    {
        var def = System.Array.Find(Definitions, d => d.Name == name);
        if (def == null) return;
        _map[name] = KeyBinding.Parse(text, KeyBinding.Parse(def.Default, KeyBinding.None), log);
    }

    /// <summary>Sets a gamepad shortcut from its config text (invalid text keeps the default).</summary>
    public void SetPad(string name, string text, IModLog log)
    {
        var def = System.Array.Find(Definitions, d => d.Name == name);
        if (def == null) return;
        _pad[name] = PadBinding.Parse(text, PadBinding.Parse(def.Pad, PadBinding.None), log);
    }

    public KeyBinding this[string name] => _map.TryGetValue(name, out var k) ? k : KeyBinding.None;

    public PadBinding Pad(string name) => _pad.TryGetValue(name, out var p) ? p : PadBinding.None;
}

/// <summary>Services provided to the core by the loader adapter.</summary>
public sealed class ModContext
{
    public IModLog Log;
    public AccessSettings Settings;

    /// <summary>Mod folder (contains Lang\, Sounds\, Tolk.dll, nvdaControllerClient64.dll).</summary>
    public string ModDirectory;
}
