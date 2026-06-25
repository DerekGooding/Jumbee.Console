namespace Jumbee.Console.Tests;

using System;

using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

public class ThemeTests
{
    #region Custom themes
    /// <summary>An ASCII glyph theme; the switch glyphs are a different cell width (5) than the default (4).</summary>
    private sealed class AsciiGlyphTheme : IGlyphTheme
    {
        public string CheckboxChecked => "[*]";
        public string RadioSelected => "(o)";
        public string SwitchOn => "[ON ]";
        public string SwitchOff => "[OFF]";
    }

    /// <summary>A style theme that overrides a single token; everything else falls back to the defaults.</summary>
    private sealed class AccentStyleTheme : IStyleTheme
    {
        public Style TextAccent => Style.Magenta1;
    }

    /// <summary>A glyph theme that overrides only the scrollbar style.</summary>
    private sealed class BlockScrollGlyphTheme : IGlyphTheme
    {
        public ScrollBarStyle ScrollBar => ScrollBarStyle.Block;
    }

    private static void WithGlyphTheme(IGlyphTheme theme, Action body)
    {
        var prev = UI.GlyphTheme;
        UI.GlyphTheme = theme;
        try { body(); } finally { UI.GlyphTheme = prev; }
    }

    private static void WithStyleTheme(IStyleTheme theme, Action body)
    {
        var prev = UI.StyleTheme;
        UI.StyleTheme = theme;
        try { body(); } finally { UI.StyleTheme = prev; }
    }
    #endregion

    #region Defaults
    [Fact]
    public void UI_ThemesDefaultToBuiltIns()
    {
        Assert.IsType<DefaultStyleTheme>(UI.StyleTheme);
        Assert.IsType<DefaultGlyphTheme>(UI.GlyphTheme);
    }

    [Fact]
    public void DefaultGlyphTheme_HasExpectedDefaults()
    {
        IGlyphTheme g = new DefaultGlyphTheme();
        Assert.Equal("[X]", g.CheckboxChecked);
        Assert.Equal("( )", g.RadioUnselected);
    }

    [Fact]
    public void CustomGlyphTheme_FallsBackToDefaultsForUnsetMembers()
    {
        IGlyphTheme g = new AsciiGlyphTheme();
        Assert.Equal("[*]", g.CheckboxChecked);     // overridden
        Assert.Equal("[ ]", g.CheckboxUnchecked);   // default (not overridden)
    }
    #endregion

    #region Glyph theme drives appearance + layout
    [Fact]
    public void Checkbox_UsesThemedGlyph()
    {
        WithGlyphTheme(new AsciiGlyphTheme(), () =>
        {
            var cb = new Checkbox("OK", isChecked: true);
            Assert.Contains("[*] OK", ConsoleSnapshot.ToText(cb, 12, 1));
        });
    }

    [Fact]
    public void Switch_GlyphWidth_DrivesControlWidth()
    {
        var defaultWidth = new Switch("P").Width;            // "(─●)" = 4 + space + 1

        WithGlyphTheme(new AsciiGlyphTheme(), () =>
        {
            var themed = new Switch("P").Width;              // "[ON ]"/"[OFF]" = 5 + space + 1
            Assert.Equal(defaultWidth + 1, themed);
        });
    }

    [Fact]
    public void RadioSet_UsesThemedGlyph()
    {
        WithGlyphTheme(new AsciiGlyphTheme(), () =>
        {
            var rs = new RadioSet("Red", "Green") { SelectedIndex = 0 };
            var text = ConsoleSnapshot.ToText(rs, 14, 2);
            Assert.Contains("(o) Red", text);   // themed selected marker
            Assert.Contains("( ) Green", text);
        });
    }

    [Fact]
    public void ThemedGlyph_DoesNotChangeBehaviour()
    {
        WithGlyphTheme(new AsciiGlyphTheme(), () =>
        {
            var cb = new Checkbox("x");
            var m = (IMouseListener)cb;
            m.OnMouseDown(new Position(0, 0));
            m.OnMouseUp(new Position(0, 0));
            Assert.True(cb.IsChecked);   // clicking still toggles, regardless of glyph
        });
    }
    #endregion

    #region Style theme drives captured tokens
    [Fact]
    public void Checkbox_CapturesAccentFromStyleTheme()
    {
        WithStyleTheme(new AccentStyleTheme(), () =>
        {
            var cb = new Checkbox("x");
            Assert.Equal(Style.Magenta1, cb.AccentStyle);          // captured override
            Assert.Equal(((IStyleTheme)new AccentStyleTheme()).Text, cb.LabelStyle);   // default token
        });
    }

    [Fact]
    public void Control_CapturesThemeAtConstruction_NotLive()
    {
        Checkbox cb = null!;
        WithStyleTheme(new AccentStyleTheme(), () => cb = new Checkbox("x"));

        // Theme reverted after construction; the control keeps what it captured (no live switching).
        Assert.Equal(Style.Magenta1, cb.AccentStyle);
    }

    [Fact]
    public void Override_StillWinsOverTheme()
    {
        var cb = new Checkbox("x") { AccentStyle = Style.Red1 };
        Assert.Equal(Style.Red1, cb.AccentStyle);
    }
    #endregion

    #region Scrollbar theming
    [Fact]
    public void DefaultGlyphTheme_ScrollBar_IsDefaultStyle()
    {
        IGlyphTheme g = new DefaultGlyphTheme();
        Assert.Equal(ScrollBarStyle.Default.Thumb.Content, g.ScrollBar.Thumb.Content);
    }

    [Fact]
    public void ControlFrame_CapturesScrollBarFromGlyphTheme()
    {
        WithGlyphTheme(new BlockScrollGlyphTheme(), () =>
        {
            var lb = new ListBox("a");
            lb.WithRoundedBorder();
            Assert.Equal('█', lb.Frame!.ScrollBarStyle.Thumb.Content);   // themed block thumb
        });
    }

    [Fact]
    public void ControlFrame_ScrollBarOverride_WinsOverTheme()
    {
        WithGlyphTheme(new BlockScrollGlyphTheme(), () =>
        {
            var lb = new ListBox("a");
            lb.WithRoundedBorder().WithScrollBarStyle(ScrollBarStyle.Shaded);
            Assert.Equal('▒', lb.Frame!.ScrollBarStyle.Thumb.Content);   // explicit override wins
        });
    }
    #endregion

    #region Style value-equality
    [Fact]
    public void Style_ValueEquality()
    {
        Assert.True(Style.Green1 == (Style)Color.Green1);
        Assert.Equal(Style.Green1, (Style)Color.Green1);
        Assert.NotEqual(Style.Green1, Style.Red1);
    }

    [Fact]
    public void Style_ColorAccessors_ReturnNullForDefault()
    {
        Assert.Null(Style.Plain.ForegroundColor);              // no foreground set
        Assert.Equal(Color.Green1, Style.Green1.ForegroundColor);
    }
    #endregion
}
