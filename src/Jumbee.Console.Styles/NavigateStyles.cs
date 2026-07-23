
using SCDecoration = Spectre.Console.Decoration;
using SCStyle = Spectre.Console.Style;

namespace Jumbee.Console;
/// <summary>
/// How a navigable control (e.g. <see cref="IStyleTheme"/> consumers like ListBox, Tree, TabPanel) renders its
/// selected/active item.
/// </summary>
/// <remarks>The default comes from <see cref="IStyleTheme.SelectionStyle"/>; the caret glyph used by
/// <see cref="Caret"/> comes from <see cref="IGlyphTheme.SelectionCaret"/>.</remarks>
public enum SelectionStyle
{
    /// <summary>Paint the selected item with the selection foreground <em>and</em> background.</summary>
    Highlight,

    /// <summary>Underline the selected item (selection foreground, no background).</summary>
    Underline,

    /// <summary>Prefix the selected item with the selection caret glyph (selection foreground, no background).</summary>
    Caret
}

/// <summary>Turns a <see cref="SelectionStyle"/> into the prefix + text style a control applies to its selected item.</summary>
public static class SelectionStylesExtensions
{
    /// <summary>The string to prepend to the selected item: the caret glyph for <see cref="SelectionStyle.Caret"/>,
    /// otherwise the empty string.</summary>
    public static string Prefix(this SelectionStyle style, string caret) => style == SelectionStyle.Caret ? caret : string.Empty;

    /// <summary>The text style for the selected item: Highlight uses foreground + background; Underline uses the
    /// foreground plus an underline; Caret uses the foreground only (the caret carries the indication).</summary>
    public static SCStyle TextStyle(this SelectionStyle style, Color? foreground, Color? background) => style switch
    {
        SelectionStyle.Highlight => new SCStyle(foreground, background),
        SelectionStyle.Underline => new SCStyle(foreground, null, SCDecoration.Underline),
        _ => new SCStyle(foreground, null),   // Caret
    };
}