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

    // ---- Focus navigation -------------------------------------------------------------------------------------
    // Spatial/region focus navigation over this layout's 2-D cell grid (Rows/Columns/this[r,c]). These compute the
    // focusable to move to; applying focus stays a UI concern (UI.SetFocus). Hoisted here from UI so any layout can
    // drive the same navigation, not only the root.

    /// <summary>Indexer access that tolerates an empty slot (a sparse Grid cell can throw from the underlying
    /// indexer); returns <see langword="null"/> in that case.</summary>
    IFocusable? CellAt(int row, int column)
    {
        try { return this[row, column]; } catch { return null; }
    }

    /// <summary>The (row, column) of the cell whose subtree currently holds focus, or <see langword="null"/> if none.</summary>
    (int Row, int Column)? FocusedCell()
    {
        for (var r = 0; r < Rows; r++)
            for (var c = 0; c < Columns; c++)
                if (CellAt(r, c)?.FocusedControl is not null) return (r, c);
        return null;
    }

    /// <summary>Computes the focusable one cell in the given direction from the focused cell (wraps per axis; skips
    /// empty cells), or <see langword="null"/> if none. Pass a row/column delta (e.g. (0, -1) = left). Landing on a
    /// cell descends to its first focusable leaf.</summary>
    IFocusable? SpatialTarget(int dRow, int dCol)
    {
        int rows = Rows, cols = Columns;
        if (rows <= 0 || cols <= 0) return null;

        var (r, c) = FocusedCell() ?? (0, 0);
        for (var step = 0; step < rows * cols; step++)   // walk in the given direction, wrapping, until a focusable cell
        {
            r = ((r + dRow) % rows + rows) % rows;
            c = ((c + dCol) % cols + cols) % cols;
            if (FirstLeaf(CellAt(r, c)) is { } target) return target;
        }
        return null;
    }

    /// <summary>Computes the next (<paramref name="direction"/> &gt; 0) or previous focusable within the currently
    /// focused region, relative to <paramref name="current"/>, wrapping — or <see langword="null"/>. A no-op (null)
    /// unless the focused cell is a multi-focusable nested layout (enter/leave a single control or composite via the
    /// spatial arrows instead).</summary>
    IFocusable? RegionCycleTarget(int direction, IFocusable? current)
    {
        if (FocusedCell() is not { } cell) return null;
        if (CellAt(cell.Row, cell.Column) is not ILayout region) return null;

        var ring = Leaves(region).ToList();
        if (ring.Count <= 1) return null;
        var index = ring.FindIndex(f => ReferenceEquals(f, current));
        var next = index < 0
            ? (direction > 0 ? 0 : ring.Count - 1)
            : ((index + direction) % ring.Count + ring.Count) % ring.Count;
        return ring[next];
    }

    /// <summary>The first focusable leaf reachable from <paramref name="node"/> (see <see cref="Leaves"/>), or null.</summary>
    static IFocusable? FirstLeaf(IFocusable? node) => Leaves(node).FirstOrDefault();

    /// <summary>The focusable leaves reachable from <paramref name="node"/>, in order: descends nested layouts and
    /// frames; a leaf is an interactive, laid-out Control (Focusable + HandlesInput + HasLayout). A composite is an
    /// opaque leaf here (it reports HandlesInput) and manages its own children's focus internally.</summary>
    static IEnumerable<IFocusable> Leaves(IFocusable? node)
    {
        if (node is null) yield break;
        if (node is ILayout nested)
        {
            for (var r = 0; r < nested.Rows; r++)
                for (var c = 0; c < nested.Columns; c++)
                    foreach (var f in Leaves(nested.CellAt(r, c))) yield return f;
        }
        else if (node is ControlFrame frame)
        {
            foreach (var f in Leaves(frame.Control)) yield return f;
        }
        else if (node is Control control && control.Focusable && control.HandlesInput && control.HasLayout)
        {
            yield return control;
        }
    }
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
            else if (f is CompositeControl composite && composite.ContentLayout is { } content)
            {
                // Route through the composite's internal layout so its nested tunnels (e.g. a TabPanel's Alt+arrow
                // tab switching) run before the key reaches the focused child — a direct FocusedControl dispatch
                // would skip them.
                if (composite.FocusedControl is not null) content.OnInput(inputEventArgs);
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
