namespace Jumbee.Console;

using System;
using System.Collections.Generic;

public enum TabBarDock
{
    Top,
    Left,
    Right,
    Bottom
}

/// <summary>
/// A tabbed container: a bar of selectable <see cref="TabHeader"/> labels docked on one edge, with the selected
/// tab's content filling the rest. Select a tab by clicking its label, by the arrow keys while the bar is focused
/// (Left/Right for a top/bottom bar, Up/Down for a left/right bar), or programmatically via <see cref="SelectedIndex"/>.
/// </summary>
public class TabPanel : Layout<TabPanelDockPanel>
{
    #region Constructors
    public TabPanel(TabBarDock tabBarDock, params (string Name, IFocusable Content)[] tabs)
        : base(new TabPanelDockPanel(tabBarDock, BarThickness(tabBarDock, tabs)))
    {
        _horizontal = control.IsHorizontalTabBar;
        foreach (var (name, content) in tabs) AddTabInternal(name, content);
        if (_tabs.Count > 0) SelectedIndex = 0;   // open on the first tab
    }

    // The bar's cross-axis size, from the label texts (see TabPanelDockPanel): one row tall for a top/bottom bar,
    // or as wide as the widest label for a left/right bar. The +2 matches TabHeader's one-space-each-side padding.
    private static int BarThickness(TabBarDock dock, (string Name, IFocusable Content)[] tabs)
    {
        if (dock is TabBarDock.Top or TabBarDock.Bottom) return 1;
        var width = 1;
        foreach (var (name, _) in tabs) width = Math.Max(width, name.Length + 2);
        return width;
    }
    #endregion

    #region Events
    /// <summary>Raised after the selected tab changes, with the new index.</summary>
    public event Action<int>? SelectionChanged;
    #endregion

    #region Properties
    /// <summary>The number of tabs.</summary>
    public int TabCount => _tabs.Count;

    /// <summary>The tab header labels, in order (for inspection or per-tab styling).</summary>
    public IReadOnlyList<TabHeader> Headers => _tabs.ConvertAll(t => t.Header);

    /// <summary>The zero-based selected tab. Setting it activates that tab (clamped to range); raises
    /// <see cref="SelectionChanged"/> when it actually changes. -1 only before the first tab is added.</summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (_tabs.Count == 0) return;
            var idx = Math.Clamp(value, 0, _tabs.Count - 1);
            if (idx == _selectedIndex) return;

            if (_selectedIndex >= 0) _tabs[_selectedIndex].Header.IsActive = false;
            _selectedIndex = idx;
            _tabs[idx].Header.IsActive = true;
            control.SetFill(_tabs[idx].Content.FocusableControl);
            SelectionChanged?.Invoke(idx);
        }
    }

    /// <summary>The selected tab's content, or <see langword="null"/> when there are no tabs.</summary>
    public IFocusable? ActiveContent => _selectedIndex >= 0 ? _tabs[_selectedIndex].Content : null;

    /// <summary>The selected tab's name, or <see langword="null"/> when there are no tabs.</summary>
    public string? ActiveTabName => _selectedIndex >= 0 ? _tabs[_selectedIndex].Name : null;

    // Logical children, flattened so input routing reaches them whether this panel is the root layout (its own
    // OnInput walks Controls) or nested in another layout (the parent routes through FocusedControl below): all tab
    // headers, then the active tab's content. The visual arrangement is handled separately by the wrapped DockPanel.
    public override int Rows => _tabs.Count + (_selectedIndex >= 0 ? 1 : 0);

    public override int Columns => 1;

    public override IFocusable this[int row, int column]
    {
        get
        {
            if (column != 0) throw new ArgumentOutOfRangeException(nameof(column));
            if (row < 0 || row >= Rows) throw new ArgumentOutOfRangeException(nameof(row));
            return row < _tabs.Count ? _tabs[row].Header : _tabs[_selectedIndex].Content;
        }
    }

    // Return the focused descendant — a focused tab header, else whatever is focused inside the active content — so
    // a parent layout routing input through this single IFocusable reaches it (the CompositeControl pattern).
    public override IFocusable? FocusedControl
    {
        get
        {
            foreach (var t in _tabs)
                if (t.Header.FocusedControl is { } focusedHeader) return focusedHeader;
            return _selectedIndex >= 0 ? _tabs[_selectedIndex].Content.FocusedControl : null;
        }
    }
    #endregion

    #region Methods
    /// <summary>Selects the tab at <paramref name="index"/> (clamped). Equivalent to setting <see cref="SelectedIndex"/>.</summary>
    public void SelectTab(int index) => SelectedIndex = index;

    private void AddTabInternal(string name, IFocusable content)
    {
        var header = new TabHeader(_tabs.Count, name, _horizontal);
        header.Activated += (_, _) => SelectedIndex = header.Index;          // click / Enter / Space -> select
        header.Navigated += (_, delta) => MoveSelection(delta);             // arrow along the bar -> adjacent tab
        control.AddHeader(header);
        _tabs.Add((name, content, header));
    }

    // Arrow navigation: move the selection by one and keep keyboard focus on the bar at the newly selected header.
    private void MoveSelection(int delta)
    {
        if (_tabs.Count == 0) return;
        var idx = Math.Clamp(_selectedIndex + delta, 0, _tabs.Count - 1);
        if (idx == _selectedIndex) return;
        SelectedIndex = idx;
        UI.SetFocus(_tabs[idx].Header);
    }
    #endregion

    #region Fields
    private readonly bool _horizontal;
    private readonly List<(string Name, IFocusable Content, TabHeader Header)> _tabs = new();
    private int _selectedIndex = -1;
    #endregion
}
