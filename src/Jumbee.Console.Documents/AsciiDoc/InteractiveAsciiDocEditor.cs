namespace Jumbee.Console.Documents;

/// <summary>
/// A live, split-pane AsciiDoc editor for the terminal: the left pane is a <see cref="CodeEditor"/> with AsciiDoc
/// syntax highlighting (see <see cref="AsciiDocLanguage"/>); the right pane is an <see cref="AsciiDocViewer"/> that
/// re-renders the document — headings, formatting, lists, admonitions, tables and blocks — as you type.
/// </summary>
/// <remarks>
/// A draggable
/// <see cref="SplitPanel"/> divider sits between them. See <see cref="InteractiveSourceEditor"/> for the (off-UI-thread,
/// coalesced) sync model.
/// </remarks>
public class InteractiveAsciiDocEditor : InteractiveSourceEditor
{
    #region Constructors
    /// <param name="asciiDoc">Initial document source (both panes start in sync).</param>
    /// <param name="orientation">Side-by-side (<see cref="SplitOrientation.Horizontal"/>, editor left) or stacked
    /// (<see cref="SplitOrientation.Vertical"/>, editor on top).</param>
    /// <param name="splitPosition">The editor pane's initial extent in cells. Adjustable at runtime via the divider.</param>
    public InteractiveAsciiDocEditor(string asciiDoc = "", SplitOrientation orientation = SplitOrientation.Horizontal,
        int splitPosition = 48)
        : base(new CodeEditor(AsciiDocLanguage.Instance) { Text = asciiDoc ?? "" }, new AsciiDocViewer(asciiDoc ?? ""),
            " AsciiDoc ", " Preview ", asciiDoc ?? "", orientation, splitPosition) { }
    #endregion

    #region Properties
    /// <summary>The preview pane rendering the live AsciiDoc.</summary>
    public AsciiDocViewer Preview => (AsciiDocViewer)PreviewControl;

    /// <summary>Render styles (heading / code / admonition colours) for the preview pane.</summary>
    public AsciiDocStyles? PreviewStyles
    {
        get => Preview.Styles;
        set => Preview.Styles = value;
    }
    #endregion

    #region Methods
    protected override void ApplyPreviewText(string text) => Preview.AsciiDoc = text;

    // GetHelpInfo overrides Control's protected internal member from another assembly, so it must be plain protected.
    protected override HelpInfo? GetHelpInfo() => new HelpInfo("AsciiDoc Editor", "Interactive AsciiDoc Editor",
        "Edit AsciiDoc on the left; a live preview renders on the right.")
        .WithKey("Type", "Edit the AsciiDoc source")
        .WithKey("Ctrl+← / Ctrl+→", "Move focus between the panes")
        .WithKey("Drag divider", "Resize the panes")
        .WithKey("↑ / ↓, PgUp / PgDn", "Scroll the focused pane");
    #endregion
}
