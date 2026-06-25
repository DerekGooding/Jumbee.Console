namespace Jumbee.Console;

using Spectre.Console;

/// <summary>
/// The glyphs controls use for state indicators. Like <see cref="IStyleTheme"/>, a glyph theme affects
/// <em>appearance only</em>: swapping glyphs never changes behaviour, though glyphs of a different cell width
/// do change a control's measured size (controls derive their layout from the themed glyph's width rather than
/// assuming a constant). Members are default-implemented; override only the glyphs you want to change, and read
/// the theme through this interface type (e.g. <see cref="UI.GlyphTheme"/>).
/// </summary>
public interface IGlyphTheme
{
    #region Checkbox
    string CheckboxChecked => "[X]";
    string CheckboxUnchecked => "[ ]";
    #endregion

    #region Radio
    string RadioSelected => "(●)";     // (●)
    string RadioUnselected => "( )";
    #endregion

    #region Switch
    string SwitchOn => "(─●)";    // (─●)
    string SwitchOff => "(●─)";   // (●─)
    #endregion

    #region Scrollbar
    /// <summary>The glyphs a control frame's vertical scrollbar uses (colours come from <see cref="IStyleTheme.ScrollBar"/>).
    /// Defaults to <see cref="ScrollBarGlyphs.Default"/>.</summary>
    ScrollBarGlyphs ScrollBar => ScrollBarGlyphs.Default;
    #endregion

    #region Helpers
    /// <summary>The cell width of the widest of two state glyphs (both states of a toggle should be equal width;
    /// this guards against a theme that isn't).</summary>
    static int CellWidth(string a, string b) => System.Math.Max(a.GetCellWidth(), b.GetCellWidth());
    #endregion
}

/// <summary>The built-in glyph theme: every glyph uses <see cref="IGlyphTheme"/>'s default values.</summary>
public sealed class DefaultGlyphTheme : IGlyphTheme;
