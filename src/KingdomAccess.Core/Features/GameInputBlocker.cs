using System;

namespace KingdomAccess.Features;

/// <summary>
/// While a gamepad layer button (LB or RB by default) is held, the game's own input maps are
/// disabled through Rewired, so that the D-pad and face buttons only trigger mod shortcuts
/// (no coin dropped, no movement, no ability by mistake). Everything is restored on release.
/// </summary>
internal static class GameInputBlocker
{
    private static bool _blocked;
    private static IModLog _log;

    public static void Initialize(IModLog log) => _log = log;

    public static void Tick(AccessSettings s)
    {
        bool want = s.GamepadEnabled && s.GamepadBlockGame && Gamepad.Connected && LayerHeld(s);
        if (want == _blocked) return;
        if (SetGameInput(!want)) _blocked = want;
    }

    private static bool LayerHeld(AccessSettings s)
    {
        foreach (var b in s.GamepadLayerButtons)
            if (Gamepad.IsDown(b)) return true;
        return false;
    }

    /// <summary>Enables or disables every input map of the game's players 1 and 2.</summary>
    private static bool SetGameInput(bool enabled)
    {
        try
        {
            if (!Rewired.ReInput.isReady) return false;
            for (int id = 0; id < 2; id++)
            {
                var player = Rewired.ReInput.players.GetPlayer(id);
                player?.controllers.maps.SetAllMapsEnabled(enabled);
            }
            return true;
        }
        catch (Exception ex)
        {
            _log?.Warn($"[Gamepad] Could not {(enabled ? "restore" : "block")} the game input: {ex.Message}");
            return true; // do not retry every frame
        }
    }

    /// <summary>Restores the game input (e.g. when the island changes).</summary>
    public static void Release()
    {
        if (_blocked) { SetGameInput(true); _blocked = false; }
    }
}
