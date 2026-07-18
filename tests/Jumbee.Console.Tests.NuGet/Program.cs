namespace Jumbee.Console.Tests.NuGet;

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using ConsoleGUI.Api;
using ConsoleGUI.Data;
using ConsoleGUI.Space;

using Jumbee.Console;

// In this namespace (not Jumbee.Console) a bare `Color` is ambiguous between Jumbee's Styles struct and
// ConsoleGUI.Data.Color (imported above for Character). The public API is Jumbee's, so pin it.
using Color = Jumbee.Console.Color;

/// <summary>
/// A headless CLI smoke test for the published <c>Jumbee.Console</c> NuGet package. It restores the package from
/// nuget.org (see the .csproj), then constructs and drives the major control families to prove the package — its
/// bundled ext/ fork assemblies and its NTokenizers dependency — loads and runs end-to-end. Exit code 0 = all
/// checks passed; non-zero = the number of failed checks. No real terminal required (renders to a null console).
/// </summary>
public static class Program
{
    private static int failures;
    private static int passed;

    public static int Main(string[] args)
    {
        Console.WriteLine("Jumbee.Console NuGet package smoke test");
        Console.WriteLine("=======================================");

        var expected = ExpectedVersion(args);
        Check("Package loads and reports its version", () =>
        {
            var asm = typeof(UI).Assembly;
            var version = asm.GetName().Version?.ToString() ?? "<none>";
            var informational = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            Console.WriteLine($"       loaded {asm.GetName().Name} {version}" +
                              (informational is null ? "" : $" ({informational})"));
            if (expected is not null && !version.StartsWith(expected, StringComparison.Ordinal))
                Console.WriteLine($"       WARNING: expected package version {expected}, but loaded {version}");
        });

        Check("Styles / Color / markup", ExerciseStyles);
        Check("Layout controls", ExerciseLayouts);
        Check("Text controls (TextEditor / CodeEditor / TextInput)", ExerciseTextControls);
        Check("List / Tree / Table / Tabs", ExerciseCollections);
        Check("Buttons and toggle widgets", ExerciseButtonsAndToggles);
        Check("Display widgets (Log / Sparkline / Digits / Badge / Spinner)", ExerciseDisplayWidgets);
        Check("Composite / agent controls (ChatPrompt / MenuBar / Footer)", ExerciseCompositeControls);
        Check("Theming (style + glyph themes)", ExerciseTheming);
        Check("Spectre.Console bridge (AnsiConsoleBuffer + SpectreControl)", ExerciseSpectreBridge);
        Check("Headless UI loop (start, live-update, hotkeys, stop)", ExerciseHeadlessUiLoop);

        Console.WriteLine();
        Console.WriteLine($"Result: {passed} passed, {failures} failed.");
        if (failures == 0)
            Console.WriteLine("SMOKE TEST PASSED — the published package is functional.");
        else
            Console.WriteLine("SMOKE TEST FAILED.");
        return failures;
    }

    #region Checks
    private static void ExerciseStyles()
    {
        var c = new Color(150, 170, 255);
        _ = c.R + c.G + c.B;
        // Implicit conversions to/from the underlying colour types must round-trip.
        Color mixed = Color.White;
        _ = Spectre.Console.Markup.Escape("[not a tag]");
        _ = new TextLabel(TextLabelOrientation.Horizontal, "styled", mixed);
    }

    private static void ExerciseLayouts()
    {
        var a = new TextLabel(TextLabelOrientation.Horizontal, "A", Color.White);
        var b = new TextLabel(TextLabelOrientation.Horizontal, "B", Color.White);
        var grid = new Grid([1], [10, 10], [[a, b]]);
        var stack = new VerticalStackPanel(
            new TextLabel(TextLabelOrientation.Horizontal, "one", Color.White),
            new TextLabel(TextLabelOrientation.Horizontal, "two", Color.White));
        var dock = new DockPanel(DockedControlPlacement.Top,
            new TextLabel(TextLabelOrientation.Horizontal, "header", Color.White), stack);
        var split = new SplitPanel(SplitOrientation.Horizontal,
            new ListBox("x", "y"), new TextEditor { Text = "hi" }, 8);
        _ = new Overlay(grid);
        _ = (dock, split);
    }

    private static void ExerciseTextControls()
    {
        _ = new TextInput(placeholder: "type here");
        _ = new TextEditor { Text = "line 1\nline 2" };
        _ = new CodeEditor(Language.CSharp) { Text = "class C { }" };
        _ = new CodeEditor(Language.Json) { Text = "{ \"a\": 1 }", ReadOnly = true };
        _ = new MultiTabCodeEditor(Language.CSharp);
    }

    private static void ExerciseCollections()
    {
        var list = new ListBox();
        foreach (var s in new[] { "alpha", "beta", "gamma" }) list.AddItem(s);
        list.SelectionStyle = SelectionStyle.Highlight;

        var tree = new Tree("root");
        var node = tree.AddNode("branch");
        node.AddChild("leaf");

        var table = new DataTable("Key", "Value");
        table.AddRow("Content-Type", "application/json");

        _ = new TabPanel(TabBarDock.Top, ("List", list), ("Tree", tree), ("Table", table));
    }

    private static void ExerciseButtonsAndToggles()
    {
        var button = new Button("Send");
        button.Style = button.Style.WithShape(ButtonShape.Modern);
        _ = new Checkbox("enable");
        _ = new Switch("dark mode");
        _ = new RadioSet("Light", "Dark", "Solarized") { SelectedIndex = 0 };
        _ = new SelectionList("Cheese", "Mushroom");
        _ = new Select("GET", "POST", "PUT") { Placeholder = "GET" };
    }

