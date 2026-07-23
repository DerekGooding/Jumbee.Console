namespace Jumbee.Console;

/// <summary>The overall shape of a <c>Button</c>.</summary>
public enum ButtonShape
{
    /// <summary>A single-row text button. The simple default.</summary>
    Flat,

    /// <summary>A modern, solid 3-row tile with a lighter top edge and darker bottom edge (a raised bevel). The bevel colours are derived from the fill unless set explicitly.</summary>
    Modern
}

/// <summary>
/// The appearance of a <c>Button</c>: its fill <see cref="Style"/> in each interaction state (text colour +
/// background), its <see cref="Shape"/>, an optional fixed/minimum width, and whether the label
/// is bold.
/// </summary>
/// <remarks>
/// A button's default style comes from <see cref="IStyleTheme.PrimaryButton"/> /
/// <see cref="IStyleTheme.SecondaryButton"/>; <see cref="Primary"/>/<see cref="Secondary"/> here are the
/// theme-independent fallbacks those tokens default to.
/// </remarks>
/// <remarks>Initializes a new <see cref="ButtonStyle"/> from the per-state fills, shape, bevel colours, bold flag, and width constraints.</remarks>
public readonly struct ButtonStyle(Style normal, Style hover, Style press,
    ButtonShape shape = ButtonShape.Flat,
    Color? bevelLight = null,
    Color? bevelDark = null,
    bool bold = true,
    int width = 0,
    int minWidth = 0) : IEquatable<ButtonStyle>
{


    #region Properties

    /// <summary>Fill (foreground text colour + background) at rest.</summary>
    public Style Normal { get; init; } = normal;

    /// <summary>Fill while the pointer is over the button.</summary>
    public Style Hover { get; init; } = hover;

    /// <summary>Fill while the button is pressed/activated.</summary>
    public Style Press { get; init; } = press;

    /// <summary>The button's shape. Defaults to <see cref="ButtonShape.Flat"/> (a simple single-row button);
    /// use <see cref="ButtonShape.Modern"/> for the raised 3-row look. Changing it re-lays the button out.</summary>
    public ButtonShape Shape { get; init; } = shape;

    /// <summary>The bevel's top-edge highlight (<see cref="ButtonShape.Modern"/>), or <see langword="null"/> to
    /// derive it by lightening the fill background.</summary>
    public Color? BevelLight { get; init; } = bevelLight;

    /// <summary>The bevel's bottom-edge shadow (<see cref="ButtonShape.Modern"/>), or <see langword="null"/> to
    /// derive it by darkening the fill background.</summary>
    public Color? BevelDark { get; init; } = bevelDark;

    /// <summary>Whether the label is drawn bold. Defaults to <see langword="true"/>.</summary>
    public bool Bold { get; init; } = bold;

    /// <summary>A fixed outer width in cells, or 0 to size to the label (subject to <see cref="MinWidth"/>).</summary>
    public int Width { get; init; } = width;

    /// <summary>A minimum outer width in cells (so short labels still read as buttons), or 0 for none.</summary>
    public int MinWidth { get; init; } = minWidth;

    /// <summary><see langword="true"/> for the 3-row raised <see cref="ButtonShape.Modern"/> shape.</summary>
    public bool IsModern => Shape == ButtonShape.Modern;

    #endregion Properties

    #region Methods

    /// <summary>Returns a copy with a fixed outer <paramref name="width"/> (0 = size to the label).</summary>
    public ButtonStyle WithWidth(int width) => this with { Width = width };

    /// <summary>Returns a copy with a different shape (e.g. opting into <see cref="ButtonShape.Modern"/>).</summary>
    public ButtonStyle WithShape(ButtonShape shape) => this with { Shape = shape };

    /// <summary>Returns a copy with one or more per-state fills overridden; a <see langword="null"/> argument leaves
    /// that state unchanged.</summary>
    public ButtonStyle WithColors(Style? normal = null, Style? hover = null, Style? press = null) =>
        this with { Normal = normal ?? Normal, Hover = hover ?? Hover, Press = press ?? Press };

    // Value equality, implemented rather than left to the runtime. A struct with reference-typed fields (Style wraps
    // a class) can't be compared bitwise, so the default ValueType.Equals compares it FIELD BY FIELD VIA REFLECTION,
    // boxing as it goes — and Button.Style's setter runs that on every assignment, via SetAtomicProperty's
    // EqualityComparer<T>.Default. Measured on EQUAL values (the usual answer to "did it change?", and the worst
    // case since nothing can early-out): 368ns and 672 bytes per comparison, versus 8.8ns and nothing with this.
    // See Color for the same fix.
    /// <summary>Determines whether this <see cref="ButtonStyle"/> equals <paramref name="other"/>.</summary>
    public bool Equals(ButtonStyle other) =>
        Normal == other.Normal && Hover == other.Hover && Press == other.Press && Shape == other.Shape
        && System.Nullable.Equals(BevelLight, other.BevelLight) && System.Nullable.Equals(BevelDark, other.BevelDark)
        && Bold == other.Bold && Width == other.Width && MinWidth == other.MinWidth;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ButtonStyle other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Normal);
        hash.Add(Hover);
        hash.Add(Press);
        hash.Add(Shape);
        hash.Add(BevelLight);
        hash.Add(BevelDark);
        hash.Add(Bold);
        hash.Add(Width);
        hash.Add(MinWidth);
        return hash.ToHashCode();
    }

    /// <summary>Equality operator.</summary>
    public static bool operator ==(ButtonStyle a, ButtonStyle b) => a.Equals(b);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(ButtonStyle a, ButtonStyle b) => !a.Equals(b);

    #endregion Methods

    #region Presets

    /// <summary>A primary action button: white text on a blue fill (flat by default; use
    /// <see cref="ButtonShape.Modern"/> for the raised look).</summary>
    public static ButtonStyle Primary { get; } = new(
        normal: Style.White | Style.Bg(new Color(40, 70, 120)),
        hover: Style.White | Style.Bg(new Color(60, 90, 150)),
        press: Style.White | Style.Bg(new Color(90, 130, 200)),
        minWidth: 16);

    /// <summary>A secondary action button: light text on a neutral grey fill.</summary>
    public static ButtonStyle Secondary { get; } = new(
        normal: Style.Grey93 | Style.Bg(new Color(55, 55, 65)),
        hover: Style.White | Style.Bg(new Color(75, 75, 88)),
        press: Style.White | Style.Bg(new Color(100, 100, 115)),
        minWidth: 16);

    #endregion Presets
}