namespace Jumbee.Console;

using System;
using System.Collections.Generic;

using ConsoleGUI;
using ConsoleGUI.Controls;
using ConsoleGUI.Space;

/// <summary>
/// The visual scaffold behind <see cref="TabPanel"/>: a ConsoleGUI <see cref="ConsoleGUI.Controls.DockPanel"/> that
/// docks a thin tab bar (a horizontal or vertical stack of <see cref="TabHeader"/> cells) on one edge and fills the
/// rest with the active tab's content. It does no selection bookkeeping — <see cref="TabPanel"/> owns the model and
/// drives this through <see cref="AddHeader"/> / <see cref="SetFill"/>.
/// </summary>
public sealed class TabPanelDockPanel : ConsoleGUI.Controls.DockPanel
{
    #region Constructors
    // barThickness = the bar's cross-axis size: the label row height for a top/bottom bar (1), or the widest label
    // for a left/right bar. A stack panel stretches each child to the cross-axis it's given, so this can't be
    // derived from a laid-out header (that would be circular); TabPanel computes it from the label texts up front.
    internal TabPanelDockPanel(TabBarDock tabBarDock, int barThickness)
    {
        IsHorizontalTabBar = tabBarDock is TabBarDock.Top or TabBarDock.Bottom;
        Placement = tabBarDock switch
        {
            TabBarDock.Top => DockedControlPlacement.Top,
            TabBarDock.Bottom => DockedControlPlacement.Bottom,
            TabBarDock.Left => DockedControlPlacement.Left,
            TabBarDock.Right => DockedControlPlacement.Right,
            _ => throw new NotImplementedException()
        };

        _bar = IsHorizontalTabBar
            ? new ConsoleGUI.Controls.HorizontalStackPanel()
            : new ConsoleGUI.Controls.VerticalStackPanel();
        // Cap ONLY the cross-axis to the label thickness; the main axis is left unconstrained so the labels lay out
        // along it.
        _boundary = IsHorizontalTabBar
            ? new ConsoleGUI.Controls.Boundary { MinHeight = barThickness, MaxHeight = barThickness, Content = _bar }
            : new ConsoleGUI.Controls.Boundary { MinWidth = barThickness, MaxWidth = barThickness, Content = _bar };
        DockedControl = _boundary;
    }
    #endregion

    #region Properties
    public bool IsHorizontalTabBar { get; }
    #endregion

    #region Methods
    // Replace the whole bar with the given headers, in order — the single path for add/remove/reorder/hide, since the
    // stack panels only append/remove and can't insert at a position.
    public void SetHeaders(IEnumerable<IControl> headers)
    {
        if (_bar is ConsoleGUI.Controls.HorizontalStackPanel h) h.Children = headers;
        else ((ConsoleGUI.Controls.VerticalStackPanel)_bar).Children = headers;
    }

    // Resize a vertical bar's cross-axis to the widest visible label (a horizontal bar stays one row tall).
    public void SetBarThickness(int thickness)
    {
        if (IsHorizontalTabBar) return;
        _boundary.MinWidth = thickness;
        _boundary.MaxWidth = thickness;
    }

    // Null clears the fill (the empty state when no tab is selectable); DrawingContext tolerates a null child.
    public void SetFill(IControl? content) => FillingControl = content!;
    #endregion

    #region Fields
    private readonly CControl _bar;
    private readonly ConsoleGUI.Controls.Boundary _boundary;
    #endregion
}
