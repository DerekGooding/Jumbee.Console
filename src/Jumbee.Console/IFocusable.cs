namespace Jumbee.Console;

using ConsoleGUI;
using ConsoleGUI.Input;

/// <summary>Handler for the <see cref="IFocusable.OnFocus"/> and <see cref="IFocusable.OnLostFocus"/> events.</summary>
public delegate void FocusableEventHandler();

/// <summary>A control that can receive keyboard focus and input.</summary>
public interface IFocusable : IControl
{
    /// <summary>Whether the control can receive focus.</summary>
    bool Focusable { get; set; }

    /// <summary>Whether the control currently has focus.</summary>
    bool IsFocused { get; set; }

    /// <summary>The control that actually holds focus (this control, or a focusable descendant/wrapped control).</summary>
    IFocusable FocusableControl { get; }

    /// <summary>Raised when the control gains focus.</summary>
    event FocusableEventHandler OnFocus;

    /// <summary>Raised when the control loses focus.</summary>
    event FocusableEventHandler OnLostFocus;

    /// <summary>Gives the control focus.</summary>
    void Focus() => IsFocused = true;

    /// <summary>Removes focus from the control.</summary>
    void UnFocus() => IsFocused = false;

    /// <summary>Whether the control consumes keyboard input while focused.</summary>
    bool HandlesInput { get; }

    /// <summary>Handles an input event routed to this control.</summary>
    void OnInput(UI.InputEventArgs inputEventArgs);

    /// <summary>Delivers a bracketed-paste payload as a single unit. Default no-op; overridden by text controls.</summary>
    void OnPaste(string text) {}

    /// <summary>The focusable control if this one is focusable and currently focused, otherwise <see langword="null"/>.</summary>
    IFocusable? FocusedControl => Focusable && IsFocused ? FocusableControl : null;
}