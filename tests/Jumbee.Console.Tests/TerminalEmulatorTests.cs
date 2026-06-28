namespace Jumbee.Console.Tests;

using System.Text;

using ConsoleGUI;
using ConsoleGUI.Data;

using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>
/// Headless tests for <see cref="TerminalEmulator"/>: key→bytes translation (including emulator-mode-dependent
/// sequences) and the manual-drive render pipeline. All use the <see langword="null"/>-command-line constructor so
/// no real pseudo-console/subprocess is spawned — the emulator is driven by feeding ANSI bytes directly.
/// </summary>
public class TerminalEmulatorTests
{
    private static TerminalEmulator Manual() => new(commandLine: null);

    private static ConsoleKeyInfo Key(ConsoleKey k, char ch = '\0', bool shift = false, bool control = false)
        => new(ch, k, shift, alt: false, control);

    private static string Hex(byte[]? b) => b is null ? "<null>" : string.Join(" ", b.Select(x => x.ToString("X2")));

    #region Key translation
    [Fact]
    public void Enter_SendsCarriageReturn()   // CR, not VtNetCore's LF — what a shell expects
        => Assert.Equal([(byte)'\r'], Manual().TranslateKey(Key(ConsoleKey.Enter)));

    [Fact]
    public void Escape_SendsSingleEsc()       // VtNetCore doubles it; we don't
        => Assert.Equal([(byte)0x1b], Manual().TranslateKey(Key(ConsoleKey.Escape)));

    [Fact]
    public void Backspace_SendsDel()
        => Assert.Equal([(byte)0x7f], Manual().TranslateKey(Key(ConsoleKey.Backspace)));

    [Fact]
    public void Arrow_SendsCsiSequence()
        => Assert.Equal(Encoding.ASCII.GetBytes("\x1b[A"), Manual().TranslateKey(Key(ConsoleKey.UpArrow)));

    [Fact]
    public void FunctionKey_SendsVtSequence()
        => Assert.Equal(Encoding.ASCII.GetBytes("\x1b[11~"), Manual().TranslateKey(Key(ConsoleKey.F1)));

    [Fact]
    public void ShiftTab_SendsBackTab()
        => Assert.Equal(Encoding.ASCII.GetBytes("\x1b[Z"), Manual().TranslateKey(Key(ConsoleKey.Tab, '\t', shift: true)));

    [Fact]
    public void Printable_SendsUtf8()
        => Assert.Equal(Encoding.UTF8.GetBytes("a"), Manual().TranslateKey(Key(ConsoleKey.A, 'a')));

    [Fact]
    public void CtrlLetter_SendsControlCode()   // Ctrl+C → 0x03
        => Assert.Equal([(byte)0x03], Manual().TranslateKey(Key(ConsoleKey.C, control: true)));

    [Fact]
    public void Arrow_FlipsToSs3_UnderApplicationCursorKeysMode()
    {
        var t = Manual();
        Assert.Equal(Encoding.ASCII.GetBytes("\x1b[A"), t.TranslateKey(Key(ConsoleKey.UpArrow)));

        t.Feed(Encoding.ASCII.GetBytes("\x1b[?1h"));   // DECCKM on (application cursor keys)

        Assert.Equal(Encoding.ASCII.GetBytes("\x1bOA"), t.TranslateKey(Key(ConsoleKey.UpArrow)));
    }
    #endregion

    #region Render pipeline (manual drive)
    [Fact]
    public void Feed_RendersTextIntoCellArea()
    {
        var t = Manual();
        ConsoleSnapshot.ToText(t, 20, 3);   // size/init the viewport first
        t.Feed(Encoding.ASCII.GetBytes("hello"));

        Assert.Contains("hello", ConsoleSnapshot.ToText(t, 20, 3));
    }

    [Fact]
    public void FocusedTerminal_DrawsCursor()
    {
        var t = Manual();
        ConsoleSnapshot.ToText(t, 20, 3);
        t.Feed(Encoding.ASCII.GetBytes("hi"));
        t.IsFocused = true;

        Assert.NotNull(FindCursor(ConsoleSnapshot.Render(t, 20, 3)));
    }

    private static (int x, int y)? FindCursor(ConsoleBuffer buf)
    {
        for (var y = 0; y < buf.Size.Height; y++)
            for (var x = 0; x < buf.Size.Width; x++)
                if (buf[x, y].Character.IsCursor) return (x, y);
        return null;
    }

    private static void Wheel(TerminalEmulator t, int delta)
        => ((ConsoleGUI.Input.IMouseWheelListener)t).OnMouseWheel(new ConsoleGUI.Space.Position(0, 0), delta);

