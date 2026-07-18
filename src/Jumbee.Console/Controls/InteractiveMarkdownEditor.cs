namespace Jumbee.Console;

using NTokenizers.Extensions.Spectre.Console.Styles;

/// <summary>
/// A live, split-pane Markdown editor for the terminal — the TUI equivalent of a web Markdown editor. The left pane
/// is a <see cref="CodeEditor"/> with Markdown syntax highlighting; the right pane is a <see cref="MarkdownViewer"/>
/// that re-renders the document — headings, tables and syntax-highlighted code — as you type (see
/// <see cref="InteractiveSourceEditor"/> for the sync model).
/// </summary>
/// <remarks>
/// To extend the preview (e.g. rendering embedded diagrams), subclass and pass a richer <see cref="MarkdownViewer"/>
/// to the <see cref="InteractiveMarkdownEditor(MarkdownViewer, string, SplitOrientation, int)">protected constructor</see>.
/// </remarks>
public class InteractiveMarkdownEditor : InteractiveSourceEditor
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
    /// <paramref name="preview"/> must already hold <paramref name="markdown"/> so both panes start in sync.</summary>
    protected InteractiveMarkdownEditor(MarkdownViewer preview, string markdown, SplitOrientation orientation,
        int splitPosition)
        : base(new CodeEditor(Language.Markdown) { Text = markdown ?? "" }, preview, " Markdown ", " Preview ",
            markdown ?? "", orientation, splitPosition) { }
    #endregion

    #region Properties
    /// <summary>The preview pane rendering the live Markdown.</summary>
    public MarkdownViewer Preview => (MarkdownViewer)PreviewControl;

    /// <summary>Render styles (heading / code / table colours) for the preview pane.</summary>
    public MarkdownStyles? PreviewStyles
    {
        get => Preview.Styles;
        set => Preview.Styles = value;
    }
    #endregion

    #region Methods
    protected override void ApplyPreviewText(string text) => Preview.Markdown = text;

    protected internal override HelpInfo? GetHelpInfo() => new HelpInfo("Markdown Editor", "Interactive Markdown Editor",
        "Edit Markdown on the left; a live preview (tables and code) renders on the right.")
        .WithKey("Type", "Edit the Markdown source")
        .WithKey("Ctrl+← / Ctrl+→", "Move focus between the panes")
        .WithKey("Drag divider", "Resize the panes")
        .WithKey("↑ / ↓, PgUp / PgDn", "Scroll the focused pane");
    #endregion
}
