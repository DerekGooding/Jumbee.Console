namespace Jumbee.Console;

using System;

using NTokenizers.Extensions.Spectre.Console.Styles;

/// <summary>
/// A live, split-pane Markdown editor for the terminal — the TUI equivalent of a web Markdown editor. The left pane
/// is a <see cref="CodeEditor"/> with Markdown syntax highlighting; the right pane is a <see cref="MarkdownViewer"/>
/// that re-renders the document — headings, tables and syntax-highlighted code — as you type. A draggable
/// <see cref="SplitPanel"/> divider sits between them (drag it, or focus it and press the arrows; Shift = bigger steps).
/// <para>
/// The preview render is comparatively slow, so it never runs on the UI thread: edits are coalesced to at most one
/// update per frame and the <see cref="MarkdownViewer"/> renders on a background thread, discarding any render
/// superseded by a newer edit. A half-typed table therefore reflows harmlessly on the next completed render rather
/// than blocking input.
/// </para>
/// <para>
/// To extend the preview (e.g. rendering embedded diagrams), subclass and pass a richer <see cref="MarkdownViewer"/>
/// to the <see cref="InteractiveMarkdownEditor(MarkdownViewer, string, SplitOrientation, int)">protected constructor</see>.
/// </para>
/// </summary>
public class InteractiveMarkdownEditor : CompositeControl
{
    #region Constructors
    /// <param name="markdown">Initial document text (both panes start in sync).</param>
    /// <param name="orientation">Side-by-side (<see cref="SplitOrientation.Horizontal"/>, editor left) or stacked
    /// (<see cref="SplitOrientation.Vertical"/>, editor on top).</param>
    /// <param name="splitPosition">The editor pane's initial extent in cells (width when horizontal, height when
    /// vertical). Adjustable at runtime via the divider.</param>
    public InteractiveMarkdownEditor(string markdown = "", SplitOrientation orientation = SplitOrientation.Horizontal,
        int splitPosition = 48)
        : this(new MarkdownViewer(markdown ?? ""), markdown ?? "", orientation, splitPosition) { }

    /// <summary>For subclasses that supply a richer preview (e.g. a mermaid-aware viewer). The
    /// <paramref name="preview"/> is framed and wired to the editor by the base; it must already hold
    /// <paramref name="markdown"/> so both panes start in sync.</summary>
    protected InteractiveMarkdownEditor(MarkdownViewer preview, string markdown, SplitOrientation orientation,
        int splitPosition)
    {
        _lastSynced = markdown ?? "";

        _editor = new CodeEditor(Language.Markdown) { Text = _lastSynced };
        _editor.WithFrame(title: " Markdown ");

        _preview = preview;
        _preview.WithFrame(title: " Preview ");

        _split = new SplitPanel(orientation, _editor, _preview, splitPosition);

        // Live preview: an edit schedules a single coalesced sync (see ScheduleSync). The editor raises Changed on
        // caret moves too, so Sync compares against the last-synced text and skips navigation-only events.
        _editor.Editor.Changed += (_, _) => ScheduleSync();

        SetContent(_split);
    }
    #endregion

    #region Events
    /// <summary>Raised (on the UI thread, coalesced per frame) after the document text actually changes — not for
    /// caret-only movement. Carries the new text.</summary>
    public event Action<string>? TextChanged;
    #endregion

    #region Properties
    /// <summary>The editor pane (focus <c>Editor.Editor</c> to type; wrapped in its own titled frame).</summary>
    public CodeEditor Editor => _editor;

    /// <summary>The preview pane rendering the live Markdown.</summary>
    public MarkdownViewer Preview => _preview;

    /// <summary>The split container hosting the two panes (for resizing, theming the divider, or changing minimums).</summary>
    public SplitPanel Split => _split;

    /// <summary>The document text. Setting it loads the editor (caret at the top) and refreshes the preview.</summary>
    public string Text
    {
        get => _editor.Text;
        set => UI.Invoke(() => { _editor.Text = value; });   // raises Changed -> ScheduleSync -> preview
    }

    /// <summary>Render styles (heading / code / table colours) for the preview pane.</summary>
    public MarkdownStyles? PreviewStyles
    {
        get => _preview.Styles;
        set => _preview.Styles = value;
    }
    #endregion

    #region Methods
    // Coalesce a burst of edits within one frame into a single preview update: mark a sync pending and post one apply
    // to the next dispatcher drain. Headless (no running loop) syncs inline so a following render/read reflects the
    // edit immediately — which is what tests rely on.
    private void ScheduleSync()
    {
        if (!UI.IsRunning) { Sync(); return; }
        if (_syncQueued) return;
        _syncQueued = true;
        UI.Post(() => { _syncQueued = false; Sync(); });
    }

    private void Sync()
    {
        var text = _editor.Text;
        if (text == _lastSynced) return;   // caret-only Changed (navigation) — no text change, no re-render
        _lastSynced = text;
        _preview.Markdown = text;          // de-dupes, renders off-thread, supersedes stale renders
        TextChanged?.Invoke(text);
    }

    // Keyboard focus lands in the editor pane by default (SetContent skips nested composites, so it can't be picked
    // up as the automatic first-focusable).
    protected override Control? FocusChild => _editor;

    // Both panes scroll inside their own frames, so this editor fills a surrounding frame's viewport rather than
    // ballooning to content height (which would make that outer frame a second, conflicting scroller).
    protected internal override bool FillsFrameViewport => true;

    protected internal override HelpInfo? GetHelpInfo() => new HelpInfo("Markdown Editor", "Interactive Markdown Editor",
        "Edit Markdown on the left; a live preview (tables and code) renders on the right.")
        .WithKey("Type", "Edit the Markdown source")
        .WithKey("Ctrl+← / Ctrl+→", "Move focus between the panes")
        .WithKey("Drag divider", "Resize the panes")
        .WithKey("↑ / ↓, PgUp / PgDn", "Scroll the focused pane");
    #endregion

    #region Fields
    private readonly CodeEditor _editor;
    private readonly MarkdownViewer _preview;
    private readonly SplitPanel _split;
    private string _lastSynced;
    private bool _syncQueued;   // a coalesced preview sync is posted for the next frame (see ScheduleSync)
    #endregion
}
