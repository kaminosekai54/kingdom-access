using KingdomAccess.Localization;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Features;

/// <summary>
/// Startup and intro: the game shows a welcome screen, a save warning, logos, loading screens and
/// an intro animation without any text to read. Each visible stage of the game's program
/// director is announced, then the start and the end of the intro animation, so the player
/// always knows what is happening and when it is their turn.
/// </summary>
internal static class StartupNarrator
{
    private static string _lastState;
    private static float _nextCheck;
    private static bool _introAnnounced, _introEnded;
    private static IModLog _log;

    public static void Initialize(IModLog log) => _log = log;

    public static void Tick(float now)
    {
        if (now < _nextCheck) return;
        _nextCheck = now + 0.25f;

        string state;
        try { state = ProgramDirector.state.ToString(); }
        catch { return; }

        if (state != _lastState)
        {
            _lastState = state;
            _log?.Info($"[Startup] Game stage: {state}");
            string text = Loc.TryT("startup." + state.ToLowerInvariant());
            if (text != null) SpeechOut.Say(text, false);
            // A new level is being built: the intro may play again (new game).
            if (state == "BuildingLevel") { _introAnnounced = false; _introEnded = false; }
        }

        if (state == "RunningGame") TickIntro();
    }

    /// <summary>Announces the intro animation (monarch arriving) and its end.</summary>
    private static void TickIntro()
    {
        try
        {
            var intro = Object.FindFirstObjectByType<IntroSequence>();
            bool running = intro != null && intro.isActiveAndEnabled && !intro._introEnded;
            if (running && !_introAnnounced)
            {
                _introAnnounced = true;
                _introEnded = false;
                SpeechOut.Say(Loc.T("startup.intro"), false);
            }
            else if (!running && _introAnnounced && !_introEnded)
            {
                _introEnded = true;
                SpeechOut.Say(Loc.T("startup.intro_end"), false);
            }
        }
        catch { }
    }
}
