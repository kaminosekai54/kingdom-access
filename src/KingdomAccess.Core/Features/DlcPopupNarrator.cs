using System.Collections.Generic;
using KingdomAccess.Localization;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Features;

/// <summary>
/// DLC presentation window (title and artwork are images, only the button has text): announces
/// which DLC it presents and what the buttons do (play, buy, cancel), plus its purchase or error
/// message when there is one.
/// </summary>
internal static class DlcPopupNarrator
{
    private static DlcPopup _popup;
    private static float _nextLookup;
    private static bool _wasOpen, _announced;
    private static float _openSince;

    public static void Tick(float now)
    {
        if ((_popup == null || !_popup.isActiveAndEnabled) && now >= _nextLookup)
        {
            _nextLookup = now + 1f;
            try { _popup = Object.FindFirstObjectByType<DlcPopup>(); } catch { _popup = null; }
        }

        bool open = false;
        try { open = _popup != null && _popup.isActiveAndEnabled && _popup.IsOpen; } catch { }
        if (open != _wasOpen)
        {
            _wasOpen = open;
            _openSince = now;
            _announced = false;
        }
        // Wait a moment: the menu narrator first reads the selected button, then this description.
        if (open && !_announced && now - _openSince > 0.4f)
        {
            _announced = true;
            SpeechOut.Say(Describe(), false);
        }
    }

    // ---------- Peaceful difficulty warning ----------

    private static PeacefulDifficultyPopup _peaceful;
    private static float _nextPeacefulLookup, _peacefulSince;
    private static bool _peacefulOpen, _peacefulAnnounced;

    /// <summary>
    /// Warning shown when the peaceful difficulty is chosen (only its OK and Back buttons have
    /// text): says what it means once the buttons have been read.
    /// </summary>
    public static void TickPeaceful(float now)
    {
        if ((_peaceful == null || !_peaceful.isActiveAndEnabled) && now >= _nextPeacefulLookup)
        {
            _nextPeacefulLookup = now + 1f;
            try { _peaceful = Object.FindFirstObjectByType<PeacefulDifficultyPopup>(); } catch { _peaceful = null; }
        }
        bool open = false;
        try
        {
            open = _peaceful != null && _peaceful.isActiveAndEnabled && _peaceful._okButton != null
                   && _peaceful._okButton.gameObject.activeInHierarchy;
        }
        catch { }
        if (open != _peacefulOpen) { _peacefulOpen = open; _peacefulSince = now; _peacefulAnnounced = false; }
        if (open && !_peacefulAnnounced && now - _peacefulSince > 0.4f)
        {
            _peacefulAnnounced = true;
            SpeechOut.Say(Loc.T("peaceful.popup"), false);
        }
    }

    private static string Describe()
    {
        var parts = new List<string>();
        try
        {
            string type = _popup._shownDLCType.ToString();
            string name = Loc.TryT("dlcname." + type.ToLowerInvariant()) ?? type;
            parts.Add(Loc.T("dlcpopup.title", name));

            bool play = Active(_popup._playButton), buy = Active(_popup._buyButton);
            if (play) parts.Add(Loc.T("dlcpopup.play"));
            else if (buy) parts.Add(Loc.T("dlcpopup.buy"));

            string msg = Visible(_popup._purchaseText);
            if (!string.IsNullOrEmpty(msg)) parts.Add(msg);
            if (_popup._errorMessage != null && _popup._errorMessage.activeInHierarchy) parts.Add(Loc.T("dlcpopup.error"));
        }
        catch { }
        return string.Join(". ", parts);
    }

    private static bool Active(UnityEngine.UI.Button b) => b != null && b.gameObject.activeInHierarchy && b.interactable;

    private static string Visible(UnityEngine.UI.Text t) =>
        t != null && t.gameObject.activeInHierarchy ? t.text?.Trim() : null;
}
