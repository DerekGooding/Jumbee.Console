namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using RazorConsole.Core;

using ConsoleGUI.Input;
using NTokenizers.Extensions.Spectre.Console;
using Spectre.Console;
using RazorConsole.Core.Rendering.Syntax;
using ColorCode;

/// <summary>
/// A source language for syntax highlighting, shared by the text/code controls (e.g. <see cref="TextEditor"/>,
/// <see cref="CodeEditor"/>). <see cref="None"/> renders plain, unhighlighted text.
/// </summary>
public enum Language
{
    None,
    Markdown,
    Json,
    Html,
    Css,
    CSharp,
    Sql,
    TypeScript,
    Xml,
    Yaml,
    Toml,
    C,
    Cpp,
    Go,
    Java,
    Kotlin,
    Python,
    Rust,
    Swift
}

/// <summary>
/// A text editor control with syntax highlighting for supported languages.
/// </summary>
public class TextEditor : Control
{
    #region Constructors
    public TextEditor(Language language = Language.None, bool showCursor = true, bool blinkCursor = false) : base()
    {
        this._language = language;
        this._showCursor = showCursor;
        this._blinkCursor = blinkCursor;
        ansiConsole.wrap = true;   // character-level soft wrap at the buffer's right edge
    }
    #endregion

    #region Properties
    public override bool HandlesInput => true;

    public bool ShowCursor
    {
        get => _showCursor;
        set => SetAtomicProperty(ref _showCursor, value);
    }
    public bool BlinkCursor
    {
        get => _blinkCursor;
        set => SetAtomicProperty(ref _blinkCursor, value);
    }
    
    public int CursorX
    {
        get => ansiConsole.CursorX;
        set
        {
            var x = Math.Clamp(value, 0, ActualWidth);
            var dx = value - ansiConsole.CursorX;
            ansiConsole.Cursor.MoveRight(dx);
            RenderCursor();
        }
    }

    public int CursorY
    {
        get => ansiConsole.CursorY;
        set
        {
            var y = Math.Clamp(value, 0, ActualHeight);
            var dy = y - ansiConsole.CursorY;
            ansiConsole.Cursor.MoveDown(dy);
            RenderCursor();
        }
    }

