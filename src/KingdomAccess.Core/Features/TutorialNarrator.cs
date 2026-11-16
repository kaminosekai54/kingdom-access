using System.Collections.Generic;
using System.Text.RegularExpressions;
using KingdomAccess.Game;
using KingdomAccess.Localization;
using KingdomAccess.Speech;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Features;

/// <summary>
/// Tutorial: a ghost shows an action (key pictogram) and a place on screen,
/// often without text. Each new hint is announced (text translated by the game if any,
/// otherwise the expected action) with where the ghost is. The TutorialHint shortcut repeats it.
/// </summary>
internal static class TutorialNarrator
{
    private static float _nextScan;
    private static Ghost _ghost;
    private static string _lastHintName;
    private static string _lastText;

    internal static IModLog Log;

    public static void Reset() { _ghost = null; _lastHintName = null; _lastText = null; _beaconHint = null; }

    // ---------- Clear guidance and audio beacon ----------

    private const float NearDistance = 8f, OnDistance = 1.5f, RearmDistance = 12f;
    private static float _nextBeep, _nextGuide;
    private static string _beaconHint;
    private static bool _nearSaid, _onSaid, _beepLogged, _muted;
    private static Component _guideTarget;
    private static string _guideText;

    /// <summary>Silences the tutorial beacon until the next tutorial step.</summary>
    public static void MuteBeacon() => _muted = true;

    /// <summary>
    /// What to do now, said plainly, and the object it concerns: coins to pick up, a vagrant to
    /// throw a coin at, the object to pay (with its action), otherwise the ghost to follow.
    /// </summary>
    public static (Component target, string text) Guidance(Player player, Ghost ghost, GhostHintData data)
    {
        float px = GameState.PlayerX(player);
        string name = (data.name ?? "").ToLowerInvariant();
        string glyph = "";
        try { glyph = data.glyphAction.ToString(); } catch { }

        // 1) The object the game's camera looks at for this step (its tag).
        try
        {
            string tag = data.cameraTargetTag;
            if (!string.IsNullOrEmpty(tag))
            {
                GameObject best = null;
                float bestD = float.MaxValue;
                foreach (var go in GameObject.FindGameObjectsWithTag(tag))
                {
                    if (go == null || !go.activeInHierarchy) continue;
                    float d = Mathf.Abs(go.transform.position.x - px);
                    if (d < bestD) { bestD = d; best = go; }
                }
                if (best != null)
                {
                    var pay = best.GetComponent<Payable>() ?? best.GetComponentInChildren<Payable>();
                    if (pay != null) return (pay, PayText(player, pay));
                    return (best.transform, Loc.T("tutorial.do.goto", ObjectNames.Identify(best.transform).Name));
                }
            }
        }
        catch { }

        // 2) What the step's name designates.
        if (Has(name, "coin", "pickup", "take", "gift"))
        {
            var coin = NearestCoin(px, ghost.transform.position.x);
            if (coin != null) return (coin, Loc.T("tutorial.do.pickup"));
        }
        if (glyph == "Throw" || Has(name, "recruit", "vagrant", "beggar", "hire"))
        {
            var beggar = Nearest<Beggar>(px, 80f);
            if (beggar != null) return (beggar, Loc.T("tutorial.do.throw"));
        }
        ObjKind? kind = null;
        string shop = null;
        if (Has(name, "campfire", "camp", "fire", "castle", "keep")) kind = ObjKind.Castle;
        else if (Has(name, "bow", "archer")) shop = "Bow";
        else if (Has(name, "hammer", "builder", "worker")) shop = "Hammer";
        else if (Has(name, "scythe", "farmer")) shop = "Scythe";
        else if (Has(name, "wall")) kind = ObjKind.Wall;
        else if (Has(name, "tower")) kind = ObjKind.Tower;
        else if (Has(name, "farm")) kind = ObjKind.Farmhouse;
        if (kind != null || shop != null)
        {
            var target = NearestPayable(player, px, kind, shop);
            if (target != null) return (target, PayText(player, target));
        }

        // 3) Nothing precise: follow the ghost.
        return (ghost, Loc.T("tutorial.do.follow"));
    }

    private static bool Has(string s, params string[] words)
    {
        foreach (var w in words) if (s.Contains(w)) return true;
        return false;
    }

    /// <summary>"build: Camp" from the object's own action and name.</summary>
    private static string PayText(Player player, Payable pay)
    {
        var a = HoverAnnouncer.Analyze(player, pay);
        string action = a.ActionKey != null ? Loc.T(a.ActionKey) : Loc.T("act.pay");
        return Loc.T("tutorial.do.pay", action, a.Name);
    }

