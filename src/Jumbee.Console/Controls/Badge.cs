namespace Jumbee.Console;

using System;
using System.Collections.Generic;

using Spectre.Console.Rendering;

/// <summary>Preset colour schemes for a <see cref="Badge"/>, resolved from the active theme.</summary>
public enum BadgeVariant
{
    /// <summary>The neutral default scheme.</summary>
    Default,
    /// <summary>The theme's primary accent scheme.</summary>
    Primary,
    /// <summary>The theme's secondary accent scheme.</summary>
    Secondary,
    /// <summary>A green "success" scheme.</summary>
    Success,
    /// <summary>A yellow "warning" scheme.</summary>
    Warning,
    /// <summary>A red "error" scheme.</summary>
    Error,
}

/// <summary>
/// A small inline status pill — short text on a filled background with a little horizontal padding (e.g.
/// <c>200 OK</c>, <c>read-only</c>, a method tag).
/// </summary>
/// <remarks>
/// Non-interactive and fixed-width (sizes to its text + padding). Use a <see cref="BadgeVariant"/> for a themed
/// scheme, or pass an explicit <see cref="Style"/>.
/// </remarks>
public class Badge : RenderableControl
{
    #region Constructors
    /// <summary>Initializes a new <see cref="Badge"/> with the given text and themed <see cref="BadgeVariant"/>.</summary>
    public Badge(string text, BadgeVariant variant = BadgeVariant.Default)
    {
        Focusable = false;
        _text = text ?? string.Empty;
        _variant = variant;
        ApplyTheme();
        Width = DisplayWidth();
    }

    /// <summary>Creates a badge with an explicit style (overrides the themed variant).</summary>
    public Badge(string text, Style style) : this(text)
    {
        Style = style;
    }
    #endregion

    #region Properties
    /// <summary>The badge's label text.</summary>
    public string Text
    {
        get => _text;
        set => SetAtomicProperty(ref _text, value ?? string.Empty, updatesLayout: true, watch: (_, _) => Width = DisplayWidth());
    }

    /// <summary>Spaces added on each side of the text. Defaults to 1.</summary>
    public int Padding
    {
        get => _padding;
        set => SetAtomicProperty(ref _padding, Math.Max(0, value), updatesLayout: true, watch: (_, _) => Width = DisplayWidth());
    }

    /// <summary>The themed colour scheme; ignored once <see cref="Style"/> is set explicitly.</summary>
    public BadgeVariant Variant
    {
        get => _variant;
        set => SetAtomicProperty(ref _variant, value, watch: (_, _) => ApplyTheme());
    }

    /// <summary>The fill/text style. Defaults to the <see cref="Variant"/>'s themed scheme.</summary>
    public Style Style
    {
        get => _style;
        set => SetAtomicProperty(ref _style, value, themeOverride: true);
    }
    #endregion

    #region Methods
    /// <inheritdoc/>
    protected override bool RendersInteractiveState => false;

    /// <inheritdoc/>
    protected override void ApplyTheme()
    {
        if (!IsThemeOverridden(nameof(Style))) _style = Resolve(_variant);
    }

    /// <inheritdoc/>
    protected override int IntrinsicHeight() => 1;
    /// <inheritdoc/>
    protected override int IntrinsicWidth() => DisplayWidth();

    private int DisplayWidth() => _text.Length + (2 * _padding);

    /// <inheritdoc/>
    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        if (maxWidth <= 0) yield break;
        var pad = new string(' ', _padding);
        var display = pad + _text + pad;
        if (display.Length > maxWidth) display = display.Substring(0, maxWidth);
        yield return new Segment(display, _style.SpectreConsoleStyle);
    }

    private static Style Resolve(BadgeVariant variant) => variant switch
    {
        BadgeVariant.Primary => UI.StyleTheme.Primary,
        BadgeVariant.Secondary => UI.StyleTheme.Secondary,
        BadgeVariant.Success => Filled(UI.StyleTheme.Success),
        BadgeVariant.Warning => Filled(UI.StyleTheme.Warning),
        BadgeVariant.Error => Filled(UI.StyleTheme.Error),
        _ => UI.StyleTheme.Secondary,
    };

    // The Success/Warning/Error theme tokens are foreground colours; a badge wants them as a fill, so move the
    // colour to the background and draw dark text on top.
    private static Style Filled(Style foregroundStyle)
        => foregroundStyle.ForegroundColor is { } color ? Style.Black | Style.Bg(color) : foregroundStyle;
    #endregion

    #region Fields
    private string _text;
    private int _padding = 1;
    private BadgeVariant _variant;
    private Style _style;
    #endregion
}
