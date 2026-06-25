namespace Jumbee.Console;

/// <summary>
/// The shape of a control frame's border. Each value selects a box-drawing glyph set; <see cref="None"/> draws
/// no border. A frame's default border comes from <see cref="IStyleTheme.FrameBorder"/>.
/// </summary>
public enum BorderStyle
{
    None,
    Ascii,
    Double,
    Heavy,
    Rounded,
    Square
}
