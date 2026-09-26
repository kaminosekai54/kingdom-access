using HarmonyLib;
using KingdomAccess.Localization;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Patches;

/// <summary>
/// Blazon editor: announces the result of each button (background, emblem, colours, crown)
/// and provides a full summary (ReadScreen shortcut).
/// </summary>
internal static class Blazon
{
    public static string Summary(BlazonEditor e)
    {
        return string.Join(", ",
            Loc.T("blazon.background", SpriteName(e._backgroundPreview)),
            Loc.T("blazon.emblem", SpriteName(e._emblemPreview)),
            Loc.T("blazon.primary", ColorOf(e.primaryColour)),
            Loc.T("blazon.secondary", ColorOf(e.secondaryColour)),
            Loc.T("blazon.emblem_colour", ColorOf(e.emblemColour)),
            Crown(e));
    }

    public static string Crown(BlazonEditor e)
    {
        try
        {
            bool cursed = e._crown != null && e._crown.sprite != null && e._cursedCrownSprite != null
                          && e._crown.sprite.Pointer == e._cursedCrownSprite.Pointer;
            return Loc.T(cursed ? "blazon.crown_cursed" : "blazon.crown_regular");
        }
        catch { return Loc.T("blazon.crown_regular"); }
    }

    private static readonly System.Text.RegularExpressions.Regex SpriteId =
        new(@"(pattern|emblem)(?:[ _]([a-z]+))?[ _](\d+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    /// <summary>
    /// Description of a background or emblem from the image name
    /// ("banner_1_pattern_10" -> "vertical band in the middle"). Biome variants:
    /// their own description if known, otherwise "number N".
    /// </summary>
    public static string SpriteName(UnityEngine.UI.Image img)
    {
        try
        {
            if (img == null || img.sprite == null) return Loc.T("blazon.none");
            string raw = img.sprite.name;
            var m = SpriteId.Match(raw);
            if (m.Success)
            {
                string kind = m.Groups[1].Value.ToLowerInvariant();
                string biome = m.Groups[2].Success ? m.Groups[2].Value.ToLowerInvariant() : null;
                string num = m.Groups[3].Value;
                string desc = biome == null ? Loc.TryT($"blazon.{kind}.{num}") : Loc.TryT($"blazon.{kind}.{biome}.{num}");
                return desc ?? Loc.T("blazon.number", int.Parse(num) + 1);
            }
            return raw.Replace("_", " ").Trim();
        }
        catch { return Loc.T("blazon.none"); }
    }

    public static string ColorOf(UnityEngine.UI.Image img)
    {
        try { return img == null ? Loc.T("blazon.none") : ColorNames.Name(img.color); }
        catch { return Loc.T("blazon.none"); }
    }

    public static void Say(string text) { if (AccessMod.Active) SpeechOut.Say(text, true); }
}

[HarmonyPatch(typeof(BlazonEditor))]
internal static class BlazonEditorPatches
{
    [HarmonyPostfix, HarmonyPatch(nameof(BlazonEditor.OnButtonNextEmblem))]
    private static void NextEmblem(BlazonEditor __instance) => Blazon.Say(Loc.T("blazon.emblem", Blazon.SpriteName(__instance._emblemPreview)));

    [HarmonyPostfix, HarmonyPatch(nameof(BlazonEditor.OnButtonPreviousEmblem))]
    private static void PrevEmblem(BlazonEditor __instance) => Blazon.Say(Loc.T("blazon.emblem", Blazon.SpriteName(__instance._emblemPreview)));

    [HarmonyPostfix, HarmonyPatch(nameof(BlazonEditor.OnButtonNextBackground))]
    private static void NextBackground(BlazonEditor __instance) => Blazon.Say(Loc.T("blazon.background", Blazon.SpriteName(__instance._backgroundPreview)));

    [HarmonyPostfix, HarmonyPatch(nameof(BlazonEditor.OnButtonPreviousBackground))]
    private static void PrevBackground(BlazonEditor __instance) => Blazon.Say(Loc.T("blazon.background", Blazon.SpriteName(__instance._backgroundPreview)));

    [HarmonyPostfix, HarmonyPatch(nameof(BlazonEditor.OnButtonRandomisePrimaryColour))]
    private static void Primary(BlazonEditor __instance) => Blazon.Say(Loc.T("blazon.primary", Blazon.ColorOf(__instance.primaryColour)));

    [HarmonyPostfix, HarmonyPatch(nameof(BlazonEditor.OnButtonRandomiseSecondaryColour))]
    private static void Secondary(BlazonEditor __instance) => Blazon.Say(Loc.T("blazon.secondary", Blazon.ColorOf(__instance.secondaryColour)));

    [HarmonyPostfix, HarmonyPatch(nameof(BlazonEditor.OnButtonRandomiseEmblemColour))]
    private static void EmblemColour(BlazonEditor __instance) => Blazon.Say(Loc.T("blazon.emblem_colour", Blazon.ColorOf(__instance.emblemColour)));

    [HarmonyPostfix, HarmonyPatch(nameof(BlazonEditor.OnButtonRandomiseEverything))]
    private static void Everything(BlazonEditor __instance) => Blazon.Say(Blazon.Summary(__instance));

    [HarmonyPostfix, HarmonyPatch(nameof(BlazonEditor.OnButtonNextCrown))]
    private static void NextCrown(BlazonEditor __instance) => Blazon.Say(Blazon.Crown(__instance));

    [HarmonyPostfix, HarmonyPatch(nameof(BlazonEditor.OnButtonPreviousCrown))]
    private static void PrevCrown(BlazonEditor __instance) => Blazon.Say(Blazon.Crown(__instance));
}

/// <summary>Approximate colour name, from the hue.</summary>
internal static class ColorNames
{
    public static string Name(Color c)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        string key;
        if (v < 0.15f) key = "black";
        else if (s < 0.15f) key = v > 0.85f ? "white" : "grey";
        else
        {
            float deg = h * 360f;
            if (deg < 15f || deg >= 345f) key = "red";
            else if (deg < 40f) key = v < 0.6f ? "brown" : "orange";
            else if (deg < 70f) key = "yellow";
            else if (deg < 160f) key = "green";
            else if (deg < 200f) key = "cyan";
            else if (deg < 255f) key = "blue";
            else if (deg < 290f) key = "purple";
            else key = "pink";
        }
        string name = Loc.T("color." + key);
        if (key != "black" && key != "white")
        {
            if (v < 0.45f) name = Loc.T("color.dark", name);
            else if (s < 0.45f && v > 0.8f) name = Loc.T("color.light", name);
        }
        return name;
    }
}
