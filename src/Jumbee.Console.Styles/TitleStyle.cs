namespace Jumbee.Console;

/// <summary>
/// Position of a control frame title: which border (top or bottom) it is drawn in, and its horizontal alignment
/// within that border.
/// </summary>
public enum TitlePos
{
    TopLeft,
    TopCenter,
    TopRight,
    BottomLeft,
    BottomCenter,
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
    public TitleStyle(TitlePos pos = TitlePos.TopLeft, TitleBorderStyle borderStyle = TitleBorderStyle.Double, TitleColorStyle color = TitleColorStyle.Normal)
    {
        Pos = pos;
        BorderStyle = borderStyle;
        Color = color;
    }

    public TitlePos Pos { get; init; }
    public TitleBorderStyle BorderStyle { get; init; }
    public TitleColorStyle Color { get; init; }

    /// <summary>The default title style (top-left, double border, normal colors), matching the original behavior.</summary>
    public static TitleStyle Default { get; } = new TitleStyle(TitlePos.TopLeft, TitleBorderStyle.Double, TitleColorStyle.Normal);

    #region Equality
    // Hand-written: without it, comparing through EqualityComparer<T>.Default (as SetAtomicProperty does on every
    // assignment) boxes both operands and compares reflectively. See Color.
    public bool Equals(TitleStyle other) => Pos == other.Pos && BorderStyle == other.BorderStyle && Color == other.Color;

    public override bool Equals(object? obj) => obj is TitleStyle other && Equals(other);

    public override int GetHashCode() => System.HashCode.Combine(Pos, BorderStyle, Color);

    public static bool operator ==(TitleStyle a, TitleStyle b) => a.Equals(b);

    public static bool operator !=(TitleStyle a, TitleStyle b) => !a.Equals(b);
    #endregion
}
