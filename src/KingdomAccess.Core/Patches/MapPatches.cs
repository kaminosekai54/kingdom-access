using HarmonyLib;
using KingdomAccess.Game;
using KingdomAccess.Localization;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Patches;

/// <summary>World map: announces the chosen island and the hovered island (locked or not).</summary>
[HarmonyPatch(typeof(UIMainMap), nameof(UIMainMap.SelectLand))]
internal static class MapSelectLandPatch
{
    private static void Postfix(int index)
    {
        if (!AccessMod.Active) return;
        SpeechOut.Say(Loc.T("map.island", index + 1));
    }
}

[HarmonyPatch(typeof(UIMainMapLand), nameof(UIMainMapLand.SelectButton))]
internal static class MapLandSelectButtonPatch
{
    private static void Postfix(UIMainMapLand __instance)
    {
        if (!AccessMod.Active || __instance == null) return;
        string name = ObjectNames.CleanName(__instance.gameObject.name);
        bool unlocked = true;
        try { unlocked = __instance.IsUnlocked; } catch { }
        SpeechOut.Say(unlocked ? name : Loc.T("map.locked", name), false);
    }
}
