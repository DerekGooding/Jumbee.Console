namespace Jumbee.Console.Examples;

using System.Collections.Generic;

/// <summary>
/// A live split-pane Markdown editor with a "Layout" dropdown (a <see cref="Select"/>) that switches the panes between
/// side-by-side and stacked; edits render live in the preview pane. Drag the divider too, or focus it and use the arrows.
/// </summary>
public sealed class InteractiveMarkdownEditorExample : CompositeControl, IExample
{
    public InteractiveMarkdownEditorExample()
    {
        editor = BuildEditor(SourceLoader.Read("interactive-md.md"), SplitOrientation.Horizontal);

        // A dropdown that picks the split orientation. Its options open into the ambient UI.Overlay set up by
        // UI.Start — no wiring needed. Pre-selected to the initial orientation before wiring, so the first real
        // change is a user's.
        layout = new Select("Side by side", "Stacked");
        layout.SelectedIndex = 0;
        layout.SelectionChanged += (_, _) =>
            SetOrientation(layout.SelectedIndex == 1 ? SplitOrientation.Vertical : SplitOrientation.Horizontal);

        // Caption + dropdown as a one-row toolbar. A HorizontalStackPanel gives its first child the full remaining
        // width (so a bare caption would eat the row and hide the dropdown), so bound each child to its own width to
        // keep them compact and left-aligned; the outer Boundary pins the whole row to one cell tall for docking.
        const string captionText = "Layout: ";
        var caption = new Boundary(new TextLabel(TextLabelOrientation.Horizontal, captionText) { Focusable = false },
            width: captionText.Length);
        var toolbar = new Boundary(new HorizontalStackPanel(caption, new Boundary(layout, width: layout.Width)), height: 1);

        // Keep a handle on the dock so a toggle can swap just its fill (the editor), leaving the toolbar in place.
        dock = new DockPanel(DockedControlPlacement.Top, toolbar, editor);
        SetContent(dock);
    }

    private static InteractiveMarkdownEditor BuildEditor(string text, SplitOrientation orientation)
    {
        var e = new InteractiveMarkdownEditor(text, orientation);
        // The editor fills its frame's viewport (FillsFrameViewport), so it needs a borderless frame — as a bare fill
        // control its sizing collapses and the panes don't render.
        e.WithFrame(borderStyle: BorderStyle.None);
        return e;
    }

    // Orientation is fixed at a SplitPanel's construction, so switching it rebuilds the editor in the new orientation
    // (carrying its text over) and swaps it into the dock's fill slot; the toolbar stays put. A no-op if unchanged.
    private void SetOrientation(SplitOrientation orientation)
    {
        if (editor.Split.Orientation == orientation) return;
        editor = BuildEditor(editor.Text, orientation);
        dock.FillControl = editor;
    }

    // Keyboard focus lands in the (current) editor, not the toolbar dropdown.
    protected override Control? FocusChild => editor;

    // Fill the host pane (both editor panes scroll inside their own frames); don't balloon to content height.
    protected override bool FillsFrameViewport => true;

    #region Fields
    private readonly DockPanel dock;
    private readonly Select layout;
    private InteractiveMarkdownEditor editor;
    #endregion

    #region IExample
    string IExample.Category => "Editors and Viewers";
    string IExample.Title => "Interactive Markdown";
    string IExample.Description =>
        "Edit Markdown live; the Layout dropdown switches the panes between side-by-side and stacked.";
    IReadOnlyList<string> IExample.SourceFiles =>
        ["InteractiveMarkdownEditorExample.cs", "InteractiveMarkdownEditor.cs", "interactive-md.md"];
    #endregion
}
