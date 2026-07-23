using ConsoleGUI.Input;
using ConsoleGUI.Space;
using Spectre.Console;

namespace Jumbee.Console;

/// <summary>A single-line text input that shows a prompt label and edits an inline entry with a terminal cursor.</summary>
/// <remarks>Initializes a new <see cref="TextPrompt"/> with the given <paramref name="prompt"/> label; <paramref name="showCursor"/> and <paramref name="blinkCursor"/> control the terminal cursor.</remarks>
public class TextPrompt(string prompt, bool showCursor = true, bool blinkCursor = false) : Prompt()
{
    #region Events

    /// <summary>Raised when the entry is committed (Enter), carrying the current input text.</summary>
    public event EventHandler<string>? Committed;

    #endregion Events

    #region Properties

    /// <summary><see langword="true"/>: the prompt handles keyboard input to edit its entry.</summary>
    public override bool HandlesInput => true;

    /// <summary>The prompt label shown before the entry (a trailing space is appended).</summary>
    public string Prompt
    {
        get;
        set
        {
            field = string.IsNullOrEmpty(value) ? "" : value + " ";
            Invalidate();
        }
    } = prompt;

    /// <summary>When <see langword="true"/>, the terminal cursor is shown at the caret while focused.</summary>
    public bool ShowCursor
    {
        get;
        set => SetAtomicProperty(ref field, value);
    } = showCursor;

    /// <summary>When <see langword="true"/>, the shown cursor blinks (DECSCUSR blinking block).</summary>
    public bool BlinkCursor
    {
        get;
        set => SetAtomicProperty(ref field, value);
    } = blinkCursor;

    /// <summary>The caret's index within the input text.</summary>
    public int CaretPosition
    {
        get => _caretPosition;
        set
        {
            _caretPosition = value;
            RenderCursor();
        }
    }

    /// <summary>The cursor's column in the buffer.</summary>
    public int CursorX
    {
        get => ansiConsole.CursorX;
        set
        {
            var dx = value - ansiConsole.CursorX;
            ansiConsole.Cursor.MoveRight(dx);
            RenderCursor();
        }
    }

    /// <summary>The cursor's row in the buffer.</summary>
    public int CursorY
    {
        get => ansiConsole.CursorY;
        set
        {
            var dy = value - ansiConsole.CursorY;
            ansiConsole.Cursor.MoveRight(dy);
            RenderCursor();
        }
    }

    #endregion Properties

    #region Methods

    /// <inheritdoc/>
    protected override void Control_OnInitialization() => RenderPrompt();

    /// <inheritdoc/>
    protected override void Render()
    {
        if (newInput)
        {
            RenderPrompt();
            ansiConsole.Write(Markup.Escape(input));
            newInput = false;
        }
        RenderCursor();
    }

    /// <summary>Clears the buffer and draws the prompt label, recording where the entry text begins.</summary>
    protected void RenderPrompt()
    {
        ansiConsole.Clear(true);
        ansiConsole.Markup(Prompt);
        inputStart = new Position(ansiConsole.CursorX, ansiConsole.CursorY);
    }

    /// <summary>Shows the terminal cursor at the caret while focused, or hides it otherwise.</summary>
    protected void RenderCursor()
    {
        // Only the focused control may own the terminal cursor; otherwise clear it so it doesn't linger.
        if (IsValidCursorPosition && IsFocused && ShowCursor)
        {
            // Blinking is now the terminal's job (DECSCUSR), not a forced repaint.
            ansiConsole.BufferCursor.Style = BlinkCursor ? CursorStyle.BlinkingBlock : CursorStyle.SteadyBlock;
            ansiConsole.Cursor.Show(true);
        }
        else
        {
            ansiConsole.Cursor.Hide();
        }
    }

    /// <inheritdoc/>
    protected override void OnInput(InputEvent inputEvent)
    {
        switch (inputEvent.Key.Key)
        {
            case ConsoleKey.LeftArrow:
                var c = _caretPosition;
                _caretPosition = Math.Max(0, _caretPosition - 1);
                if (c != _caretPosition)
                {
                    --CursorX;
                }
                inputEvent.Handled = true;
                break;

            case ConsoleKey.RightArrow:
                c = _caretPosition;
                _caretPosition = Math.Min(input.Length, _caretPosition + 1);
                if (c != _caretPosition)
                {
                    ++CursorX;
                }
                inputEvent.Handled = true;
                break;

            case ConsoleKey.Home:
                _caretPosition = 0;
                inputEvent.Handled = true;
                break;

            case ConsoleKey.End:
                _caretPosition = input.Length;
                inputEvent.Handled = true;
                break;

            case ConsoleKey.Backspace:
                if (_caretPosition > 0)
                {
                    input = input.Remove(--_caretPosition, 1);
                    newInput = true;
                    inputEvent.Handled = true;
                }
                break;

            case ConsoleKey.Delete:
                if (_caretPosition < input.Length)
                {
                    input = input.Remove(_caretPosition--, 1);
                    newInput = true;

                    inputEvent.Handled = true;
                }
                break;

            case ConsoleKey.Enter:
                AttemptCommit();
                inputEvent.Handled = true;
                break;

            default:
                if (!char.IsControl(inputEvent.Key.KeyChar))
                {
                    input = input.Insert(_caretPosition++, inputEvent.Key.KeyChar.ToString());
                    newInput = true;
                    inputEvent.Handled = true;
                }
                break;
        }
        Invalidate();
    }

    /// <summary><see langword="true"/> when the cursor position lies within the control's bounds.</summary>
    protected bool IsValidCursorPosition => CursorX < Size.Width && CursorY < Size.Height;

    private void AttemptCommit() => Committed?.Invoke(this, input);

    #endregion Methods

    #region Fields

    private string input = string.Empty;
    private bool newInput;
    private int _caretPosition = 0;
    private Position inputStart = default;

    #endregion Fields
}