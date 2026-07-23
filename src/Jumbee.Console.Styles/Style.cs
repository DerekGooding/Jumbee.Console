namespace Jumbee.Console;

using SCDecoration = Spectre.Console.Decoration;
using SCStyle = Spectre.Console.Style;
using SystemDrawingColor = System.Drawing.Color;

/// <summary>Horizontal alignment of text within its available width.</summary>
public enum Justify
{
    /// <summary>Align to the left edge.</summary>
    Left,

    /// <summary>Centre within the available width.</summary>
    Center,

    /// <summary>Align to the right edge.</summary>
    Right
}

/// <summary>Whether a chart is laid out horizontally or vertically.</summary>
public enum ChartOrientation
{
    /// <summary>Bars/series run left-to-right.</summary>
    Horizontal,

    /// <summary>Bars/series run top-to-bottom.</summary>
    Vertical,
}

/// <summary>
/// Terminal cursor shapes/blink, mapping to DECSCUSR (CSI Ps SP q) values. <see cref="Default"/> (0) leaves
/// the terminal's configured cursor.
/// </summary>
public enum CursorStyle
{
    /// <summary>Leave the terminal's configured cursor unchanged (DECSCUSR 0).</summary>
    Default = 0,

    /// <summary>A blinking block cursor (DECSCUSR 1).</summary>
    BlinkingBlock = 1,

    /// <summary>A steady block cursor (DECSCUSR 2).</summary>
    SteadyBlock = 2,

    /// <summary>A blinking underline cursor (DECSCUSR 3).</summary>
    BlinkingUnderline = 3,

    /// <summary>A steady underline cursor (DECSCUSR 4).</summary>
    SteadyUnderline = 4,

    /// <summary>A blinking vertical-bar cursor (DECSCUSR 5).</summary>
    BlinkingBar = 5,

    /// <summary>A steady vertical-bar cursor (DECSCUSR 6).</summary>
    SteadyBar = 6,
}

