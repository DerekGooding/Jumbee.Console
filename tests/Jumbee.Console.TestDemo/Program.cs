namespace Jumbee.Console.TestDemo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using ConsoleGUI;
using ConsoleGUI.Input;

using Vezel.Cathode.Text.Control;


using Jumbee.Console;
using static Jumbee.Console.Style;


public class Program
{
    static async Task Main(string[] args)
    {
        //ConsoleManager.EmulateBlinkingCursor = true;
        ToggleDemo(args);
        //SelectDemo(args);
        //OverlayDemo(args);
        //WheelDemo(args);
        //ButtonDemo(args);
        //GridTest(args);
        //GridTest(args);
        //SpectreControlTests.LiveDisplayTests();
        //InputDemo(args);
        //DockPanelTest(args);
        //TitleStyleTest(args);
        //ScrollBarStyleTest(args);
        //TreeAutoScrollTest(args);
        //SpectreControlTests.ProgressTests();
        Console.Clear();
        Console.WriteLine("Average UI draw time: {0}ms. Average UI paint time: {1}ms.", UI.AverageDrawTime, UI.AveragePaintTime);
        Console.WriteLine("Average control paint times:");
        foreach(var c in UI.AverageControlPaintTimes)
        {
            Console.WriteLine("{0}: {1}ms", c.Key.GetType().Name, c.Value);
        }
        Console.WriteLine("Max control paint times:");
        foreach (var c in UI.MaxControlPaintTimes)
        {
            Console.WriteLine("{0}: {1}ms", c.Key.GetType().Name, c.Value);
        }

        Console.WriteLine($"Average CPU Usage: {UI.ProcessMetrics.AverageCpuUsage:F2}%");
        Console.WriteLine($"Average Memory Usage: {UI.ProcessMetrics.AverageMemoryUsage / 1024 / 1024:F2} MB");
        Console.WriteLine($"Total Allocated: {UI.ProcessMetrics.TotalAllocatedBytes / 1024 / 1024:F2} MB");
        Console.WriteLine($"GC Fragmentation: {UI.ProcessMetrics.GcFragmentation}");
        Console.WriteLine($"Average ThreadPool Threads: {UI.ProcessMetrics.ThreadPoolThreads:F2}");
        Console.WriteLine($"Total Lock Contentions: {UI.ProcessMetrics.TotalLockContentions}");
    }

    // Step C demo: the VT input pipeline end-to-end. Click an editor to focus it (mouse), type, and paste
    // (bracketed paste arrives as one chunk via TextEditor.OnPaste). Ctrl+Q quits.
    static void InputDemo(string[] args)
    {
        var editor1 = new TextEditor();
        var editor2 = new TextEditor();
        var grid = new Grid(
            [10, 10],
            [70],
            [
                [editor1.WithFrame(title: "Editor 1 — click to focus, type, paste (Ctrl+Q quits)")],
                [editor2.WithFrame(title: "Editor 2 — click to focus")],
            ]);

        var run = UI.Start(grid, width: 72, height: 22, isAnsiTerminal: false);
        UI.SetFocus(editor1);
        run.Wait();
    }

    // Interactive Select/dropdown demo. Click the selector (or focus it and press Enter/Space) to open the
    // options anchored below; arrow keys + Enter or a click choose; Escape / click-outside cancels. The chosen
    // value shows in the status line. Needs a VT terminal (e.g. Windows Terminal).
    static void SelectDemo(string[] args)
    {
        var status = new TextLabel(TextLabelOrientation.Horizontal, "Pick a colour from the dropdown.".PadRight(36), Color.White);
        var select = new Select("Red", "Green", "Blue", "Magenta", "Cyan", "Yellow") { Placeholder = "Choose a colour ▾" };

        var bottom = new Jumbee.Console.Grid([2, 1], [38], [[status], [select]]);
        var overlay = new Overlay(bottom);
        select.Overlay = overlay;   // the dropdown floats into this overlay

        select.SelectionChanged += (_, value) => status.Text = $"You picked: {value}".PadRight(36);

        var run = UI.Start(overlay, width: 42, height: 14, isAnsiTerminal: true, input: new Jumbee.Console.VtInputSource(anyMotion: true));
        UI.SetFocus(select);
        run.Wait();
    }

