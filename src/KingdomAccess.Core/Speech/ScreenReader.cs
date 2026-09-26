using System;
using System.IO;
using System.Runtime.InteropServices;

namespace KingdomAccess.Speech;

/// <summary>
/// Output to the active screen reader through Tolk (NVDA, JAWS, SuperNova, ZoomText...),
/// with the Windows voice (SAPI) as a fallback when no screen reader is detected.
/// </summary>
internal static class ScreenReader
{
    private const string TolkDll = "Tolk.dll";

    [DllImport(TolkDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Tolk_Load();

    [DllImport(TolkDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Tolk_Unload();

    [DllImport(TolkDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Tolk_TrySAPI([MarshalAs(UnmanagedType.I1)] bool trySapi);

    [DllImport(TolkDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Tolk_PreferSAPI([MarshalAs(UnmanagedType.I1)] bool preferSapi);

    [DllImport(TolkDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr Tolk_DetectScreenReader();

    [DllImport(TolkDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool Tolk_Output(string text, [MarshalAs(UnmanagedType.I1)] bool interrupt);

    [DllImport(TolkDll, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool Tolk_Silence();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetDllDirectory(string path);

    private static bool _loaded;

    public static bool IsLoaded => _loaded;

    /// <summary>Name of the detected screen reader, or null.</summary>
    public static string ActiveReader
    {
        get
        {
            if (!_loaded) return null;
            try
            {
                IntPtr p = Tolk_DetectScreenReader();
                return p == IntPtr.Zero ? null : Marshal.PtrToStringUni(p);
            }
            catch { return null; }
        }
    }

    public static bool Initialize(string modDirectory, bool sapiFallback, IModLog log)
    {
        try
        {
            string tolkPath = Path.Combine(modDirectory, TolkDll);
            if (!File.Exists(tolkPath))
            {
                log.Error($"[Speech] Tolk.dll not found in {modDirectory}");
                return false;
            }

            // Tolk loads nvdaControllerClient64.dll by name: point it to the mod folder.
            SetDllDirectory(modDirectory);
            NativeLibrary.Load(tolkPath);

            Tolk_TrySAPI(sapiFallback);
            Tolk_PreferSAPI(false);
            Tolk_Load();
            _loaded = true;

            log.Info($"[Speech] Tolk loaded. Screen reader detected: {ActiveReader ?? "none"}");
            return true;
        }
        catch (Exception ex)
        {
            log.Error($"[Speech] Failed to load Tolk: {ex}");
            return false;
        }
    }

    public static void Output(string text, bool interrupt)
    {
        if (!_loaded || string.IsNullOrEmpty(text)) return;
        try { Tolk_Output(text, interrupt); } catch { }
    }

    public static void Silence()
    {
        if (!_loaded) return;
        try { Tolk_Silence(); } catch { }
    }

    public static void Shutdown()
    {
        if (!_loaded) return;
        try { Tolk_Unload(); } catch { }
        _loaded = false;
    }
}
