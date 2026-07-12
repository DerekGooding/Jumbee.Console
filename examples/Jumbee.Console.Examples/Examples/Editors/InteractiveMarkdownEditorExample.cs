namespace Jumbee.Console.Examples;

using System;
using System.Collections.Generic;

/// <summary>
/// A live split-pane Markdown editor with a "Layout" dropdown (a <see cref="Select"/>) that snaps the divider to
/// preset editor/preview ratios; edits render live on the right. Drag the divider too, or focus it and use the arrows.
/// </summary>
public sealed class InteractiveMarkdownEditorExample : CompositeControl, IExample
{
    public InteractiveMarkdownEditorExample()
    {
        editor = new InteractiveMarkdownEditor(SourceLoader.Read("interactive-md.md"));
        // The editor fills its frame's viewport (FillsFrameViewport), so give it a borderless frame here — otherwise,
        // as a bare fill control its sizing collapses and the panes don't render.
        editor.WithFrame(borderStyle: BorderStyle.None);

        // A full-width dropdown of split presets, docked as a one-row toolbar (Select is intrinsically 1 tall, so the
        // editor fills the rest). Its options open into the ambient UI.Overlay set up by UI.Start — no wiring needed.
        // Picking a preset snaps the divider (SplitPanel.SplitPosition is a live setter); the placeholder is the caption.
        layout = new Select("Editor 30%", "Even 50%", "Editor 70%") { Placeholder = "Layout" };
        layout.SelectionChanged += (_, _) => ApplyPreset();

        SetContent(new DockPanel(DockedControlPlacement.Top, layout, editor));
    }

    // Snap the divider to the picked ratio of the split's current width (the setter clamps to the pane minimums).
    // A no-op until the split has been laid out (width > 0), i.e. only fires for a real user selection.
    private void ApplyPreset()
    {
        var total = editor.Split.Size.Width;
        if (total <= 0) return;
        var fraction = layout.SelectedIndex switch { 0 => 0.30, 1 => 0.50, _ => 0.70 };
        editor.Split.SplitPosition = (int)Math.Round(total * fraction);
    }

    // Keyboard focus lands in the editor, not the toolbar dropdown.
    protected override Control? FocusChild => editor;

    // Fill the host pane (both editor panes scroll inside their own frames); don't balloon to content height.
    protected override bool FillsFrameViewport => true;

    #region Fields
    private readonly InteractiveMarkdownEditor editor;
    private readonly Select layout;
    #endregion

    #region IExample
    string IExample.Category => "Editors and Viewers";
    string IExample.Title => "Interactive Markdown";
    string IExample.Description =>
        "Edit Markdown live; the Layout dropdown snaps the split to preset editor/preview ratios.";
    IReadOnlyList<string> IExample.SourceFiles =>
        ["InteractiveMarkdownEditorExample.cs", "InteractiveMarkdownEditor.cs", "interactive-md.md"];
    #endregion
}