    // Interactive toggle-widgets demo: a Checkbox, a Switch, a single-select RadioSet and a multi-select
    // SelectionList. Click a control to focus it, then Space toggles / arrows navigate (or just click rows).
    // The status line reflects the latest change. Needs a VT terminal (e.g. Windows Terminal).
    static void ToggleDemo(string[] args)
    {
        // Start under the "cool" theme so the frames pick up its border shape/colour. Set the theme statics
        // BEFORE constructing controls so they capture it (the switch below uses UI.SetTheme for the live path).
        UI.StyleTheme = new CoolStyleTheme();
        UI.GlyphTheme = new DefaultGlyphTheme();

        var status = new TextLabel(TextLabelOrientation.Horizontal, "Click a control, then Space / arrows.".PadRight(44), Color.White);

        var notify = new Checkbox("Enable notifications");
        var dark = new Jumbee.Console.Switch("Dark mode");

        // Keep the title placement inline (compact) but leave the BORDER unset so it follows the theme: the
        // switch then changes the frame's border shape AND colour (cool = rounded cyan, retro = heavy magenta).
        var inlineTitle = new TitleStyle(TitlePos.TopLeft, TitleBorderStyle.Inline, TitleColorStyle.Normal);

        var theme = new RadioSet("Light", "Dark", "Solarized") { SelectedIndex = 0 };
        theme.WithFrame().WithTitle("Theme (one of)", inlineTitle);

        var toppings = new SelectionList("Cheese", "Mushroom", "Pepperoni", "Olives");
        toppings.WithFrame().WithTitle("Toppings (any of)", inlineTitle);

        // Live theme switch: clicking re-skins every control above in place via UI.SetTheme — checkbox/radio/
        // switch glyphs, accent colours, the button, AND the frame borders (shape + colour).
        var retro = false;
        var themeBtn = new Button("Switch theme (cool ⇄ retro)");   // plain button → follows the theme's Primary
        themeBtn.Activated += (_, _) =>
        {
            retro = !retro;
            UI.SetTheme(
                retro ? new RetroStyleTheme() : new CoolStyleTheme(),
                retro ? new RetroGlyphTheme() : new DefaultGlyphTheme());
            status.Text = $"Theme: {(retro ? "retro" : "cool")}".PadRight(44);
        };

        notify.Changed += (_, v) => status.Text = $"Notifications: {(v ? "on" : "off")}".PadRight(44);
        dark.Changed += (_, v) => status.Text = $"Dark mode: {(v ? "on" : "off")}".PadRight(44);
        theme.SelectionChanged += (_, _) => status.Text = $"Theme: {theme.SelectedValue}".PadRight(44);
        toppings.SelectionChanged += (_, _) => status.Text = ("Toppings: " + string.Join(", ", toppings.SelectedValues)).PadRight(44);

        var grid = new Jumbee.Console.Grid(
            [2, 1, 1, 5, 6, 2],
            [46],
            [
                [status],
                [notify],
                [dark],
                [theme],
                [toppings],
                [themeBtn],
            ]);

        var run = UI.Start(grid, width: 50, height: 20, isAnsiTerminal: true, input: new Jumbee.Console.VtInputSource(anyMotion: true));
        UI.SetFocus(notify);
        run.Wait();
    }

    // Interactive overlay/modal demo. Click "Open dialog" to float a MODAL framed popup over a dimming scrim:
    // the background is blocked (clicking outside does nothing), and the dialog closes with its button or Esc
    // (the overlay's CloseKey). Needs a VT terminal (e.g. Windows Terminal).
    // Builds a button whose default theme styles are overridden with a custom colour scheme (showing that a
    // control can still depart from the active theme via its style setters).
    static Button ColorButton(string text, Color baseColor) => new Button(text)
    {
        NormalStyle = Style.White | Style.Bg(baseColor),
        HoverStyle = Style.White | Style.Bg(Lighten(baseColor, 25)),
        PressStyle = Style.White | Style.Bg(Lighten(baseColor, 55)),
    };

    static Color Lighten(Color c, int by) =>
        new((byte)Math.Min(255, c.R + by), (byte)Math.Min(255, c.G + by), (byte)Math.Min(255, c.B + by));

