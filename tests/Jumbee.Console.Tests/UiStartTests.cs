namespace Jumbee.Console.Tests;

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Jumbee.Console;

using Xunit;

public class UiStartTests
{
    // Begin each test from a fully stopped UI (shared process-wide static state across sequential tests).
    public UiStartTests() => UiTestHarness.EnsureStopped();

    private sealed class FakeInputSource : IInputSource
    {
        private readonly ConcurrentQueue<TerminalInputEvent> _events = new();
        public void Push(ConsoleKey key) => _events.Enqueue(new KeyInputEvent(key, '\0', TerminalModifiers.None));
        public void Push(TerminalInputEvent evt) => _events.Enqueue(evt);
        public bool TryRead(out TerminalInputEvent? evt)
        {
            if (_events.TryDequeue(out var e)) { evt = e; return true; }
            evt = null;
            return false;
        }
    }

    // Stamps an 'X' at (0,0) of its buffer each render — used to observe the render output.
    private sealed class StampControl : Control
    {
        public override bool HandlesInput => false;
        protected override void Render()
        {
            if (ActualWidth > 0 && ActualHeight > 0)
                consoleBuffer.Write(new Position(0, 0), new ConsoleGUI.Data.Character('X'));
        }
    }

    // Marks (0,0) as a steady-block cursor cell (style 2) with known fg/bg, to observe legacy software-cursor rendering.
    private sealed class CursorStampControl : Control
    {
        public override bool HandlesInput => false;
        protected override void Render()
        {
            if (ActualWidth <= 0 || ActualHeight <= 0) return;
            var deco = ConsoleGUI.Data.CursorEncoding.EncodeStyle(ConsoleGUI.Data.Decoration.None, 2); // steady block
            consoleBuffer.Write(new Position(0, 0), new ConsoleGUI.Data.Character(
                'Z', new ConsoleGUI.Data.Color(255, 0, 0), new ConsoleGUI.Data.Color(0, 0, 255), deco, isCursor: true));
        }
    }

    private sealed class KeyRecorderControl : Control
    {
        public readonly ConcurrentQueue<ConsoleKey> Received = new();
        public readonly ConcurrentQueue<string> Pasted = new();
        public override bool HandlesInput => true;
        protected override void Render() { }
        protected override void OnInput(InputEvent e)
        {
            Received.Enqueue(e.Key.Key);
            e.Handled = true;
        }
        public override void OnPaste(string text) => Pasted.Enqueue(text);
    }

    private static bool ScreenContains(ConsoleBuffer screen, string text)
    {
        for (var y = 0; y < screen.Size.Height; y++)
        {
            var row = new System.Text.StringBuilder();
            for (var x = 0; x < screen.Size.Width; x++)
            {
                var c = screen[x, y].Character.Content;
                row.Append(c is null or '\0' ? ' ' : c.Value);
            }
            if (row.ToString().Contains(text)) return true;
        }
        return false;
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
    public void Start_RoutesInjectedInput_ToFocusedControl_AndStopCompletesRunTask()
    {
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);   // swallow ConsoleManager's ANSI output

        var control = new KeyRecorderControl();
        var grid = new Grid([5], [20], [[control]]);
        var input = new FakeInputSource();
        Task run;

        try
        {
            control.IsFocused = true;

            run = UI.Start(
                grid, width: 20, height: 5, paintInterval: 20,
                console: new ConsoleBuffer { Size = new Size(20, 5) },
                input: input);

            input.Push(ConsoleKey.DownArrow);
            input.Push(ConsoleKey.UpArrow);
            input.Push(ConsoleKey.Enter);

            Assert.True(
                WaitUntil(() => control.Received.Count >= 3, 3000),
                $"Expected 3 keys routed, got {control.Received.Count}.");
            Assert.Equal(
                new[] { ConsoleKey.DownArrow, ConsoleKey.UpArrow, ConsoleKey.Enter },
                control.Received.ToArray());
        }
        finally
        {
            UI.Stop();
            Console.SetOut(originalOut);
        }

        Assert.True(run.Wait(2000), "Stop() should complete the run task.");
    }

    [Fact]
    public void Start_NonAnsi_RendersThroughIConsoleWrite()
    {
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);

        var screen = new ConsoleBuffer { Size = new Size(10, 3) };
        var grid = new Grid([3], [10], [[new StampControl()]]);
        Task run;

        try
        {
            // isAnsiTerminal: false routes rendering through IConsole.Write instead of emitting ANSI.
            run = UI.Start(grid, width: 10, height: 3, paintInterval: 20, isAnsiTerminal: false, console: screen);

            Assert.True(
                WaitUntil(() => screen[0, 0].Content == 'X', 3000),
                "Non-ANSI rendering should write the cell through IConsole.Write into the screen buffer.");
        }
        finally
        {
            UI.Stop();
            Console.SetOut(originalOut);
        }

