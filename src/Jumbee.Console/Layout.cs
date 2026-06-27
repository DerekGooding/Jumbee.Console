namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using ConsoleGUI;
using ConsoleGUI.Common;
using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;
using Spectre.Console.Interop;

public interface ILayout : IFocusable, IDrawingContextListener
{
    int Rows { get; }

    int Columns { get; }

    IControl CControl { get; }

    IFocusable this[int row, int column] { get; }

    IEnumerable<IFocusable> Controls { get; }
}

public abstract class Layout<T> : ILayout where T:CControl, IDrawingContextListener
{
    #region Constructors
    protected Layout(T control)
    {
        this.control = control;
    }
    #endregion

    #region Indexers
    public abstract IFocusable this[int row, int column] { get; }
    #endregion

    #region Properties
    public abstract int Rows { get; }

    public abstract int Columns { get; }

    public Cell this[Position position] => control[position];

    public Size Size => control.Size;   

    public IControl CControl => control;
    
    public IDrawingContext Context
    {
        get => ((IControl) control).Context;
        set => ((IControl)control).Context = value;
    }

    public IEnumerable<IFocusable> Controls
    {
        get
        {
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    yield return this[r, c];
                }
            }
        }
    }

    public bool Focusable { get; set; } = true;

    public IFocusable FocusableControl => this;

    public bool IsFocused
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                if (value)
                    OnFocus?.Invoke();
                else
                    OnLostFocus?.Invoke();
            }
        }
    }

    public bool HandlesInput => true;

    /// <summary>
    /// The focused descendant within this layout (or <see langword="null"/>), so a parent can tell that this layout
    /// is on the focus path and route input into it. Walks <see cref="Controls"/> for the focused leaf; this is what
    /// lets keyboard input — and each ancestor layout's tunnel (<see cref="InterceptInput"/>) — reach a control even
    /// when the layout is nested several levels deep.
    /// </summary>
    public virtual IFocusable? FocusedControl
    {
        get
        {
            foreach (var f in Controls)
                if (f?.FocusedControl is { } focused) return focused;
            return Focusable && IsFocused ? FocusableControl : null;
        }
    }
    #endregion

    #region Events
    public event FocusableEventHandler? OnFocus;

    public event FocusableEventHandler? OnLostFocus;
    #endregion

    #region Methods
    public void OnRedraw(DrawingContext drawingContext) => control.OnRedraw(drawingContext);

    public void OnUpdate(DrawingContext drawingContext, Rect rect) => control.OnUpdate(drawingContext, rect);

    public void OnInput(UI.InputEventArgs inputEventArgs)
    {
        // Tunnel phase: let this layout consume the input before it routes down (e.g. an Overlay closing on its
        // CloseKey, or a TabPanel switching tabs on Alt+arrows). A consumed key is marked handled so ancestor
        // layouts stop routing it. This runs for every layout on the focus path — the root always, and each nested
        // layout reached below — so a layout can define its own navigation keys even when deeply nested.
        if (InterceptInput(inputEventArgs))
        {
            if (inputEventArgs.InputEvent is { } consumed) consumed.Handled = true;
            return;
        }

        // Deliver to the focused descendant. For a nested layout, recurse through its OwnInput so it gets its own
        // tunnel pass (and routes on to its focused child); for a leaf/frame/composite, dispatch via FocusedControl.
        // Cells may be null (an empty slot in a custom layout), so guard. Stop once the event is handled.
        foreach (var f in Controls)
        {
            if (f is ILayout nested)
            {
                if (nested.FocusedControl is not null) nested.OnInput(inputEventArgs);
            }
            else
            {
                f?.FocusedControl?.OnInput(inputEventArgs);
            }
            if (inputEventArgs.InputEvent?.Handled == true) return;
        }
    }

    /// <summary>Lets a layout intercept input before it routes to the focused control. Return true if handled.</summary>
    protected virtual bool InterceptInput(UI.InputEventArgs inputEventArgs) => false;

    public void OnPaste(string text) => Controls.ForEach(f => f?.FocusedControl?.OnPaste(text));
    #endregion

    #region Fields
    public readonly T control;
    #endregion
}
