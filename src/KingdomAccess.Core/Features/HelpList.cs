using System.Collections.Generic;
using System.Linq;
using KingdomAccess.Localization;
using UnityEngine;

namespace KingdomAccess.Features;

/// <summary>
/// Help list (F1 by default): every shortcut with its currently configured key, spoken in the
/// mod language, browsable with the list keys. Built from <see cref="AccessKeys.Definitions"/>,
/// so it always matches the configuration file.
/// </summary>
internal static class HelpList
{
    public static void Show(AccessSettings s)
    {
        var entries = new List<NavEntry>();
        foreach (var d in AccessKeys.Definitions)
        {
            var key = s.Keys[d.Name];
            if (key.Key == KeyCode.None) continue;
            string action = Loc.TryT("keyhelp." + d.Name.ToLowerInvariant()) ?? d.Description;
            entries.Add(new NavEntry { Text = $"{action} : {Spoken(key)}", Name = action });
        }
        string summary = Loc.T("help.intro", entries.Count, Spoken(s.Keys["PreviousItem"]), Spoken(s.Keys["NextItem"]));
        ListNav.ShowStatic(entries, summary + " " + (entries.Count > 0 ? entries[0].Text : ""));
    }

    /// <summary>Key text for speech: "Shift+PageUp" becomes "Maj Page haut" in French.</summary>
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
}
