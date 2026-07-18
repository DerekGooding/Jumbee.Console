namespace Jumbee.Console;

/// <summary>
/// The shape of a control frame's border.
/// </summary>
/// <remarks>Each value selects a box-drawing glyph set; <see cref="None"/> draws no border. A frame's default
/// border comes from <see cref="IStyleTheme.FrameBorder"/>.</remarks>
public enum BorderStyle
{
    /// <summary>No border is drawn.</summary>
    None,
    /// <summary>An ASCII border using <c>+</c>, <c>-</c>, and <c>|</c> characters.</summary>
    Ascii,
    /// <summary>A double-line box-drawing border.</summary>
    Double,
    /// <summary>A heavy (thick) line box-drawing border.</summary>
    Heavy,
    /// <summary>A single-line border with rounded corners.</summary>
    Rounded,
    /// <summary>A single-line border with square corners.</summary>
    Square
}
