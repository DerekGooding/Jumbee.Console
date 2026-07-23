namespace Jumbee.Console.Documents;

using NTokenizers.Extensions.Spectre.Console.Styles;

/// <summary>
/// A live, split-pane Markdown editor whose preview renders embedded <c>```mermaid</c> code blocks as diagrams — the
/// interactive complement to <see cref="MarkdownExtendedViewer"/>.
/// </summary>
/// <remarks>
/// The left pane is a <see cref="CodeEditor"/> editing
/// Markdown (with the Mermaid grammar highlighting the contents of <c>```mermaid</c> fences); the right pane is a
/// <see cref="MarkdownExtendedViewer"/> that re-renders the document — prose, tables, code and box-drawn mermaid
/// diagrams — as you type. See <see cref="InteractiveSourceEditor"/> for the (off-UI-thread, coalesced) sync model.
/// </remarks>
public class InteractiveMarkdownExtendedEditor : InteractiveSourceEditor
{
    #region Constructors

    /// <param name="markdown">Initial document text (both panes start in sync).</param>
    /// <param name="orientation">Side-by-side (<see cref="SplitOrientation.Horizontal"/>, editor left) or stacked
    /// (<see cref="SplitOrientation.Vertical"/>, editor on top).</param>
    /// <param name="splitPosition">The editor pane's initial extent in cells. Adjustable at runtime via the divider.</param>
    public InteractiveMarkdownExtendedEditor(string markdown = "", SplitOrientation orientation = SplitOrientation.Horizontal,
        int splitPosition = 48)
        : base(new CodeEditor(MarkdownWithMermaidLanguage.Instance) { Text = markdown ?? "" },
            new MarkdownExtendedViewer(markdown ?? ""), " Markdown ", " Preview ", markdown ?? "", orientation, splitPosition)
    { }

    #endregion Constructors

    #region Properties

    /// <summary>The preview pane rendering the live Markdown (with embedded mermaid diagrams).</summary>
    public MarkdownExtendedViewer Preview => (MarkdownExtendedViewer)PreviewControl;

    /// <summary>Render styles (heading / code / table colours) for the preview pane.</summary>
    public MarkdownStyles? PreviewStyles
    {
        get => Preview.Styles;
        set => Preview.Styles = value;
    }

    /// <summary>Colours / scale for mermaid diagrams embedded in the preview.</summary>
    public MermaidStyles DiagramStyles
    {
        get => Preview.DiagramStyles;
        set => Preview.DiagramStyles = value;
    }

    #endregion Properties

    #region Methods

    /// <summary>Pushes the edited <paramref name="text"/> into the preview pane's <see cref="MarkdownViewer.Markdown"/>.</summary>
    protected override void ApplyPreviewText(string text) => Preview.Markdown = text;

    /// <inheritdoc/>
    // GetHelpInfo overrides Control's protected internal member from another assembly, so it must be plain protected.
    protected override HelpInfo? GetHelpInfo() => new HelpInfo("Markdown Editor", "Interactive Markdown + Mermaid Editor",
        "Edit Markdown (with embedded ```mermaid diagrams) on the left; a live preview renders on the right.")
        .WithKey("Type", "Edit the Markdown source")
        .WithKey("Ctrl+← / Ctrl+→", "Move focus between the panes")
        .WithKey("Drag divider", "Resize the panes")
        .WithKey("↑ / ↓, PgUp / PgDn", "Scroll the focused pane");

    #endregion Methods
}