namespace Jumbee.Console;

using ConsoleGUI;
using ConsoleGUI.Input;

public delegate void FocusableEventHandler(); 

public interface IFocusable : IControl
{
    bool Focusable { get; set; }
    
    bool IsFocused { get; set; }

    IFocusable FocusableControl { get; }
   
    event FocusableEventHandler OnFocus;

    event FocusableEventHandler OnLostFocus;
    
    void Focus() => IsFocused = true;

    void UnFocus() => IsFocused = false;

    bool HandlesInput { get; }

    void OnInput(UI.InputEventArgs inputEventArgs);

    /// <summary>Delivers a bracketed-paste payload as a single unit. Default no-op; overridden by text controls.</summary>
    void OnPaste(string text) {}

    IFocusable? FocusedControl => Focusable && IsFocused ? FocusableControl : null;
}