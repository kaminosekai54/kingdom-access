using System;
using UnityEngine;

namespace KingdomAccess;

/// <summary>
/// A configurable keyboard shortcut, written as text in the config file:
/// "O", "Shift+V", "Ctrl+Shift+F3", "PageDown", "Ctrl+LeftArrow"... Key names are Unity KeyCode
/// names. Modifiers must match exactly, so "V" does not fire while Shift is held (that is
/// "Shift+V"). An empty string or "None" disables the shortcut.
/// </summary>
public sealed class KeyBinding
{
    public KeyCode Key { get; }
    public bool Ctrl { get; }
    public bool Shift { get; }
    public bool Alt { get; }
    public string Text { get; }

    public static readonly KeyBinding None = new(KeyCode.None, false, false, false, "");

    private KeyBinding(KeyCode key, bool ctrl, bool shift, bool alt, string text)
    {
        Key = key; Ctrl = ctrl; Shift = shift; Alt = alt; Text = text;
    }

    /// <summary>Parses "Ctrl+Shift+F3". Returns <paramref name="fallback"/> if the text is invalid.</summary>
    public static KeyBinding Parse(string text, KeyBinding fallback, IModLog log = null)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Equals("None", StringComparison.OrdinalIgnoreCase)) return None;
        bool ctrl = false, shift = false, alt = false;
        KeyCode key = KeyCode.None;
        foreach (string raw in text.Split('+'))
        {
            string part = raw.Trim();
            if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || part.Equals("Control", StringComparison.OrdinalIgnoreCase)) ctrl = true;
            else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase)) shift = true;
            else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase)) alt = true;
            else if (!Enum.TryParse(part, true, out key))
            {
                log?.Warn($"[Keys] Unknown key \"{part}\" in \"{text}\", using default \"{fallback?.Text}\".");
                return fallback ?? None;
            }
        }
        return key == KeyCode.None ? (fallback ?? None) : new KeyBinding(key, ctrl, shift, alt, text.Trim());
    }

    /// <summary>True on the frame the shortcut is pressed (with exactly its modifiers).</summary>
    public bool Pressed()
    {
        if (Key == KeyCode.None || !Input.GetKeyDown(Key)) return false;
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        return ctrl == Ctrl && shift == Shift && alt == Alt;
    }

    public override string ToString() => Text;
}