/// <summary>
/// A text style — foreground/background colour and text decoration — wrapping a Spectre.Console style. Exposes the
/// named colour palette (<see cref="Red1"/>, <see cref="Cyan1"/>, …) and decoration presets (<see cref="Bold"/>,
/// <see cref="Italic"/>, …) as ready-made tokens; combine them with <c>|</c>.
/// </summary>
public readonly partial struct Style : System.IEquatable<Style>
{
    #region Constructors

    /// <summary>Wraps an existing Spectre.Console <see cref="SCStyle"/>.</summary>
    public Style(SCStyle spectreConsoleStyle)
    {
        SpectreConsoleStyle = spectreConsoleStyle;
    }

    /// <summary>Parses a style from a Spectre markup style string (e.g. <c>"bold red on blue"</c>).</summary>
    public Style(string style) : this(SCStyle.Parse(style)) { }

    #endregion Constructors

    #region Indexers

    //public string this[string text] => $"[{this.ToMarkup()}]{EscapeMarkup(text)}[/]";

    /// <summary>Wraps <paramref name="text"/> in this style's markup, producing a Spectre <see cref="Spectre.Console.Markup"/>
    /// renderable (the text is markup-escaped first).</summary>
    public Spectre.Console.Markup this[string text] => new Spectre.Console.Markup($"[{this.ToMarkup()}]{EscapeMarkup(text)}[/]");

    #endregion Indexers

    #region Methods

    /// <summary>Escapes Spectre markup control characters in <paramref name="text"/> so it renders literally.</summary>
    public static string EscapeMarkup(string text) => Spectre.Console.Markup.Escape(text);

    /// <summary>A style carrying only a background colour, for composing with a foreground via <c>|</c>
    /// (e.g. <c>Style.White | Style.Bg(color)</c>).</summary>
    public static Style Bg(Color background) => new Style(new SCStyle(background: background));

    /// <summary>The foreground colour, or <see langword="null"/> when the style leaves it as the terminal default.</summary>
    public Color? ForegroundColor => ColorOrNull(SpectreConsoleStyle?.Foreground);

    /// <summary>The background colour, or <see langword="null"/> when the style leaves it as the terminal default.</summary>
    public Color? BackgroundColor => ColorOrNull(SpectreConsoleStyle?.Background);

    private static Color? ColorOrNull(Spectre.Console.Color? color) =>
        color is { } c && c != Spectre.Console.Color.Default ? Color.FromSpectreColor(c) : null;

    /// <summary>Returns the Spectre markup representation of this style (e.g. <c>"bold red"</c>).</summary>
    public string ToMarkup() => SpectreConsoleStyle.ToMarkup();

    /// <summary>Determines whether this style equals <paramref name="other"/> (compared by the wrapped Spectre style).</summary>
    // Value equality delegates to the wrapped Spectre Style (which is IEquatable), so SetAtomicProperty and
    // other equality checks on Style tokens short-circuit correctly instead of comparing by reference.
    public bool Equals(Style other) => Equals(SpectreConsoleStyle, other.SpectreConsoleStyle);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Style s && Equals(s);

    /// <inheritdoc/>
    public override int GetHashCode() => SpectreConsoleStyle?.GetHashCode() ?? 0;

    /// <summary>Equality operator; see <see cref="Equals(Style)"/>.</summary>
    public static bool operator ==(Style a, Style b) => a.Equals(b);

    /// <summary>Inequality operator; see <see cref="Equals(Style)"/>.</summary>
    public static bool operator !=(Style a, Style b) => !a.Equals(b);

    #endregion Methods

    #region Operators

    /// <summary>Unwraps the underlying Spectre.Console <see cref="SCStyle"/>.</summary>
    public static implicit operator SCStyle(Style style) => style.SpectreConsoleStyle;

    /// <summary>Wraps a Spectre.Console <see cref="SCStyle"/> as a <see cref="Style"/>.</summary>
    public static implicit operator Style(SCStyle spectreConsoleStyle) => new Style(spectreConsoleStyle);

    /// <summary>Converts the style to its Spectre markup string (see <see cref="ToMarkup"/>).</summary>
    public static implicit operator string(Style style) => style.ToMarkup();

    /// <summary>Builds a foreground-only style from a <see cref="Color"/>.</summary>
    public static implicit operator Style(Color color) => new Style(new SCStyle(color));

    /// <summary>Extracts the style's foreground <see cref="Color"/>.</summary>
    public static implicit operator Color(Style style) => style.SpectreConsoleStyle.Foreground;

    /// <summary>Combines two styles; properties set on the right operand override the left.</summary>
    public static Style operator |(Style a, Style b) => new Style(a.SpectreConsoleStyle.Combine(b.SpectreConsoleStyle));

    /// <summary>Converts the style's foreground colour to a <see cref="System.Drawing.Color"/>.</summary>
    public static implicit operator SystemDrawingColor(Style style)
    {
        var scColor = style.SpectreConsoleStyle.Foreground;
        return SystemDrawingColor.FromArgb(scColor.R, scColor.G, scColor.B);
    }

    #endregion Operators

    #region Fields

    /// <summary>The wrapped Spectre.Console style this <see cref="Style"/> delegates to.</summary>
    public readonly SCStyle SpectreConsoleStyle;

    #region Text decorations

    /// <summary>A plain style with no colour or text decoration.</summary>
    public static readonly Style Plain = SCStyle.Plain;

    /// <summary>The <c>Bold</c> text decoration.</summary>
    public static readonly Style Bold = new SCStyle(decoration: SCDecoration.Bold);

    /// <summary>The <c>Dim</c> text decoration.</summary>
    public static readonly Style Dim = new SCStyle(decoration: SCDecoration.Dim);

    /// <summary>The <c>Italic</c> text decoration.</summary>
    public static readonly Style Italic = new SCStyle(decoration: SCDecoration.Italic);

    /// <summary>The <c>Underline</c> text decoration.</summary>
    public static readonly Style Underline = new SCStyle(decoration: SCDecoration.Underline);

    /// <summary>The <c>Invert</c> text decoration.</summary>
    public static readonly Style Invert = new SCStyle(decoration: SCDecoration.Invert);

    /// <summary>The <c>Conceal</c> text decoration.</summary>
    public static readonly Style Conceal = new SCStyle(decoration: SCDecoration.Conceal);

    /// <summary>The <c>SlowBlink</c> text decoration.</summary>
    public static readonly Style SlowBlink = new SCStyle(decoration: SCDecoration.SlowBlink);

    /// <summary>The <c>RapidBlink</c> text decoration.</summary>
    public static readonly Style RapidBlink = new SCStyle(decoration: SCDecoration.RapidBlink);

    /// <summary>The <c>Strikethrough</c> text decoration.</summary>
    public static readonly Style Strikethrough = new SCStyle(decoration: SCDecoration.Strikethrough);

    #endregion Text decorations

    #region Colors

    /// <summary>A foreground <see cref="Style"/> in the "Black" colour (<see cref="Color.Black"/>).</summary>
    public static readonly Style Black = Color.Black;

    /// <summary>A foreground <see cref="Style"/> in the "Maroon" colour (<see cref="Color.Maroon"/>).</summary>
    public static readonly Style Maroon = Color.Maroon;

    /// <summary>A foreground <see cref="Style"/> in the "Green" colour (<see cref="Color.Green"/>).</summary>
    public static readonly Style Green = Color.Green;

    /// <summary>A foreground <see cref="Style"/> in the "Olive" colour (<see cref="Color.Olive"/>).</summary>
    public static readonly Style Olive = Color.Olive;

    /// <summary>A foreground <see cref="Style"/> in the "Navy" colour (<see cref="Color.Navy"/>).</summary>
    public static readonly Style Navy = Color.Navy;

    /// <summary>A foreground <see cref="Style"/> in the "Purple" colour (<see cref="Color.Purple"/>).</summary>
    public static readonly Style Purple = Color.Purple;

    /// <summary>A foreground <see cref="Style"/> in the "Teal" colour (<see cref="Color.Teal"/>).</summary>
    public static readonly Style Teal = Color.Teal;

    /// <summary>A foreground <see cref="Style"/> in the "Silver" colour (<see cref="Color.Silver"/>).</summary>
    public static readonly Style Silver = Color.Silver;

    /// <summary>A foreground <see cref="Style"/> in the "Grey" colour (<see cref="Color.Grey"/>).</summary>
    public static readonly Style Grey = Color.Grey;

    /// <summary>A foreground <see cref="Style"/> in the "Red" colour (<see cref="Color.Red"/>).</summary>
    public static readonly Style Red = Color.Red;

    /// <summary>A foreground <see cref="Style"/> in the "Lime" colour (<see cref="Color.Lime"/>).</summary>
    public static readonly Style Lime = Color.Lime;

    /// <summary>A foreground <see cref="Style"/> in the "Yellow" colour (<see cref="Color.Yellow"/>).</summary>
    public static readonly Style Yellow = Color.Yellow;

    /// <summary>A foreground <see cref="Style"/> in the "Blue" colour (<see cref="Color.Blue"/>).</summary>
    public static readonly Style Blue = Color.Blue;

    /// <summary>A foreground <see cref="Style"/> in the "Fuchsia" colour (<see cref="Color.Fuchsia"/>).</summary>
    public static readonly Style Fuchsia = Color.Fuchsia;

    /// <summary>A foreground <see cref="Style"/> in the "Aqua" colour (<see cref="Color.Aqua"/>).</summary>
    public static readonly Style Aqua = Color.Aqua;

    /// <summary>A foreground <see cref="Style"/> in the "White" colour (<see cref="Color.White"/>).</summary>
    public static readonly Style White = Color.White;

    /// <summary>A foreground <see cref="Style"/> in the "Grey0" colour (<see cref="Color.Grey0"/>).</summary>
    public static readonly Style Grey0 = Color.Grey0;

    /// <summary>A foreground <see cref="Style"/> in the "NavyBlue" colour (<see cref="Color.NavyBlue"/>).</summary>
    public static readonly Style NavyBlue = Color.NavyBlue;

    /// <summary>A foreground <see cref="Style"/> in the "DarkBlue" colour (<see cref="Color.DarkBlue"/>).</summary>
    public static readonly Style DarkBlue = Color.DarkBlue;

    /// <summary>A foreground <see cref="Style"/> in the "Blue3" colour (<see cref="Color.Blue3"/>).</summary>
    public static readonly Style Blue3 = Color.Blue3;

    /// <summary>A foreground <see cref="Style"/> in the "Blue3_1" colour (<see cref="Color.Blue3_1"/>).</summary>
    public static readonly Style Blue3_1 = Color.Blue3_1;

    /// <summary>A foreground <see cref="Style"/> in the "Blue1" colour (<see cref="Color.Blue1"/>).</summary>
    public static readonly Style Blue1 = Color.Blue1;

    /// <summary>A foreground <see cref="Style"/> in the "DarkGreen" colour (<see cref="Color.DarkGreen"/>).</summary>
    public static readonly Style DarkGreen = Color.DarkGreen;

    /// <summary>A foreground <see cref="Style"/> in the "DeepSkyBlue4" colour (<see cref="Color.DeepSkyBlue4"/>).</summary>
    public static readonly Style DeepSkyBlue4 = Color.DeepSkyBlue4;

    /// <summary>A foreground <see cref="Style"/> in the "DeepSkyBlue4_1" colour (<see cref="Color.DeepSkyBlue4_1"/>).</summary>
    public static readonly Style DeepSkyBlue4_1 = Color.DeepSkyBlue4_1;

    /// <summary>A foreground <see cref="Style"/> in the "DeepSkyBlue4_2" colour (<see cref="Color.DeepSkyBlue4_2"/>).</summary>
    public static readonly Style DeepSkyBlue4_2 = Color.DeepSkyBlue4_2;

    /// <summary>A foreground <see cref="Style"/> in the "DodgerBlue3" colour (<see cref="Color.DodgerBlue3"/>).</summary>
    public static readonly Style DodgerBlue3 = Color.DodgerBlue3;

    /// <summary>A foreground <see cref="Style"/> in the "DodgerBlue2" colour (<see cref="Color.DodgerBlue2"/>).</summary>
    public static readonly Style DodgerBlue2 = Color.DodgerBlue2;

    /// <summary>A foreground <see cref="Style"/> in the "Green4" colour (<see cref="Color.Green4"/>).</summary>
    public static readonly Style Green4 = Color.Green4;

    /// <summary>A foreground <see cref="Style"/> in the "SpringGreen4" colour (<see cref="Color.SpringGreen4"/>).</summary>
    public static readonly Style SpringGreen4 = Color.SpringGreen4;

    /// <summary>A foreground <see cref="Style"/> in the "Turquoise4" colour (<see cref="Color.Turquoise4"/>).</summary>
    public static readonly Style Turquoise4 = Color.Turquoise4;

    /// <summary>A foreground <see cref="Style"/> in the "DeepSkyBlue3" colour (<see cref="Color.DeepSkyBlue3"/>).</summary>
    public static readonly Style DeepSkyBlue3 = Color.DeepSkyBlue3;

    /// <summary>A foreground <see cref="Style"/> in the "DeepSkyBlue3_1" colour (<see cref="Color.DeepSkyBlue3_1"/>).</summary>
    public static readonly Style DeepSkyBlue3_1 = Color.DeepSkyBlue3_1;

    /// <summary>A foreground <see cref="Style"/> in the "DodgerBlue1" colour (<see cref="Color.DodgerBlue1"/>).</summary>
    public static readonly Style DodgerBlue1 = Color.DodgerBlue1;

    /// <summary>A foreground <see cref="Style"/> in the "Green3" colour (<see cref="Color.Green3"/>).</summary>
    public static readonly Style Green3 = Color.Green3;

    /// <summary>A foreground <see cref="Style"/> in the "SpringGreen3" colour (<see cref="Color.SpringGreen3"/>).</summary>
    public static readonly Style SpringGreen3 = Color.SpringGreen3;

    /// <summary>A foreground <see cref="Style"/> in the "DarkCyan" colour (<see cref="Color.DarkCyan"/>).</summary>
    public static readonly Style DarkCyan = Color.DarkCyan;

    /// <summary>A foreground <see cref="Style"/> in the "LightSeaGreen" colour (<see cref="Color.LightSeaGreen"/>).</summary>
    public static readonly Style LightSeaGreen = Color.LightSeaGreen;

    /// <summary>A foreground <see cref="Style"/> in the "DeepSkyBlue2" colour (<see cref="Color.DeepSkyBlue2"/>).</summary>
    public static readonly Style DeepSkyBlue2 = Color.DeepSkyBlue2;

    /// <summary>A foreground <see cref="Style"/> in the "DeepSkyBlue1" colour (<see cref="Color.DeepSkyBlue1"/>).</summary>
    public static readonly Style DeepSkyBlue1 = Color.DeepSkyBlue1;

    /// <summary>A foreground <see cref="Style"/> in the "Green3_1" colour (<see cref="Color.Green3_1"/>).</summary>
    public static readonly Style Green3_1 = Color.Green3_1;

    /// <summary>A foreground <see cref="Style"/> in the "SpringGreen3_1" colour (<see cref="Color.SpringGreen3_1"/>).</summary>
    public static readonly Style SpringGreen3_1 = Color.SpringGreen3_1;

    /// <summary>A foreground <see cref="Style"/> in the "SpringGreen2" colour (<see cref="Color.SpringGreen2"/>).</summary>
    public static readonly Style SpringGreen2 = Color.SpringGreen2;

    /// <summary>A foreground <see cref="Style"/> in the "Cyan3" colour (<see cref="Color.Cyan3"/>).</summary>
    public static readonly Style Cyan3 = Color.Cyan3;

    /// <summary>A foreground <see cref="Style"/> in the "DarkTurquoise" colour (<see cref="Color.DarkTurquoise"/>).</summary>
    public static readonly Style DarkTurquoise = Color.DarkTurquoise;

    /// <summary>A foreground <see cref="Style"/> in the "Turquoise2" colour (<see cref="Color.Turquoise2"/>).</summary>
    public static readonly Style Turquoise2 = Color.Turquoise2;

    /// <summary>A foreground <see cref="Style"/> in the "Green1" colour (<see cref="Color.Green1"/>).</summary>
    public static readonly Style Green1 = Color.Green1;

    /// <summary>A foreground <see cref="Style"/> in the "SpringGreen2_1" colour (<see cref="Color.SpringGreen2_1"/>).</summary>
    public static readonly Style SpringGreen2_1 = Color.SpringGreen2_1;

    /// <summary>A foreground <see cref="Style"/> in the "SpringGreen1" colour (<see cref="Color.SpringGreen1"/>).</summary>
    public static readonly Style SpringGreen1 = Color.SpringGreen1;

    /// <summary>A foreground <see cref="Style"/> in the "MediumSpringGreen" colour (<see cref="Color.MediumSpringGreen"/>).</summary>
    public static readonly Style MediumSpringGreen = Color.MediumSpringGreen;

    /// <summary>A foreground <see cref="Style"/> in the "Cyan2" colour (<see cref="Color.Cyan2"/>).</summary>
    public static readonly Style Cyan2 = Color.Cyan2;

    /// <summary>A foreground <see cref="Style"/> in the "Cyan1" colour (<see cref="Color.Cyan1"/>).</summary>
    public static readonly Style Cyan1 = Color.Cyan1;

    /// <summary>A foreground <see cref="Style"/> in the "DarkRed" colour (<see cref="Color.DarkRed"/>).</summary>
    public static readonly Style DarkRed = Color.DarkRed;

    /// <summary>A foreground <see cref="Style"/> in the "DeepPink4" colour (<see cref="Color.DeepPink4"/>).</summary>
    public static readonly Style DeepPink4 = Color.DeepPink4;

    /// <summary>A foreground <see cref="Style"/> in the "Purple4" colour (<see cref="Color.Purple4"/>).</summary>
    public static readonly Style Purple4 = Color.Purple4;

    /// <summary>A foreground <see cref="Style"/> in the "Purple4_1" colour (<see cref="Color.Purple4_1"/>).</summary>
    public static readonly Style Purple4_1 = Color.Purple4_1;

    /// <summary>A foreground <see cref="Style"/> in the "Purple3" colour (<see cref="Color.Purple3"/>).</summary>
    public static readonly Style Purple3 = Color.Purple3;

    /// <summary>A foreground <see cref="Style"/> in the "BlueViolet" colour (<see cref="Color.BlueViolet"/>).</summary>
    public static readonly Style BlueViolet = Color.BlueViolet;

    /// <summary>A foreground <see cref="Style"/> in the "Orange4" colour (<see cref="Color.Orange4"/>).</summary>
    public static readonly Style Orange4 = Color.Orange4;

    /// <summary>A foreground <see cref="Style"/> in the "Grey37" colour (<see cref="Color.Grey37"/>).</summary>
    public static readonly Style Grey37 = Color.Grey37;

    /// <summary>A foreground <see cref="Style"/> in the "MediumPurple4" colour (<see cref="Color.MediumPurple4"/>).</summary>
    public static readonly Style MediumPurple4 = Color.MediumPurple4;

    /// <summary>A foreground <see cref="Style"/> in the "SlateBlue3" colour (<see cref="Color.SlateBlue3"/>).</summary>
    public static readonly Style SlateBlue3 = Color.SlateBlue3;

    /// <summary>A foreground <see cref="Style"/> in the "SlateBlue3_1" colour (<see cref="Color.SlateBlue3_1"/>).</summary>
    public static readonly Style SlateBlue3_1 = Color.SlateBlue3_1;

    /// <summary>A foreground <see cref="Style"/> in the "RoyalBlue1" colour (<see cref="Color.RoyalBlue1"/>).</summary>
    public static readonly Style RoyalBlue1 = Color.RoyalBlue1;

    /// <summary>A foreground <see cref="Style"/> in the "Chartreuse4" colour (<see cref="Color.Chartreuse4"/>).</summary>
    public static readonly Style Chartreuse4 = Color.Chartreuse4;

    /// <summary>A foreground <see cref="Style"/> in the "DarkSeaGreen4" colour (<see cref="Color.DarkSeaGreen4"/>).</summary>
    public static readonly Style DarkSeaGreen4 = Color.DarkSeaGreen4;

    /// <summary>A foreground <see cref="Style"/> in the "PaleTurquoise4" colour (<see cref="Color.PaleTurquoise4"/>).</summary>
    public static readonly Style PaleTurquoise4 = Color.PaleTurquoise4;

    /// <summary>A foreground <see cref="Style"/> in the "SteelBlue" colour (<see cref="Color.SteelBlue"/>).</summary>
    public static readonly Style SteelBlue = Color.SteelBlue;

    /// <summary>A foreground <see cref="Style"/> in the "SteelBlue3" colour (<see cref="Color.SteelBlue3"/>).</summary>
    public static readonly Style SteelBlue3 = Color.SteelBlue3;

    /// <summary>A foreground <see cref="Style"/> in the "CornflowerBlue" colour (<see cref="Color.CornflowerBlue"/>).</summary>
    public static readonly Style CornflowerBlue = Color.CornflowerBlue;

    /// <summary>A foreground <see cref="Style"/> in the "Chartreuse3" colour (<see cref="Color.Chartreuse3"/>).</summary>
    public static readonly Style Chartreuse3 = Color.Chartreuse3;

    /// <summary>A foreground <see cref="Style"/> in the "DarkSeaGreen4_1" colour (<see cref="Color.DarkSeaGreen4_1"/>).</summary>
    public static readonly Style DarkSeaGreen4_1 = Color.DarkSeaGreen4_1;

    /// <summary>A foreground <see cref="Style"/> in the "CadetBlue" colour (<see cref="Color.CadetBlue"/>).</summary>
    public static readonly Style CadetBlue = Color.CadetBlue;

    /// <summary>A foreground <see cref="Style"/> in the "CadetBlue_1" colour (<see cref="Color.CadetBlue_1"/>).</summary>
    public static readonly Style CadetBlue_1 = Color.CadetBlue_1;

    /// <summary>A foreground <see cref="Style"/> in the "SkyBlue3" colour (<see cref="Color.SkyBlue3"/>).</summary>
    public static readonly Style SkyBlue3 = Color.SkyBlue3;

    /// <summary>A foreground <see cref="Style"/> in the "SteelBlue1" colour (<see cref="Color.SteelBlue1"/>).</summary>
    public static readonly Style SteelBlue1 = Color.SteelBlue1;

    /// <summary>A foreground <see cref="Style"/> in the "Chartreuse3_1" colour (<see cref="Color.Chartreuse3_1"/>).</summary>
    public static readonly Style Chartreuse3_1 = Color.Chartreuse3_1;

    /// <summary>A foreground <see cref="Style"/> in the "PaleGreen3" colour (<see cref="Color.PaleGreen3"/>).</summary>
    public static readonly Style PaleGreen3 = Color.PaleGreen3;

    /// <summary>A foreground <see cref="Style"/> in the "SeaGreen3" colour (<see cref="Color.SeaGreen3"/>).</summary>
    public static readonly Style SeaGreen3 = Color.SeaGreen3;

    /// <summary>A foreground <see cref="Style"/> in the "Aquamarine3" colour (<see cref="Color.Aquamarine3"/>).</summary>
    public static readonly Style Aquamarine3 = Color.Aquamarine3;

    /// <summary>A foreground <see cref="Style"/> in the "MediumTurquoise" colour (<see cref="Color.MediumTurquoise"/>).</summary>
    public static readonly Style MediumTurquoise = Color.MediumTurquoise;

    /// <summary>A foreground <see cref="Style"/> in the "SteelBlue1_1" colour (<see cref="Color.SteelBlue1_1"/>).</summary>
    public static readonly Style SteelBlue1_1 = Color.SteelBlue1_1;

    /// <summary>A foreground <see cref="Style"/> in the "Chartreuse2" colour (<see cref="Color.Chartreuse2"/>).</summary>
    public static readonly Style Chartreuse2 = Color.Chartreuse2;

    /// <summary>A foreground <see cref="Style"/> in the "SeaGreen2" colour (<see cref="Color.SeaGreen2"/>).</summary>
    public static readonly Style SeaGreen2 = Color.SeaGreen2;

    /// <summary>A foreground <see cref="Style"/> in the "SeaGreen1" colour (<see cref="Color.SeaGreen1"/>).</summary>
    public static readonly Style SeaGreen1 = Color.SeaGreen1;

    /// <summary>A foreground <see cref="Style"/> in the "SeaGreen1_1" colour (<see cref="Color.SeaGreen1_1"/>).</summary>
    public static readonly Style SeaGreen1_1 = Color.SeaGreen1_1;

    /// <summary>A foreground <see cref="Style"/> in the "Aquamarine1" colour (<see cref="Color.Aquamarine1"/>).</summary>
    public static readonly Style Aquamarine1 = Color.Aquamarine1;

    /// <summary>A foreground <see cref="Style"/> in the "DarkSlateGray2" colour (<see cref="Color.DarkSlateGray2"/>).</summary>
    public static readonly Style DarkSlateGray2 = Color.DarkSlateGray2;

    /// <summary>A foreground <see cref="Style"/> in the "DarkRed_1" colour (<see cref="Color.DarkRed_1"/>).</summary>
    public static readonly Style DarkRed_1 = Color.DarkRed_1;

    /// <summary>A foreground <see cref="Style"/> in the "DeepPink4_1" colour (<see cref="Color.DeepPink4_1"/>).</summary>
    public static readonly Style DeepPink4_1 = Color.DeepPink4_1;

    /// <summary>A foreground <see cref="Style"/> in the "DarkMagenta" colour (<see cref="Color.DarkMagenta"/>).</summary>
    public static readonly Style DarkMagenta = Color.DarkMagenta;

    /// <summary>A foreground <see cref="Style"/> in the "DarkMagenta_1" colour (<see cref="Color.DarkMagenta_1"/>).</summary>
    public static readonly Style DarkMagenta_1 = Color.DarkMagenta_1;

    /// <summary>A foreground <see cref="Style"/> in the "DarkViolet" colour (<see cref="Color.DarkViolet"/>).</summary>
    public static readonly Style DarkViolet = Color.DarkViolet;

    /// <summary>A foreground <see cref="Style"/> in the "Purple_1" colour (<see cref="Color.Purple_1"/>).</summary>
    public static readonly Style Purple_1 = Color.Purple_1;

    /// <summary>A foreground <see cref="Style"/> in the "Orange4_1" colour (<see cref="Color.Orange4_1"/>).</summary>
    public static readonly Style Orange4_1 = Color.Orange4_1;

    /// <summary>A foreground <see cref="Style"/> in the "LightPink4" colour (<see cref="Color.LightPink4"/>).</summary>
    public static readonly Style LightPink4 = Color.LightPink4;

    /// <summary>A foreground <see cref="Style"/> in the "Plum4" colour (<see cref="Color.Plum4"/>).</summary>
    public static readonly Style Plum4 = Color.Plum4;

    /// <summary>A foreground <see cref="Style"/> in the "MediumPurple3" colour (<see cref="Color.MediumPurple3"/>).</summary>
    public static readonly Style MediumPurple3 = Color.MediumPurple3;

    /// <summary>A foreground <see cref="Style"/> in the "MediumPurple3_1" colour (<see cref="Color.MediumPurple3_1"/>).</summary>
    public static readonly Style MediumPurple3_1 = Color.MediumPurple3_1;

    /// <summary>A foreground <see cref="Style"/> in the "SlateBlue1" colour (<see cref="Color.SlateBlue1"/>).</summary>
    public static readonly Style SlateBlue1 = Color.SlateBlue1;

    /// <summary>A foreground <see cref="Style"/> in the "Yellow4" colour (<see cref="Color.Yellow4"/>).</summary>
    public static readonly Style Yellow4 = Color.Yellow4;

    /// <summary>A foreground <see cref="Style"/> in the "Wheat4" colour (<see cref="Color.Wheat4"/>).</summary>
    public static readonly Style Wheat4 = Color.Wheat4;

    /// <summary>A foreground <see cref="Style"/> in the "Grey53" colour (<see cref="Color.Grey53"/>).</summary>
    public static readonly Style Grey53 = Color.Grey53;

    /// <summary>A foreground <see cref="Style"/> in the "LightSlateGrey" colour (<see cref="Color.LightSlateGrey"/>).</summary>
    public static readonly Style LightSlateGrey = Color.LightSlateGrey;

    /// <summary>A foreground <see cref="Style"/> in the "MediumPurple" colour (<see cref="Color.MediumPurple"/>).</summary>
    public static readonly Style MediumPurple = Color.MediumPurple;

    /// <summary>A foreground <see cref="Style"/> in the "LightSlateBlue" colour (<see cref="Color.LightSlateBlue"/>).</summary>
    public static readonly Style LightSlateBlue = Color.LightSlateBlue;

    /// <summary>A foreground <see cref="Style"/> in the "Yellow4_1" colour (<see cref="Color.Yellow4_1"/>).</summary>
    public static readonly Style Yellow4_1 = Color.Yellow4_1;

    /// <summary>A foreground <see cref="Style"/> in the "DarkOliveGreen3" colour (<see cref="Color.DarkOliveGreen3"/>).</summary>
    public static readonly Style DarkOliveGreen3 = Color.DarkOliveGreen3;

    /// <summary>A foreground <see cref="Style"/> in the "DarkSeaGreen" colour (<see cref="Color.DarkSeaGreen"/>).</summary>
    public static readonly Style DarkSeaGreen = Color.DarkSeaGreen;

    /// <summary>A foreground <see cref="Style"/> in the "LightSkyBlue3" colour (<see cref="Color.LightSkyBlue3"/>).</summary>
    public static readonly Style LightSkyBlue3 = Color.LightSkyBlue3;

    /// <summary>A foreground <see cref="Style"/> in the "LightSkyBlue3_1" colour (<see cref="Color.LightSkyBlue3_1"/>).</summary>
    public static readonly Style LightSkyBlue3_1 = Color.LightSkyBlue3_1;

    /// <summary>A foreground <see cref="Style"/> in the "SkyBlue2" colour (<see cref="Color.SkyBlue2"/>).</summary>
    public static readonly Style SkyBlue2 = Color.SkyBlue2;

    /// <summary>A foreground <see cref="Style"/> in the "Chartreuse2_1" colour (<see cref="Color.Chartreuse2_1"/>).</summary>
    public static readonly Style Chartreuse2_1 = Color.Chartreuse2_1;

    /// <summary>A foreground <see cref="Style"/> in the "DarkOliveGreen3_1" colour (<see cref="Color.DarkOliveGreen3_1"/>).</summary>
    public static readonly Style DarkOliveGreen3_1 = Color.DarkOliveGreen3_1;

    /// <summary>A foreground <see cref="Style"/> in the "PaleGreen3_1" colour (<see cref="Color.PaleGreen3_1"/>).</summary>
    public static readonly Style PaleGreen3_1 = Color.PaleGreen3_1;

    /// <summary>A foreground <see cref="Style"/> in the "DarkSeaGreen3" colour (<see cref="Color.DarkSeaGreen3"/>).</summary>
    public static readonly Style DarkSeaGreen3 = Color.DarkSeaGreen3;

    /// <summary>A foreground <see cref="Style"/> in the "DarkSlateGray3" colour (<see cref="Color.DarkSlateGray3"/>).</summary>
    public static readonly Style DarkSlateGray3 = Color.DarkSlateGray3;

    /// <summary>A foreground <see cref="Style"/> in the "SkyBlue1" colour (<see cref="Color.SkyBlue1"/>).</summary>
    public static readonly Style SkyBlue1 = Color.SkyBlue1;

    /// <summary>A foreground <see cref="Style"/> in the "Chartreuse1" colour (<see cref="Color.Chartreuse1"/>).</summary>
    public static readonly Style Chartreuse1 = Color.Chartreuse1;

    /// <summary>A foreground <see cref="Style"/> in the "LightGreen" colour (<see cref="Color.LightGreen"/>).</summary>
    public static readonly Style LightGreen = Color.LightGreen;

    /// <summary>A foreground <see cref="Style"/> in the "LightGreen_1" colour (<see cref="Color.LightGreen_1"/>).</summary>
    public static readonly Style LightGreen_1 = Color.LightGreen_1;

    /// <summary>A foreground <see cref="Style"/> in the "PaleGreen1" colour (<see cref="Color.PaleGreen1"/>).</summary>
    public static readonly Style PaleGreen1 = Color.PaleGreen1;

    /// <summary>A foreground <see cref="Style"/> in the "Aquamarine1_1" colour (<see cref="Color.Aquamarine1_1"/>).</summary>
    public static readonly Style Aquamarine1_1 = Color.Aquamarine1_1;

    /// <summary>A foreground <see cref="Style"/> in the "DarkSlateGray1" colour (<see cref="Color.DarkSlateGray1"/>).</summary>
    public static readonly Style DarkSlateGray1 = Color.DarkSlateGray1;

    /// <summary>A foreground <see cref="Style"/> in the "Red3" colour (<see cref="Color.Red3"/>).</summary>
    public static readonly Style Red3 = Color.Red3;

    /// <summary>A foreground <see cref="Style"/> in the "DeepPink4_2" colour (<see cref="Color.DeepPink4_2"/>).</summary>
    public static readonly Style DeepPink4_2 = Color.DeepPink4_2;

    /// <summary>A foreground <see cref="Style"/> in the "MediumVioletRed" colour (<see cref="Color.MediumVioletRed"/>).</summary>
    public static readonly Style MediumVioletRed = Color.MediumVioletRed;

    /// <summary>A foreground <see cref="Style"/> in the "Magenta3" colour (<see cref="Color.Magenta3"/>).</summary>
    public static readonly Style Magenta3 = Color.Magenta3;

    /// <summary>A foreground <see cref="Style"/> in the "DarkViolet_1" colour (<see cref="Color.DarkViolet_1"/>).</summary>
    public static readonly Style DarkViolet_1 = Color.DarkViolet_1;

    /// <summary>A foreground <see cref="Style"/> in the "Purple_2" colour (<see cref="Color.Purple_2"/>).</summary>
    public static readonly Style Purple_2 = Color.Purple_2;

    /// <summary>A foreground <see cref="Style"/> in the "DarkOrange3" colour (<see cref="Color.DarkOrange3"/>).</summary>
    public static readonly Style DarkOrange3 = Color.DarkOrange3;

    /// <summary>A foreground <see cref="Style"/> in the "IndianRed" colour (<see cref="Color.IndianRed"/>).</summary>
    public static readonly Style IndianRed = Color.IndianRed;

    /// <summary>A foreground <see cref="Style"/> in the "HotPink3" colour (<see cref="Color.HotPink3"/>).</summary>
    public static readonly Style HotPink3 = Color.HotPink3;

    /// <summary>A foreground <see cref="Style"/> in the "MediumOrchid3" colour (<see cref="Color.MediumOrchid3"/>).</summary>
    public static readonly Style MediumOrchid3 = Color.MediumOrchid3;

    /// <summary>A foreground <see cref="Style"/> in the "MediumOrchid" colour (<see cref="Color.MediumOrchid"/>).</summary>
    public static readonly Style MediumOrchid = Color.MediumOrchid;

    /// <summary>A foreground <see cref="Style"/> in the "MediumPurple2" colour (<see cref="Color.MediumPurple2"/>).</summary>
    public static readonly Style MediumPurple2 = Color.MediumPurple2;

    /// <summary>A foreground <see cref="Style"/> in the "DarkGoldenrod" colour (<see cref="Color.DarkGoldenrod"/>).</summary>
    public static readonly Style DarkGoldenrod = Color.DarkGoldenrod;

    /// <summary>A foreground <see cref="Style"/> in the "LightSalmon3" colour (<see cref="Color.LightSalmon3"/>).</summary>
    public static readonly Style LightSalmon3 = Color.LightSalmon3;

    /// <summary>A foreground <see cref="Style"/> in the "RosyBrown" colour (<see cref="Color.RosyBrown"/>).</summary>
    public static readonly Style RosyBrown = Color.RosyBrown;

    /// <summary>A foreground <see cref="Style"/> in the "Grey63" colour (<see cref="Color.Grey63"/>).</summary>
    public static readonly Style Grey63 = Color.Grey63;

    /// <summary>A foreground <see cref="Style"/> in the "MediumPurple2_1" colour (<see cref="Color.MediumPurple2_1"/>).</summary>
    public static readonly Style MediumPurple2_1 = Color.MediumPurple2_1;

    /// <summary>A foreground <see cref="Style"/> in the "MediumPurple1" colour (<see cref="Color.MediumPurple1"/>).</summary>
    public static readonly Style MediumPurple1 = Color.MediumPurple1;

    /// <summary>A foreground <see cref="Style"/> in the "Gold3" colour (<see cref="Color.Gold3"/>).</summary>
    public static readonly Style Gold3 = Color.Gold3;

    /// <summary>A foreground <see cref="Style"/> in the "DarkKhaki" colour (<see cref="Color.DarkKhaki"/>).</summary>
    public static readonly Style DarkKhaki = Color.DarkKhaki;

    /// <summary>A foreground <see cref="Style"/> in the "NavajoWhite3" colour (<see cref="Color.NavajoWhite3"/>).</summary>
    public static readonly Style NavajoWhite3 = Color.NavajoWhite3;

    /// <summary>A foreground <see cref="Style"/> in the "Grey69" colour (<see cref="Color.Grey69"/>).</summary>
    public static readonly Style Grey69 = Color.Grey69;

    /// <summary>A foreground <see cref="Style"/> in the "LightSteelBlue3" colour (<see cref="Color.LightSteelBlue3"/>).</summary>
    public static readonly Style LightSteelBlue3 = Color.LightSteelBlue3;

    /// <summary>A foreground <see cref="Style"/> in the "LightSteelBlue" colour (<see cref="Color.LightSteelBlue"/>).</summary>
    public static readonly Style LightSteelBlue = Color.LightSteelBlue;

    /// <summary>A foreground <see cref="Style"/> in the "Yellow3" colour (<see cref="Color.Yellow3"/>).</summary>
    public static readonly Style Yellow3 = Color.Yellow3;

    /// <summary>A foreground <see cref="Style"/> in the "DarkOliveGreen3_2" colour (<see cref="Color.DarkOliveGreen3_2"/>).</summary>
    public static readonly Style DarkOliveGreen3_2 = Color.DarkOliveGreen3_2;

    /// <summary>A foreground <see cref="Style"/> in the "DarkSeaGreen3_1" colour (<see cref="Color.DarkSeaGreen3_1"/>).</summary>
    public static readonly Style DarkSeaGreen3_1 = Color.DarkSeaGreen3_1;

    /// <summary>A foreground <see cref="Style"/> in the "DarkSeaGreen2" colour (<see cref="Color.DarkSeaGreen2"/>).</summary>
    public static readonly Style DarkSeaGreen2 = Color.DarkSeaGreen2;

    /// <summary>A foreground <see cref="Style"/> in the "LightCyan3" colour (<see cref="Color.LightCyan3"/>).</summary>
    public static readonly Style LightCyan3 = Color.LightCyan3;

    /// <summary>A foreground <see cref="Style"/> in the "LightSkyBlue1" colour (<see cref="Color.LightSkyBlue1"/>).</summary>
    public static readonly Style LightSkyBlue1 = Color.LightSkyBlue1;

    /// <summary>A foreground <see cref="Style"/> in the "GreenYellow" colour (<see cref="Color.GreenYellow"/>).</summary>
    public static readonly Style GreenYellow = Color.GreenYellow;

    /// <summary>A foreground <see cref="Style"/> in the "DarkOliveGreen2" colour (<see cref="Color.DarkOliveGreen2"/>).</summary>
    public static readonly Style DarkOliveGreen2 = Color.DarkOliveGreen2;

    /// <summary>A foreground <see cref="Style"/> in the "PaleGreen1_1" colour (<see cref="Color.PaleGreen1_1"/>).</summary>
    public static readonly Style PaleGreen1_1 = Color.PaleGreen1_1;

    /// <summary>A foreground <see cref="Style"/> in the "DarkSeaGreen2_1" colour (<see cref="Color.DarkSeaGreen2_1"/>).</summary>
    public static readonly Style DarkSeaGreen2_1 = Color.DarkSeaGreen2_1;

    /// <summary>A foreground <see cref="Style"/> in the "DarkSeaGreen1" colour (<see cref="Color.DarkSeaGreen1"/>).</summary>
    public static readonly Style DarkSeaGreen1 = Color.DarkSeaGreen1;

    /// <summary>A foreground <see cref="Style"/> in the "PaleTurquoise1" colour (<see cref="Color.PaleTurquoise1"/>).</summary>
    public static readonly Style PaleTurquoise1 = Color.PaleTurquoise1;

    /// <summary>A foreground <see cref="Style"/> in the "Red3_1" colour (<see cref="Color.Red3_1"/>).</summary>
    public static readonly Style Red3_1 = Color.Red3_1;

    /// <summary>A foreground <see cref="Style"/> in the "DeepPink3" colour (<see cref="Color.DeepPink3"/>).</summary>
    public static readonly Style DeepPink3 = Color.DeepPink3;

    /// <summary>A foreground <see cref="Style"/> in the "DeepPink3_1" colour (<see cref="Color.DeepPink3_1"/>).</summary>
    public static readonly Style DeepPink3_1 = Color.DeepPink3_1;

    /// <summary>A foreground <see cref="Style"/> in the "Magenta3_1" colour (<see cref="Color.Magenta3_1"/>).</summary>
    public static readonly Style Magenta3_1 = Color.Magenta3_1;

    /// <summary>A foreground <see cref="Style"/> in the "Magenta3_2" colour (<see cref="Color.Magenta3_2"/>).</summary>
    public static readonly Style Magenta3_2 = Color.Magenta3_2;

    /// <summary>A foreground <see cref="Style"/> in the "Magenta2" colour (<see cref="Color.Magenta2"/>).</summary>
    public static readonly Style Magenta2 = Color.Magenta2;

    /// <summary>A foreground <see cref="Style"/> in the "DarkOrange3_1" colour (<see cref="Color.DarkOrange3_1"/>).</summary>
    public static readonly Style DarkOrange3_1 = Color.DarkOrange3_1;

    /// <summary>A foreground <see cref="Style"/> in the "IndianRed_1" colour (<see cref="Color.IndianRed_1"/>).</summary>
    public static readonly Style IndianRed_1 = Color.IndianRed_1;

    /// <summary>A foreground <see cref="Style"/> in the "HotPink3_1" colour (<see cref="Color.HotPink3_1"/>).</summary>
    public static readonly Style HotPink3_1 = Color.HotPink3_1;

    /// <summary>A foreground <see cref="Style"/> in the "HotPink2" colour (<see cref="Color.HotPink2"/>).</summary>
    public static readonly Style HotPink2 = Color.HotPink2;

    /// <summary>A foreground <see cref="Style"/> in the "Orchid" colour (<see cref="Color.Orchid"/>).</summary>
    public static readonly Style Orchid = Color.Orchid;

    /// <summary>A foreground <see cref="Style"/> in the "MediumOrchid1" colour (<see cref="Color.MediumOrchid1"/>).</summary>
    public static readonly Style MediumOrchid1 = Color.MediumOrchid1;

    /// <summary>A foreground <see cref="Style"/> in the "Orange3" colour (<see cref="Color.Orange3"/>).</summary>
    public static readonly Style Orange3 = Color.Orange3;

    /// <summary>A foreground <see cref="Style"/> in the "LightSalmon3_1" colour (<see cref="Color.LightSalmon3_1"/>).</summary>
    public static readonly Style LightSalmon3_1 = Color.LightSalmon3_1;

    /// <summary>A foreground <see cref="Style"/> in the "LightPink3" colour (<see cref="Color.LightPink3"/>).</summary>
    public static readonly Style LightPink3 = Color.LightPink3;

    /// <summary>A foreground <see cref="Style"/> in the "Pink3" colour (<see cref="Color.Pink3"/>).</summary>
    public static readonly Style Pink3 = Color.Pink3;

    /// <summary>A foreground <see cref="Style"/> in the "Plum3" colour (<see cref="Color.Plum3"/>).</summary>
    public static readonly Style Plum3 = Color.Plum3;

    /// <summary>A foreground <see cref="Style"/> in the "Violet" colour (<see cref="Color.Violet"/>).</summary>
    public static readonly Style Violet = Color.Violet;

    /// <summary>A foreground <see cref="Style"/> in the "Gold3_1" colour (<see cref="Color.Gold3_1"/>).</summary>
    public static readonly Style Gold3_1 = Color.Gold3_1;

    /// <summary>A foreground <see cref="Style"/> in the "LightGoldenrod3" colour (<see cref="Color.LightGoldenrod3"/>).</summary>
    public static readonly Style LightGoldenrod3 = Color.LightGoldenrod3;

    /// <summary>A foreground <see cref="Style"/> in the "Tan" colour (<see cref="Color.Tan"/>).</summary>
    public static readonly Style Tan = Color.Tan;

    /// <summary>A foreground <see cref="Style"/> in the "MistyRose3" colour (<see cref="Color.MistyRose3"/>).</summary>
    public static readonly Style MistyRose3 = Color.MistyRose3;

    /// <summary>A foreground <see cref="Style"/> in the "Thistle3" colour (<see cref="Color.Thistle3"/>).</summary>
    public static readonly Style Thistle3 = Color.Thistle3;

    /// <summary>A foreground <see cref="Style"/> in the "Plum2" colour (<see cref="Color.Plum2"/>).</summary>
    public static readonly Style Plum2 = Color.Plum2;

    /// <summary>A foreground <see cref="Style"/> in the "Yellow3_1" colour (<see cref="Color.Yellow3_1"/>).</summary>
    public static readonly Style Yellow3_1 = Color.Yellow3_1;

    /// <summary>A foreground <see cref="Style"/> in the "Khaki3" colour (<see cref="Color.Khaki3"/>).</summary>
    public static readonly Style Khaki3 = Color.Khaki3;

    /// <summary>A foreground <see cref="Style"/> in the "LightGoldenrod2" colour (<see cref="Color.LightGoldenrod2"/>).</summary>
    public static readonly Style LightGoldenrod2 = Color.LightGoldenrod2;

    /// <summary>A foreground <see cref="Style"/> in the "LightYellow3" colour (<see cref="Color.LightYellow3"/>).</summary>
    public static readonly Style LightYellow3 = Color.LightYellow3;

    /// <summary>A foreground <see cref="Style"/> in the "Grey84" colour (<see cref="Color.Grey84"/>).</summary>
    public static readonly Style Grey84 = Color.Grey84;

    /// <summary>A foreground <see cref="Style"/> in the "LightSteelBlue1" colour (<see cref="Color.LightSteelBlue1"/>).</summary>
    public static readonly Style LightSteelBlue1 = Color.LightSteelBlue1;

    /// <summary>A foreground <see cref="Style"/> in the "Yellow2" colour (<see cref="Color.Yellow2"/>).</summary>
    public static readonly Style Yellow2 = Color.Yellow2;

    /// <summary>A foreground <see cref="Style"/> in the "DarkOliveGreen1" colour (<see cref="Color.DarkOliveGreen1"/>).</summary>
    public static readonly Style DarkOliveGreen1 = Color.DarkOliveGreen1;

    /// <summary>A foreground <see cref="Style"/> in the "DarkOliveGreen1_1" colour (<see cref="Color.DarkOliveGreen1_1"/>).</summary>
    public static readonly Style DarkOliveGreen1_1 = Color.DarkOliveGreen1_1;

    /// <summary>A foreground <see cref="Style"/> in the "DarkSeaGreen1_1" colour (<see cref="Color.DarkSeaGreen1_1"/>).</summary>
    public static readonly Style DarkSeaGreen1_1 = Color.DarkSeaGreen1_1;

    /// <summary>A foreground <see cref="Style"/> in the "Honeydew2" colour (<see cref="Color.Honeydew2"/>).</summary>
    public static readonly Style Honeydew2 = Color.Honeydew2;

    /// <summary>A foreground <see cref="Style"/> in the "LightCyan1" colour (<see cref="Color.LightCyan1"/>).</summary>
    public static readonly Style LightCyan1 = Color.LightCyan1;

    /// <summary>A foreground <see cref="Style"/> in the "Red1" colour (<see cref="Color.Red1"/>).</summary>
    public static readonly Style Red1 = Color.Red1;

    /// <summary>A foreground <see cref="Style"/> in the "DeepPink2" colour (<see cref="Color.DeepPink2"/>).</summary>
    public static readonly Style DeepPink2 = Color.DeepPink2;

    /// <summary>A foreground <see cref="Style"/> in the "DeepPink1" colour (<see cref="Color.DeepPink1"/>).</summary>
    public static readonly Style DeepPink1 = Color.DeepPink1;

    /// <summary>A foreground <see cref="Style"/> in the "DeepPink1_1" colour (<see cref="Color.DeepPink1_1"/>).</summary>
    public static readonly Style DeepPink1_1 = Color.DeepPink1_1;

    /// <summary>A foreground <see cref="Style"/> in the "Magenta2_1" colour (<see cref="Color.Magenta2_1"/>).</summary>
    public static readonly Style Magenta2_1 = Color.Magenta2_1;

    /// <summary>A foreground <see cref="Style"/> in the "Magenta1" colour (<see cref="Color.Magenta1"/>).</summary>
    public static readonly Style Magenta1 = Color.Magenta1;

    /// <summary>A foreground <see cref="Style"/> in the "OrangeRed1" colour (<see cref="Color.OrangeRed1"/>).</summary>
    public static readonly Style OrangeRed1 = Color.OrangeRed1;

    /// <summary>A foreground <see cref="Style"/> in the "IndianRed1" colour (<see cref="Color.IndianRed1"/>).</summary>
    public static readonly Style IndianRed1 = Color.IndianRed1;

    /// <summary>A foreground <see cref="Style"/> in the "IndianRed1_1" colour (<see cref="Color.IndianRed1_1"/>).</summary>
    public static readonly Style IndianRed1_1 = Color.IndianRed1_1;

    /// <summary>A foreground <see cref="Style"/> in the "HotPink" colour (<see cref="Color.HotPink"/>).</summary>
    public static readonly Style HotPink = Color.HotPink;

    /// <summary>A foreground <see cref="Style"/> in the "HotPink_1" colour (<see cref="Color.HotPink_1"/>).</summary>
    public static readonly Style HotPink_1 = Color.HotPink_1;

    /// <summary>A foreground <see cref="Style"/> in the "MediumOrchid1_1" colour (<see cref="Color.MediumOrchid1_1"/>).</summary>
    public static readonly Style MediumOrchid1_1 = Color.MediumOrchid1_1;

    /// <summary>A foreground <see cref="Style"/> in the "DarkOrange" colour (<see cref="Color.DarkOrange"/>).</summary>
    public static readonly Style DarkOrange = Color.DarkOrange;

    /// <summary>A foreground <see cref="Style"/> in the "Salmon1" colour (<see cref="Color.Salmon1"/>).</summary>
    public static readonly Style Salmon1 = Color.Salmon1;

    /// <summary>A foreground <see cref="Style"/> in the "LightCoral" colour (<see cref="Color.LightCoral"/>).</summary>
    public static readonly Style LightCoral = Color.LightCoral;

    /// <summary>A foreground <see cref="Style"/> in the "PaleVioletRed1" colour (<see cref="Color.PaleVioletRed1"/>).</summary>
    public static readonly Style PaleVioletRed1 = Color.PaleVioletRed1;

    /// <summary>A foreground <see cref="Style"/> in the "Orchid2" colour (<see cref="Color.Orchid2"/>).</summary>
    public static readonly Style Orchid2 = Color.Orchid2;

    /// <summary>A foreground <see cref="Style"/> in the "Orchid1" colour (<see cref="Color.Orchid1"/>).</summary>
    public static readonly Style Orchid1 = Color.Orchid1;

    /// <summary>A foreground <see cref="Style"/> in the "Orange1" colour (<see cref="Color.Orange1"/>).</summary>
    public static readonly Style Orange1 = Color.Orange1;

    /// <summary>A foreground <see cref="Style"/> in the "SandyBrown" colour (<see cref="Color.SandyBrown"/>).</summary>
    public static readonly Style SandyBrown = Color.SandyBrown;

    /// <summary>A foreground <see cref="Style"/> in the "LightSalmon1" colour (<see cref="Color.LightSalmon1"/>).</summary>
    public static readonly Style LightSalmon1 = Color.LightSalmon1;

    /// <summary>A foreground <see cref="Style"/> in the "LightPink1" colour (<see cref="Color.LightPink1"/>).</summary>
    public static readonly Style LightPink1 = Color.LightPink1;

    /// <summary>A foreground <see cref="Style"/> in the "Pink1" colour (<see cref="Color.Pink1"/>).</summary>
    public static readonly Style Pink1 = Color.Pink1;

    /// <summary>A foreground <see cref="Style"/> in the "Plum1" colour (<see cref="Color.Plum1"/>).</summary>
    public static readonly Style Plum1 = Color.Plum1;

    /// <summary>A foreground <see cref="Style"/> in the "Gold1" colour (<see cref="Color.Gold1"/>).</summary>
    public static readonly Style Gold1 = Color.Gold1;

    /// <summary>A foreground <see cref="Style"/> in the "LightGoldenrod2_1" colour (<see cref="Color.LightGoldenrod2_1"/>).</summary>
    public static readonly Style LightGoldenrod2_1 = Color.LightGoldenrod2_1;

    /// <summary>A foreground <see cref="Style"/> in the "LightGoldenrod2_2" colour (<see cref="Color.LightGoldenrod2_2"/>).</summary>
    public static readonly Style LightGoldenrod2_2 = Color.LightGoldenrod2_2;

    /// <summary>A foreground <see cref="Style"/> in the "NavajoWhite1" colour (<see cref="Color.NavajoWhite1"/>).</summary>
    public static readonly Style NavajoWhite1 = Color.NavajoWhite1;

    /// <summary>A foreground <see cref="Style"/> in the "MistyRose1" colour (<see cref="Color.MistyRose1"/>).</summary>
    public static readonly Style MistyRose1 = Color.MistyRose1;

    /// <summary>A foreground <see cref="Style"/> in the "Thistle1" colour (<see cref="Color.Thistle1"/>).</summary>
    public static readonly Style Thistle1 = Color.Thistle1;

    /// <summary>A foreground <see cref="Style"/> in the "Yellow1" colour (<see cref="Color.Yellow1"/>).</summary>
    public static readonly Style Yellow1 = Color.Yellow1;

    /// <summary>A foreground <see cref="Style"/> in the "LightGoldenrod1" colour (<see cref="Color.LightGoldenrod1"/>).</summary>
    public static readonly Style LightGoldenrod1 = Color.LightGoldenrod1;

    /// <summary>A foreground <see cref="Style"/> in the "Khaki1" colour (<see cref="Color.Khaki1"/>).</summary>
    public static readonly Style Khaki1 = Color.Khaki1;

    /// <summary>A foreground <see cref="Style"/> in the "Wheat1" colour (<see cref="Color.Wheat1"/>).</summary>
    public static readonly Style Wheat1 = Color.Wheat1;

    /// <summary>A foreground <see cref="Style"/> in the "Cornsilk1" colour (<see cref="Color.Cornsilk1"/>).</summary>
    public static readonly Style Cornsilk1 = Color.Cornsilk1;

    /// <summary>A foreground <see cref="Style"/> in the "Grey100" colour (<see cref="Color.Grey100"/>).</summary>
    public static readonly Style Grey100 = Color.Grey100;

    /// <summary>A foreground <see cref="Style"/> in the "Grey3" colour (<see cref="Color.Grey3"/>).</summary>
    public static readonly Style Grey3 = Color.Grey3;

    /// <summary>A foreground <see cref="Style"/> in the "Grey7" colour (<see cref="Color.Grey7"/>).</summary>
    public static readonly Style Grey7 = Color.Grey7;

    /// <summary>A foreground <see cref="Style"/> in the "Grey11" colour (<see cref="Color.Grey11"/>).</summary>
    public static readonly Style Grey11 = Color.Grey11;

    /// <summary>A foreground <see cref="Style"/> in the "Grey15" colour (<see cref="Color.Grey15"/>).</summary>
    public static readonly Style Grey15 = Color.Grey15;

    /// <summary>A foreground <see cref="Style"/> in the "Grey19" colour (<see cref="Color.Grey19"/>).</summary>
    public static readonly Style Grey19 = Color.Grey19;

    /// <summary>A foreground <see cref="Style"/> in the "Grey23" colour (<see cref="Color.Grey23"/>).</summary>
    public static readonly Style Grey23 = Color.Grey23;

    /// <summary>A foreground <see cref="Style"/> in the "Grey27" colour (<see cref="Color.Grey27"/>).</summary>
    public static readonly Style Grey27 = Color.Grey27;

    /// <summary>A foreground <see cref="Style"/> in the "Grey30" colour (<see cref="Color.Grey30"/>).</summary>
    public static readonly Style Grey30 = Color.Grey30;

    /// <summary>A foreground <see cref="Style"/> in the "Grey35" colour (<see cref="Color.Grey35"/>).</summary>
    public static readonly Style Grey35 = Color.Grey35;

    /// <summary>A foreground <see cref="Style"/> in the "Grey39" colour (<see cref="Color.Grey39"/>).</summary>
    public static readonly Style Grey39 = Color.Grey39;

    /// <summary>A foreground <see cref="Style"/> in the "Grey42" colour (<see cref="Color.Grey42"/>).</summary>
    public static readonly Style Grey42 = Color.Grey42;

    /// <summary>A foreground <see cref="Style"/> in the "Grey46" colour (<see cref="Color.Grey46"/>).</summary>
    public static readonly Style Grey46 = Color.Grey46;

    /// <summary>A foreground <see cref="Style"/> in the "Grey50" colour (<see cref="Color.Grey50"/>).</summary>
    public static readonly Style Grey50 = Color.Grey50;

    /// <summary>A foreground <see cref="Style"/> in the "Grey54" colour (<see cref="Color.Grey54"/>).</summary>
    public static readonly Style Grey54 = Color.Grey54;

    /// <summary>A foreground <see cref="Style"/> in the "Grey58" colour (<see cref="Color.Grey58"/>).</summary>
    public static readonly Style Grey58 = Color.Grey58;

    /// <summary>A foreground <see cref="Style"/> in the "Grey62" colour (<see cref="Color.Grey62"/>).</summary>
    public static readonly Style Grey62 = Color.Grey62;

    /// <summary>A foreground <see cref="Style"/> in the "Grey66" colour (<see cref="Color.Grey66"/>).</summary>
    public static readonly Style Grey66 = Color.Grey66;

    /// <summary>A foreground <see cref="Style"/> in the "Grey70" colour (<see cref="Color.Grey70"/>).</summary>
    public static readonly Style Grey70 = Color.Grey70;

    /// <summary>A foreground <see cref="Style"/> in the "Grey74" colour (<see cref="Color.Grey74"/>).</summary>
    public static readonly Style Grey74 = Color.Grey74;

    /// <summary>A foreground <see cref="Style"/> in the "Grey78" colour (<see cref="Color.Grey78"/>).</summary>
    public static readonly Style Grey78 = Color.Grey78;

    /// <summary>A foreground <see cref="Style"/> in the "Grey82" colour (<see cref="Color.Grey82"/>).</summary>
    public static readonly Style Grey82 = Color.Grey82;

    /// <summary>A foreground <see cref="Style"/> in the "Grey85" colour (<see cref="Color.Grey85"/>).</summary>
    public static readonly Style Grey85 = Color.Grey85;

    /// <summary>A foreground <see cref="Style"/> in the "Grey89" colour (<see cref="Color.Grey89"/>).</summary>
    public static readonly Style Grey89 = Color.Grey89;

    /// <summary>A foreground <see cref="Style"/> in the "Grey93" colour (<see cref="Color.Grey93"/>).</summary>
    public static readonly Style Grey93 = Color.Grey93;

    #endregion Colors

    #endregion Fields
}