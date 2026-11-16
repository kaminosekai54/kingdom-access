using System.Collections.Generic;
using KingdomAccess.Game;
using KingdomAccess.Localization;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Features;

/// <summary>
/// Tutorial journal: every step of the tutorial, in order, with what to do, whether it is done,
/// in progress or still to do, and where it is from the player. Browsed like any list: the walk
/// shortcuts go to the selected step and the item beacon leads to it by sound.
/// </summary>
internal static class TutorialJournal
{
    private static List<NavEntry> _shown;

    /// <summary>True while the journal is the list being browsed.</summary>
    public static bool IsOpen => _shown != null && ListNav.IsShowing(_shown);

    /// <summary>F6: opens the journal, or closes it when it is open.</summary>
    public static void Toggle(Player player)
    {
        if (IsOpen) { Close(); return; }
        Show(player);
    }

    public static void Close()
    {
        if (_shown == null) return;
        _shown = null;
        ListNav.Reset();
        SpeechOut.Say(Loc.T("tutorial.journal.closed"));
    }

    public static void Show(Player player)
    {
        TutorialManager manager = null;
        try { manager = Object.FindFirstObjectByType<TutorialManager>(); } catch { }
        List<GhostHintData> steps = new();
        try
        {
            if (manager != null && manager.hintDatas != null)
                foreach (var h in manager.hintDatas) if (h != null) steps.Add(h);
        }
        catch { }
        if (steps.Count == 0) { SpeechOut.Say(Loc.T("tutorial.journal.none")); return; }

        // Current step: the one the ghost shows; earlier steps are done.
        int current = -1;
        var ghost = TutorialNarrator.CurrentGhost;
        var currentData = ghost != null ? TutorialNarrator.CurrentHint(ghost) : null;
        if (currentData != null)
            for (int i = 0; i < steps.Count; i++)
                if (steps[i].Pointer == currentData.Pointer || steps[i].name == currentData.name) { current = i; break; }
        // The game's "tutorial complete" flag is only trusted when no step is in progress.
        bool allDone = false;
        if (current < 0 && ghost == null) { try { allDone = TutorialManager.IsStartingTutorialComplete; } catch { } }
        try
        {
            var names = new List<string>();
            foreach (var st in steps) names.Add(st.name);
            TutorialNarrator.Log?.Info($"[Tutorial] journal: {steps.Count} steps, current {current} ({currentData?.name}), complete flag {allDone}: {string.Join(", ", names)}");
        }
        catch { }

        // Where each step happens: its trigger in the world (nearest one to the player).
        float px = GameState.PlayerX(player);
        var triggers = new Dictionary<string, GhostHintTrigger>();
        try
        {
            foreach (var t in Object.FindObjectsByType<GhostHintTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t == null || t.hintData == null) continue;
                string n = t.hintData.name;
                if (!triggers.TryGetValue(n, out var prev)
                    || Mathf.Abs(t.transform.position.x - px) < Mathf.Abs(prev.transform.position.x - px))
                    triggers[n] = t;
            }
        }
        catch { }

        var entries = new List<NavEntry>();
        int focus = 0;
        for (int i = 0; i < steps.Count; i++)
        {
            var data = steps[i];
            string state = allDone || (current >= 0 && i < current) ? Loc.T("tutorial.step.done")
                         : i == current ? Loc.T("tutorial.step.current")
                         : Loc.T("tutorial.step.todo");
            if (i == current) focus = i;

            Component target = null;
            if (i == current && ghost != null) target = ghost;
            else if (triggers.TryGetValue(data.name, out var trig)) target = trig;
            string where = target != null ? Directions.DistanceSide(target.transform.position.x - px) : Loc.T("tutorial.step.nopos");

            string what = TutorialNarrator.StepText(data);
            string text = $"{i + 1}. {what}. {state}. {where}";
            int index = i;
            var step = data;
            var trigger = triggers.TryGetValue(data.name, out var tg) ? tg : null;
            entries.Add(new NavEntry
            {
                Text = text, Name = what, Target = target,
                Live = () => LiveLine(player, index, step, steps, trigger)
            });
        }

        string summary = (allDone && current < 0 ? Loc.T("tutorial.journal.finished") + " " : "") + Loc.T("tutorial.journal", entries.Count, AccessMod.KeyName("PreviousItem"), AccessMod.KeyName("NextItem"),
                               AccessMod.KeyName("ItemBeacon"), AccessMod.KeyName("WalkToItem"));
        ListNav.ShowStatic(entries, summary + " " + entries[focus].Text);
        _shown = entries;
        ListNav.Select(focus);
    }

    /// <summary>A journal line computed when it is read: current state and distance.</summary>
    private static string LiveLine(Player player, int i, GhostHintData data, List<GhostHintData> steps, GhostHintTrigger trigger)
    {
        var ghost = TutorialNarrator.CurrentGhost;
        var currentData = ghost != null ? TutorialNarrator.CurrentHint(ghost) : null;
        int current = -1;
        if (currentData != null)
            for (int k = 0; k < steps.Count; k++)
                if (steps[k].Pointer == currentData.Pointer || steps[k].name == currentData.name) { current = k; break; }
        bool allDone = false;
        if (current < 0 && ghost == null) { try { allDone = TutorialManager.IsStartingTutorialComplete; } catch { } }
        string state = allDone || (current >= 0 && i < current) ? Loc.T("tutorial.step.done")
                     : i == current ? Loc.T("tutorial.step.current")
                     : Loc.T("tutorial.step.todo");
        Component target = i == current && ghost != null ? ghost : trigger;
        string where = Loc.T("tutorial.step.nopos");
        var p = GameState.Player ?? player;
        if (target != null && p != null)
        {
            float dx = target.transform.position.x - GameState.PlayerX(p);
            where = Mathf.Abs(dx) < 1.5f ? Loc.T("dir.here") : Directions.DistanceSide(dx);
        }
        if (i == current && ghost != null && p != null)
        {
            var g = TutorialNarrator.Guidance(p, ghost, data);
            if (g.target != null)
            {
                float gdx = g.target.transform.position.x - GameState.PlayerX(p);
                where = Mathf.Abs(gdx) < 1.5f ? Loc.T("dir.here") : Directions.DistanceSide(gdx);
                return $"{i + 1}. {g.text}, {where}. {state}";
            }
        }
        return $"{i + 1}. {TutorialNarrator.StepText(data)}. {state}. {where}";
    }
}
