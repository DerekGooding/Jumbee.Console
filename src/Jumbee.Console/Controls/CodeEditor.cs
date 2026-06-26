namespace Jumbee.Console;

/// <summary>
/// A composite control pairing a <see cref="TextEditor"/> with a <see cref="LineNumberGutter"/> docked to its
/// left. The gutter is kept in sync with the editor (line count + active line) by listening to the editor's
/// <see cref="TextEditor.Changed"/> event — the canonical "child controls react to each other" pattern that
/// <see cref="CompositeControl"/> exists to support.
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
            RowsProvider = () => (_editor.VisualLineNumbers(), _editor.CaretVisualRow),
        };

        // Inter-child wiring: the gutter widens with the line count and repaints as the editor changes.
        _editor.Changed += (_, _) => SyncGutter();

        SetContent(new DockPanel(DockedControlPlacement.Left, _gutter, _editor));
        SyncGutter();
    }
    #endregion

    #region Properties
    /// <summary>The wrapped text editor (focus this to type; e.g. <c>UI.SetFocus(codeEditor.Editor)</c>).</summary>
    public TextEditor Editor => _editor;

    /// <summary>The line-number gutter.</summary>
    public LineNumberGutter Gutter => _gutter;

    /// <summary>The editor's text. Setting it refreshes the gutter.</summary>
    public string Text
    {
        get => _editor.Text;
        set => _editor.Text = value;   // raises Changed -> SyncGutter
    }
    #endregion

    #region Methods
    private void SyncGutter()
    {
        // Width follows the logical line count; the row labels + active row come from RowsProvider at render time.
        _gutter.LineCount = _editor.LineCount;
    }
    #endregion

    #region Fields
    private readonly TextEditor _editor;
    private readonly LineNumberGutter _gutter;
    #endregion
}
