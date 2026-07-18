namespace Jumbee.Console;

/// <summary>
/// Position of a control frame title: which border (top or bottom) it is drawn in, and its horizontal alignment
/// within that border.
/// </summary>
public enum TitlePos
{
    /// <summary>Top border, left-aligned.</summary>
    TopLeft,
    /// <summary>Top border, centered.</summary>
    TopCenter,
    /// <summary>Top border, right-aligned.</summary>
    TopRight,
    /// <summary>Bottom border, left-aligned.</summary>
    BottomLeft,
    /// <summary>Bottom border, centered.</summary>
    BottomCenter,
    /// <summary>Bottom border, right-aligned.</summary>
    BottomRight
}

/// <summary>
/// The way a control frame title is drawn relative to the top border.
/// </summary>
public enum TitleBorderStyle
{
    /// <summary>Title is drawn inside the single top border row, replacing some of the border line characters.</summary>
    Inline,

    /// <summary>Title is drawn on its own row between a top border line and a separator line.</summary>
    Double
}

/// <summary>
/// The way a control frame title is colored relative to the border color.
/// </summary>
public enum TitleColorStyle
{
    /// <summary>Title is drawn in the frame's foreground/background colors.</summary>
    Normal,

    /// <summary>Title foreground and background are swapped with the border color.</summary>
    Reverse
}

/// <summary>
/// Describes how a control frame title is aligned, bordered, and colored.
/// </summary>
/// <remarks>A frame's default title style comes from <see cref="IStyleTheme.TitleStyle"/>.</remarks>
public readonly struct TitleStyle : System.IEquatable<TitleStyle>
{
    /// <summary>Initializes a new <see cref="TitleStyle"/> with the given position, border style, and color style.</summary>
    public TitleStyle(TitlePos pos = TitlePos.TopLeft, TitleBorderStyle borderStyle = TitleBorderStyle.Double, TitleColorStyle color = TitleColorStyle.Normal)
    {
        Pos = pos;
        BorderStyle = borderStyle;
        Color = color;
    }

    /// <summary>The title's border and alignment position.</summary>
    public TitlePos Pos { get; init; }
    /// <summary>How the title is drawn relative to the top border.</summary>
    public TitleBorderStyle BorderStyle { get; init; }
    /// <summary>How the title is colored relative to the border color.</summary>
    public TitleColorStyle Color { get; init; }

    /// <summary>The default title style (top-left, double border, normal colors), matching the original behavior.</summary>
    public static TitleStyle Default { get; } = new TitleStyle(TitlePos.TopLeft, TitleBorderStyle.Double, TitleColorStyle.Normal);

    #region Equality
    // Hand-written: without it, comparing through EqualityComparer<T>.Default (as SetAtomicProperty does on every
    // assignment) boxes both operands and compares reflectively. See Color.
    /// <summary>Determines whether this <see cref="TitleStyle"/> equals <paramref name="other"/>.</summary>
    public bool Equals(TitleStyle other) => Pos == other.Pos && BorderStyle == other.BorderStyle && Color == other.Color;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is TitleStyle other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => System.HashCode.Combine(Pos, BorderStyle, Color);

    /// <summary>Equality operator.</summary>
    public static bool operator ==(TitleStyle a, TitleStyle b) => a.Equals(b);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(TitleStyle a, TitleStyle b) => !a.Equals(b);
    #endregion
}
