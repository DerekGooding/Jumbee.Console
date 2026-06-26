namespace Jumbee.Console;

using System;
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
            newInput = true;
            Invalidate();
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

    /// <summary>The zero-based line the caret is currently on.</summary>
    public int CaretLine => GetCursorPositionFromCaret(caretPosition).y;
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
        newInput = true;
        AutoScroll();
        Invalidate();
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
        AutoScroll();
        Invalidate();
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

    private (int x, int y) GetCursorPositionFromCaret(int caret)
    {
        int x = 0;
        int y = 0;
        for (int i = 0; i < caret && i < input.Length; i++)
        {
            char c = input[i];
            if (c == '\n')
            {
                x = 0;
                y++;
            }
            else if (c == '\r') continue;
            else
            {
                x += c.GetCellWidth();
            }
        }
        return (x, y);
    }

    private int GetCaretIndex(int targetLine, int targetX)
    {
        int line = 0;
        int currentX = 0;
        int i = 0;

        while (i < input.Length && line < targetLine)
        {
            if (input[i] == '\n') line++;
            i++;
        }

        if (line < targetLine) return input.Length;

        while (i < input.Length && input[i] != '\n')
        {
            if (input[i] == '\r')
            {
                i++;
                continue;
            }

            int w = input[i].ToString().GetCellWidth();

            if (currentX + (w / 2.0) > targetX) break;

            currentX += w;
            i++;

            if (currentX >= targetX) break;
        }

        return i;
    }

    private void MoveCaretVertically(int direction)
    {
        var (currentX, currentY) = GetCursorPositionFromCaret(caretPosition);
        var targetY = Math.Max(0, currentY + direction);
        
        caretPosition = GetCaretIndex(targetY, currentX);
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

    SpectreMarkupFormatter ccFormatter = new SpectreMarkupFormatter() ;
    SyntaxTheme ccSyntaxTheme = SyntaxTheme.CreateDefault();
    SyntaxOptions ccSyntaxOptions = new SyntaxOptions() { TabWidth = 0,   };
    #endregion
}
