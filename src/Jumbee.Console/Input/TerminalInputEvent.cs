namespace Jumbee.Console;

using System;

/// <summary>
/// Keyboard/mouse modifier flags decoded from the input stream.
/// </summary>
[Flags]
public enum TerminalModifiers
{
    None = 0,
    Shift = 1,
    Alt = 2,
    Control = 4,
}

/// <summary>Mouse button (or wheel direction) reported by the terminal.</summary>
public enum TerminalMouseButton
{
    None,
    Left,
    Middle,
    Right,
    WheelUp,
    WheelDown,
}

/// <summary>The kind of mouse action reported.</summary>
public enum TerminalMouseKind
{
    Down,
    Up,
    Drag,
    Move,
    Wheel,
}

/// <summary>
/// Base type for the unified terminal input stream produced by <see cref="AnsiInputDecoder"/>: a single
/// sequence of key / mouse / paste / focus events, replacing the keyboard-only <see cref="ConsoleKeyInfo"/> path.
/// </summary>
public abstract record TerminalInputEvent;

/// <summary>A key press.</summary>
/// <remarks>Bridges to/from the existing <see cref="ConsoleGUI.Input.InputEvent"/> path via <see cref="ConsoleKeyInfo"/>.</remarks>
public sealed record KeyInputEvent(ConsoleKey Key, char KeyChar, TerminalModifiers Modifiers) : TerminalInputEvent
{
    public ConsoleKeyInfo ToConsoleKeyInfo() => new(
        KeyChar, Key,
        shift: (Modifiers & TerminalModifiers.Shift) != 0,
        alt: (Modifiers & TerminalModifiers.Alt) != 0,
        control: (Modifiers & TerminalModifiers.Control) != 0);

    public static KeyInputEvent From(ConsoleKeyInfo key)
    {
        var mods = TerminalModifiers.None;
        if ((key.Modifiers & ConsoleModifiers.Shift) != 0) mods |= TerminalModifiers.Shift;
        if ((key.Modifiers & ConsoleModifiers.Alt) != 0) mods |= TerminalModifiers.Alt;
        if ((key.Modifiers & ConsoleModifiers.Control) != 0) mods |= TerminalModifiers.Control;
        return new KeyInputEvent(key.Key, key.KeyChar, mods);
    }
}

/// <summary>A bracketed-paste payload, delivered as one event so it is never re-interpreted as keystrokes.</summary>
public sealed record PasteInputEvent(string Text) : TerminalInputEvent;

/// <summary>A mouse action at cell coordinates (0-based).</summary>
public sealed record MouseInputEvent(int X, int Y, TerminalMouseButton Button, TerminalMouseKind Kind, TerminalModifiers Modifiers) : TerminalInputEvent;

/// <summary>Terminal focus gained/lost (DEC mode 1004).</summary>
public sealed record FocusInputEvent(bool HasFocus) : TerminalInputEvent;

/// <summary>Terminal resized. Not decoded from the char stream — raised by the input source on a resize signal.</summary>
public sealed record ResizeInputEvent(int Width, int Height) : TerminalInputEvent;
