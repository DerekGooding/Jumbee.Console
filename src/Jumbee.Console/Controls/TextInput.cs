
using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;
using System;

namespace Jumbee.Console;
/// <summary>
/// A single-line text entry control: caret, selection (Shift+navigation), horizontal scrolling when the text
/// exceeds the width, an optional muted placeholder shown while empty, and optional password masking.
/// </summary>
/// <remarks>
/// Submitting with Enter raises <see cref="Submitted"/>; any edit raises <see cref="Changed"/>. The native terminal
/// cursor is owned while focused (only the focused control draws it), mirroring <see cref="TextEditor"/>.
/// </remarks>
public class TextInput : Control
{
    #region Constructors

    /// <summary>Initializes a new <see cref="TextInput"/> with the given initial <paramref name="text"/> and <paramref name="placeholder"/> hint.</summary>
    public TextInput(string text = "", string placeholder = "")
    {
        _text = SingleLine(text);
        _placeholder = placeholder ?? string.Empty;
        _caret = _text.Length;
        ApplyTheme();
    }

    #endregion Constructors

    #region Properties

    /// <summary>Reports <see langword="true"/> so input routing delivers keys to the control.</summary>
    public override bool HandlesInput => true;

    /// <summary>Reports <see langword="true"/>: the text cursor indicates focus, so no default focus tint is drawn.</summary>
    protected override bool RendersOwnFocus => true;   // the text cursor shows focus

