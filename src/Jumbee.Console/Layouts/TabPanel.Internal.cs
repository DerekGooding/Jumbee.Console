namespace Jumbee.Console;

using System;

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
            ? new Boundary { MinHeight = barThickness, MaxHeight = barThickness, Content = _bar }
            : new Boundary { MinWidth = barThickness, MaxWidth = barThickness, Content = _bar };
        DockedControl = _boundary;
    }
    #endregion

    #region Properties
    public bool IsHorizontalTabBar { get; }
    #endregion

    #region Methods
    public void AddHeader(IControl header)
    {
        if (_bar is ConsoleGUI.Controls.HorizontalStackPanel h) h.Add(header);
        else ((ConsoleGUI.Controls.VerticalStackPanel)_bar).Add(header);
    }

    public void SetFill(IControl content) => FillingControl = content;
    #endregion

    #region Fields
    private readonly CControl _bar;
    private readonly Boundary _boundary;
    #endregion
}
