namespace Jumbee.Console.AgentHarnessDemo;

using System.Collections.Generic;

using Jumbee.Console;

using NTokenizers.Extensions.Spectre.Console.Styles;

/// <summary>
/// Composes the whole harness UI from Jumbee.Console controls and wires the scripted <see cref="AgentSimulator"/> to
/// the chat prompt. Layout: a session rail (left), a chat column with transcript + prompt (centre), and a task
/// list over a document viewer (right) — three resizable <see cref="SplitPanel"/> regions.
/// </summary>
internal sealed class AgentHarnessApp
{
    #region Constructors
    public AgentHarnessApp()
    {
        // Capture the Claude palette before any control is constructed (controls read the theme once, on creation).
        UI.StyleTheme = new ClaudeDarkTheme();

        // Markup fragments for the panel header bars, resolved once from the theme.
        _coral = ((Style)Palette.Coral).ToMarkup();
        _text = ((Style)Palette.Text).ToMarkup();
        _muted = ((Style)Palette.TextMuted).ToMarkup();
        _faint = ((Style)Palette.TextFaint).ToMarkup();

        _sidebar = new Sidebar(Sessions());
        _transcript = new TranscriptView();
        _tasks = new TaskListView();
        // NTokenizers ships only a bright Default MarkdownStyles (yellow headings, blue bold, red lists). Build a fresh
        // instance tuned to the warm palette and set it before UI.Start (a new instance — never mutate the shared
        // Default). MarkdownViewer.Styles marshals via UI.Invoke, which runs inline before the UI thread exists.
        _doc = new MarkdownViewer { Styles = DarkMarkdownStyles() };

        _prompt = new ChatPrompt("Reply to Claude…  (type / for commands)") { Prompt = "❯" };
        _prompt.WithSuggestions("/help", "/model", "/clear", "/compact", "/review", "/plan", "/cost", "/resume");

        _model = MakeSelect(["Opus 4.8", "Sonnet 5", "Haiku 4.5", "Opus 4.7"]);
        _effort = MakeSelect(["High", "Medium", "Low", "Max"]);
        _permission = MakeSelect(["Auto", "Plan", "Accept Edits", "Read Only"]);

        _footer = new Footer(
            new FooterHint("↵", "Send"),
            new FooterHint("⇧↵", "Newline"),
            new FooterHint("F1", "Help"),
            new FooterHint("^Q", "Quit"));

        _root = BuildLayout();

        _sim = new AgentSimulator(_transcript, _tasks, _doc, _prompt);
        SeedSession();

        _prompt.Submitted += (_, text) => _sim.Run(text);
    }
    #endregion

    #region Properties
    /// <summary>The composed root layout — exposed for the headless verify/dump render.</summary>
    internal ILayout Root => _root;
    #endregion

    #region Methods
    public void Run()
    {
        UI.RegisterHotKey(UI.HotKeys.Ctrl(System.ConsoleKey.Q), UI.Stop);
        var run = UI.Start(_root, width: 150, height: 44, isAnsiTerminal: true, input: new VtInputSource(anyMotion: true));
        UI.SetFocus(_prompt.Input);
        run.Wait();
    }

    // ── Layout ──────────────────────────────────────────────────────────────────────────────────────────────────

    private ILayout BuildLayout()
    {
        // Centre column: a session header and the scrolling transcript, with a docked input block at the bottom —
        // stacked bottom-up as footer, selectors row, framed prompt (the IdeDemo nested-DockPanel idiom, where each
        // docked edge takes its child's fixed height and the rest fills).
        var promptRow = new Boundary(_prompt.WithFrame(BorderStyle.Rounded, borderFgColor: Palette.Coral), height: 3);
        var selectorsRow = new Boundary(ControlsRow(), height: 1);
        var header = HeaderBar($" [{_coral}]✦[/] [{_text} bold]Silvergun[/]   [{_faint}]main · claude-opus-4-8[/]",
                               $"[{_muted}]↻  ⋯[/]", rightCells: 5);

        var centre = new DockPanel(DockedControlPlacement.Bottom, _footer,
            new DockPanel(DockedControlPlacement.Bottom, selectorsRow,
                new DockPanel(DockedControlPlacement.Bottom, promptRow,
                    new DockPanel(DockedControlPlacement.Top, header, _transcript.WithFrame(BorderStyle.None)))));

        // Right column: the live task list over the document viewer.
        var tasks = new DockPanel(DockedControlPlacement.Top,
            HeaderBar($" [{_muted}]‹[/]  [{_text}]Rename analyzer/disassembler tests[/]", $"[{_faint}]✕[/]", rightCells: 3),
            _tasks.WithFrame(BorderStyle.None));
        var doc = new DockPanel(DockedControlPlacement.Top,
            HeaderBar($" [{_coral}]▤[/]  [{_text}]prior-art-supply-chain.md[/]", "", rightCells: 0),
            _doc.WithFrame(BorderStyle.None));
        var right = new SplitPanel(SplitOrientation.Vertical, tasks, doc, splitPosition: 22);

        // Left rail | (centre | right).
        var centreAndRight = new SplitPanel(SplitOrientation.Horizontal, centre, right, splitPosition: 74);
        return new SplitPanel(SplitOrientation.Horizontal, SidebarPane(), centreAndRight, splitPosition: 30);
    }

