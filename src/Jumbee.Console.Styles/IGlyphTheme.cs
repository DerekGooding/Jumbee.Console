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

    #region Selection
    /// <summary>The glyph prefixed to the selected item when a control's <see cref="SelectionStyle"/> is
    /// <see cref="SelectionStyle.Caret"/> (includes its trailing spacing). Defaults to <c>"▶ "</c>.</summary>
    string SelectionCaret => "▶ ";
    #endregion

    #region Tabs
    /// <summary>The close glyph shown on a closable tab. Drawn only on the active/hovered tab; other tabs reserve
    /// a same-width blank. Defaults to <c>"✕"</c> (override with <c>"x"</c> for an ASCII terminal).</summary>
    string TabClose => "✕";

    /// <summary>The glyph on a tab panel's "+" new-tab button. Defaults to <c>"+"</c>.</summary>
    string TabAdd => "+";
    #endregion

    #region Tree
    /// <summary>Disclosure glyph shown before an <em>expanded</em> node that has children (includes trailing
    /// spacing). Defaults to <c>"▼ "</c>. Both tree glyphs should share a cell width so labels stay aligned.</summary>
    string TreeExpanded => "▼ ";
    /// <summary>Disclosure glyph shown before a <em>collapsed</em> node that has children (includes trailing
    /// spacing). Defaults to <c>"► "</c> (U+25BA — the text-presentation counterpart of <c>▼</c>; the emoji-variant
    /// <c>▶</c> U+25B6 tofus in some fonts).</summary>
    string TreeCollapsed => "► ";

    /// <summary>Glyph shown before a node that has <em>no</em> children (a leaf), including trailing spacing. Should
    /// share a cell width with the disclosure glyphs so labels stay aligned. Defaults to <c>"• "</c>.</summary>
    string TreeLeaf => "• ";
    #endregion

    #region Helpers
    /// <summary>The cell width of the widest of two state glyphs (both states of a toggle should be equal width;
    /// this guards against a theme that isn't).</summary>
    static int CellWidth(string a, string b) => System.Math.Max(a.GetCellWidth(), b.GetCellWidth());
    #endregion
}

/// <summary>The built-in glyph theme: every glyph uses <see cref="IGlyphTheme"/>'s default values.</summary>
public sealed class DefaultGlyphTheme : IGlyphTheme;
