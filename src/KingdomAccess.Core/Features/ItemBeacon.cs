using KingdomAccess.Game;
using KingdomAccess.Localization;
using KingdomAccess.Speech;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Features;

/// <summary>
/// Audio beacon on the item selected in the last list (scanner, radar, tutorial journal...):
/// a beep faster and louder when closer, in the ear of the side where the item is. Stops with
/// "you have arrived" when the player reaches it, or when the shortcut is pressed again.
/// </summary>
internal static class ItemBeacon
{
    private const float ArriveDistance = 1.5f;
    private static Component _target;
    private static string _name;
    private static float _nextBeep;

    public static bool Active => _target != null;

    public static void Toggle()
    {
        // The selected item of the last list, otherwise the object in front of the player.
        var target = ListNav.SelectedTarget ?? HoverAnnouncer.CurrentTarget;
        if (_target != null && (target == null || target == _target))
        {
            Stop();
            SpeechOut.Say(Loc.T("beacon.off"));
            return;
        }
        if (target == null) { SpeechOut.Say(Loc.T("beacon.none")); return; }
        _target = target;
        _name = (ListNav.SelectedTarget != null ? ListNav.SelectedName : null) ?? HoverAnnouncer.CurrentName ?? "";
        _nextBeep = 0f;
        SpeechOut.Say(Loc.T("beacon.on", _name));
    }

    public static void Stop() { _target = null; _name = null; }

    /// <summary>Stops every beacon: this one and the tutorial one (until its next step).</summary>
    public static void StopAll()
    {
        Stop();
        TutorialNarrator.MuteBeacon();
        SpeechOut.Say(Loc.T("beacon.all_off"));
    }

    public static void Tick(Player player, float now)
    {
        if (_target == null) return;
        if (_target == null || !_target.gameObject.activeInHierarchy) { Stop(); return; }

        float dx = _target.transform.position.x - GameState.PlayerX(player);
        float d = Mathf.Abs(dx);
        if (d <= ArriveDistance)
        {
            SpeechOut.Say(Loc.T("beacon.reached", _name), true);
            Stop();
            return;
        }
        BeepToward(dx, now, ref _nextBeep);
    }

    /// <summary>
    /// The beacon beep toward something dx away: faster and louder when closer, on its side.
    /// Shared by every beacon of the mod (items, tutorial, Greed cave).
    /// </summary>
    public static void BeepToward(float dx, float now, ref float nextBeep)
    {
        if (now < nextBeep) return;
        float d = Mathf.Abs(dx);
        float closeness = Mathf.Clamp01(1f - d / 40f);
        nextBeep = now + Mathf.Lerp(1.2f, 0.15f, closeness);
        GameAudio.Beep(Mathf.Lerp(0.25f, 0.55f, closeness), d < 1f ? 0f : Mathf.Sign(dx) * Mathf.Clamp01(d / 10f));
    }
}
