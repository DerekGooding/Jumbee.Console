namespace Jumbee.Console;

/// <summary>
/// A complete theme bundling an <see cref="IStyleTheme"/> and an <see cref="IGlyphTheme"/>, for callers that want
/// to customise both colours/styles and glyphs as one unit and apply them together (via <c>UI.SetTheme(ITheme)</c>).
/// </summary>
/// <remarks>Both halves are default-implemented, so a theme may supply only the side it cares about.</remarks>
public interface ITheme
{
    /// <summary>The style half (colours/decoration plus the non-glyph selectors). Defaults to <see cref="DefaultStyleTheme"/>.</summary>
    IStyleTheme Styles => new DefaultStyleTheme();

    /// <summary>The glyph half. Defaults to <see cref="DefaultGlyphTheme"/>.</summary>
    IGlyphTheme Glyphs => new DefaultGlyphTheme();
}
