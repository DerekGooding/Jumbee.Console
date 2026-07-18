namespace Jumbee.Console.Documents;

using Jumbee.Console.Documents.Mermaid;

/// <summary>
/// A live, split-pane Mermaid editor for the terminal: the left pane is a <see cref="CodeEditor"/> with Mermaid syntax
/// highlighting (see <see cref="MermaidLanguage"/>); the right pane is a <see cref="MermaidViewer"/> that re-renders
/// the diagram as you type.
/// </summary>
/// <remarks>
/// A draggable <see cref="SplitPanel"/> divider sits between them (drag it, or focus it and
/// press the arrows). See <see cref="InteractiveSourceEditor"/> for the (off-UI-thread, coalesced) sync model.
/// </remarks>
public class InteractiveMermaidEditor : InteractiveSourceEditor
{
    #region Constructors
    /// <param name="mermaid">Initial diagram source (both panes start in sync).</param>
    /// <param name="orientation">Side-by-side (<see cref="SplitOrientation.Horizontal"/>, editor left) or stacked
    /// (<see cref="SplitOrientation.Vertical"/>, editor on top).</param>
    /// <param name="splitPosition">The editor pane's initial extent in cells. Adjustable at runtime via the divider.</param>
    public InteractiveMermaidEditor(string mermaid = "", SplitOrientation orientation = SplitOrientation.Horizontal,
        int splitPosition = 40)
        : base(new CodeEditor(MermaidLanguage.Instance) { Text = mermaid ?? "" }, new MermaidViewer(mermaid ?? ""),
            " Mermaid ", " Diagram ", mermaid ?? "", orientation, splitPosition) { }
    #endregion

    #region Properties
    /// <summary>The preview pane rendering the live diagram.</summary>
    public MermaidViewer Preview => (MermaidViewer)PreviewControl;

    /// <summary>Colours / scale for the rendered diagram.</summary>
    public MermaidStyles DiagramStyles
    {
        get => Preview.Styles ?? MermaidStyles.Default;
        set => Preview.Styles = value;
    }
    #endregion

    #region Methods
    protected override void ApplyPreviewText(string text) => Preview.Mermaid = text;

    // GetHelpInfo overrides Control's protected internal member from another assembly, so it must be plain protected.
    protected override HelpInfo? GetHelpInfo() => new HelpInfo("Mermaid Editor", "Interactive Mermaid Editor",
        "Edit Mermaid on the left; the diagram (flowchart / state) renders live on the right.")
        .WithKey("Type", "Edit the Mermaid source")
        .WithKey("Ctrl+← / Ctrl+→", "Move focus between the panes")
        .WithKey("Drag divider", "Resize the panes")
        .WithKey("↑ / ↓, ← / →", "Scroll / pan the diagram");
    #endregion
}
