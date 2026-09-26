using System.Collections.Generic;
using KingdomAccess.Game;
using KingdomAccess.Localization;
using KingdomAccess.Speech;
using UnityEngine;
using SpeechOut = KingdomAccess.Speech.Speech;

namespace KingdomAccess.Features;

/// <summary>
/// Game time: phase (dawn, day, evening, night), hour, season, time until night or dawn.
/// Plays a sound and announces each phase change.
/// </summary>
internal static class DayClock
{
    private static DayPhase? _lastPhase;
    private static float _nextCheck;
    private static bool _loggedTimes;

    public static void Reset()
    {
        _lastPhase = null;
        _loggedTimes = false;
    }

    public static DayPhase? Phase()
    {
        var d = GameState.Director;
        if (d == null) return null;
        try { return d.CurrentTimesOfDay.GetDayPhase(d.currentTime); }
        catch { return d.IsDaytime ? DayPhase.Day : DayPhase.Night; }
    }

    public static string PhaseName(DayPhase p) => Loc.T("phase." + p.ToString().ToLowerInvariant());

    public static void Tick(AccessSettings s, IModLog log, float now)
    {
        if (now < _nextCheck) return;
        _nextCheck = now + 0.5f;

        var d = GameState.Director;
        if (d == null) return;

        if (!_loggedTimes)
        {
            _loggedTimes = true;
            try
            {
                var t = d.CurrentTimesOfDay;
                log.Info($"[Heure] actuelle={d.currentTime:0.00} aube={t.dawnStart:0.0}-{t.dawnEnd:0.0} jour={t.dayStart:0.0}-{t.dayEnd:0.0} soir={t.eveningStart:0.0}-{t.eveningEnd:0.0} nuit={t.nightStart:0.0}-{t.nightEnd:0.0} secondes/heure={d.secondsPerInGameHour:0.0}");
            }
            catch { }
        }

        var phase = Phase();
        if (!phase.HasValue) return;
        if (_lastPhase.HasValue && phase.Value != _lastPhase.Value && s.AnnounceDayPhases)
        {
            Sounds.Play("phase_" + phase.Value.ToString().ToLowerInvariant());
            SpeechOut.Say(Loc.T("phase.change." + phase.Value.ToString().ToLowerInvariant()), false);
            DlcObjects.OnPhaseChange(phase.Value);
        }
        _lastPhase = phase;
    }

    /// <summary>"Day 5, spring, evening, 19 o'clock. Night in 1 hour."</summary>
    public static string Describe()
    {
        var d = GameState.Director;
        if (d == null) return Loc.T("report.unavailable");

        var parts = new List<string> { Loc.T("time.daynum", d.CurrentIslandDays) };
        try { parts.Add(Loc.T("season." + d.CurrentSeason.ToString().ToLowerInvariant())); } catch { }

        var phase = Phase();
        if (phase.HasValue) parts.Add(PhaseName(phase.Value));

        try
        {
            float now = d.currentTime;
            var t = d.CurrentTimesOfDay;
            parts.Add(Loc.T("time.hour", Mathf.FloorToInt(Mathf.Repeat(now, 24f))));

            bool night = phase == DayPhase.Night;
            float target = night ? t.dawnStart : t.nightStart;
            float left = Mathf.Repeat(target - now, 24f);
            string until = left < 1f ? Loc.T("time.less_hour") : Loc.T(Mathf.RoundToInt(left) == 1 ? "time.hours.one" : "time.hours.many", Mathf.RoundToInt(left));
            parts.Add(Loc.T(night ? "time.until_dawn" : "time.until_night", until));
        }
        catch { }

        return string.Join(", ", parts);
    }
}
