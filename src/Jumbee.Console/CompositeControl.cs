namespace Jumbee.Console;

using ConsoleGUI.Common;
using ConsoleGUI.Data;
using ConsoleGUI.Space;

/// <summary>
/// Base class for <em>composite</em> controls: a <see cref="Control"/> that owns and lays out several child
/// controls and presents them as a single control. It is a real <see cref="Control"/> (so it has its own
/// console buffer, participates in theming/painting, can be framed, and drops into any layout cell), but its
/// content is an internal <see cref="ILayout"/> arranging the children.
/// </summary>
/// <remarks>
/// The children are composited through a <see cref="DrawingContext"/> over the internal layout (the same
/// mechanism <see cref="ControlFrame"/> uses for its single child): each child keeps its own buffer and renders
/// itself, and its cells — carrying its own mouse listener — are surfaced through this control's indexer, so
/// mouse hit-testing and click-to-focus reach the children unchanged. Keyboard input routes in via
/// <see cref="FocusedControl"/>, which returns the focused descendant.
/// <para>
/// Subclasses build their child controls and an arranging layout in their constructor, wire any inter-child
/// behaviour (e.g. <c>editor.Changed += …</c>), and call <see cref="SetContent"/>. The composite draws no content
/// of its own by default; override <see cref="Control.Render"/> to paint a background/chrome into the buffer
/// behind the children.
/// </para>
/// </remarks>
public abstract class CompositeControl : Control, IDrawingContextListener
{
    #region Constructors
    protected CompositeControl() : base() { }

    protected CompositeControl(ILayout content) : base() => SetContent(content);
    #endregion

    #region Properties
    /// <summary>The internal layout arranging the child controls (set via <see cref="SetContent"/>).</summary>
    protected ILayout? Content => _content;

    /// <summary>The internal layout, exposed so the parent's input routing can deliver keys through it — running any
    /// nested layout tunnels (e.g. a <see cref="TabPanel"/>'s Alt+arrow tab switching) on the way to the focused
    /// child, which a direct <see cref="FocusedControl"/> dispatch would skip.</summary>
    internal ILayout? ContentLayout => _content;

    /// <summary>
    /// Returns the focused descendant so keyboard input routed by the parent layout reaches the right child;
    /// falls back to the composite itself when it is focused and no child is.
    /// </summary>
    public override IFocusable? FocusedControl
    {
        get
        {
            if (_content is not null)
            {
                foreach (var c in _content.Controls)
                {
                    if (c?.FocusedControl is { } focused) return focused;
                }
            }
            return base.FocusedControl;
        }
    }

    /// <summary>A composite is an opaque focusable leaf to the navigation/routing layer: it reports that it handles
    /// input (keys reach its focused child via <see cref="FocusedControl"/>) so <c>ILayout.Leaves</c> treats it as a
    /// single navigable unit rather than descending into its children.</summary>
    public override bool HandlesInput => true;

    /// <summary>The child that receives focus when the composite is focused (and input/caret follow). Defaults to the
    /// first focusable child; override to choose a different default.</summary>
    protected virtual Control? FocusChild => _firstFocusable;
    #endregion

    #region Indexers
    public override Cell this[Position position]
    {
        get
        {
            // A child covers this cell: return it as-is so it keeps the child's own mouse listener (in child
            // coordinates), letting ConsoleManager route hover/click to the child and click-to-focus work.
            if (_contentContext.Contains(position))
                return _contentContext[position];

            // Otherwise the composite's own surface (whatever Render drew into the buffer, e.g. a background).
            if (position.X >= 0 && position.Y >= 0 && position.X < Size.Width && position.Y < Size.Height)
            {
                var cell = consoleBuffer[position];
                return WantsMouse ? cell.WithMouseListener(this, position) : cell;
            }
            return emptyCell;
        }
    }
    #endregion

    #region Methods
    /// <summary>
    /// Sets the internal layout that arranges the children. Call once from the subclass constructor after building
    /// the child controls and the layout, and after wiring any inter-child event handlers.
    /// </summary>
    protected void SetContent(ILayout content)
    {
        _content = content;
        if (!ReferenceEquals(_contentContext, DrawingContext.Dummy)) _contentContext.Dispose();
        _contentContext = new DrawingContext(this, content.CControl);
        foreach (var f in content.Controls) SetOwnership(f);
        Initialize();
    }

    // The composite is the navigable focus unit; route focus inward to a child so keyboard input (via
    // FocusedControl) and the child's caret/selection follow. A no-op when a descendant is already focused (e.g. a
    // child claimed by a more specific path). Overriding FocusChild changes which child this picks.
    protected override void Control_OnFocus()
    {
        if (FocusChild is { } child && !child.IsFocused) child.IsFocused = true;
    }

    // Clear the focused descendant when the composite loses focus (covers the direct UnFocus path; UI.SetFocus
    // already clears registered children when focus moves elsewhere).
    protected override void Control_OnLostFocus()
    {
        if (FocusedControl is Control child && !ReferenceEquals(child, this)) child.IsFocused = false;
    }

    // Claim focusable descendants as ours so focus resolves to this composite (the navigable unit); the first one
    // becomes the default FocusChild. Stops at nested composites (they own their own children). Runs at construction
    // before layout, so it walks structure rather than laid-out leaves (it can't depend on HasLayout).    
    private void SetOwnership(IFocusable? node)
    {
        switch (node)
        {
            case null or CompositeControl: return;                   // a nested composite owns its own children
            case ILayout nested: foreach (var c in nested.Controls) SetOwnership(c); return;
            case ControlFrame frame: SetOwnership(frame.Control); return;
            case Control control when control.Focusable:
                control.Owner ??= this;
                _firstFocusable ??= control;
                return;
        }
    }

    // Children render themselves into their own buffers; the composite paints nothing by default. Subclasses may
    // override to draw a background/chrome behind the children (the indexer composites children over the buffer).
    protected override void Render() { }

    // Runs on the UI thread after Control.Initialize has resized the composite (Control subscribes this to its
    // OnInitialization event). Size the internal layout to fill the composite's current area.
    protected override void Control_OnInitialization()
    {
        base.Control_OnInitialization();
        _contentContext.SetOffset(new Vector(0, 0));
        _contentContext.SetLimits(Size, Size);
    }

    void IDrawingContextListener.OnRedraw(DrawingContext drawingContext) => Initialize();

    void IDrawingContextListener.OnUpdate(DrawingContext drawingContext, Rect rect) => Invalidate();

    public override void Dispose()
    {
        if (!ReferenceEquals(_contentContext, DrawingContext.Dummy)) _contentContext.Dispose();
        base.Dispose();
    }
    #endregion

    #region Fields
    private ILayout? _content;
    private DrawingContext _contentContext = DrawingContext.Dummy;
    // The first focusable descendant claimed in SetContent — the default FocusChild (e.g. a CodeEditor's editor).
    private Control? _firstFocusable;
    #endregion
}
