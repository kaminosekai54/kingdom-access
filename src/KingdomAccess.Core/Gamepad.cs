using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace KingdomAccess;

/// <summary>Gamepad buttons, Xbox layout (PlayStation: A = Cross, B = Circle, X = Square, Y = Triangle).</summary>
public enum PadButton
{
    None, A, B, X, Y, LB, RB, LT, RT, Back, Start, LS, RS, DpadUp, DpadDown, DpadLeft, DpadRight
}

/// <summary>
/// Gamepad reading through XInput (Xbox pads, and any pad Steam Input exposes as one).
/// Independent from the game's own input library, so the mod can read the pad even while the
/// game's input is blocked. Polled once per frame.
/// </summary>
public static class Gamepad
{
    [StructLayout(LayoutKind.Sequential)]
    private struct XInputGamepad
    {
        public ushort Buttons;
        public byte LeftTrigger, RightTrigger;
        public short ThumbLX, ThumbLY, ThumbRX, ThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputState
    {
        public uint PacketNumber;
        public XInputGamepad Pad;
    }

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern uint GetState14(uint index, out XInputState state);

    [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
    private static extern uint GetState910(uint index, out XInputState state);

    private static readonly Dictionary<PadButton, ushort> Masks = new()
    {
        { PadButton.DpadUp, 0x0001 }, { PadButton.DpadDown, 0x0002 }, { PadButton.DpadLeft, 0x0004 },
        { PadButton.DpadRight, 0x0008 }, { PadButton.Start, 0x0010 }, { PadButton.Back, 0x0020 },
        { PadButton.LS, 0x0040 }, { PadButton.RS, 0x0080 }, { PadButton.LB, 0x0100 }, { PadButton.RB, 0x0200 },
        { PadButton.A, 0x1000 }, { PadButton.B, 0x2000 }, { PadButton.X, 0x4000 }, { PadButton.Y, 0x8000 },
    };

    private const byte TriggerThreshold = 100;

    private static readonly HashSet<PadButton> Down = new();
    private static readonly HashSet<PadButton> Pressed = new();
    private static bool _useLegacyDll, _unavailable;

    /// <summary>True if an XInput pad was read this frame.</summary>
    public static bool Connected { get; private set; }

    /// <summary>Reads the first connected pad. Call once per frame.</summary>
    public static void Poll()
    {
        Pressed.Clear();
        if (_unavailable) { Connected = false; return; }

        XInputState state = default;
        bool ok = false;
        for (uint i = 0; i < 4 && !ok; i++) ok = Read(i, out state);
        Connected = ok;
        var now = new HashSet<PadButton>();
        if (ok)
        {
            foreach (var kv in Masks)
                if ((state.Pad.Buttons & kv.Value) != 0) now.Add(kv.Key);
            if (state.Pad.LeftTrigger > TriggerThreshold) now.Add(PadButton.LT);
            if (state.Pad.RightTrigger > TriggerThreshold) now.Add(PadButton.RT);
        }
        foreach (var b in now)
            if (!Down.Contains(b)) Pressed.Add(b);
        Down.Clear();
        foreach (var b in now) Down.Add(b);
    }

    private static bool Read(uint index, out XInputState state)
    {
        state = default;
        try
        {
            return (_useLegacyDll ? GetState910(index, out state) : GetState14(index, out state)) == 0;
        }
        catch (DllNotFoundException)
        {
            if (!_useLegacyDll) { _useLegacyDll = true; return Read(index, out state); }
            _unavailable = true;
            return false;
        }
        catch { return false; }
    }

    public static bool IsDown(PadButton b) => Down.Contains(b);
    public static bool WasPressed(PadButton b) => Pressed.Contains(b);
}

/// <summary>
/// A configurable gamepad shortcut: "LB+DpadLeft", "RB+A"... The last button triggers the
/// shortcut; the others must be held. Empty or "None" disables it.
/// </summary>
public sealed class PadBinding
{
    public PadButton[] Held { get; }
    public PadButton Button { get; }
    public string Text { get; }

    public static readonly PadBinding None = new(Array.Empty<PadButton>(), PadButton.None, "");

    private PadBinding(PadButton[] held, PadButton button, string text) { Held = held; Button = button; Text = text; }

    public static PadBinding Parse(string text, PadBinding fallback, IModLog log = null)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Equals("None", StringComparison.OrdinalIgnoreCase)) return None;
        var parts = text.Split('+');
        var buttons = new List<PadButton>();
        foreach (string raw in parts)
        {
            if (!Enum.TryParse(raw.Trim(), true, out PadButton b) || b == PadButton.None)
            {
                log?.Warn($"[Gamepad] Unknown button \"{raw.Trim()}\" in \"{text}\", using default \"{fallback?.Text}\".");
                return fallback ?? None;
            }
            buttons.Add(b);
        }
        var main = buttons[buttons.Count - 1];
        buttons.RemoveAt(buttons.Count - 1);
        return new PadBinding(buttons.ToArray(), main, text.Trim());
    }

    /// <summary>True on the frame the last button is pressed while the others are held.</summary>
    public bool Pressed()
    {
        if (Button == PadButton.None || !Gamepad.WasPressed(Button)) return false;
        foreach (var h in Held) if (!Gamepad.IsDown(h)) return false;
        return true;
    }

    public override string ToString() => Text;
}