    // The sidebar with a brand row docked above it.
    private ILayout SidebarPane() => new DockPanel(DockedControlPlacement.Top,
        HeaderBar($" [{_coral}]✦[/] [{_text} bold]Claude[/]", $"[{_muted}]≡[/]", rightCells: 3),
        _sidebar.Layout);

    // The model / effort / permission selectors laid out left-to-right under the prompt.
    private ILayout ControlsRow() => new HorizontalStackPanel(
        Spacer(1), _model, Spacer(1), _effort, Spacer(1), _permission);

    // A one-row header bar: left markup filling, right markup pinned to the right edge. The left is always the fill
    // child of a DockPanel (a bare Boundary won't stretch a content-sized control to full width), so a 1-cell right
    // spacer stands in when there's no right content.
    private static IFocusable HeaderBar(string left, string right, int rightCells)
    {
        var l = Markup(left);
        var rightWidth = System.Math.Max(1, rightCells);
        var row = new DockPanel(DockedControlPlacement.Right, new Boundary(Markup(right), width: rightWidth), l);
        return new Boundary(row, height: 1);
    }

    private static SpectreControl<Spectre.Console.Markup> Markup(string markup) =>
        new(new Spectre.Console.Markup(markup)) { Focusable = false };

    private static IFocusable Spacer(int width) =>
        new SpectreControl<Spectre.Console.Markup>(new Spectre.Console.Markup("")) { Focusable = false, Width = width };

    // The selectors sit at the bottom of the screen, so their dropdowns open upward (Above) to stay on-screen.
    private static Select MakeSelect(string[] options) =>
        new Select(options)
        {
            Foreground = Palette.Text,
            Background = Palette.RaisedBg,
            SelectedIndex = 0,
            PopupPosition = SelectPopupPosition.Above,
        };

    // ── Seed content ────────────────────────────────────────────────────────────────────────────────────────────

    private static IEnumerable<SessionItem> Sessions() =>
    [
        new("Silvergun", Active: true),
        new("OnlyHumans"),
        new("OnlyHumans VS tests"),
        new("Jumbee.Console"),
        new("Camel", Warn: true),
        new("E theorem prover MSYS build errors"),
        new("VsSolidity"),
        new("General coding session"),
        new("SRL-2015"),
        new("SRL-ROCBA"),
        new("SRL-2018"),
        new("DFWRS2008"),
        new("NITROBA"),
        new("NIST_HACKING_CASE"),
        new("ALIHADI"),
        new("NIST_DATA_LEAKAGE"),
    ];

    // Pre-populates the panes so the app opens mid-session, like the reference screenshot.
    private void SeedSession()
    {
        _transcript.AddUser("go ahead with increment 2");

        _transcript.AddAssistant(
            $"Increment 1 verified (55 IL + 29 package, CLI builds) and now in review. The [{_coral}]CciAssembly[/] " +
            $"contract tests are being added in parallel. Once clean, the capability scan is fully CCI-native and " +
            $"lock-free — increments 2 (ObfuscationAnalysis retarget) and eventually the metadata consolidation follow.");

        MarkDone(_transcript.AddTool("◇", "Read 6 files", Palette.Blue));
        MarkDone(_transcript.AddTool("◆", "Edited RESEARCH-JOURNAL.md", Palette.Green, "+29 -0"));
        MarkDone(_transcript.AddTool("▸", "Ran a command, ran 2 agents", Palette.Yellow));

        _transcript.AddAssistant(
            $"Two things for your input:\n" +
            $"[{_text}]1.[/] Research decision — Application Inspector [{_coral}]RulesEngine[/] is a standalone NuGet library.\n" +
            $"[{_text}]2.[/] Still open: the SharpCompress NU1902 decision (bump / pin / accept).");

        // Task list (right-top) — the "Rename analyzer/disassembler tests" run from the screenshot.
        Done(_tasks.AddStep("Read the task spec and mapped references"));
        Done(_tasks.AddStep("Examined each affected file"));
        Done(_tasks.AddStep("Read 6 files", indent: 1));
        Done(_tasks.AddStep("Read the analysis-net test files"));
        Done(_tasks.AddStep("Renamed analysis-net → Disassembler first"));
        Fail(_tasks.AddStep("Failed to rename test files in collision-safe order"));
        Active(_tasks.AddStep("Verifying state and moving untracked cci files"));

        _doc.Markdown = ResearchDoc;
    }