    /// <summary>The entered text (newlines stripped). Setting it moves the caret to the end and raises <see cref="Changed"/>.</summary>
    public string Text
    {
        get => _text;
        set
        {
            var v = SingleLine(value);
            if (v == _text) return;
            _text = v;
            _caret = _text.Length;
            _selAnchor = -1;
            Invalidate();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Muted hint shown while the text is empty.</summary>
    public string Placeholder
    {
        get => _placeholder;
        set => SetAtomicProperty(ref _placeholder, value ?? string.Empty);
    }

    /// <summary>When set, each character renders as this glyph (password entry); the real text is still in <see cref="Text"/>.</summary>
    public char? PasswordChar
    {
        get => _passwordChar;
        set => SetAtomicProperty(ref _passwordChar, value);
    }

    /// <summary>When <see langword="true"/>, edits are ignored (caret/selection navigation still works).</summary>
    public bool ReadOnly
    {
        get => _readOnly;
        set => SetAtomicProperty(ref _readOnly, value);
    }

    /// <summary>The caret's index into <see cref="Text"/> (0..length). Setting it clears any selection.</summary>
    public int CaretIndex
    {
        get => _caret;
        set { _caret = Math.Clamp(value, 0, _text.Length); _selAnchor = -1; Invalidate(); }
    }

    /// <summary>The selected substring, or empty when there is no selection.</summary>
    public string SelectedText => HasSelection ? _text.Substring(SelStart, SelEnd - SelStart) : string.Empty;

    /// <summary>Style of the entered text. Defaults to <see cref="IStyleTheme.Text"/>.</summary>
    public Style TextStyle { get => _textStyle; set => SetAtomicProperty(ref _textStyle, value, themeOverride: true); }

    /// <summary>Style of the placeholder hint. Defaults to <see cref="IStyleTheme.TextMuted"/>.</summary>
    public Style PlaceholderStyle { get => _placeholderStyle; set => SetAtomicProperty(ref _placeholderStyle, value, themeOverride: true); }

    /// <summary>Style of selected text. Defaults to <see cref="IStyleTheme.Selection"/>.</summary>
    public Style SelectionStyle { get => _selectionStyle; set => SetAtomicProperty(ref _selectionStyle, value, themeOverride: true); }

    #endregion Properties

    #region Events

    /// <summary>Raised whenever the text changes (typing, deletion, paste, or the <see cref="Text"/> setter).</summary>
    public event EventHandler? Changed;

    /// <summary>Raised when Enter is pressed. Read <see cref="Text"/> for the value.</summary>
    public event EventHandler? Submitted;

    #endregion Events

    #region Methods

    /// <summary>The input's fixed height of one row.</summary>
    protected override int IntrinsicHeight() => 1;   // one row tall

    /// <summary>Returns 0 so the input fills the available width.</summary>
    protected override int IntrinsicWidth() => 0;    // fill the available width

    /// <inheritdoc/>
    protected override void ApplyTheme()
    {
        if (!IsThemeOverridden(nameof(TextStyle))) _textStyle = UI.StyleTheme.Text;
        if (!IsThemeOverridden(nameof(PlaceholderStyle))) _placeholderStyle = UI.StyleTheme.TextMuted;
        if (!IsThemeOverridden(nameof(SelectionStyle))) _selectionStyle = UI.StyleTheme.Selection;
    }

    /// <summary>Renders the (scrolled) text or placeholder, highlighting any selection.</summary>
    protected override void Render()
    {
        ansiConsole.Clear(true);   // clear the buffer (and reset the cursor) before redrawing the line
        var width = ActualWidth;
        if (width <= 0) return;

        EnsureCaretVisible(width);

        if (_text.Length == 0 && _placeholder.Length > 0)
        {
            for (var i = 0; i < _placeholder.Length && i < width; i++)
                consoleBuffer.Write(new Position(i, 0), Glyph(_placeholder[i], _placeholderStyle));
        }
        else
        {
            var selStart = HasSelection ? SelStart : -1;
            var selEnd = HasSelection ? SelEnd : -1;
            for (var col = 0; col < width; col++)
            {
                var idx = _scroll + col;
                if (idx >= _text.Length) break;
                var glyph = _passwordChar ?? _text[idx];
                var selected = idx >= selStart && idx < selEnd;
                consoleBuffer.Write(new Position(col, 0), Glyph(glyph, selected ? _selectionStyle : _textStyle));
            }
        }

        RenderCursor(width);
    }

    private void RenderCursor(int width)
    {
        if (!IsFocused) return;   // only the focused control owns the caret

        // Mark the caret cell with IsCursor directly (preserving its glyph/colours), like TerminalEmulator, rather
        // than via the AnsiConsoleBuffer cursor's save/restore — that path restores a stale saved cell after the
        // text is rewritten each frame and wiped the first character.
        var caretCol = Math.Clamp(_caret - _scroll, 0, Math.Max(0, width - 1));
        var cell = consoleBuffer[caretCol, 0].Character;
        var deco = CursorEncoding.EncodeStyle(cell.Decoration ?? ConsoleGUI.Data.Decoration.None, (int)CursorStyle.SteadyBlock);
        consoleBuffer.Write(new Position(caretCol, 0),
            new Character(cell.Content ?? ' ', cell.Foreground, cell.Background, deco, isCursor: true));
    }

    // Keep the caret within the visible window by scrolling horizontally (no wrap on a single line).
    private void EnsureCaretVisible(int width)
    {
        // The whole text fits: show it from the start (don't scroll the first char off just to seat the
        // end-of-text caret in its own column — that hid the leading character after accepting a suggestion).
        if (_text.Length <= width)
        {
            _scroll = 0;
            return;
        }

        if (_caret < _scroll) _scroll = _caret;
        else if (_caret >= _scroll + width) _scroll = _caret - (width - 1);
        _scroll = Math.Clamp(_scroll, 0, Math.Max(0, _text.Length - 1));
    }

    /// <summary>
    /// Optional first look at each key, before the field's own handling. Return <see langword="true"/> to consume
    /// the key (the field ignores it).
    /// </summary>
    /// <remarks>An attached <see cref="Autocomplete"/> uses this to grab Up/Down/Enter/Esc for its suggestion popup
    /// while the field keeps focus and continues to edit on other keys.</remarks>
    public Func<InputEvent, bool>? KeyInterceptor { get; set; }

    /// <inheritdoc/>
    protected override void OnInput(InputEvent inputEvent)
    {
        if (KeyInterceptor is { } intercept && intercept(inputEvent))
        {
            inputEvent.Handled = true;
            return;
        }

        var key = inputEvent.Key;
        var shift = (key.Modifiers & ConsoleModifiers.Shift) != 0;
        var ctrl = (key.Modifiers & ConsoleModifiers.Control) != 0;

        switch (key.Key)
        {
            case ConsoleKey.Tab:
                return;   // leave Tab for focus traversal

            case ConsoleKey.LeftArrow:
                if (!shift && HasSelection) CollapseSelection(SelStart);
                else MoveCaret(_caret - 1, shift);
                break;

            case ConsoleKey.RightArrow:
                if (!shift && HasSelection) CollapseSelection(SelEnd);
                else MoveCaret(_caret + 1, shift);
                break;

            case ConsoleKey.Home:
                MoveCaret(0, shift);
                break;

            case ConsoleKey.End:
                MoveCaret(_text.Length, shift);
                break;

            case ConsoleKey.Backspace:
                if (_readOnly) break;
                if (HasSelection) DeleteSelection();
                else if (_caret > 0) { _text = _text.Remove(--_caret, 1); }
                else break;
                RaiseChanged();
                break;

            case ConsoleKey.Delete:
                if (_readOnly) break;
                if (HasSelection) DeleteSelection();
                else if (_caret < _text.Length) { _text = _text.Remove(_caret, 1); }
                else break;
                RaiseChanged();
                break;

            case ConsoleKey.Enter:
                Submitted?.Invoke(this, EventArgs.Empty);
                break;

            case ConsoleKey.A when ctrl:
                _selAnchor = 0;
                _caret = _text.Length;
                break;

            default:
                if (!_readOnly && !char.IsControl(key.KeyChar))
                {
                    if (HasSelection) DeleteSelection();
                    _text = _text.Insert(_caret++, key.KeyChar.ToString());
                    RaiseChanged();
                }
                else
                {
                    return;   // not handled: let unrecognised keys bubble (e.g. for hotkeys)
                }
                break;
        }

        inputEvent.Handled = true;
        Invalidate();
    }

    /// <summary>Inserts pasted <paramref name="text"/> (flattened to a single line) at the caret, replacing any selection.</summary>
    public override void OnPaste(string text)
    {
        if (_readOnly || string.IsNullOrEmpty(text)) return;
        var body = SingleLine(text);
        if (HasSelection) DeleteSelection();
        _text = _text.Insert(_caret, body);
        _caret += body.Length;
        _selAnchor = -1;
        RaiseChanged();
        Invalidate();
    }

    /// <inheritdoc/>
    protected internal override HelpInfo? GetHelpInfo() => new HelpInfo("Input", "Text input", "A single-line text field.")
        .WithKey("Arrows", "Move the caret")
        .WithKey("Shift+Arrows", "Select")
        .WithKey("Ctrl+A", "Select all")
        .WithKey("Enter", "Submit");

    private void MoveCaret(int target, bool shift)
    {
        target = Math.Clamp(target, 0, _text.Length);
        if (shift) { if (_selAnchor < 0) _selAnchor = _caret; }
        else _selAnchor = -1;
        _caret = target;
    }

    private void CollapseSelection(int to)
    {
        _caret = Math.Clamp(to, 0, _text.Length);
        _selAnchor = -1;
    }

    private void DeleteSelection()
    {
        var s = SelStart;
        _text = _text.Remove(s, SelEnd - s);
        _caret = s;
        _selAnchor = -1;
    }

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);

    private bool HasSelection => _selAnchor >= 0 && _selAnchor != _caret;
    private int SelStart => Math.Min(_selAnchor, _caret);
    private int SelEnd => Math.Max(_selAnchor, _caret);

    private static string SingleLine(string? text) =>
        string.IsNullOrEmpty(text) ? string.Empty : text.Replace("\r", "").Replace("\n", "");

    private static Character Glyph(char content, Style style)
    {
        var fg = style.ForegroundColor is { } f ? f.ToConsoleGUIColor() : (ConsoleGUI.Data.Color?)null;
        var bg = style.BackgroundColor is { } b ? b.ToConsoleGUIColor() : (ConsoleGUI.Data.Color?)null;
        var deco = style.SpectreConsoleStyle?.Decoration ?? Spectre.Console.Decoration.None;
        ConsoleGUI.Data.Decoration? decoration = deco == Spectre.Console.Decoration.None ? null : (ConsoleGUI.Data.Decoration)deco;
        return new Character(content, fg, bg, decoration);
    }

    #endregion Methods

    #region Fields

    private string _text;
    private string _placeholder;
    private int _caret;
    private int _scroll;
    private int _selAnchor = -1;   // -1 = no selection
    private char? _passwordChar;
    private bool _readOnly;
    private Style _textStyle;
    private Style _placeholderStyle;
    private Style _selectionStyle;

    #endregion Fields
}