using System.Collections.Generic;
using System.Text.RegularExpressions;
using KingdomAccess.Localization;

namespace KingdomAccess.Speech;

/// <summary>
/// Single entry point for speech: cleans the text, sends it to the screen reader
/// and keeps it in the browsable history.
/// </summary>
public static class Speech
{
    private static readonly Regex RichTextTags = new("<.*?>", RegexOptions.Compiled);
    private static readonly List<string> History = new();
    private static int _cursor = -1;
    private static int _maxHistory = 50;

    internal static void Configure(int historySize)
    {
        _maxHistory = historySize < 5 ? 5 : historySize;
    }

    /// <summary>
    /// Speaks a text. interrupt = true cuts what the screen reader is saying
    /// (shortcut answers, hovered object); false queues it (zone announcements).
    /// </summary>
    public static void Say(string text, bool interrupt = true)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        text = RichTextTags.Replace(text, string.Empty).Trim();
        if (text.Length == 0) return;

        AddToHistory(text);
        ScreenReader.Output(text, interrupt);
    }

    public static void Silence() => ScreenReader.Silence();

    /// <summary>Repeats the last message.</summary>
    public static void RepeatLast()
    {
        if (History.Count == 0) { ScreenReader.Output(Loc.T("history.empty"), true); return; }
        _cursor = History.Count - 1;
        ScreenReader.Output(History[_cursor], true);
    }

    /// <summary>Goes back in the history.</summary>
    public static void Previous()
    {
        if (History.Count == 0) { ScreenReader.Output(Loc.T("history.empty"), true); return; }
        if (_cursor <= 0)
        {
            _cursor = 0;
            ScreenReader.Output(Loc.T("history.start") + ". " + History[0], true);
            return;
        }
        _cursor--;
        ScreenReader.Output(History[_cursor], true);
    }

    /// <summary>Goes forward in the history.</summary>
    public static void Next()
    {
        if (History.Count == 0) { ScreenReader.Output(Loc.T("history.empty"), true); return; }
        if (_cursor >= History.Count - 1)
        {
            _cursor = History.Count - 1;
            ScreenReader.Output(Loc.T("history.end") + ". " + History[_cursor], true);
            return;
        }
        _cursor++;
        ScreenReader.Output(History[_cursor], true);
    }

    private static void AddToHistory(string text)
    {
        if (History.Count > 0 && History[^1] == text)
        {
            _cursor = History.Count;
            return;
        }
        History.Add(text);
        while (History.Count > _maxHistory) History.RemoveAt(0);
        _cursor = History.Count;
    }
}
