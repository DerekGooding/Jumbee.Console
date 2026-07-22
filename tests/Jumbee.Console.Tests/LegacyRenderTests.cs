namespace Jumbee.Console.Tests;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using ConsoleGUI.Api;
using ConsoleGUI.Space;

using Jumbee.Console;

using Xunit;

/// <summary>
/// First-class coverage of the non-ANSI / Windows-classic render path (<c>UI.Start(isAnsiTerminal: false)</c> →
/// <c>ConsoleManager.AnsiEnabled = false</c> → composite written cell-by-cell through <see cref="IConsole"/>).
/// Uses a recording <see cref="IConsole"/> so we can assert both what is drawn and the delta behaviour (unchanged
/// cells are not rewritten), plus the legacy software cursor including its blink.
/// </summary>
public class LegacyRenderTests
{
    public LegacyRenderTests() => UiTestHarness.EnsureStopped();

    // An IConsole sink that stores cells in an inner ConsoleBuffer (so the test can read them back) and counts how
    // many times each position is written — the legacy path only writes changed cells, so the counts reveal deltas.
    private sealed class RecordingConsole : IConsole
    {
        private readonly ConsoleBuffer _inner = new();
        private int[,] _writes = new int[1, 1];

        public RecordingConsole(int width, int height) => Size = new Size(width, height);

        public ConsoleBuffer Inner => _inner;
        public int TotalWrites { get; private set; }
        public int WritesAt(int x, int y) => _writes[x, y];
        public void ResetCounts() { TotalWrites = 0; Array.Clear(_writes); }

        public Size Size
        {
            get => _inner.Size;
            set
            {
                _inner.Size = value;
                _writes = new int[Math.Max(1, value.Width), Math.Max(1, value.Height)];
            }
        }

        public bool KeyAvailable => false;
        public void Initialize() => _inner.Initialize();
        public void OnRefresh() { }
        public ConsoleKeyInfo ReadKey() => throw new NotSupportedException();

        public void Write(Position position, in ConsoleGUI.Data.Character character)
        {
            _inner.Write(position, character);
            if (position.X >= 0 && position.X < _writes.GetLength(0) && position.Y >= 0 && position.Y < _writes.GetLength(1))
                _writes[position.X, position.Y]++;
            TotalWrites++;
        }
    }

    // Writes a string along row 0; the text can be changed to force a partial repaint.
    private sealed class TextStampControl : Control
    {
        private string _text;
        public TextStampControl(string text) => _text = text;
        public string Text { get => _text; set => SetAtomicProperty(ref _text, value); }
        public override bool HandlesInput => false;
        protected override void Render()
        {
            for (var i = 0; i < _text.Length && i < ActualWidth; i++)
                consoleBuffer.Write(new Position(i, 0), new ConsoleGUI.Data.Character(_text[i]));
        }
    }

    // Stamps a coloured glyph at (0,0) to verify the legacy path preserves fg/bg.
    private sealed class ColorGlyphControl : Control
    {
        public override bool HandlesInput => false;
        protected override void Render()
        {
            if (ActualWidth <= 0 || ActualHeight <= 0) return;
            consoleBuffer.Write(new Position(0, 0), new ConsoleGUI.Data.Character(
                'X', new ConsoleGUI.Data.Color(0, 255, 0), new ConsoleGUI.Data.Color(0, 0, 255)));
        }
    }

    // Stamps a BLINKING block cursor (DECSCUSR style 1) at (0,0) with known fg/bg.
    private sealed class BlinkBlockCursorControl : Control
    {
        public override bool HandlesInput => false;
        protected override void Render()
        {
            if (ActualWidth <= 0 || ActualHeight <= 0) return;
            var deco = ConsoleGUI.Data.CursorEncoding.EncodeStyle(ConsoleGUI.Data.Decoration.None, 1); // 1 = blinking block
            consoleBuffer.Write(new Position(0, 0), new ConsoleGUI.Data.Character(
                'Z', new ConsoleGUI.Data.Color(255, 0, 0), new ConsoleGUI.Data.Color(0, 0, 255), deco, isCursor: true));
        }
    }