    static void OverlayDemo(string[] args)
    {
        var status = new TextLabel(TextLabelOrientation.Horizontal, "Click 'Open dialog' to pop a window.".PadRight(40), Color.White);
        var openBtn = ColorButton("Open dialog", new Color(40, 70, 120));

        var bottom = new Jumbee.Console.Grid([2, 3], [44], [[status], [openBtn]]);
        var overlay = new Overlay(bottom);

        var closeBtn = ColorButton("Close (or press Esc)", new Color(110, 40, 40));
        closeBtn.WithRoundedBorder(Yellow).WithTitle("Modal dialog");

        openBtn.Activated += (_, _) => { status.Text = "Modal open — Close or Esc (bg blocked).".PadRight(40); overlay.ShowModal(closeBtn); };
        closeBtn.Activated += (_, _) => { overlay.Hide(); status.Text = "Dialog closed.".PadRight(40); };

        var run = UI.Start(overlay, width: 48, height: 14, isAnsiTerminal: true, input: new Jumbee.Console.VtInputSource(anyMotion: true));
        UI.SetFocus(openBtn);
        run.Wait();
    }

    // Interactive mouse-wheel demo: a list taller than its frame. Scroll the wheel over it (needs a VT terminal,
    // e.g. Windows Terminal); Alt+Up / Alt+Down still scroll too. The scrollbar thumb tracks the position.
    static void WheelDemo(string[] args)
    {
        var list = new ListBox { SelectedForegroundColor = Color.White, SelectedBackgroundColor = Color.Blue };
        for (int i = 1; i <= 40; i++) list.AddItem($"Item {i:00}  — scroll the wheel over me");

        list.WithRoundedBorder(Cyan1)
            .WithTitle("Mouse wheel to scroll  (Alt+Up/Down also works)")
            .WithScrollBarGlyphs(ScrollBarGlyphs.Block)
            .WithScrollBarStyle(ScrollBarStyle.Default.WithColors(thumb: Cyan1, arrows: Yellow));

        list.IsFocused = true;

        var grid = new Jumbee.Console.Grid([16], [50], [[list]]);

        var run = UI.Start(grid, width: 56, height: 20, isAnsiTerminal: true, input: new Jumbee.Console.VtInputSource());
        run.Wait();
    }

    // Interactive Button demo. Click the buttons with the mouse (needs a VT terminal, e.g. Windows Terminal),
    // or focus one and press Enter/Space. Pressing a button tints its background; the status line updates.
    static void ButtonDemo(string[] args)
    {
        var count = 0;

        var status = new TextLabel(TextLabelOrientation.Horizontal, "Count: 0".PadRight(38), Color.White);
        void SetStatus(string last) => status.Text = $"Count: {count}   (last: {last})".PadRight(38);

        var inc = ColorButton("Increment (+1)", new Color(30, 90, 50));
        var dec = ColorButton("Decrement (-1)", new Color(110, 70, 30));
        var reset = ColorButton("Reset", new Color(60, 60, 110));
        var quit = ColorButton("Quit", new Color(110, 40, 40));

        inc.Activated += (_, _) => { count++; SetStatus("Increment"); };
        dec.Activated += (_, _) => { count--; SetStatus("Decrement"); };
        reset.Activated += (_, _) => { count = 0; SetStatus("Reset"); };
        quit.Activated += (_, _) => Environment.Exit(0);

        var grid = new Jumbee.Console.Grid(
            [2, 3, 3, 3, 3],
            [40],
            [
                [status],
                [inc],
                [dec],
                [reset],
                [quit],
            ]);

        // Focus a button so Enter/Space works immediately; clicks work regardless of focus.
        // anyMotion: true → DEC 1003 so hover (mouse-over without a button held) updates the button background.
        var run = UI.Start(grid, width: 44, height: 16, isAnsiTerminal: true, input: new Jumbee.Console.VtInputSource(anyMotion: true));
        UI.SetFocus(inc);
        run.Wait();
    }