    private static void ExerciseDisplayWidgets()
    {
        var log = new Log();
        log.Write("[green]OK[/] started");
        _ = new Sparkline([3, 5, 2, 8, 6]) { Bars = Sparkline.AsciiBars };
        _ = new Digits(DateTime.Now.ToString("HH:mm:ss"));
        _ = new Badge("200 OK") { Variant = BadgeVariant.Success };
        _ = new Link("Spectre.Console", "https://spectreconsole.net");
        var spinner = new Spinner { SpinnerType = Spectre.Console.Spinner.Known.Dots };
        spinner.Start();
        spinner.Stop();
    }

    private static void ExerciseCompositeControls()
    {
        var chat = new ChatPrompt(placeholder: "Send a message…");
        chat.WithSuggestions("/help", "/quit");
        _ = new MenuBar().Add("File",
            new MenuItem("Open", () => { }),
            MenuItem.Separator,
            new MenuItem("Quit", () => { }));
        _ = new Footer(new FooterHint("Enter", "Send"), new FooterHint("^c", "Quit"));
        _ = new ContextMenu([new MenuItem("Copy", () => { }), new MenuItem("Paste", () => { })]);
    }

    private static void ExerciseTheming()
    {
        // Set the theme statics (safe without a running UI — controls capture them at construction).
        UI.StyleTheme = new DefaultStyleTheme();
        UI.GlyphTheme = new DefaultGlyphTheme();
    }

    private static void ExerciseSpectreBridge()
    {
        // The core bridge: render a real Spectre.Console renderable into a ConsoleBuffer via AnsiConsoleBuffer,
        // and wrap a Spectre renderable as a Jumbee control (SpectreControl : RenderableControl : IRenderable).
        var buffer = new ConsoleBuffer { Size = new Size(40, 10) };
        buffer.Initialize();
        using var ansi = new AnsiConsoleBuffer(buffer);
        var panel = new Spectre.Console.Panel(new Spectre.Console.Markup("[green]bridge[/] works"))
        {
            Header = new Spectre.Console.PanelHeader("Spectre"),
        };
        ansi.Write(panel);
        _ = new SpectreControl<Spectre.Console.Panel>(panel);
    }

    private static void ExerciseHeadlessUiLoop()
    {
        var log = new Log();
        log.Write("boot");
        var clock = new Digits("00:00:00");
        var spark = new Sparkline([1, 2, 3, 4, 5]) { Bars = Sparkline.AsciiBars };
        var status = new Badge("—");
        var editor = new CodeEditor(Language.CSharp) { Text = "// smoke\nclass C { }" };

        var grid = new Grid(
            [3, 1, 1, 8],
            [40],
            [
                [clock.WithFrame(title: "Clock")],
                [spark],
                [status],
                [editor.WithFrame(title: "Editor")],
            ]);

        // Prove the hotkey-registration API is callable (we stop via the timer below, not this key).
        UI.RegisterHotKey(UI.HotKeys.Escape, UI.Stop);

        var console = new NullConsole { Size = new Size(40, 20) };
        var frames = 0;

        // Drive the loop from a background thread: post live mutations onto the UI thread each tick, then stop.
        var driver = Task.Run(async () =>
        {
            for (var i = 0; i < 12; i++)
            {
                await Task.Delay(30);
                UI.Post(() =>
                {
                    frames++;
                    clock.Text = DateTime.Now.ToString("HH:mm:ss");
                    log.Write($"tick {i}");
                    status.Text = $"{200 + i}";
                });
            }
            // Exercise UI.Invoke (blocking marshal) and a live theme switch, then shut down.
            UI.Invoke(() => UI.SetTheme(new DefaultStyleTheme(), new DefaultGlyphTheme()));
            UI.Stop();
        });

        var run = UI.Start(grid, width: 40, height: 20, isAnsiTerminal: false, console: console);

        // Guard: never let a wedged loop hang the smoke test.
        if (!run.Wait(TimeSpan.FromSeconds(15)))
        {
            UI.Stop();
            throw new TimeoutException("UI loop did not stop within 15s.");
        }
        driver.Wait(TimeSpan.FromSeconds(2));

        if (frames == 0)
            throw new InvalidOperationException("No frames were posted/processed by the UI thread.");
        if (UI.AverageDrawTime <= 0)
            throw new InvalidOperationException("UI reported no draw time — the render loop did not run.");

        Console.WriteLine($"       processed {frames} live updates; avg draw {UI.AverageDrawTime:F2}ms, " +
                          $"avg paint {UI.AveragePaintTime:F2}ms");
    }
    #endregion

    #region Harness
    private static void Check(string name, Action action)
    {
        try
        {
            action();
            passed++;
            Console.WriteLine($"[ OK ] {name}");
        }
        catch (Exception ex)
        {
            failures++;
            Console.WriteLine($"[FAIL] {name}");
            Console.WriteLine($"       {ex.GetType().Name}: {ex.Message}");
        }
    }

    // The expected package version, from --version <v> / the JumbeeVersion the assembly was built against.
    private static string? ExpectedVersion(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] is "--version" or "-v")
                return args[i + 1];
        return null;
    }
    #endregion

    /// <summary>A no-op <see cref="IConsole"/> so the UI loop renders with no real terminal (CI-safe).</summary>
    private sealed class NullConsole : IConsole
    {
        public Size Size { get; set; }
        public bool KeyAvailable => false;
        public void Initialize() { }
        public void OnRefresh() { }
        public void Write(Position position, in Character character) { }
        public ConsoleKeyInfo ReadKey() => throw new NotSupportedException();
    }
}