        Assert.True(run.Wait(2000), "Stop() should complete the run task.");
    }

    [Fact]
    public void Start_NonAnsi_RendersSoftwareCursor_BlockInverted()
    {
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);

        var screen = new ConsoleBuffer { Size = new Size(10, 3) };
        var grid = new Grid([3], [10], [[new CursorStampControl()]]);
        Task run;

        try
        {
            run = UI.Start(grid, width: 10, height: 3, paintInterval: 20, isAnsiTerminal: false, console: screen);

            Assert.True(WaitUntil(() => screen[0, 0].Content == 'Z', 3000), "software cursor cell should be rendered");
            // A steady block cursor renders the cell with fg/bg inverted (hardware cursor unused).
            Assert.True(screen[0, 0].Foreground == new ConsoleGUI.Data.Color(0, 0, 255), "fg should be the source bg");
            Assert.True(screen[0, 0].Background == new ConsoleGUI.Data.Color(255, 0, 0), "bg should be the source fg");
        }
        finally
        {
            UI.Stop();
            Console.SetOut(originalOut);
        }

        Assert.True(run.Wait(2000), "Stop() should complete the run task.");
    }

    [Fact]
    public void Start_RoutesPasteEvent_ToFocusedControl()
    {
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);

        var control = new KeyRecorderControl();
        var grid = new Grid([5], [20], [[control]]);
        var input = new FakeInputSource();
        Task run;

        try
        {
            control.IsFocused = true;
            run = UI.Start(grid, width: 20, height: 5, paintInterval: 20,
                console: new ConsoleBuffer { Size = new Size(20, 5) }, input: input);

            input.Push(new PasteInputEvent("hello\nworld"));

            Assert.True(
                WaitUntil(() => control.Pasted.Count >= 1, 3000),
                "Expected the paste to reach the focused control.");
            Assert.Equal("hello\nworld", control.Pasted.ToArray()[0]);
        }
        finally
        {
            UI.Stop();
            Console.SetOut(originalOut);
        }

        Assert.True(run.Wait(2000), "Stop() should complete the run task.");
    }

    [Fact]
    public void Start_WrapsPlainRootInOverlay_AndOverlayControlsDefaultToIt()
    {
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);

        var select = new Select("Alpha", "Beta", "Gamma");   // note: no explicit .Overlay set
        var grid = new Grid([3], [20], [[select]]);          // a plain layout, not an Overlay
        Task run;

        try
        {
            run = UI.Start(grid, width: 20, height: 8, paintInterval: 20,
                console: new ConsoleBuffer { Size = new Size(20, 8) }, input: new FakeInputSource());

            // Start wraps a non-Overlay root in a UI-owned overlay, exposed as UI.Overlay.
            Assert.NotNull(UI.Overlay);
            Assert.Null(UI.Overlay!.Top);   // nothing shown yet

            // Opening the dropdown with no explicit host must fall back to the ambient UI.Overlay.
            UI.Invoke(() => select.Open());
            Assert.True(
                WaitUntil(() => UI.Overlay!.Top is not null, 2000),
                "A Select with no explicit Overlay should open its dropdown into the ambient UI.Overlay.");
        }
        finally
        {
            UI.Stop();
            Console.SetOut(originalOut);
        }

        Assert.True(run.Wait(2000), "Stop() should complete the run task.");
    }

    [Fact]
    public void RightClick_OnTreeNode_SelectsIt_RaisesOpening_AndShowsContextMenu()
    {
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);

        var tree = new Tree("Root");        // row 0
        var alpha = tree.AddNode("Alpha");  // row 1
        tree.AddNode("Beta");               // row 2
        var menu = new ContextMenu([new MenuItem("Delete")]);
        tree.ContextMenu = menu;
        Tree.TreeNode? opened = null;
        tree.ContextMenuOpening += (_, n) => opened = n;

        var grid = new Grid([6], [24], [[tree]]);
        var screen = new ConsoleBuffer { Size = new Size(24, 6) };
        var input = new FakeInputSource();
        Task run;

        try
        {
            // Non-ANSI so rendering lands in `screen`, letting us wait for the tree to actually paint (so its cells
            // carry mouse listeners) before injecting the click.
            run = UI.Start(grid, width: 24, height: 6, paintInterval: 20, isAnsiTerminal: false, console: screen, input: input);
            UI.Invoke(() => UI.SetFocus(tree));
            Assert.True(WaitUntil(() => screen[0, 0].Content == '▼', 3000), "tree should render before the click");

            // Right-press then release on Alpha's row (y = 1).
            input.Push(new MouseInputEvent(5, 1, TerminalMouseButton.Right, TerminalMouseKind.Down, TerminalModifiers.None));
            input.Push(new MouseInputEvent(5, 1, TerminalMouseButton.Right, TerminalMouseKind.Up, TerminalModifiers.None));

            Assert.True(WaitUntil(() => opened is not null, 3000), "right-click should raise ContextMenuOpening");
            Assert.Same(alpha, opened);                 // the right-clicked node, now selected
            Assert.Same(alpha, tree.SelectedNode);
            Assert.True(WaitUntil(() => UI.Overlay?.Top is not null, 2000), "the context menu should be shown");
        }
        finally
        {
            UI.Stop();
            Console.SetOut(originalOut);
        }

        Assert.True(run.Wait(2000), "Stop() should complete the run task.");
    }

    [Fact]
    public void RightClick_OnListBoxRow_SelectsIt_RaisesOpening_AndShowsContextMenu()
    {
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);

        var list = new ListBox("Alpha", "Beta", "Gamma");   // rows 0,1,2
        var menu = new ContextMenu([new MenuItem("Delete")]);
        list.ContextMenu = menu;
        ListBox.ListBoxItem? opened = null;
        list.ContextMenuOpening += (_, item) => opened = item;

        var grid = new Grid([6], [24], [[list]]);
        var screen = new ConsoleBuffer { Size = new Size(24, 6) };
        var input = new FakeInputSource();
        Task run;

        try
        {
            run = UI.Start(grid, width: 24, height: 6, paintInterval: 20, isAnsiTerminal: false, console: screen, input: input);
            UI.Invoke(() => UI.SetFocus(list));
            Assert.True(WaitUntil(() => screen[0, 0].Content == 'A', 3000), "list should render before the click");

            // Right-press then release on Beta's row (y = 1).
            input.Push(new MouseInputEvent(2, 1, TerminalMouseButton.Right, TerminalMouseKind.Down, TerminalModifiers.None));
            input.Push(new MouseInputEvent(2, 1, TerminalMouseButton.Right, TerminalMouseKind.Up, TerminalModifiers.None));

            Assert.True(WaitUntil(() => opened is not null, 3000), "right-click should raise ContextMenuOpening");
            Assert.Equal("Beta", opened!.Text);
            Assert.Equal(1, list.SelectedIndex);
            Assert.True(WaitUntil(() => UI.Overlay?.Top is not null, 2000), "the context menu should be shown");
        }
        finally
        {
            UI.Stop();
            Console.SetOut(originalOut);
        }

        Assert.True(run.Wait(2000), "Stop() should complete the run task.");
    }

    [Fact]
    public void ContextMenu_Submenu_OpensLive_AndRendersIntoScreen()
    {
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);

        var menu = new ContextMenu([new MenuItem("Recent", new MenuItem[] { new("alpha.cs"), new("beta.cs") })]);
        var grid = new Grid([12], [40], [[new TextLabel(TextLabelOrientation.Horizontal, "bg".PadRight(40), Color.White)]]);
        var screen = new ConsoleBuffer { Size = new Size(40, 14) };
        var input = new FakeInputSource();
        Task run;

        try
        {
            // Non-ANSI so rendering lands in `screen`, and a persistent (not re-created-per-frame) layout — the very
            // conditions under which a self-resizing popup previously failed to re-lay-out when its submenu opened.
            run = UI.Start(grid, width: 40, height: 14, paintInterval: 20, isAnsiTerminal: false, console: screen, input: input);
            UI.Invoke(() => menu.Show(1, 1));   // shows + focuses the menu in the ambient overlay
            Assert.True(WaitUntil(() => ScreenContains(screen, "Recent"), 3000), "the menu should render");

            // Open the submenu (Right on the highlighted "Recent"); it must appear in the live screen buffer.
            input.Push(ConsoleKey.RightArrow);
            Assert.True(WaitUntil(() => ScreenContains(screen, "alpha.cs"), 3000),
                "opening a submenu must re-lay-out the popup so the child items render live");
        }
        finally
        {
            UI.Stop();
            Console.SetOut(originalOut);
        }

        Assert.True(run.Wait(2000), "Stop() should complete the run task.");
    }

    [Fact]
    public void Stop_InvokedOnUiThread_DoesNotSelfJoinHang_AndCompletesRunTask()
    {
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);

        var control = new KeyRecorderControl();
        var grid = new Grid([5], [20], [[control]]);
        Task run;

        try
        {
            run = UI.Start(
                grid, width: 20, height: 5, paintInterval: 20,
                console: new ConsoleBuffer { Size = new Size(20, 5) },
                input: new FakeInputSource());

            // Simulate the Ctrl-Q hotkey path: Stop runs on the UI thread. The dispatcher must not try to
            // Join its own thread (which would block for the full timeout).
            UI.Post(() => UI.Stop());

            Assert.True(run.Wait(2000), "Stop() from the UI thread should complete the run task promptly.");
            Assert.True(WaitUntil(() => !UI.Dispatcher.IsRunning, 2000), "Dispatcher should have stopped.");
            Assert.False(UI.IsRunning);
        }
        finally
        {
            UI.Stop();
            Console.SetOut(originalOut);
        }
    }
}
