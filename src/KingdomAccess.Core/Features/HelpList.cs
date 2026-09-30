using System.Collections.Generic;
using System.Linq;
using KingdomAccess.Localization;
using UnityEngine;

namespace KingdomAccess.Features;

/// <summary>
/// Help list (F1 by default): every shortcut with its currently configured key and gamepad
/// buttons, spoken in the mod language, browsable with the list shortcuts. Built from
/// <see cref="AccessKeys.Definitions"/>, so it always matches the configuration file.
/// </summary>
internal static class HelpList
{
    public static void Show(AccessSettings s)
    {
        var entries = new List<NavEntry>();
        foreach (var d in AccessKeys.Definitions)
        {
            var key = s.Keys[d.Name];
            var pad = s.GamepadEnabled ? s.Keys.Pad(d.Name) : PadBinding.None;
            if (key.Key == KeyCode.None && pad.Button == PadButton.None) continue;
            string action = Loc.TryT("keyhelp." + d.Name.ToLowerInvariant()) ?? d.Description;
            var how = new List<string>();
            if (key.Key != KeyCode.None) how.Add(Spoken(key));
            if (pad.Button != PadButton.None) how.Add(Loc.T("help.pad", Spoken(pad)));
            entries.Add(new NavEntry { Text = $"{action} : {string.Join(", ", how)}", Name = action });
        }
        string summary = Loc.T("help.intro", entries.Count, AccessMod.KeyName("PreviousItem"), AccessMod.KeyName("NextItem"));
        ListNav.ShowStatic(entries, summary + " " + (entries.Count > 0 ? entries[0].Text : ""));
    }

    /// <summary>Key text for speech: "Ctrl+PageUp" becomes "Contrôle Page haut" in French.</summary>
    public static string Spoken(KeyBinding k)
    {
        if (k == null || k.Key == KeyCode.None) return Loc.T("key.none");
        var parts = new List<string>();
        if (k.Ctrl) parts.Add(Loc.T("key.ctrl"));
        if (k.Shift) parts.Add(Loc.T("key.shift"));
        if (k.Alt) parts.Add(Loc.T("key.alt"));
        string name = k.Key.ToString();
        parts.Add(Loc.TryT("key." + name.ToLowerInvariant()) ?? name);
        return string.Join(" ", parts.Where(p => p.Length > 0));
    }

    /// <summary>Gamepad buttons for speech: "LB+DpadRight" becomes "LB plus croix droite".</summary>
    public static string Spoken(PadBinding p)
    {
        if (p == null || p.Button == PadButton.None) return Loc.T("key.none");
        var names = p.Held.Select(PadName).ToList();
        names.Add(PadName(p.Button));
        return string.Join(" " + Loc.T("pad.plus") + " ", names);
    }

    private static string PadName(PadButton b) => Loc.TryT("pad." + b.ToString().ToLowerInvariant()) ?? b.ToString();
}
