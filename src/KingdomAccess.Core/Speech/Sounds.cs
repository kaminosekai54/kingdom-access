using System;
using System.IO;
using System.Runtime.InteropServices;

namespace KingdomAccess.Speech;

/// <summary>
/// Mod sounds (WAV files in the Sounds folder). Users can replace them
/// by keeping the same file name.
/// </summary>
public static class Sounds
{
    [DllImport("winmm.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);

    private const uint SND_ASYNC = 0x0001;
    private const uint SND_NODEFAULT = 0x0002;
    private const uint SND_FILENAME = 0x00020000;

    private static string _dir;
    private static bool _enabled = true;

    internal static void Initialize(string modDirectory, bool enabled)
    {
        _dir = Path.Combine(modDirectory, "Sounds");
        _enabled = enabled;
    }

    /// <summary>Plays Sounds\name.wav if it exists (without blocking the game).</summary>
    public static void Play(string name)
    {
        if (!_enabled || _dir == null) return;
        string path = Path.Combine(_dir, name + ".wav");
        if (!File.Exists(path)) return;
        try { PlaySound(path, IntPtr.Zero, SND_ASYNC | SND_FILENAME | SND_NODEFAULT); } catch { }
    }
}