    private static bool WaitUntil(Func<bool> condition, int timeoutMs)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (condition()) return true;
            Thread.Sleep(10);
        }
        return condition();
    }

    [Fact]
    public void NonAnsi_PreservesGlyphColours_ThroughIConsole()
    {
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);
        var console = new RecordingConsole(10, 3);
        var grid = new Grid([3], [10], [[new ColorGlyphControl()]]);
        Task run;
        try
        {
            run = UI.Start(grid, width: 10, height: 3, fps: 50, isAnsiTerminal: false, console: console);
            Assert.True(WaitUntil(() => console.Inner[0, 0].Content == 'X', 3000), "coloured glyph should render");
            Assert.Equal(new ConsoleGUI.Data.Color(0, 255, 0), console.Inner[0, 0].Foreground);
            Assert.Equal(new ConsoleGUI.Data.Color(0, 0, 255), console.Inner[0, 0].Background);
        }
        finally { UI.Stop(); Console.SetOut(originalOut); }
        Assert.True(run.Wait(2000));
    }

    [Fact]
    public void NonAnsi_UnchangedFrames_WriteNothing()
    {
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);
        var console = new RecordingConsole(20, 5);
        var grid = new Grid([5], [20], [[new TextStampControl("AAAA")]]);
        Task run;
        try
        {
            run = UI.Start(grid, width: 20, height: 5, fps: 66, isAnsiTerminal: false, console: console);
            Assert.True(WaitUntil(() => console.Inner[0, 0].Content == 'A', 3000), "initial content should render");

            console.ResetCounts();
            Thread.Sleep(150);   // several idle frames with nothing changing

            // The legacy path diffs against its own buffer and skips unchanged cells, so idle frames emit no writes.
            Assert.Equal(0, console.TotalWrites);
        }
        finally { UI.Stop(); Console.SetOut(originalOut); }
        Assert.True(run.Wait(2000));
    }

    [Fact]
    public void NonAnsi_PartialChange_RewritesOnlyChangedCells()
    {
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);
        var console = new RecordingConsole(20, 5);
        var stamp = new TextStampControl("AAAA");
        var grid = new Grid([5], [20], [[stamp]]);
        Task run;
        try
        {
            run = UI.Start(grid, width: 20, height: 5, fps: 66, isAnsiTerminal: false, console: console);
            Assert.True(WaitUntil(() => console.Inner[0, 0].Content == 'A', 3000), "initial content should render");

            console.ResetCounts();
            stamp.Text = "BBBB";   // changes only columns 0..3 of row 0
            Assert.True(WaitUntil(() => console.Inner[0, 0].Content == 'B', 3000), "the change should repaint");

            Assert.True(console.TotalWrites > 0, "the changed cells must be written");
            Assert.True(console.TotalWrites < 20 * 5, $"a 4-cell change must not rewrite the whole screen (wrote {console.TotalWrites})");
            Assert.True(console.WritesAt(0, 0) >= 1, "the changed cell (0,0) should be rewritten");
            Assert.Equal(0, console.WritesAt(19, 4));   // an untouched far corner is never rewritten
        }
        finally { UI.Stop(); Console.SetOut(originalOut); }
        Assert.True(run.Wait(2000));
    }

    [Fact]
    public void NonAnsi_BlinkingSoftwareCursor_TogglesBetweenInvertedAndPlain()
    {
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);
        var prevEmulate = ConsoleGUI.ConsoleManager.EmulateBlinkingCursor;
        ConsoleGUI.ConsoleManager.EmulateBlinkingCursor = true;   // self-blink the software cursor (vs. native hw blink)

        var screen = new ConsoleBuffer { Size = new Size(10, 3) };
        var grid = new Grid([3], [10], [[new BlinkBlockCursorControl()]]);
        var red = new ConsoleGUI.Data.Color(255, 0, 0);
        var blue = new ConsoleGUI.Data.Color(0, 0, 255);
        Task run;
        try
        {
            run = UI.Start(grid, width: 10, height: 3, fps: 66, isAnsiTerminal: false, console: screen);
            Assert.True(WaitUntil(() => screen[0, 0].Content == 'Z', 3000), "cursor cell should render");

            // A blinking block cursor alternates: ON = inverted (fg/bg swapped), OFF = the plain glyph. Over more than
            // one blink half-period (~530ms) we must observe both phases. Poll the cell and collect the two states.
            bool seenInverted = false, seenPlain = false;
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 2200 && !(seenInverted && seenPlain))
            {
                var fg = screen[0, 0].Foreground;
                var bg = screen[0, 0].Background;
                if (fg == blue && bg == red) seenInverted = true;   // block ON: source fg/bg inverted
                if (fg == red && bg == blue) seenPlain = true;      // OFF: the plain glyph, original colours
                Thread.Sleep(12);
            }

            Assert.True(seenInverted, "the blinking cursor should show its inverted (on) phase");
            Assert.True(seenPlain, "the blinking cursor should show its plain (off) phase");
        }
        finally
        {
            UI.Stop();
            ConsoleGUI.ConsoleManager.EmulateBlinkingCursor = prevEmulate;
            Console.SetOut(originalOut);
        }
        Assert.True(run.Wait(2000));
    }
}
