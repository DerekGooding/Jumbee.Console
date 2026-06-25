namespace Jumbee.Console;

/// <summary>
/// A set of semantic <see cref="Style"/> tokens (foreground + background + decoration) that controls compose
/// when resolving their default appearance. A theme defines <em>appearance only</em>; it never changes a
/// control's behaviour. Members are default-implemented, so a custom theme overrides only the tokens it wants
/// to change. Because the members are default interface implementations, hold and read a theme through this
/// interface type (e.g. <see cref="UI.StyleTheme"/>), not through a concrete class.
/// </summary>
/// <remarks>
/// Controls must read these tokens <em>once</em> (in their constructor) into plain fields — never on the
/// render path — so theming costs nothing per frame. See <see cref="DefaultStyleTheme"/> for the built-in values.
/// </remarks>
public interface IStyleTheme
{
    #region Text
    /// <summary>Primary body/label text.</summary>
    Style Text => Style.Grey93;

    /// <summary>Secondary, de-emphasised text.</summary>
    Style TextMuted => Style.Grey58 | Style.Dim;

    /// <summary>Accent-coloured text/marks (a checked box's tick, a selected radio's dot, a switch's "on" thumb).</summary>
    Style TextAccent => Style.Green1;

    /// <summary>Disabled text/controls.</summary>
    Style TextDisabled => Style.Grey42 | Style.Dim;
    #endregion

    #region Structural
    /// <summary>A panel/container fill.</summary>
    Style Surface => Style.Bg(new Color(20, 20, 28));

    /// <summary>A frame border at rest.</summary>
    Style Border => Style.Grey50;

    /// <summary>A frame border when its control is focused.</summary>
    Style BorderFocused => Style.Cyan1;

    /// <summary>A frame title.</summary>
    Style Title => Style.Grey85;
    #endregion

    #region Interactive states
    /// <summary>A selected/highlighted row (foreground + background).</summary>
    Style Selection => Style.White | Style.Bg(new Color(40, 50, 80));

    /// <summary>A row/control under the pointer (background tint).</summary>
    Style Hover => Style.Bg(new Color(45, 45, 60));

    /// <summary>A primary action surface at rest (e.g. a default button).</summary>
    Style Primary => Style.White | Style.Bg(new Color(40, 70, 120));

    /// <summary>A primary action surface under the pointer.</summary>
    Style PrimaryHover => Style.White | Style.Bg(new Color(60, 90, 150));

    /// <summary>A primary action surface while pressed/active.</summary>
    Style PrimaryActive => Style.White | Style.Bg(new Color(90, 130, 200));
    #endregion

    #region Status
    Style Success => Style.Green1;
    Style Warning => Style.Yellow1;
    Style Error => Style.Red1;
    Style Info => Style.SkyBlue1;
    #endregion

    #region Scrollbar
    /// <summary>The per-part colours/decoration a control frame's vertical scrollbar uses (glyphs come from
    /// <see cref="IGlyphTheme.ScrollBar"/>). Defaults to <see cref="ScrollBarStyle.Default"/>.</summary>
    ScrollBarStyle ScrollBar => ScrollBarStyle.Default;
    #endregion
}

/// <summary>The built-in style theme: every token uses <see cref="IStyleTheme"/>'s default values.</summary>
public sealed class DefaultStyleTheme : IStyleTheme;
