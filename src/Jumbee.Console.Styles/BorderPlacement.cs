namespace Jumbee.Console;
/// <summary>
/// Which edges of a control frame's border are drawn.
/// </summary>
/// <remarks>A <see cref="FlagsAttribute"/> set — combine sides with <c>|</c> (e.g. <c>Top | Bottom</c> for
/// horizontal rules only). Defaults to <see cref="All"/>. Set via <c>ControlFrame.BorderPlacement</c>.</remarks>
[Flags]
public enum BorderPlacement
{
    /// <summary>No edges — the border shape draws nothing (like <see cref="BorderStyle.None"/>).</summary>
    None = 0b0000,

    /// <summary>The left edge.</summary>
    Left = 0b0001,

    /// <summary>The top edge.</summary>
    Top = 0b0010,

    /// <summary>The right edge.</summary>
    Right = 0b0100,

    /// <summary>The bottom edge.</summary>
    Bottom = 0b1000,

    /// <summary>All four edges (the default).</summary>
    All = 0b1111
}