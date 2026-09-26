using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace KingdomAccess.Localization;

/// <summary>
/// Mod texts. The language automatically follows the game (Language.current.languageCode),
/// unless a code is forced in the configuration. Falls back to English, then to the key.
/// Adding a language = dropping Lang\xx.json next to fr.json and en.json.
/// </summary>
public static class Loc
{
    private const string Fallback = "en";
    private static readonly Dictionary<string, Dictionary<string, string>> Tables = new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, string> _current = new();
    private static Dictionary<string, string> _fallback = new();
    private static string _override = "";
    private static string _lastGameCode;
    private static float _nextCheck;
    private static IModLog _log;

    public static string CurrentCode { get; private set; } = Fallback;

    internal static void Initialize(string modDirectory, string languageOverride, IModLog log)
    {
        _log = log;
        _override = (languageOverride ?? "").Trim();
        string dir = Path.Combine(modDirectory, "Lang");
        if (Directory.Exists(dir))
        {
            foreach (string file in Directory.GetFiles(dir, "*.json"))
            {
                try
                {
                    var table = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file));
                    if (table != null) Tables[Path.GetFileNameWithoutExtension(file)] = table;
                }
                catch (Exception ex)
                {
                    log.Error($"[Langue] Fichier illisible {file} : {ex.Message}");
                }
            }
        }
        log.Info($"[Langue] Langues disponibles : {string.Join(", ", Tables.Keys)}");
        Tables.TryGetValue(Fallback, out _fallback);
        _fallback ??= new Dictionary<string, string>();
        Apply(_override.Length > 0 ? _override : Fallback);
    }

    /// <summary>Called every frame; checks the game language once per second.</summary>
    internal static void Tick(float time)
    {
        if (_override.Length > 0 || time < _nextCheck) return;
        _nextCheck = time + 1f;

        string code = GameLanguageCode();
        if (string.IsNullOrEmpty(code) || code == _lastGameCode) return;
        _lastGameCode = code;
        Apply(code);
    }

    private static string GameLanguageCode()
    {
        try
        {
            var texts = global::Language.current;
            return texts == null ? null : texts.languageCode;
        }
        catch { return null; }
    }

    private static void Apply(string code)
    {
        string resolved = Resolve(code);
        _current = Tables.TryGetValue(resolved, out var t) ? t : _fallback;
        if (resolved != CurrentCode)
            _log?.Info($"[Language] Mod language: {resolved} (game: {code})");
        CurrentCode = resolved;
    }

    private static string Resolve(string code)
    {
        if (string.IsNullOrEmpty(code)) return Fallback;
        code = code.Trim().Replace('_', '-');
        if (Tables.ContainsKey(code)) return code;
        int dash = code.IndexOf('-');
        if (dash > 0 && Tables.ContainsKey(code[..dash])) return code[..dash];
        return Fallback;
    }

    /// <summary>
    /// Adds the texts of another mod (folder with fr.json, en.json...). Keys are merged
    /// with those of the accessibility mod, for each language.
    /// </summary>
    public static void AddFolder(string dir)
    {
        if (!Directory.Exists(dir)) return;
        foreach (string file in Directory.GetFiles(dir, "*.json"))
        {
            try
            {
                var extra = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file));
                if (extra == null) continue;
                string code = Path.GetFileNameWithoutExtension(file);
                if (!Tables.TryGetValue(code, out var table)) Tables[code] = table = new Dictionary<string, string>();
                foreach (var kv in extra) table[kv.Key] = kv.Value;
            }
            catch (Exception ex) { _log?.Error($"[Langue] Fichier illisible {file} : {ex.Message}"); }
        }
        Tables.TryGetValue(Fallback, out _fallback);
        _fallback ??= new Dictionary<string, string>();
        string current = CurrentCode;
        CurrentCode = "";
        Apply(current);
    }

    /// <summary>Translated text; {0}, {1}... are replaced by the arguments.</summary>
    public static string T(string key, params object[] args)
    {
        if (!_current.TryGetValue(key, out string text) && !_fallback.TryGetValue(key, out text))
            text = key;
        if (args == null || args.Length == 0) return text;
        try { return string.Format(text, args); }
        catch { return text; }
    }

    /// <summary>Translated text if the key exists, otherwise null.</summary>
    public static string TryT(string key)
    {
        if (_current.TryGetValue(key, out string text)) return text;
        return _fallback.TryGetValue(key, out text) ? text : null;
    }
}
