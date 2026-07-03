namespace Jumbee.Console;

/// <summary>
/// A handle to a tab in a <see cref="TabPanel"/> — stable across add/remove/reorder (unlike a positional index).
/// Use it to relabel, hide/show, disable/enable, or query whether the tab is selected; pass it to
/// <see cref="TabPanel.RemoveTab(TabItem)"/> to remove it.
/// </summary>
public sealed class TabItem
{
    #region Constructors
    internal TabItem(TabPanel owner, string name, IFocusable content, TabHeader header)
    {
        _owner = owner;
        _name = name;
        Content = content;
        Header = header;
    }
    #endregion

    #region Properties
    /// <summary>The tab's content control.</summary>
    public IFocusable Content { get; }

    internal TabHeader Header { get; }

    /// <summary>The tab label. Setting it relabels the header.</summary>
    public string Name
    {
        get => _name;
        set { if (_name == value) return; _name = value; _owner.RelabelTab(this); }
    }

    /// <summary>When <see langword="true"/> the tab is removed from the bar but kept in the model (can be shown
    /// again). Hiding the selected tab moves selection to the nearest visible, enabled tab.</summary>
    public bool IsHidden
    {
        get => _hidden;
        set { if (_hidden == value) return; _hidden = value; _owner.OnTabVisibilityChanged(this); }
    }

    /// <summary>When <see langword="true"/> the tab is shown greyed-out and can't be selected or focused. Disabling
    /// the selected tab moves selection to the nearest selectable tab.</summary>
    public bool IsDisabled
    {
        get => _disabled;
        set { if (_disabled == value) return; _disabled = value; _owner.OnTabEnabledChanged(this); }
    }

    /// <summary>Whether this is the currently selected tab.</summary>
    public bool IsSelected => ReferenceEquals(_owner.ActiveTab, this);
    #endregion

    #region Fields
    private readonly TabPanel _owner;
    private string _name;
    private bool _hidden;
    private bool _disabled;
    #endregion
}