    /// <summary>Nearest available payable of a kind (or a shop of a type) from the player.</summary>
    private static Payable NearestPayable(Player player, float px, ObjKind? kind, string shopType)
    {
        Payable best = null;
        float bestD = float.MaxValue;
        var payables = GameState.Payables;
        for (int i = 0; payables != null && i < payables.Length; i++)
        {
            var pay = payables[i];
            if (pay == null || !pay.gameObject.activeInHierarchy || GameState.IsPlayerOrSteed(player, pay.gameObject)) continue;
            if (shopType != null)
            {
                var tag = pay.GetComponent<ShopTag>();
                if (tag == null || !tag.type.ToString().StartsWith(shopType)) continue;
            }
            else if (ObjectNames.Identify(pay).Kind != kind) continue;
            float d = Mathf.Abs(pay.transform.position.x - px);
            if (d < bestD) { bestD = d; best = pay; }
        }
        return best;
    }

    private static T Nearest<T>(float x, float max) where T : Component
    {
        T best = null;
        float bestD = max;
        try
        {
            foreach (var c in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            {
                if (c == null || !c.gameObject.activeInHierarchy) continue;
                float d = Mathf.Abs(c.transform.position.x - x);
                if (d < bestD) { bestD = d; best = c; }
            }
        }
        catch { }
        return best;
    }

    private static Component NearestCoin(float px, float gx)
    {
        Component best = null;
        float bestD = 20f;
        try
        {
            foreach (var c in Object.FindObjectsByType<DroppableCurrency>(FindObjectsSortMode.None))
            {
                if (c == null || !c.gameObject.activeInHierarchy || c._moving || !c._hasHitGround) continue;
                float x = c.transform.position.x;
                float d = Mathf.Min(Mathf.Abs(x - px), Mathf.Abs(x - gx));
                if (d < bestD) { bestD = d; best = c; }
            }
        }
        catch { }
        return best;
    }

    /// <summary>"Pick up the coins, 5 to the right".</summary>
    private static string WithDistance(Player player, Component target, string text)
    {
        float dx = target.transform.position.x - GameState.PlayerX(player);
        return Mathf.Abs(dx) < OnDistance ? $"{text}, {Loc.T("dir.here")}" : $"{text}, {Directions.DistanceSide(dx)}";
    }

    /// <summary>
    /// While a tutorial step is active, a beep leads to what the step concerns: faster and louder
    /// when closer, on its side. Close to it the clear instruction is spoken; on it, "you are
    /// there". No step in progress (or muted): no beep. Called every frame.
    /// </summary>
    public static void BeaconTick(Player player, AccessSettings s, float now)
    {
        var ghost = CurrentGhost;
        var data = ghost != null ? HintData(ghost) : null;
        if (player == null || data == null) { _beaconHint = null; _guideTarget = null; return; }

        if (data.name != _beaconHint)
        {
            _beaconHint = data.name;
            _nearSaid = _onSaid = _beepLogged = _muted = false;
            _nextGuide = 0f;
            try
            {
                var g0 = Guidance(player, ghost, data);
                Log?.Info($"[Tutorial] step {data.name}: glyph {data.glyphAction}, camera tag '{data.cameraTargetTag}', ghost x={ghost.transform.position.x:0.0}, player x={GameState.PlayerX(player):0.0}, target {(g0.target != null ? g0.target.name : "none")} x={(g0.target != null ? g0.target.transform.position.x : 0f):0.0} -> {g0.text}");
            }
            catch { }
        }
        if (now >= _nextGuide || _guideTarget == null)
        {
            _nextGuide = now + 0.5f;
            var g = Guidance(player, ghost, data);
            if (g.target != _guideTarget) { _nearSaid = _onSaid = false; }
            _guideTarget = g.target;
            _guideText = g.text;
        }
        if (_guideTarget == null) return;

        float dx = _guideTarget.transform.position.x - GameState.PlayerX(player);
        float d = Mathf.Abs(dx);
        if (d > RearmDistance) { _nearSaid = false; _onSaid = false; }
        if (d <= NearDistance && d > OnDistance && !_nearSaid)
        {
            _nearSaid = true;
            SpeechOut.Say(WithDistance(player, _guideTarget, _guideText), true);
        }
        if (d <= OnDistance)
        {
            if (!_onSaid) { _onSaid = _nearSaid = true; SpeechOut.Say(Loc.T("tutorial.arrived_action", _guideText), true); }
            return;
        }
        if (_muted || now < _nextBeep) return;
        float closeness = Mathf.Clamp01(1f - d / 40f);
        _nextBeep = now + Mathf.Lerp(1.2f, 0.15f, closeness);
        GameAudio.Beep(Mathf.Lerp(0.25f, 0.55f, closeness), Mathf.Sign(dx) * Mathf.Clamp01(d / 10f));
        if (!_beepLogged) { _beepLogged = true; Log?.Info($"[Tutorial] beacon on '{_guideText}', distance {d:0.0}"); }
    }

    /// <summary>The expected action of a hint (key pictogram), or null.</summary>
    private static string ActionText(GhostHintData data)
    {
        string action = null;
        try { action = Loc.TryT("glyph." + data.glyphAction.ToString().ToLowerInvariant()); } catch { }
        return action ?? Loc.TryT("tutorialhint." + Key(data.name));
    }

    /// <summary>The tutorial ghost, for the scanner and auto-walk.</summary>
    public static Ghost CurrentGhost => _ghost != null && _ghost.gameObject.activeInHierarchy ? _ghost : null;

    public static void Tick(Player player, AccessSettings s, float now)
    {
        if (!s.AnnounceScreenText || now < _nextScan) return;
        _nextScan = now + 0.5f;

        Ghost ghost = null;
        try
        {
            foreach (var g in Object.FindObjectsByType<Ghost>(FindObjectsSortMode.None))
                if (g != null && g.gameObject.activeInHierarchy && !g.IsPlayerTwoGhost) { ghost = g; break; }
        }
        catch { }
        _ghost = ghost;
        if (ghost == null) { _lastHintName = null; return; }

        var data = HintData(ghost);
        string name = data != null ? data.name : null;
        if (name == null || name == _lastHintName) return;
        _lastHintName = name;
        _lastText = Describe(player, ghost, data);
        try { Log?.Info($"[Tutoriel] indice {name}, action {data.glyphAction} -> {_lastText}"); } catch { }
        SpeechOut.Say(_lastText, false);
    }

    /// <summary>Repeats the current hint with the current position of the ghost.</summary>
    public static void Repeat(Player player)
    {
        var ghost = CurrentGhost;
        var data = ghost != null ? HintData(ghost) : null;
        if (data == null) { SpeechOut.Say(Loc.T("tutorial.none")); return; }
        SpeechOut.Say(Describe(player, ghost, data));
    }

    /// <summary>Hint currently shown by a ghost, or null.</summary>
    public static GhostHintData CurrentHint(Ghost g) => HintData(g);

    /// <summary>What a step asks: the game's texts of the hint, then the expected action.</summary>
    public static string StepText(GhostHintData data)
    {
        var texts = new List<string>();
        string action = ActionText(data);
        if (action != null) texts.Add(action);
        AddLoc(texts, data.PlayerPresentHintlocIdHint);
        AddLoc(texts, data.PlayerBeckonHintlocIdHint);
        AddLoc(texts, data.PlayerNotPresentHintlocIdHint);
        if (texts.Count == 0) texts.Add(Regex.Replace(data.name ?? "", "([a-z])([A-Z])", "$1 $2"));
        return string.Join(". ", texts);
    }

    private static GhostHintData HintData(Ghost g)
    {
        try
        {
            var data = g.CurrentHintData;
            if (data != null) return data;
            var trig = g._currentTrigger;
            return trig != null ? trig.hintData : null;
        }
        catch { return null; }
    }

    private static string Describe(Player player, Ghost ghost, GhostHintData data)
    {
        var parts = new List<string> { Loc.T("tutorial.prefix") };

        // Game texts attached to the hint (translated into the game language).
        var texts = new List<string>();
        AddLoc(texts, data.PlayerPresentHintlocIdHint);
        AddLoc(texts, data.PlayerBeckonHintlocIdHint);
        AddLoc(texts, data.PlayerNotPresentHintlocIdHint);
        try
        {
            var beckon = ghost._beckonText;
            if (beckon != null && beckon.gameObject.activeInHierarchy) AddText(texts, beckon.Text);
        }
        catch { }
        // The clear instruction (what to do, where) comes first, then the game's flavour text.
        if (player != null)
        {
            var g = Guidance(player, ghost, data);
            if (g.target != null) parts.Add(WithDistance(player, g.target, g.text));
        }
        else
        {
            string action = ActionText(data);
            if (action != null) parts.Add(action);
        }
        if (texts.Count > 0) parts.Add(string.Join(". ", texts));

        return string.Join(". ", parts);
    }

    private static void AddLoc(List<string> list, FilteredLocID id)
    {
        try
        {
            string key = id.locId;
            if (string.IsNullOrEmpty(key)) return;
            string val = global::Language.Get(key);
            if (val != key) AddText(list, val);
        }
        catch { }
    }

    private static readonly Regex Tags = new("<.*?>", RegexOptions.Compiled);

    private static void AddText(List<string> list, string t)
    {
        if (string.IsNullOrWhiteSpace(t)) return;
        t = Tags.Replace(t, "").Replace('\n', ' ').Trim();
        if (t.Length < 2 || t.StartsWith("#") || list.Contains(t)) return;
        list.Add(t);
    }

    private static string Key(string s) => Regex.Replace((s ?? "").ToLowerInvariant(), "[^a-z0-9]", "");
}
