namespace Jumbee.Console;
/// <summary>
/// A single-child layout that pins its child's size.
/// </summary>
/// <remarks>
/// Wrap a control or layout that would otherwise fill its slot to give it a fixed (or bounded) extent — e.g. cap a
/// toolbar's height to one row so it can be docked at the top of a <see cref="DockPanel"/> without collapsing the fill
/// region (a <see cref="HorizontalStackPanel"/> on its own expands to the full height). Leave a dimension unset to let
/// the child size freely within the slot.
/// </remarks>
public class Boundary : Layout<ConsoleGUI.Controls.Boundary>
{
    #region Constructors

    /// <param name="content">The child to bound.</param>
    /// <param name="width">Fixed width in cells, or <see langword="null"/> to size freely.</param>
    /// <param name="height">Fixed height in cells, or <see langword="null"/> to size freely.</param>
    public Boundary(IFocusable content, int? width = null, int? height = null) : base(new ConsoleGUI.Controls.Boundary())
    {
        Content = content;
        control.Content = content.RenderNode();
        if (width is not null) control.Width = width;
        if (height is not null) control.Height = height;
    }

    #endregion Constructors

    #region Properties

    /// <summary>The bounded child.</summary>
    public IFocusable Content { get; }

    /// <summary>Fixed width in cells (sets min = max = value), or <see langword="null"/> to size freely.</summary>
    public int? Width { set => control.Width = value; }

    /// <summary>Fixed height in cells (sets min = max = value), or <see langword="null"/> to size freely.</summary>
    public int? Height { set => control.Height = value; }

    /// <summary>Minimum width in cells, or <see langword="null"/> for none.</summary>
    public int? MinWidth { get => control.MinWidth; set => control.MinWidth = value; }

    /// <summary>Maximum width in cells, or <see langword="null"/> for none.</summary>
    public int? MaxWidth { get => control.MaxWidth; set => control.MaxWidth = value; }

    /// <summary>Minimum height in cells, or <see langword="null"/> for none.</summary>
    public int? MinHeight { get => control.MinHeight; set => control.MinHeight = value; }

    /// <summary>Maximum height in cells, or <see langword="null"/> for none.</summary>
    public int? MaxHeight { get => control.MaxHeight; set => control.MaxHeight = value; }

    /// <summary>Number of rows in the layout grid (always 1).</summary>
    public override int Rows => 1;

    /// <summary>Number of columns in the layout grid (always 1).</summary>
    public override int Columns => 1;

    #endregion Properties

    #region Indexers

    /// <summary>Gets the bounded child at cell (0, 0).</summary>
    public override IFocusable this[int row, int column] => row == 0 && column == 0
        ? Content
        : throw new ArgumentOutOfRangeException(row != 0 ? nameof(row) : nameof(column));

    #endregion Indexers
}