    // A MarkdownStyles tuned to the warm dark palette (coral headings/accents, muted prose) — a fresh instance so the
    // shared MarkdownStyles.Default is never mutated.
    private static MarkdownStyles DarkMarkdownStyles()
    {
        static Spectre.Console.Style S(Color fg, Spectre.Console.Decoration d = Spectre.Console.Decoration.None) =>
            new(foreground: fg, decoration: d);

        var s = new MarkdownStyles
        {
            Heading = S(Palette.Coral, Spectre.Console.Decoration.Bold),
            Bold = S(Palette.Text, Spectre.Console.Decoration.Bold),
            Italic = S(Palette.TextMuted, Spectre.Console.Decoration.Italic),
            CodeInline = S(Palette.Coral),
            CodeBlock = S(Palette.Green),
            Link = S(Palette.Blue, Spectre.Console.Decoration.Underline),
            Blockquote = S(Palette.TextMuted, Spectre.Console.Decoration.Italic),
            UnorderedListItem = S(Palette.Coral),
            OrderedListItem = S(Palette.Coral),
            TableCell = S(Palette.Text),
            HorizontalRule = S(Palette.Border),
            Emphasis = S(Palette.Yellow, Spectre.Console.Decoration.Italic),
            MarkedText = S(Palette.Yellow),
            InsertedText = S(Palette.Green),
            DefaultStyle = S(Palette.Text),
        };
        s.MarkdownHeadingStyles.Level1 = S(Palette.Coral, Spectre.Console.Decoration.Bold);
        s.MarkdownHeadingStyles.Level2To4 = S(Palette.Text, Spectre.Console.Decoration.Bold);
        s.MarkdownHeadingStyles.Level5AndAbove = S(Palette.TextMuted, Spectre.Console.Decoration.Bold);
        return s;
    }

    private static void MarkDone(ToolBlock t) => t.Status = ToolStatus.Done;
    private static void Done(AgentStep s) => s.Status = StepStatus.Done;
    private static void Fail(AgentStep s) => s.Status = StepStatus.Failed;
    private static void Active(AgentStep s) => s.Status = StepStatus.Active;
    #endregion

    #region Fields
    private const string ResearchDoc =
        "# Prior-art landscape\n\n" +
        "## supply-chain / capability analysis for package security\n\n" +
        "**Compiled:** 2026-07-16 — every claim carries a source URL. Confidence `[V]` = page fetched & quoted this " +
        "session, `[K]` = well-established.\n\n" +
        "## Executive summary\n\n" +
        "- **Correction (post-publication):** the earlier claim that Capslock is Go-only is **wrong** — it has grown " +
        "into a multi-implementation project: **JCapsLock** analyzes Java bytecode, and **cargo-capslock** analyzes " +
        "Rust at the LLVM IR level.\n" +
        "- CIL-level (.NET) capability analysis specifically remains **unoccupied**; Silvergun is not the first to " +
        "analyze compiled bytecode for capabilities, but is the first to do so for the CLR.\n\n" +
        "## Comparators\n\n" +
        "| Tool | Target | Level |\n" +
        "|------|--------|-------|\n" +
        "| Capslock | Go | source + call graph |\n" +
        "| JCapsLock | Java | bytecode |\n" +
        "| cargo-capslock | Rust | LLVM IR |\n" +
        "| **Silvergun** | **.NET** | **CIL** |\n\n" +
        "## Open questions\n\n" +
        "1. Does the AppInspector `RulesEngine` cleanly consume as a library?\n" +
        "2. Bump vs pin vs accept for the SharpCompress `NU1902` advisory.\n";

    private readonly string _coral;
    private readonly string _text;
    private readonly string _muted;
    private readonly string _faint;

    private readonly Sidebar _sidebar;
    private readonly TranscriptView _transcript;
    private readonly TaskListView _tasks;
    private readonly MarkdownViewer _doc;
    private readonly ChatPrompt _prompt;
    private readonly Select _model;
    private readonly Select _effort;
    private readonly Select _permission;
    private readonly Footer _footer;
    private readonly AgentSimulator _sim;
    private readonly ILayout _root;
    #endregion
}