    static void GridTest(string[] args)
    {
        // --- Spectre.Console Controls ---
        // 1. Table
        var table = new Spectre.Console.Table();
        /*
        table.Title("[bold yellow]Jumbee Console[/]");
        table.AddColumn("Library");
        table.AddColumn("Role");
        table.AddColumn("Status");
        table.AddRow("Spectre.Console", "Widgets & Styling", "[green]Integrated[/]");
        table.AddRow("ConsoleGUI", "Layout & Windowing", "[blue]Integrated[/]");
        table.AddRow("Jumbee", "The Bridge", "[bold red]Working![/]");
        table.Border(TableBorder.DoubleEdge);
        */
        // 2. Bar Chart
        var barChart = new BarChart(ChartOrientation.Horizontal,
            ("Planning", 12, Yellow),
            ("Coding", 54, Green),
            ("Testing", 33, Red)
        )
        {
            BarWidth = 50,
            Label = "[green bold]Activity[/]",
            CenterLabel = true
        };
        var table3 = new Spectre.Console.Table()
                .AddColumn(new Spectre.Console.TableColumn("Line"));
        var disp = new SpectreLiveDisplay(table3);
        //disp.Display.Overflow = Spectre.Console.VerticalOverflow.Ellipsis;
        
        // 3. Tree
        var treeControl = new Tree("Root", guide: TreeGuide.BoldLine)
        {
            SelectedForegroundColor = Color.White,
            SelectedBackgroundColor = Color.Blue
        };
        treeControl.AddNode("Foo").AddChildren("Bar", "Baz", "Qux");

        // Example of adding a subtree (since AddNode takes IRenderable)
        //var subTree = new Tree(rootText: "Subtree");
        //subTree.AddNode("Leaf 1");
        //subTree.AddNode("Leaf 2");
        //treeControl.AddNode(subTree);

        // --- Wrap Spectre.Console Controls for ConsoleGUI ---
        //var tableControl = new SpectreControl<Spectre.Console.Table>(table);

        //tableControl.Content.Border = TableBorder.Rounded;
        // var chartControl = new SpectreControl<Spectre.Console.BarChart>(barChart); // No longer needed
        // var treeControl = new SpectreControl<Spectre.Console.Tree>(root); // Replaced by Jumbee.Console.Tree above

        // --- ConsoleGUI Controls ---
        // Spinner
        var spinner = new Spinner
        {
            SpinnerType = Spectre.Console.Spinner.Known.Dots,
            Text = "Waiting for input...",
            Style = Spectre.Console.Style.Parse("green bold")
        };
        spinner.Start();

        // The TextPrompt control
        var prompt = new TextPrompt("[yellow]What is your name?[/]", blinkCursor: true) { Width = 20};
        prompt.Committed += (sender, name) =>
        {
            spinner.Text = $"Hello, [blue]{name}[/]!";
            spinner.SpinnerType = Spectre.Console.Spinner.Known.Ascii; // Change spinner style on success
        };

        var p = prompt
            .WithAsciiBorder()
            .WithTitle("Write here");
        var grid = new Jumbee.Console.Grid([15, 15], [40, 80], [
            [spinner.WithFrame(borderStyle: BorderStyle.Rounded, fgColor: Red, title: "Spinna benz"), prompt],
            [disp, barChart]
        ]);
        //treeControl.IsFocused = true;
        // Start the user interface
        p.Focus();
        var t = UI.Start(grid, 130, 40);
        disp.Start((ctx) =>
        {
            for (int i = 1; i <= 100; i++)
            {
                //ctx.
                table3.Rows.Add([new Spectre.Console.Markup($"Line {i}")]);
                ctx.Refresh();
                Thread.Sleep(50);
            }
        });
        //UI.Start(internalGrid, width:250, height: 60, isTrueColorTerminal: true);
        // Create a separate timer to update the chartControl content periodically
        var random = new Random();
        var chartTimer = new Timer(_ =>
        {
            // The SpectreControl.Content setter will acquire the lock internally
            var newPlanning = (double)random.Next(10, 30);
            var newCoding = (double)random.Next(40, 70);
            var newTesting = (double)random.Next(20, 40);

            // Update existing items using the bulk-update indexer
            barChart["Planning", "Coding", "Testing"] = [newPlanning, newCoding, newTesting];

        }, null, 0, 50);

        t.Wait();        
    }
    
