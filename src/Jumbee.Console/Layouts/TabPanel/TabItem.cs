namespace Jumbee.Console;

/// <summary>
/// A handle to a tab in a <see cref="TabPanel"/> — stable across add/remove/reorder (unlike a positional index).
/// </summary>
/// <remarks>
/// Use it to relabel, hide/show, disable/enable, or query whether the tab is selected; pass it to
/// <see cref="TabPanel.RemoveTab(TabItem)"/> to remove it.
/// </remarks>
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
    /// again).</summary>
    /// <remarks>Hiding the selected tab moves selection to the nearest visible, enabled tab.</remarks>
    public bool IsHidden
    {
        get => _hidden;
        set { if (_hidden == value) return; _hidden = value; _owner.OnTabVisibilityChanged(this); }
    }

    /// <summary>When <see langword="true"/> the tab is shown greyed-out and can't be selected or focused.</summary>
    /// <remarks>Disabling the selected tab moves selection to the nearest selectable tab.</remarks>
    public bool IsDisabled
    {
        get => _disabled;
        set { if (_disabled == value) return; _disabled = value; _owner.OnTabEnabledChanged(this); }
    }

    /// <summary>Whether this is the currently selected tab.</summary>
    public bool IsSelected => ReferenceEquals(_owner.ActiveTab, this);

    /// <summary>Per-tab override of whether this tab shows a close (✕) glyph.</summary>
    /// <remarks>Independent of the panel-wide <see cref="TabPanel.ClosableTabs"/> default (though setting that
    /// re-applies to every tab). Use to keep a specific tab non-closable (e.g. a pinned document).</remarks>
    public bool Closable
    {
        get => Header.Closable;
        set { if (Header.Closable == value) return; _owner.SetTabClosable(this, value); }
    }
    #endregion

    #region Fields
    private readonly TabPanel _owner;
    private string _name;
    private bool _hidden;
    private bool _disabled;
    #endregion
}
