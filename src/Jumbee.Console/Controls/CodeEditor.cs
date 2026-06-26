namespace Jumbee.Console;

using System;

/// <summary>
/// A composite control pairing a <see cref="TextEditor"/> with a <see cref="LineNumberGutter"/> docked to its
/// left. The gutter is kept in sync with the editor (line count + active line) by listening to the editor's
/// <see cref="TextEditor.Changed"/> event — the canonical "child controls react to each other" pattern that
/// <see cref="CompositeControl"/> exists to support.
/// <para>
/// The composite sizes to its content (the editor's wrapped row count), so wrapping it in a <see cref="ControlFrame"/>
/// (e.g. <c>codeEditor.WithFrame()</c>) scrolls the gutter and text together with an accurate scrollbar; the
/// editor's caret is kept in view by <see cref="AutoScroll"/> driving that frame.
/// </para>
/// </summary>
public class CodeEditor : CompositeControl
{
    #region Constructors
    public CodeEditor(Language language = Language.None)
    {
        _editor = new TextEditor(language);
        _gutter = new LineNumberGutter
        {
            // Pulled each render: wrap-aware labels (0 = a soft-wrapped continuation row) + the caret's visual row.
            // The gutter is content-tall like the editor, so the surrounding frame scrolls them together aligned.
            RowsProvider = () => (_editor.VisualLineNumbers(), _editor.CaretVisualRow),
        };

        // Inter-child wiring: the gutter follows the editor, and the composite re-measures + scrolls to the caret.
        _editor.Changed += (_, _) => OnEditorChanged();

        // A wheel notch over the editor scrolls OUR frame — the same scroll target AutoScroll drives. The editor
        // isn't framed itself (its own OnMouseWheel is a no-op), so we bubble its MouseWheeled event up to the
        // composite. This is the "composite owns its children and wires their events itself" pattern.
        _editor.MouseWheeled += (_, delta) => Frame?.Scroll(delta);

        SetContent(new DockPanel(DockedControlPlacement.Left, _gutter, _editor));
        SyncGutter();
    }
    #endregion

    #region Properties
    /// <summary>The wrapped text editor (focus this to type; e.g. <c>UI.SetFocus(codeEditor.Editor)</c>).</summary>
    public TextEditor Editor => _editor;

    /// <summary>The line-number gutter.</summary>
    public LineNumberGutter Gutter => _gutter;

    /// <summary>The editor's text. Setting it loads the document with the caret at the start (top of the file).</summary>
    public string Text
    {
        get => _editor.Text;
        set
        {
            _editor.Text = value;       // raises Changed -> OnEditorChanged
            _editor.CaretIndex = 0;     // open at the top, like a file editor, not at the end of the text
        }
    }
    #endregion

    #region Methods
    // Our content height is the editor's wrapped row count at the editor's width (our width minus the gutter), so a
    // surrounding frame sizes us to content and its scrollbar/scroll-range are accurate.
    protected override int MeasureHeight(int width) =>
        Math.Max(1, _editor.VisualRowCount(Math.Max(1, width - _gutter.Width)));

    private void OnEditorChanged()
    {
        SyncGutter();
        Initialize();   // re-measure our content height (the row count may have changed) for the frame's scroll range
        AutoScroll();   // keep the caret in view by scrolling our frame
    }

    private void SyncGutter()
    {
        // Width follows the logical line count; the row labels/active row come from RowsProvider at render time.
        _gutter.LineCount = _editor.LineCount;
        _gutter.Refresh();
    }

    // The editor itself isn't framed, so scroll OUR ControlFrame to keep the editor's caret row within the viewport.
    private void AutoScroll()
    {
        if (Frame is null) return;
        var caretRow = _editor.CaretVisualRow;
        var top = Frame.Top;
        var viewport = Frame.ViewportSize.Height;
        if (viewport <= 0) return;

        if (caretRow < top) Frame.Top = caretRow;
        else if (caretRow >= top + viewport) Frame.Top = caretRow - viewport + 1;
    }
    #endregion

    #region Fields
    private readonly TextEditor _editor;
    private readonly LineNumberGutter _gutter;
    #endregion
}