    static void ListBoxTest(string[] args)
    {
        var listBox = new ListBox
        {
            SelectedForegroundColor = Color.White,
            SelectedBackgroundColor = Color.DarkMagenta
        };
        listBox.AddItem("[red]Item 1 (Markup)[/]");
        listBox.AddItem("Item 2 (Green FG)", Color.Green);
        listBox.AddItem("Item 3 (Blue BG)", background: Color.Blue);
        listBox.AddItem("Item 4 (Yellow on Navy)", Color.Yellow, Color.Navy);

        var dynamicItem = listBox.AddItem("I will change color...", Color.White);

        var prompt = new TextPrompt("[yellow]Type a command (add/remove/clear/color/exit):[/]", blinkCursor: true) { Width = 80 };
        prompt.Committed += (sender, text) =>
        {
            if (text.StartsWith("add "))
            {
                var content = text.Substring(4);
                listBox.AddItem(content);
            }
            else if (text == "color")
            {
                dynamicItem.ForegroundColor = Color.FromSpectreColor(Spectre.Console.Color.FromInt32(new Random().Next(0, 255)));
                dynamicItem.Text = $"My color is now: {dynamicItem.ForegroundColor?.ToString() ?? "Default"}";
            }
            else if (text == "remove")
            {
                var items = listBox.Items.ToList();
                if (items.Count > 0)
                {
                    listBox.RemoveItem(items.Last());
                }
            }
            else if (text == "clear")
            {
                listBox.Clear();
            }
            else if (text == "exit")
            {
                Environment.Exit(0);
            }
        };

        var grid = new Jumbee.Console.Grid([20], [50, 50], [
            [listBox.WithRoundedBorder(Purple).WithHeight(3).WithWidth(5), prompt.WithFrame(title: "Controls")]
        ]);
        
        listBox.IsFocused = true;

        var t = UI.Start(grid);
        
        // Add some dynamic items automatically
        var random = new Random();
        var timer = new Timer(_ =>
        {
            var r = random.Next(0, 100);
            if (r < 30)
            {
                listBox.AddItem($"Auto Item {DateTime.Now.Second}");
            }
        }, null, 0, 1000);

        t.Wait();
    }
    
    static void TitleStyleTest(string[] args)
    {
        // Builds a framed label demonstrating one TitleStyle combination.
        Control Box(string content, BorderStyle border, TitlePos pos, TitleBorderStyle titleBorder, TitleColorStyle titleColor, Color color) =>
            new TextLabel(TextLabelOrientation.Horizontal, content, Color.White)
                .WithWidth(26)
                .WithHeight(1)
                .WithBorder(border, color)
                .WithTitle("Title", new TitleStyle(pos, titleBorder, titleColor));

        // Columns: Inline title (top, Reverse colors) vs. Double title (bottom, Normal colors).
        // Rows: Left, Center, Right alignment.
        var grid = new Jumbee.Console.Grid(
            [6, 6, 6],
            [30, 30],
            [
                [Box("inline top-left",      BorderStyle.Rounded, TitlePos.TopLeft,      TitleBorderStyle.Inline, TitleColorStyle.Reverse, Green),
                 Box("double bottom-left",   BorderStyle.Double,  TitlePos.BottomLeft,   TitleBorderStyle.Double, TitleColorStyle.Normal,  Red)],
                [Box("inline top-center",    BorderStyle.Heavy,   TitlePos.TopCenter,    TitleBorderStyle.Inline, TitleColorStyle.Reverse, Cyan1),
                 Box("double bottom-center", BorderStyle.Double,  TitlePos.BottomCenter, TitleBorderStyle.Double, TitleColorStyle.Reverse, Magenta1)],
                [Box("inline top-right",     BorderStyle.Square,  TitlePos.TopRight,     TitleBorderStyle.Inline, TitleColorStyle.Reverse, Yellow),
                 Box("double bottom-right",  BorderStyle.Double,  TitlePos.BottomRight,  TitleBorderStyle.Double, TitleColorStyle.Normal,  Blue)],
            ]);

        var t = UI.Start(grid, 64, 20);
        t.Wait();
    }

