namespace Jumbee.Console.Tests;

using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ConsoleGUI;
using ConsoleGUI.Api;
using ConsoleGUI.Data;
using ConsoleGUI.Space;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>
/// Verifies the dirty-rectangle render path: a small content change re-composites only that control's screen
/// region, not the whole buffer. Drives the real <see cref="ConsoleManager"/> via <see cref="AnsiConsoleSession"/>
/// and reads <see cref="ConsoleManager.LastFrameDirtyCells"/> — the cell count the last flush actually scanned.
/// </summary>
public class DirtyRectRenderTests
{
    public DirtyRectRenderTests() => UiTestHarness.EnsureStopped();

    [Fact]
    public async Task SmallChange_RecompositesOnlyThatControlsArea_NotWholeScreen()
    {
        const int w = 40, h = 10;
        var top = new TextLabel(TextLabelOrientation.Horizontal, "aaaaa");
        var bottom = new TextLabel(TextLabelOrientation.Horizontal, "bbbbb");
        var stack = new VerticalStackPanel(top, bottom);

        using var session = await AnsiConsoleSession.StartAsync(stack.CControl, w, h);

        // The first frame is a full redraw (the surface was just initialized) — it touches the whole buffer.
        Assert.Equal((long)w * h, ConsoleManager.LastFrameDirtyCells);

        // Change one label's text (same length, so no relayout) and render one more frame.
        bottom.Text = "ccccc";
        await session.FrameAsync();

        // Only the changed control's region should have been re-composited — a small fraction of the buffer, not
        // the whole screen. (The exact count is the laid-out area of the label's stack, well under the full w*h.)
        var dirty = ConsoleManager.LastFrameDirtyCells;
        Assert.True(dirty > 0, "expected the change to dirty something");
        Assert.True(dirty <= w * 4, $"expected a partial redraw (a few rows), got {dirty} of {(long)w * h} cells");

        // And the change actually landed on screen (the diff still emitted the new glyphs).
        Assert.Contains("ccccc", ConsoleSnapshot.ToText(session.Screen.Buffer));
    }

    [Fact]
    public async Task IdleFrame_WithNoChange_RecompositesNothing()
    {
        var label = new TextLabel(TextLabelOrientation.Horizontal, "hello");
        using var session = await AnsiConsoleSession.StartAsync(new VerticalStackPanel(label).CControl, 30, 5);

        // A frame with nothing dirtied re-composites zero cells.
        await session.FrameAsync();
        Assert.Equal(0, ConsoleManager.LastFrameDirtyCells);
    }

    // Regression for the startup-blank bug: the LIVE UI loop (UI.OnFrame) must paint controls into their buffers
    // BEFORE compositing, so the first frames emit real content — not the empty buffers a composite-then-paint order
    // would flush. The AnsiConsoleSession harness can't catch this (it already paints then draws); only the real
    // frame loop can, so this drives UI.Start end to end and reads back the emitted ANSI.
    [Fact]
    public async Task LiveUiLoop_EmitsPaintedContent_OnStartup()
    {
        UiTestHarness.EnsureStopped();
        const int w = 60, h = 8;
        var capture = new StringBuilder();
        var prevOutput = ConsoleManager.AnsiOutput;
        try
        {
            ConsoleManager.AnsiOutput = acsb =>
            {
                var s = acsb.ToString();
                return Task.Run(() => { lock (capture) capture.Append(s); });
            };

            // A SplitPanel of two framed panes — the browser's actual layout shape. Both panes must appear on the
            // live loop's first frames, exactly as they do through the full-redraw snapshot path (Bisect above).
            _ = UI.Start(TwoPaneSplit(), w, h, paintInterval: 15, isAnsiTerminal: true, console: new TestConsole(w, h), input: new NoInput());
            Thread.Sleep(250);                 // let several frames run
            await ConsoleManager.OutputIdle.ConfigureAwait(false);
        }
        finally
        {
            UI.Stop();
            await ConsoleManager.OutputIdle.ConfigureAwait(false);
            ConsoleManager.AnsiOutput = prevOutput;
        }

        var screen = new AnsiScreen(w, h);
        lock (capture) screen.Feed(capture.ToString());
        var text = ConsoleSnapshot.ToText(screen.Buffer);
        Assert.Contains("PANELEFT", text);     // both panes composited on startup, not left blank
        Assert.Contains("PANERIGHT", text);
    }

    private static SplitPanel TwoPaneSplit() => new(
        SplitOrientation.Horizontal,
        new TextLabel(TextLabelOrientation.Horizontal, "PANELEFT").WithFrame(title: "L"),
        new TextLabel(TextLabelOrientation.Horizontal, "PANERIGHT").WithFrame(title: "R"),
        splitPosition: 24);

    [Fact]
    public async Task SplitPanel_RendersBothPanes_ViaSnapshotPath()
    {
        // Baseline for the live-loop test below: the two-pane split renders both panes through the full-redraw path.
        var text = await AnsiConsoleSnapshot.ToTextAsync(TwoPaneSplit(), 60, 8);
        Assert.Contains("PANELEFT", text);
        Assert.Contains("PANERIGHT", text);
    }

    private sealed class NoInput : IInputSource
    {
        public bool TryRead(out TerminalInputEvent? evt) { evt = null; return false; }
    }

    private sealed class TestConsole(int w, int h) : IConsole
    {
        public Size Size { get; set; } = new Size(w, h);
        public bool KeyAvailable => false;
        public void Initialize() { }
        public void OnRefresh() { }
        public void Write(Position position, in Character character) { }
        public ConsoleKeyInfo ReadKey() => throw new System.NotSupportedException();
    }
}
