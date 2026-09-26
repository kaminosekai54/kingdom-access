using System.Collections.Generic;
using KingdomAccess.Localization;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Features;

/// <summary>
/// New game screen (choice of world, difficulty and monarch appearance).
/// This screen reads the gamepad itself, outside the standard UI: follow the active screen and
/// the highlighted element, and announce them.
/// </summary>
internal static class NewGameNarrator
{
    private static BiomeSelect _menu;
    private static float _nextLookup;
    private static bool _wasShown;
    private static int _lastScreen = -1;
    private static int _lastBiome = -1, _lastDifficulty = -1, _lastSkin = -1;

    public static void Tick(float now)
    {
        if (_menu == null && now >= _nextLookup)
        {
            _nextLookup = now + 1f;
            try { _menu = Object.FindFirstObjectByType<BiomeSelect>(); } catch { }
        }
        if (_menu == null) return;

        bool shown;
        try { shown = _menu.isActiveAndEnabled && _menu._isShown; }
        catch { _menu = null; return; }

        if (!shown)
        {
            _wasShown = false;
            _lastScreen = _lastBiome = _lastDifficulty = _lastSkin = -1;
            return;
        }

        int screen = (int)_menu._activeScreen;
        int biome = _menu._activeBiomeSelectorIndex;
        int difficulty = _menu._activeDifficultySelectorIndex;
        int skin = _menu._activeSkinSelectorIndex;

        if (!_wasShown || screen != _lastScreen)
        {
            _wasShown = true;
            _lastScreen = screen;
            _lastBiome = biome; _lastDifficulty = difficulty; _lastSkin = skin;
            string title = Loc.T("newgame.screen." + _menu._activeScreen.ToString().ToLowerInvariant());
            SpeechOut.Say($"{title}. {Current(screen)}. {Loc.T("newgame.help")}", true);
            return;
        }

        if (screen == (int)BiomeSelect.Screen.BiomeSelect && biome != _lastBiome) { _lastBiome = biome; SpeechOut.Say(Current(screen), true); }
        if (screen == (int)BiomeSelect.Screen.DifficultySelect && difficulty != _lastDifficulty) { _lastDifficulty = difficulty; SpeechOut.Say(Current(screen), true); }
        if (screen == (int)BiomeSelect.Screen.SkinSelect && skin != _lastSkin) { _lastSkin = skin; SpeechOut.Say(Current(screen), true); }
    }

    private static string Current(int screen)
    {
        try
        {
            if (screen == (int)BiomeSelect.Screen.BiomeSelect) return BiomeText();
            if (screen == (int)BiomeSelect.Screen.DifficultySelect) return DifficultyText();
            if (screen == (int)BiomeSelect.Screen.SkinSelect) return SkinText();
        }
        catch { }
        return "";
    }

    private static string Position(int i, int count) => Loc.T("scan.position", i + 1, count);

    private static string BiomeText()
    {
        var list = _menu._filteredBiomeSelectors;
        int i = _menu._activeBiomeSelectorIndex;
        if (list == null || i < 0 || i >= list.Count) return "";
        var sel = list[i];
        string name = Loc.TryT("biome." + sel.biomeIndex) ?? Loc.T("newgame.biome_n", sel.biomeIndex + 1);
        var parts = new List<string> { name, Position(i, list.Count) };
        if (IsLocked(_menu._lockedBiomeSelectors, sel)) parts.Add(Loc.T("lock.generic"));
        return string.Join(", ", parts);
    }

    private static string DifficultyText()
    {
        var list = _menu._difficultySelectorsList;
        int i = _menu._activeDifficultySelectorIndex;
        if (list == null || i < 0 || i >= list.Count) return "";
        var sel = list[i];
        // The texts displayed by the game are already translated.
        string name = null;
        try
        {
            var texts = new List<string>();
            var src = sel._difficultyTexts;
            for (int k = 0; src != null && k < src.Count; k++)
            {
                var t = src[k];
                if (t != null && !string.IsNullOrWhiteSpace(t.text)) texts.Add(t.text.Trim());
            }
            if (texts.Count > 0) name = string.Join(", ", texts);
        }
        catch { }
        name ??= Loc.TryT("difficulty." + sel.DifficultyIndex.ToString().ToLowerInvariant()) ?? sel.DifficultyIndex.ToString();
        return $"{name}, {Position(i, list.Count)}";
    }

    private static string SkinText()
    {
        var list = _menu._filteredSkinSelectors;
        int i = _menu._activeSkinSelectorIndex;
        if (list == null || i < 0 || i >= list.Count) return "";
        var sel = list[i];
        string type = sel._monarchType.ToString();
        string name = Loc.TryT("monarch." + type.ToLowerInvariant()) ?? System.Text.RegularExpressions.Regex.Replace(type, "([a-z])([A-Z])", "$1 $2");
        var parts = new List<string> { name, Position(i, list.Count) };
        if (IsLocked(_menu._lockedSkinSelectors, sel)) parts.Add(Loc.T("lock.generic"));
        return string.Join(", ", parts);
    }

    private static bool IsLocked<T>(Il2CppSystem.Collections.Generic.List<T> locked, T item) where T : Il2CppSystem.Object
    {
        try
        {
            if (locked == null) return false;
            for (int k = 0; k < locked.Count; k++)
                if (locked[k] != null && locked[k].Pointer == item.Pointer) return true;
        }
        catch { }
        return false;
    }

    /// <summary>Called by the patches after a choice changes: announces the highlighted element.</summary>
    internal static void OnChanged(BiomeSelect menu, BiomeSelect.Screen screen, IModLog log)
    {
        if (!AccessMod.Active || menu == null) return;
        _menu = menu;
        string text = Current((int)screen);
        _lastScreen = (int)screen;
        try
        {
            _lastBiome = menu._activeBiomeSelectorIndex;
            _lastDifficulty = menu._activeDifficultySelectorIndex;
            _lastSkin = menu._activeSkinSelectorIndex;
            log?.Info($"[NewGame] {screen} world={_lastBiome} difficulty={_lastDifficulty} monarch={_lastSkin} -> {text}");
        }
        catch { }
        if (!string.IsNullOrEmpty(text)) SpeechOut.Say(text, true);
    }
}

/// <summary>
/// Choice changes go through these three game methods: announce right after.
/// More reliable than watching the indexes, which is not enough on this screen.
/// </summary>
[HarmonyLib.HarmonyPatch(typeof(BiomeSelect))]
internal static class BiomeSelectPatches
{
    internal static IModLog Log;

    [HarmonyLib.HarmonyPostfix, HarmonyLib.HarmonyPatch(nameof(BiomeSelect.ChangeSelectedBiome))]
    private static void Biome(BiomeSelect __instance) => NewGameNarrator.OnChanged(__instance, BiomeSelect.Screen.BiomeSelect, Log);

    [HarmonyLib.HarmonyPostfix, HarmonyLib.HarmonyPatch(nameof(BiomeSelect.ChangeSelectedDifficulty))]
    private static void Difficulty(BiomeSelect __instance) => NewGameNarrator.OnChanged(__instance, BiomeSelect.Screen.DifficultySelect, Log);

    [HarmonyLib.HarmonyPostfix, HarmonyLib.HarmonyPatch(nameof(BiomeSelect.ChangeSelectedSkin))]
    private static void Skin(BiomeSelect __instance) => NewGameNarrator.OnChanged(__instance, BiomeSelect.Screen.SkinSelect, Log);
}