    static void ScrollBarStyleTest(string[] args)
    {
        // A list long enough to overflow its frame so the vertical scrollbar is shown.
        ListBox MakeList(Color selectColor)
        {
            var lb = new ListBox { SelectedForegroundColor = Color.White, SelectedBackgroundColor = selectColor };
            for (int i = 1; i <= 40; i++) lb.AddItem($"Item {i}");
            return lb;
        }

        var blockList  = MakeList(Blue);
        var shadedList = MakeList(Purple);
        var lineList   = MakeList(Green);

        blockList
            .WithRoundedBorder(Blue).WithTitle("Block")
            .WithScrollBarGlyphs(ScrollBarGlyphs.Block)
            .WithScrollBarStyle(ScrollBarStyle.Default.WithColors(thumb: Cyan1, arrows: White));
        shadedList
            .WithRoundedBorder(Purple).WithTitle("Shaded")
            .WithScrollBarGlyphs(ScrollBarGlyphs.Shaded)
            .WithScrollBarStyle(ScrollBarStyle.Default.WithColors(thumb: Magenta1, track: Silver, arrows: Red));
        lineList
            .WithRoundedBorder(Green).WithTitle("Line")
            .WithScrollBarGlyphs(ScrollBarGlyphs.Line)
            .WithScrollBarStyle(ScrollBarStyle.Default.WithColors(thumb: Yellow, arrows: Green));

        var grid = new Jumbee.Console.Grid(
            [20],
            [26, 26, 26],
            [[blockList, shadedList, lineList]]);

        // Focus one so Alt+Up / Alt+Down scrolls it and moves the thumb.
        blockList.IsFocused = true;

        var t = UI.Start(grid, 84, 24);
        t.Wait();
    }

    static void TreeAutoScrollTest(string[] args)
    {
        var tree = new Tree("Project", TreeGuide.BoldLine, Green | Dim)
        {
            SelectedForegroundColor = Color.White,
            SelectedBackgroundColor = Color.Blue
        };

        // Build a tree tall enough to overflow its frame so navigating scrolls the view.
        for (int i = 1; i <= 8; i++)
        {
            var folder = tree.AddNode($"Folder {i}");
            folder.AddChildren($"file{i}a.cs", $"file{i}b.cs", $"file{i}c.cs");
        }

        tree.WithRoundedBorder(Purple)
            .WithTitle("Tree (Up/Down to navigate)")
            .WithScrollBarGlyphs(ScrollBarGlyphs.Block)
            .WithScrollBarStyle(ScrollBarStyle.Default.WithColors(thumb: Cyan1, arrows: Yellow));

        tree.IsFocused = true;

        var grid = new Jumbee.Console.Grid(
            [16],
            [44],
            [[tree]]);

        var t = UI.Start(grid, 60, 20);
        t.Wait();
    }

    static void DockPanelTest(string[] args)
    {
        var p = new TextEditor(TextEditor.Language.Markdown, blinkCursor: false)
           .WithRoundedBorder(Purple)
           .WithTitle("Editor");
        var tree = new Tree("tree", TreeGuide.Line, Green | Dim) { Width = 20, Height=10 };
        tree.AddNodes("Y".WithStyle(Red | Dim), "Z".WithStyle(Blue | Underline)).WithTitle("Functions");
        p.Focus();
        var d = new DockPanel(DockedControlPlacement.Left, tree, p);
        //var g = new Grid([10], [100, 100], [p, tree.WithRoundedBorder(Blue)]);
        var t = UI.Start(d);
        t.Wait();
        Console.WriteLine("Average draw time: {0} Average paint time: {1}.", UI.AverageDrawTime, UI.AveragePaintTime);
        
    }