    /// <summary>The full text content. Setting it replaces the buffer, moves the caret to the end, and raises
    /// <see cref="Changed"/>.</summary>
    public string Text
    {
        get => input;
        set
        {
            input = NormalizeNewlines(value);
            caretPosition = input.Length;
            _desiredColumn = -1;
            newInput = true;
            Initialize();   // new content -> re-measure the row count (for a framed editor's scroll range)
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>The number of lines (newline count + 1).</summary>
    public int LineCount
    {
        get
        {
            var count = 1;
            foreach (var c in input) if (c == '\n') count++;
            return count;
        }
    }

    /// <summary>The zero-based logical line the caret is on (newline count before it).</summary>
    public int CaretLine
    {
        get
        {
            int line = 0;
            for (int i = 0; i < caretPosition && i < input.Length; i++)
                if (input[i] == '\n') line++;
            return line;
        }
    }

    /// <summary>The zero-based visual (wrapped) row the caret is on.</summary>
    public int CaretVisualRow => GetCursorPositionFromCaret(caretPosition).y;

    /// <summary>
    /// For each visual (wrapped) row, the 1-based logical line number when the row starts a logical line, or 0
    /// for a wrapped continuation row. A line-number gutter uses this to stay aligned with soft-wrapped text.
    /// </summary>
    public IReadOnlyList<int> VisualLineNumbers()
    {
        var rows = BuildVisualRows();
        var labels = new List<int>(rows.Count);
        int logicalLine = 1;
        for (int r = 0; r < rows.Count; r++)
        {
            var start = rows[r].start;
            bool firstOfLine = start == 0 || input[start - 1] == '\n';
            if (r > 0 && firstOfLine) logicalLine++;
            labels.Add(firstOfLine ? logicalLine : 0);
        }
        return labels;
    }
    #endregion

    #region Events
    /// <summary>Raised after the text or caret position changes (typing, paste, delete, navigation, or setting
    /// <see cref="Text"/>). Composites use it to keep adornments — e.g. a line-number gutter — in sync.</summary>
    public event EventHandler? Changed;
    #endregion

    #region Methods   
    protected override void Control_OnInitialization()
    {
        if (!string.IsNullOrEmpty(input))
        {
            ansiConsole.Clear(true);
            WriteText(_language, input);
        }
        // Draw the cursor as part of initialization too, so a focused editor shows it on the very first paint
        // (Render is otherwise skipped once Validate clears the paint request).
        RenderCursor();
        Validate();
    }

    protected override void Render()
    {
        if (newInput)
        {
            ansiConsole.Clear(true);
            WriteText(_language, input);
            newInput = false;
        }
        RenderCursor();
    }
    
    
    // Insert a whole paste at the caret in one shot (no per-key re-interpretation; newlines kept verbatim).
    public override void OnPaste(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        text = NormalizeNewlines(text);
        input = input.Insert(caretPosition, text);
        caretPosition += text.Length;
        _desiredColumn = -1;
        newInput = true;
        AutoScroll();
        Initialize();   // a paste changes the row count -> re-measure (for a framed editor's scroll range)
        Changed?.Invoke(this, EventArgs.Empty);
    }

    // Editor state uses '\n' as the only line separator. Collapse CRLF/CR (e.g. from a Windows clipboard paste)
    // so a stray '\r' can't sit invisibly in the buffer: both the renderer and the caret-position math skip '\r',
    // which makes it a zero-width character the caret can land on — pressing an arrow key then appears to do
    // nothing and the drawn cursor falls out of sync with the logical caret.
    private static string NormalizeNewlines(string? text) =>
        string.IsNullOrEmpty(text) ? string.Empty : text.Replace("\r\n", "\n").Replace('\r', '\n');

    protected override void OnInput(InputEvent inputEvent)
    {
        var vertical = inputEvent.Key.Key is ConsoleKey.UpArrow or ConsoleKey.DownArrow
            or ConsoleKey.PageUp or ConsoleKey.PageDown;
        switch (inputEvent.Key.Key)
        {
            case ConsoleKey.LeftArrow:
                if (caretPosition > 0)
                {
                    caretPosition--;                    
                }
                inputEvent.Handled = true;
                break;
            case ConsoleKey.RightArrow:
                if (caretPosition < input.Length)
                {
                    caretPosition++;                    
                }
                inputEvent.Handled = true;
                break;
            case ConsoleKey.UpArrow:
                MoveCaretVertically(-1);
                inputEvent.Handled = true;
                break;
            case ConsoleKey.DownArrow:
                MoveCaretVertically(1);
                inputEvent.Handled = true;
                break;
            case ConsoleKey.PageUp:
                MoveCaretVertically(-(Frame?.ViewportSize.Height ?? 10));
                inputEvent.Handled = true;
                break;
            case ConsoleKey.PageDown:
                MoveCaretVertically(Frame?.ViewportSize.Height ?? 10);
                inputEvent.Handled = true;
                break;
            case ConsoleKey.Home:
                MoveCaretHome();                
                inputEvent.Handled = true;
                break;
            case ConsoleKey.End:
                MoveCaretEnd();                
                inputEvent.Handled = true;
                break;
            case ConsoleKey.Backspace:
                if (caretPosition > 0)
                {
                    input = input.Remove(--caretPosition, 1);
                    newInput = true;                    
                    inputEvent.Handled = true;
                }
                break;
            case ConsoleKey.Delete:
                if (caretPosition < input.Length)
                {
                    input = input.Remove(caretPosition, 1);
                    newInput = true;                    
                    inputEvent.Handled = true;
                }
                break;
            case ConsoleKey.Enter:
                input = input.Insert(caretPosition++, "\n");
                newInput = true;                
                inputEvent.Handled = true;
                break;
            default:
                if (!char.IsControl(inputEvent.Key.KeyChar))
                {
                    input = input.Insert(caretPosition++, inputEvent.Key.KeyChar.ToString());
                    newInput = true;
                    inputEvent.Handled = true;
                }
                break;
        }
        if (!vertical) _desiredColumn = -1;   // a horizontal move or an edit drops the remembered column
        AutoScroll();
        // An edit (newInput) can change the wrapped row count, so re-lay-out for a framed editor's scroll range;
        // navigation only needs a repaint.
        if (newInput) Initialize(); else Invalidate();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    protected void RenderCursor()
    {
        var pos = GetCursorPositionFromCaret(caretPosition);
        ansiConsole.SetCursorPosition(pos.x, pos.y);
        if (IsFocused && _showCursor)
        {
            ansiConsole.BufferCursor.Style = _blinkCursor ? CursorStyle.BlinkingBlock : CursorStyle.SteadyBlock;
            ansiConsole.Cursor.Show();
        }
        else
        {
            ansiConsole.Cursor.Hide();
        }
    }

    /// <summary>The column the text wraps at — the rendered buffer width.</summary>
    private int WrapWidth => Math.Max(1, ActualWidth);

    // Report the wrapped row count as the content height so a surrounding ControlFrame sizes the editor to its
    // content and scrolls accurately (measured at the layout width, which may differ from the current ActualWidth).
    protected override int MeasureHeight(int width) => BuildVisualRows(width).Count;

    /// <summary>The number of visual (wrapped) rows the text occupies at the given width — the editor's content
    /// height. A composite (e.g. <see cref="CodeEditor"/>) uses it to size/scroll itself around the editor.</summary>
    public int VisualRowCount(int width) => BuildVisualRows(Math.Max(1, width)).Count;

    // Splits the document into visual rows (as the renderer does) using the same character wrap. Each row is a
    // half-open [start, end) range of indices into `input`; a '\n' ends a row and is not part of it. The wrap
    // width defaults to the current buffer width, but can be supplied (e.g. while measuring at a new layout width).
    private List<(int start, int end)> BuildVisualRows(int? wrapWidth = null)
    {
        var rows = new List<(int, int)>();
        int width = Math.Max(1, wrapWidth ?? WrapWidth);
        int n = input.Length;
        int rowStart = 0, col = 0, i = 0;
        while (i < n)
        {
            char c = input[i];
            if (c == '\n')
            {
                rows.Add((rowStart, i));
                i++;
                rowStart = i;
                col = 0;
            }
            else
            {
                int w = c.GetCellWidth();
                if (w <= 0) { i++; continue; }                 // zero-width: renderer skips it too
                if (col > 0 && col + w > width)                // glyph won't fit -> wrap before it
                {
                    rows.Add((rowStart, i));
                    rowStart = i;
                    col = 0;
                }
                col += w;
                i++;
            }
        }
        rows.Add((rowStart, n));
        return rows;
    }

    // Visual column of an index within its row (sum of cell widths from the row start).
    private int ColumnOf(int start, int caret)
    {
        int col = 0;
        for (int i = start; i < caret && i < input.Length; i++)
        {
            char c = input[i];
            if (c == '\n' || c == '\r') continue;
            int w = c.GetCellWidth();
            if (w > 0) col += w;
        }
        return col;
    }

    private (int x, int y) GetCursorPositionFromCaret(int caret)
    {
        caret = Math.Clamp(caret, 0, input.Length);
        var rows = BuildVisualRows();
        for (int r = 0; r < rows.Count; r++)
        {
            var (start, end) = rows[r];
            // The caret is on row r when strictly inside it, or at its end only if that end is a hard break (a
            // newline or the document end). At a wrap point the caret belongs to the next row's start, so we fall
            // through to the next iteration (whose start == caret).
            bool atHardEnd = caret == end && (r == rows.Count - 1 || input[end] == '\n');
            if (caret < end || atHardEnd)
            {
                int x = ColumnOf(start, caret);
                // A caret past the last cell of a completely full row shows at the start of the next visual row.
                if (x >= WrapWidth) return (x - WrapWidth, r + 1);
                return (x, r);
            }
        }
        var last = rows[^1];
        return (ColumnOf(last.start, input.Length), rows.Count - 1);
    }

    // The index on visual row `visualRow` nearest (rounded down to) column `targetColumn`.
    private int CaretIndexAt(int visualRow, int targetColumn)
    {
        var rows = BuildVisualRows();
        visualRow = Math.Clamp(visualRow, 0, rows.Count - 1);
        var (start, end) = rows[visualRow];
        int i = start, col = 0;
        while (i < end && col < targetColumn)
        {
            int w = input[i].GetCellWidth();
            if (w <= 0) { i++; continue; }
            if (col + w > targetColumn) break;
            col += w;
            i++;
        }
        return i;
    }

    private void MoveCaretVertically(int direction)
    {
        var (currentX, currentY) = GetCursorPositionFromCaret(caretPosition);
        // Remember the column the user is aiming for so a run of Up/Down keeps it even across short lines.
        if (_desiredColumn < 0) _desiredColumn = currentX;
        caretPosition = CaretIndexAt(currentY + direction, _desiredColumn);
    }
    
    private void MoveCaretHome()
    {
        int i = caretPosition;
        while (i > 0 && input[i-1] != '\n')
        {
            i--;
        }
        caretPosition = i;
    }

    private void MoveCaretEnd()
    {
         while (caretPosition < input.Length && input[caretPosition] != '\n')
         {
             caretPosition++;
         }
    }

    private void AutoScroll()
    {
        if (Frame != null)
        {
            var (x, y) = GetCursorPositionFromCaret(caretPosition);

            int top = Frame.Top;
            int viewportHeight = Frame.ViewportSize.Height;

            if (y < top)
            {
                Frame.Top = y;
            }
            else if (y >= top + viewportHeight)
            {
                Frame.Top = y - viewportHeight + 1;
            }
        }
    }

    private void WriteText(Language language, string text)
    {
        // Render each logical line unwrapped (a large profile width) so Spectre never word-wraps; the buffer then
        // applies one deterministic character wrap at its real width (ansiConsole.wrap), which BuildVisualRows /
        // the caret math mirror exactly. The editor owns this profile and always wants the inflated render width,
        // so it stays set between calls (re-set here every render; always >= 2, never the buffer's possibly-0 width).
        ansiConsole.Profile.Width = LongestLineWidth(text) + 1;
        WriteHighlighted(language, text);
    }

    private static int LongestLineWidth(string text)
    {
        int max = 1, col = 0;
        foreach (var c in text)
        {
            if (c == '\n') { if (col > max) max = col; col = 0; }
            else if (c != '\r') col += c.GetCellWidth();
        }
        return Math.Max(max, col);
    }

    private void WriteHighlighted(Language language, string text)
    {
        switch (language)
        {
            case Language.None:
                ansiConsole.Write(text); 
                break;
            case Language.Markdown:
                ansiConsole.Markup(ccFormatter.Format(text, Languages.Markdown, ccSyntaxTheme, ccSyntaxOptions));
                break;
            case Language.CSharp:
                ansiConsole.Markup(ccFormatter.Format(text, Languages.CSharp, ccSyntaxTheme, ccSyntaxOptions));
                break;
            case Language.TypeScript:
                ansiConsole.Markup(ccFormatter.Format(text, Languages.Typescript, ccSyntaxTheme, ccSyntaxOptions));
                break;
            case Language.Sql:
                ansiConsole.Markup(ccFormatter.Format(text, Languages.Sql, ccSyntaxTheme, ccSyntaxOptions));
                break;
            case Language.Json:
                ansiConsole.WriteJson(text);
                break;
            case Language.Html:
                ansiConsole.Markup(ccFormatter.Format(text, Languages.Html, ccSyntaxTheme, ccSyntaxOptions));
                break;
            case Language.Css:
                ansiConsole.Markup(ccFormatter.Format(text, Languages.Css, ccSyntaxTheme, ccSyntaxOptions));
                break;
            case Language.Xml:
                ansiConsole.Markup(ccFormatter.Format(text, Languages.Xml, ccSyntaxTheme, ccSyntaxOptions));
                break;
            case Language.Yaml:
                ansiConsole.WriteYaml(text);
                break;
            case Language.Toml:
                ansiConsole.WriteToml(text);
                break;
            case Language.C:
                ansiConsole.WriteC(text);
                break;
            case Language.Cpp:
                ansiConsole.WriteCpp(text);
                break;
            case Language.Go:
                ansiConsole.WriteGo(text);
                break;
            case Language.Java:
                ansiConsole.WriteJava(text);
                break;
            case Language.Kotlin:
                ansiConsole.WriteKotlin(text);
                break;
            case Language.Python:
                ansiConsole.WritePython(text);
                break;
            case Language.Rust:
                ansiConsole.WriteRust(text);
                break;
            case Language.Swift:
                ansiConsole.WriteSwift(text);
                break;
        }
    }
    #endregion

    #region Fields
    private Language _language;
    private bool _showCursor;
    private bool _blinkCursor;
    private string input = string.Empty;
    private bool newInput;
    private int caretPosition = 0;
    // The visual column a vertical (Up/Down) move aims for, preserved across a run of them; -1 = recompute from
    // the caret's current column on the next vertical move. Reset by any horizontal move or edit.
    private int _desiredColumn = -1;

    SpectreMarkupFormatter ccFormatter = new SpectreMarkupFormatter() ;
    SyntaxTheme ccSyntaxTheme = SyntaxTheme.CreateDefault();
    SyntaxOptions ccSyntaxOptions = new SyntaxOptions() { TabWidth = 0,   };
    #endregion
}
