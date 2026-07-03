namespace Jumbee.Console;

using System;

/// <summary>How a <see cref="SplitPanel"/> arranges its two panes.</summary>
public enum SplitOrientation
{
    /// <summary>Panes side by side (first = left, second = right), separated by a vertical divider.</summary>
    Horizontal,

    /// <summary>Panes stacked (first = top, second = bottom), separated by a horizontal divider.</summary>
    Vertical
}

/// <summary>
/// A container that splits its area between two panes with a draggable divider between them. The first pane has a
/// fixed extent (<see cref="SplitPosition"/>, in cells) and the second fills the rest, so resizing the container
/// keeps the first pane put and grows/shrinks the second. Resize by dragging the divider, or by focusing it and
/// pressing the arrow keys (Shift = larger step). Nest split panels for richer layouts (e.g. a sidebar beside a
/// vertically-split editor/terminal). Composes with the same focus-routing model as <see cref="TabPanel"/>.
/// </summary>
public class SplitPanel : Layout<SplitPanelDockPanel>
{
    #region Constructors
    public SplitPanel(SplitOrientation orientation, IFocusable first, IFocusable second, int splitPosition = 20)
        : base(new SplitPanelDockPanel(orientation, first.FocusableControl, second.FocusableControl,
            Math.Max(DefaultMin, splitPosition)))
    {
        _orientation = orientation;
        _first = first;
        _second = second;
        _divider = control.Divider;
        _splitPosition = Math.Max(DefaultMin, splitPosition);
        _divider.Dragged += delta => SplitPosition += delta;
        _divider.Nudged += delta => SplitPosition += delta;
    }
    #endregion

    #region Events
    /// <summary>Raised after <see cref="SplitPosition"/> changes, with the new first-pane extent.</summary>
    public event Action<int>? SplitChanged;
    #endregion

    #region Properties
    /// <summary>The draggable divider (for theming or focusing).</summary>
    public SplitDivider Divider => _divider;

    /// <summary>The first pane (left/top).</summary>
    public IFocusable First => _first;

    /// <summary>The second pane (right/bottom).</summary>
    public IFocusable Second => _second;

    /// <summary>The first pane's extent in cells (width for a horizontal split, height for a vertical one). Clamped
    /// to <see cref="MinFirst"/> and to leaving the divider plus <see cref="MinSecond"/> for the second pane; raises
    /// <see cref="SplitChanged"/> when it actually changes.</summary>
    public int SplitPosition
    {
        get => _splitPosition;
        set
        {
            var total = _orientation == SplitOrientation.Horizontal ? control.Size.Width : control.Size.Height;
            // Once laid out, don't let the first pane crowd out the divider + the second pane's minimum.
            var max = total > 0 ? total - 1 - _minSecond : int.MaxValue;
            var clamped = Math.Clamp(value, _minFirst, Math.Max(_minFirst, max));
            if (clamped == _splitPosition) return;
            _splitPosition = clamped;
            control.SetFirstExtent(clamped);
            SplitChanged?.Invoke(clamped);
        }
    }

    /// <summary>Minimum extent of the first pane in cells (default 3).</summary>
    public int MinFirst
    {
        get => _minFirst;
        set { _minFirst = Math.Max(1, value); SplitPosition = _splitPosition; }
    }

    /// <summary>Minimum extent of the second pane in cells (default 3).</summary>
    public int MinSecond
    {
        get => _minSecond;
        set { _minSecond = Math.Max(1, value); SplitPosition = _splitPosition; }
    }

    // Logical children for input routing (like TabPanel): first pane, divider, second pane. Both panes and the
    // divider are focusable; the divider takes arrow-key resizes when focused.
    public override int Rows => 3;

    public override int Columns => 1;

    public override IFocusable this[int row, int column]
    {
        get
        {
            if (column != 0) throw new ArgumentOutOfRangeException(nameof(column));
            return row switch
            {
                0 => _first,
                1 => _divider,
                2 => _second,
                _ => throw new ArgumentOutOfRangeException(nameof(row))
            };
        }
    }

    // Surface the focused descendant so a parent layout routing through this single IFocusable reaches it.
    public override IFocusable? FocusedControl => _first.FocusedControl ?? _divider.FocusedControl ?? _second.FocusedControl;
    #endregion

    #region Fields
    private const int DefaultMin = 3;
    private readonly SplitOrientation _orientation;
    private readonly IFocusable _first;
    private readonly IFocusable _second;
    private readonly SplitDivider _divider;
    private int _splitPosition;
    private int _minFirst = DefaultMin;
    private int _minSecond = DefaultMin;
    #endregion
}