    static void AnsiControlSequenceBuilderTest(string[] args)
    {
        var cb = new AnsiControlSequenceBuilder();
        var t = new Stopwatch();
        t.Start();
        Console.CursorLeft = Console.CursorLeft + 9;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("This is red text that is really long xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");
        Console.CursorLeft = Console.CursorLeft + 10;
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("This is blue text that is really long x");
        t.Stop();
        Console.WriteLine($"Elapsed: {t.ElapsedMilliseconds} ms");
        t.Reset();
        t.Restart();
        cb.MoveCursorRight(9);
        cb.SetForegroundColor(Red);
        cb.PrintLine("This is red text that is really longs xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");
        cb.MoveCursorRight(10);
        cb.SetForegroundColor(Blue);
        cb.PrintLine("This is blue text that is really long x");
        //Task.Run(cb.WriteToSystemConsole2);
        cb.WriteToSystemConsole();
        t.Stop();
        Console.WriteLine($"Elapsed: {t.ElapsedMilliseconds} ms");
        Thread.Sleep(50);



    }
    /*
    static void Test2(string[] args)
    {
        // --- Helpers ---
        IControl CreateBox(string text, Jumbee.Console.Color color)
        {
            return new ConsoleGUI.Controls.Background
            {
                Color = color,
                Content = new ConsoleGUI.Controls.Border
                {
                    Content = new ConsoleGUI.Controls.TextBlock
                    {
                        Text = text,
                        Color = Jumbee.Console.Color.Black,
                    }
                }
            };
        }

        // --- 1. HorizontalStackPanel Test ---
        var hStack = new HorizontalStackPanel(
            CreateBox("H-Item 1", Red),
            CreateBox("H-Item 2", Green),
            CreateBox("H-Item 3", Blue)
        );
        //var hStackFrame = hStack.WithFrame(borderStyle: BorderStyle.Single, title: "Horizontal Stack");

        // --- 2. VerticalStackPanel Test ---
        var vStack = new Jumbee.Console.VerticalStackPanel(
            CreateBox("V-Item 1", Cyan1),
            CreateBox("V-Item 2", Magenta1),
            CreateBox("V-Item 3", Yellow)
        );
        //var vStackFrame = vStack.WithFrame(borderStyle: BorderStyle.Single, title: "Vertical Stack");

        // --- 3. DockedControl Test ---
        var dockedContent = CreateBox("Docked (Left)", DarkSlateGray1);
        var fillingContent = CreateBox("Filling Content", White);
        
        var dockedPanel = new Jumbee.Console.DockPanel(
            DockedControlPlacement.Left,
            dockedContent,
            fillingContent
        );
        //var dockedFrame = dockedPanel.WithFrame(borderStyle: BorderStyle.Single, title: "Docked Panel (Left)");

        var tabpanel = new TabPanel(TabBarDock.Top, controls: [("Tab 1", CreateBox("T-Item 1", Magenta1)), ("Tab 2", CreateBox("T-Item 2", Cyan1))]);

        var vt = new TextLabel(TextLabelOrientation.Horizontal, "hello", Red);
        // --- Main Layout ---
        // Combine them into a grid for display
        var grid = new Jumbee.Console.Grid([20, 10, 20, 20], [60], [
            [tabpanel],
            [hStack],
            [vStack],
            [dockedPanel],
            
        ]);

        //var mainFrame = grid.WithFrame(borderStyle: BorderStyle.Double, title: "Jumbee Console Layout Tests");

        // Start the user interface
        UI.Start(grid);

        // Main loop
        while (true)
        {
            ConsoleManager.ReadInput([new InputListener()]);
            Thread.Sleep(50);
        }


    }
    */
}

// Example custom themes used by ToggleDemo's live theme switch. A theme overrides only the tokens/glyphs it
// wants; everything else falls back to the built-in defaults. Both set the frame border shape/colour
// (FrameBorder + BorderText + TitleText) so a switch visibly re-skins the frames around the radio/selection lists.
file sealed class CoolStyleTheme : IStyleTheme
{
    public BorderStyle FrameBorder => BorderStyle.Rounded;
    public Style BorderText => Style.Cyan1;
    public Style TitleText => Style.Cyan1;
    public Style TextAccent => Style.Green1;
    public Style Selection => Style.White | Style.Bg(new Color(40, 50, 80));
    public Style Primary => Style.White | Style.Bg(new Color(40, 70, 120));
    public Style PrimaryHover => Style.White | Style.Bg(new Color(60, 90, 150));
    public Style PrimaryActive => Style.White | Style.Bg(new Color(90, 130, 200));
}

file sealed class RetroStyleTheme : IStyleTheme
{
    public BorderStyle FrameBorder => BorderStyle.Heavy;
    public Style BorderText => Style.Magenta1;
    public Style TitleText => Style.Magenta1;
    public Style TextAccent => Style.Magenta1;
    public Style Selection => Style.White | Style.Bg(new Color(80, 30, 80));
    public Style Primary => Style.White | Style.Bg(new Color(90, 30, 90));
    public Style PrimaryHover => Style.White | Style.Bg(new Color(120, 45, 120));
    public Style PrimaryActive => Style.White | Style.Bg(new Color(160, 70, 160));
}

file sealed class RetroGlyphTheme : IGlyphTheme
{
    public string CheckboxChecked => "[*]";
    public string RadioSelected => "<o>";
    public string RadioUnselected => "< >";
    public string SwitchOn => "[ON ]";
    public string SwitchOff => "[OFF]";
}