    [Fact]
    public void Scrollback_WheelUp_RevealsHistory_ThenSnapsBack()
    {
        var t = Manual();
        ConsoleSnapshot.ToText(t, 24, 6);   // init a small viewport
        for (var i = 0; i < 30; i++) t.Feed(Encoding.ASCII.GetBytes($"L{i:00}\r\n"));

        var live = ConsoleSnapshot.ToText(t, 24, 6);
        Assert.Contains("L29", live);          // following the live bottom
        Assert.DoesNotContain("L05", live);

        Wheel(t, -12);                          // scroll up into history
        var scrolled = ConsoleSnapshot.ToText(t, 24, 6);
        Assert.DoesNotContain("L29", scrolled); // bottom no longer shown
        Assert.Contains("L1", scrolled);        // an earlier (teens) line is now visible

        Wheel(t, +100);                         // snap back to the live bottom
        Assert.Contains("L29", ConsoleSnapshot.ToText(t, 24, 6));
    }

    [Fact]
    public void Wheel_ScrollsScrollback_UnlessProgramTracksMouse()
    {
        var t = Manual();
        ConsoleSnapshot.ToText(t, 24, 6);
        for (var i = 0; i < 30; i++) t.Feed(Encoding.ASCII.GetBytes($"L{i:00}\r\n"));

        // No mouse tracking: the wheel scrolls our scrollback into history.
        Wheel(t, -12);
        Assert.DoesNotContain("L29", ConsoleSnapshot.ToText(t, 24, 6));
        Wheel(t, +100);   // snap back to the live bottom
        Assert.Contains("L29", ConsoleSnapshot.ToText(t, 24, 6));

        // Program enables mouse tracking (DECSET 1000): the wheel is forwarded to it, not consumed as scrollback.
        t.Feed(Encoding.ASCII.GetBytes("\x1b[?1000h"));
        Wheel(t, -12);
        Assert.Contains("L29", ConsoleSnapshot.ToText(t, 24, 6));   // view unchanged — wheel went to the program
    }

    [Fact]
    public void Scrollback_DrawsThumb_InLastColumn_WhenHistoryExists()
    {
        var t = Manual();
        ConsoleSnapshot.ToText(t, 24, 6);
        for (var i = 0; i < 30; i++) t.Feed(Encoding.ASCII.GetBytes($"L{i:00}\r\n"));

        var buf = ConsoleSnapshot.Render(t, 24, 6);
        var col = buf.Size.Width - 1;
        var hasThumb = Enumerable.Range(0, buf.Size.Height).Any(y => buf[col, y].Character.Content == '█');

        Assert.True(hasThumb, "expected a scrollbar thumb in the last column once there is scrollback");
    }

    [Fact]
    public void WindowTitle_SetByOsc_UpdatesPropertyAndEvent()
    {
        var t = Manual();
        string? seen = null;
        t.TitleChanged += s => seen = s;

        t.Feed(Encoding.ASCII.GetBytes("\x1b]0;Hello Title\x07"));   // OSC 0 set window title (BEL-terminated)

        Assert.Equal("Hello Title", t.WindowTitle);
        Assert.Equal("Hello Title", seen);
    }

    [Fact]
    public void Italic_Sgr_SetsItalicDecoration()
    {
        var t = Manual();
        ConsoleSnapshot.ToText(t, 10, 2);
        t.Feed(Encoding.ASCII.GetBytes("\x1b[3mI\x1b[0m"));   // italic 'I'

        var deco = ConsoleSnapshot.Render(t, 10, 2)[0, 0].Character.Decoration ?? Decoration.None;
        Assert.True((deco & Decoration.Italic) != 0);
    }

    [Fact]
    public void HiddenCursor_Dectcem_NotDrawn()
    {
        var t = Manual();
        ConsoleSnapshot.ToText(t, 10, 2);
        t.Feed(Encoding.ASCII.GetBytes("hi"));
        t.IsFocused = true;
        Assert.NotNull(FindCursor(ConsoleSnapshot.Render(t, 10, 2)));   // visible by default

        t.Feed(Encoding.ASCII.GetBytes("\x1b[?25l"));                    // DECTCEM hide
        Assert.Null(FindCursor(ConsoleSnapshot.Render(t, 10, 2)));
    }

    [Fact]
    public void Framed_SizesToViewport_NotScrollBalloon()
    {
        // A scrolling ControlFrame offers unbounded height; without MeasureHeight the terminal would balloon to
        // ~1000 rows (oversizing the PTY and breaking auto-scroll). It must instead size to the frame's viewport.
        var t = Manual();
        t.WithFrame(title: "term");

        ConsoleSnapshot.Render(t, 24, 8);

        Assert.True(t.ActualHeight is > 0 and <= 8, $"expected viewport-sized height, got {t.ActualHeight}");
    }
    #endregion
}
