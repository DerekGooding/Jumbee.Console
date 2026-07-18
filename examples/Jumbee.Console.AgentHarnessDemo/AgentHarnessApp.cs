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
        _green = ((Style)Palette.Green).ToMarkup();
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
        _sidebar.CustomizeInvoked += OpenCustomizeDialog;          // left-nav "Customize" → modal
        _sidebar.Sessions.ContextMenu = BuildSessionMenu();        // right-click a session → menu
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
        UI.Post(_transcript.ScrollToBottom);   // open pinned to the newest message (frame is attached now)
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
        var header = HeaderBar($" [{_coral}]✦[/] [{_text} bold]Harbor API[/]   [{_faint}]main · claude-opus-4-8[/]",
                               $"[{_muted}]↻  ⋯[/]", rightCells: 5);

        var centre = new DockPanel(DockedControlPlacement.Bottom, _footer,
            new DockPanel(DockedControlPlacement.Bottom, selectorsRow,
                new DockPanel(DockedControlPlacement.Bottom, promptRow,
                    new DockPanel(DockedControlPlacement.Top, header, _transcript.WithFrame(BorderStyle.None)))));

        // Right column: the live task list over the document viewer. The task pane is short so its checklist scrolls.
        var tasks = new DockPanel(DockedControlPlacement.Top,
            HeaderBar($" [{_muted}]‹[/]  [{_text}]Add cursor pagination to /orders[/]", $"[{_faint}]✕[/]", rightCells: 3),
            _tasks.WithFrame(BorderStyle.None));
        var doc = new DockPanel(DockedControlPlacement.Top,
            HeaderBar($" [{_coral}]▤[/]  [{_text}]pagination-design.md[/]", "", rightCells: 0),
            _doc.WithFrame(BorderStyle.None));
        var right = new SplitPanel(SplitOrientation.Vertical, tasks, doc, splitPosition: 15);

        // Left rail | (centre | right).
        var centreAndRight = new SplitPanel(SplitOrientation.Horizontal, centre, right, splitPosition: 74);
        return new SplitPanel(SplitOrientation.Horizontal, SidebarPane(), centreAndRight, splitPosition: 30);
    }

    // The sidebar with a brand row docked above it.
    private ILayout SidebarPane() => new DockPanel(DockedControlPlacement.Top,
        HeaderBar($" [{_coral}]✦[/] [{_text} bold]Jumbee.Console[/]", $"[{_muted}]≡[/]", rightCells: 3),
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

    // ── Interactivity ───────────────────────────────────────────────────────────────────────────────────────────

    // The right-click menu for a session row (demo only — Rename/Delete pop a dialog, the rest are inert). The ListBox
    // selects the clicked row before showing this, so handlers could read Sessions.SelectedItem.
    private static ContextMenu BuildSessionMenu() => new(
    [
        new MenuItem("Open in", [new MenuItem("New window"), new MenuItem("Split right"), new MenuItem("Browser")]),
        new MenuItem("Pin") { Shortcut = "P" },
        new MenuItem("Mark as unread") { Shortcut = "U" },
        new MenuItem("Rename", () => Dialog.Message("Rename", "Renaming isn't wired up in this UI demo.")) { Shortcut = "R" },
        new MenuItem("Fork") { Shortcut = "F" },
        MenuItem.Separator,
        new MenuItem("Move to group", [new MenuItem("Work"), new MenuItem("Personal"), new MenuItem("Research")]),
        MenuItem.Separator,
        new MenuItem("Archive") { Shortcut = "A" },
        new MenuItem("Delete", () => Dialog.Confirm("Delete chat", "Delete this conversation? This can't be undone.", _ => { })) { Shortcut = "D" },
    ]);

    // Left-nav "Customize" opens a modal Settings-style panel (a Skills/Customize list), like the desktop app.
    private void OpenCustomizeDialog()
    {
        var list = new ListBox
        {
            HighlightFullWidth = true,
            SelectedBackgroundColor = Palette.RaisedBg,
            SelectedForegroundColor = Palette.Text,
        };
        (string Glyph, string Label)[] rows =
        [
            ("◐", "General"), ("○", "Account"), ("◇", "Privacy"), ("▤", "Billing"), ("▣", "Usage"),
            ("◈", "Capabilities"), ("↺", "Reflect"), ("◔", "Time and focus"), ("◆", "Claude Code"),
            ("≣", "Cowork"), ("★", "Skills"), ("▦", "Connectors"),
        ];
        foreach (var (glyph, label) in rows)
            list.AddItem(new Spectre.Console.Markup($"  [{_muted}]{glyph}[/]  [{_text}]{label}[/]"));
        list.SelectedIndex = 10;   // Skills, as in the screenshot
        list.Width = 40;
        list.Height = rows.Length;

        Dialog.Show("Customize", list, DialogButtons.Close, _ => { });
    }

    // ── Seed content ────────────────────────────────────────────────────────────────────────────────────────────

    // Fictitious recents — a believable mix of coding and personal chats, no real project or data.
    private static IEnumerable<SessionItem> Sessions() =>
    [
        new("Harbor API", Active: true),
        new("weekend recipe planner"),
        new("resume rewrite"),
        new("regex for log parsing"),
        new("docker compose won't start", Warn: true),
        new("vacation itinerary — Lisbon"),
        new("sql query help"),
        new("book notes: The Left Hand of Darkness"),
        new("css grid cheatsheet"),
        new("budget spreadsheet formulas"),
        new("guitar practice plan"),
        new("interview prep — system design"),
        new("garden planting schedule"),
        new("unit test flakiness"),
        new("markdown to slides"),
        new("name ideas for a puppy"),
    ];

    // Pre-populates the panes so the app opens mid-session (all fictitious). Deliberately long so the transcript and
    // both right-hand panes scroll.
    private void SeedSession()
    {
        // ── Transcript: a fictional "add pagination to /orders" session ──────────────────────────────────────────
        _transcript.AddUser("the /orders endpoint loads every order in one response and the dashboard keeps timing out. can we paginate it?");
        _transcript.AddAssistant(
            $"Yes. Two common approaches:\n" +
            $"[{_text}]•[/] [{_coral}]Offset[/] (`?page=2&limit=50`) — trivial and lets you jump to any page, but it drifts " +
            $"when rows are inserted mid-scroll and gets slow on deep pages (the DB still scans the skipped rows).\n" +
            $"[{_text}]•[/] [{_coral}]Cursor[/] / keyset — stable under writes and stays fast because it seeks by an indexed " +
            $"key. The trade-off is no random page jumps.\n\n" +
            $"For an orders feed that's constantly appended to, I'd go with cursor pagination.");
        MarkDone(_transcript.AddTool("◇", "Read 5 files", Palette.Blue));

        _transcript.AddUser("makes sense — let's do cursor pagination keyed on created_at");
        _transcript.AddAssistant(
            $"Plan: add a `limit` and an opaque `cursor` query param, translate the cursor into a keyset predicate " +
            $"[{_coral}]WHERE (created_at, id) < (@ts, @id)[/], and return a `nextCursor` in the response envelope. " +
            $"Starting on the controller and repository.");
        MarkDone(_transcript.AddTool("◆", "Edited OrdersController.cs", Palette.Green, "+38 -6"));
        MarkDone(_transcript.AddTool("◆", "Edited OrderRepository.cs", Palette.Green, "+52 -9"));
        _transcript.AddAssistant(
            $"The cursor is a base64 of `{{createdAt, id}}` so clients treat it as opaque. Added a small encoder/decoder.");
        MarkDone(_transcript.AddTool("◆", "Created CursorToken.cs", Palette.Green, "+64 -0"));

        _transcript.AddUser("double-check the ordering is stable — we had duplicates at the same timestamp before");
        _transcript.AddAssistant(
            $"Good catch. Ordering by `created_at` alone isn't a total order, so two orders sharing a timestamp can " +
            $"straddle a page boundary and get skipped or repeated. The fix is a tiebreaker: order by " +
            $"[{_coral}](created_at, id)[/] and carry the same compound key in the cursor.");
        var failing = _transcript.AddTool("▸", "Ran a command — dotnet test", Palette.Yellow);
        failing.Status = ToolStatus.Error;
        failing.Label = "Ran a command — dotnet test (1 failing)";
        _transcript.Refresh();
        _transcript.AddAssistant($"Confirmed — a case with three orders in the same second was flaky. Adding the `id` tiebreaker.");
        MarkDone(_transcript.AddTool("◆", "Edited OrderRepository.cs", Palette.Green, "+7 -3"));
        MarkDone(_transcript.AddTool("▸", "Ran a command — dotnet test", Palette.Yellow));
        _transcript.AddAssistant($"[{_green}]All 24 tests green[/] now, including the same-timestamp case.");

        _transcript.AddUser("nice. what does the response look like now?");
        _transcript.AddAssistant(
            $"The envelope is `{{ data, nextCursor, hasMore }}`. When `hasMore` is false, `nextCursor` is null " +
            $"and the client stops. Remaining before this is PR-ready:\n" +
            $"[{_coral}]1.[/] update the integration tests for the new envelope\n" +
            $"[{_coral}]2.[/] add `nextCursor` to the OpenAPI schema and regenerate the client types\n" +
            $"[{_coral}]3.[/] open the PR.");

        // ── Task list (right-top): the pagination checklist. Ends at an Active step with pending ones below, so it
        //    scrolls and the simulator can walk it forward with AdvanceStep(). ──────────────────────────────────────
        Done(_tasks.AddStep("Read the orders controller"));
        Done(_tasks.AddStep("Read the order repository + query builder"));
        Done(_tasks.AddStep("Read 5 files", indent: 1));
        Done(_tasks.AddStep("Add limit query param (default 50, max 200)"));
        Done(_tasks.AddStep("Add opaque cursor query param"));
        Done(_tasks.AddStep("Translate cursor → keyset WHERE predicate"));
        Done(_tasks.AddStep("Encode/decode the cursor token (base64)"));
        Fail(_tasks.AddStep("Ordering unstable on duplicate timestamps"));
        Done(_tasks.AddStep("Recovered — added a (created_at, id) tiebreaker"));
        Done(_tasks.AddStep("Re-ran the same-timestamp test case"));
        Done(_tasks.AddStep("Add nextCursor + hasMore to the response"));
        Active(_tasks.AddStep("Update integration tests for the new envelope"));
        _tasks.AddStep("Add nextCursor to the OpenAPI schema");
        _tasks.AddStep("Regenerate the API client types");
        _tasks.AddStep("Open the PR for review");

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
        "# Pagination design\n\n" +
        "## /orders endpoint\n\n" +
        "**Status:** in progress · cursor pagination\n\n" +
        "## Problem\n\n" +
        "The `/orders` endpoint returns the full table in one response. On large accounts the dashboard request times " +
        "out and the payload is several megabytes.\n\n" +
        "## Options considered\n\n" +
        "| Approach | Pros | Cons |\n" +
        "|----------|------|------|\n" +
        "| Offset (`page`/`limit`) | trivial; jump to any page | drifts on insert; slow deep pages |\n" +
        "| Cursor / keyset | stable under writes; fast seeks | no random page jumps; needs a total order |\n\n" +
        "## Decision\n\n" +
        "Use **cursor (keyset) pagination** ordered by the compound key `(created_at, id)`:\n\n" +
        "- `GET /orders?limit=50&cursor=<opaque>`\n" +
        "- The cursor is a base64 of `{ createdAt, id }` — clients treat it as opaque.\n" +
        "- The query seeks: `WHERE (created_at, id) < (@ts, @id) ORDER BY created_at DESC, id DESC LIMIT @limit`.\n" +
        "- Response envelope: `{ data, nextCursor, hasMore }`; `nextCursor` is null when `hasMore` is false.\n\n" +
        "## Why the tiebreaker\n\n" +
        "`created_at` alone is not a total order — orders created in the same second can straddle a page boundary and " +
        "be skipped or duplicated. Adding `id` as a tiebreaker makes the key unique and the paging deterministic.\n\n" +
        "## Rollout\n\n" +
        "1. Ship additively (the new fields don't break existing clients).\n" +
        "2. Update the OpenAPI schema and regenerate client types.\n" +
        "3. Frontend switches the dashboard to follow `nextCursor`.\n\n" +
        "## Open questions\n\n" +
        "1. Do we need an offset fallback for the admin export? *(leaning no — use a streaming export instead)*\n" +
        "2. Cap `limit` at 200? *(yes for now)*\n\n" +
        "> Note: the same keyset technique applies to `/invoices` and `/events` once this lands.\n";

    private readonly string _coral;
    private readonly string _green;
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
