using ConsoleGUI;

namespace Jumbee.Console;

/// <summary>
/// The visual scaffold behind <see cref="SplitPanel"/>: two nested ConsoleGUI <see cref="DockPanel"/>s laying out
/// <c>[first | divider | second]</c> along the split axis.
/// </summary>
/// <remarks>
/// The first pane is docked with a fixed extent (a <see cref="Boundary"/>), the 1-cell divider is docked next, and the
/// second pane fills the rest — so on container resize the first pane keeps its size and the second pane absorbs the
/// change (the sidebar-stays-put behaviour). <see cref="SplitPanel"/> owns the model and drives this through
/// <see cref="SetFirstExtent"/>.
/// </remarks>
public sealed class SplitPanelDockPanel : ConsoleGUI.Controls.DockPanel
{
    #region Constructors

    internal SplitPanelDockPanel(SplitOrientation orientation, IControl first, IControl second, int firstExtent)
    {
        Orientation = orientation;
        Divider = new SplitDivider(orientation);
        var horizontal = orientation == SplitOrientation.Horizontal;

        _firstBoundary = horizontal
            ? new ConsoleGUI.Controls.Boundary { MinWidth = firstExtent, MaxWidth = firstExtent, Content = first }
            : new ConsoleGUI.Controls.Boundary { MinHeight = firstExtent, MaxHeight = firstExtent, Content = first };

        var dividerBoundary = horizontal
            ? new ConsoleGUI.Controls.Boundary { MinWidth = 1, MaxWidth = 1, Content = Divider }
            : new ConsoleGUI.Controls.Boundary { MinHeight = 1, MaxHeight = 1, Content = Divider };

        var placement = horizontal ? DockedControlPlacement.Left : DockedControlPlacement.Top;

        // Inner panel = [divider | second]; outer panel = [first | inner]. Both dock the fixed part and fill the rest.
        var inner = new ConsoleGUI.Controls.DockPanel { Placement = placement, DockedControl = dividerBoundary, FillingControl = second };
        Placement = placement;
        DockedControl = _firstBoundary;
        FillingControl = inner;
    }

    #endregion Constructors

    #region Properties

    internal SplitDivider Divider { get; }
    internal SplitOrientation Orientation { get; }

    #endregion Properties

    #region Methods

    internal void SetFirstExtent(int extent)
    {
        // Use Width/Height (single re-layout) rather than setting Min+Max separately (two cascades) — this is called
        // on every mouse-move while dragging the divider.
        if (Orientation == SplitOrientation.Horizontal) _firstBoundary.Width = extent;
        else _firstBoundary.Height = extent;
    }

    #endregion Methods

    #region Fields

    private readonly ConsoleGUI.Controls.Boundary _firstBoundary;

    #endregion Fields
}