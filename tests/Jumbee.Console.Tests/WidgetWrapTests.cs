namespace Jumbee.Console.Tests;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>
/// Tests for the Phase 2 display widgets: Sparkline, Digits, Log.
/// </summary>
public class WidgetWrapTests
{
    #region Sparkline
    [Fact]
    public void Sparkline_RendersBarsScaledToMax()
    {
        var spark = new Sparkline(1, 2, 3, 4);
        var text = ConsoleSnapshot.ToText(spark, 4, 1);
        Assert.Contains("█", text);   // the max value -> full bar
        Assert.Contains("▂", text);   // the smallest value -> low bar
    }

    [Fact]
    public void Sparkline_Empty_RendersNothing()
    {
        var spark = new Sparkline();
        var text = ConsoleSnapshot.ToText(spark, 4, 1);
        Assert.Equal("", text.Trim());
    }

    [Fact]
    public void Sparkline_CustomRamp_RendersAsAscendingStaircase()
    {
        // The ASCII ramp is font-independent; a strict 1..8 ramp must map to each glyph in order.
        var spark = new Sparkline(1, 2, 3, 4, 5, 6, 7, 8) { Bars = Sparkline.AsciiBars };
        var text = ConsoleSnapshot.ToText(spark, 8, 1).TrimEnd('\n');
        Assert.Equal(Sparkline.AsciiBars, text);
    }
    #endregion

    #region Digits
    [Fact]
    public void Digits_RendersThreeRowGlyphs()
    {
        var digits = new Digits("8");
        var text = ConsoleSnapshot.ToText(digits, 3, 3);
        Assert.Contains("|_|", text);   // an 8 has filled middle/bottom segments
    }

    [Fact]
    public void Digits_WidthIsThreeCellsPerCharacter()
    {
        var digits = new Digits("12");
        Assert.Equal(6, digits.Width);   // intrinsic preferred width: 3 cells per character

        ConsoleSnapshot.Render(digits, 6, 3);   // lay out at the natural size
        Assert.Equal(6, digits.ActualWidth);
        Assert.Equal(3, digits.ActualHeight);
    }
    #endregion

    #region Log
    [Fact]
    public void Log_ShowsMostRecentEntriesThatFit()
    {
        var log = new Log();
        for (var i = 0; i < 5; i++) log.Write($"line{i}");

        var text = ConsoleSnapshot.ToText(log, 20, 3);

        Assert.Contains("line4", text);   // newest is visible
        Assert.Contains("line2", text);   // last 3 fit
        Assert.DoesNotContain("line0", text);   // oldest scrolled off
    }

    [Fact]
    public void Log_MaxEntries_DiscardsOldest()
    {
        var log = new Log { MaxEntries = 2 };
        log.Write("a");
        log.Write("b");
        log.Write("c");

        var text = ConsoleSnapshot.ToText(log, 20, 5);

        Assert.Contains("c", text);
        Assert.Contains("b", text);
        Assert.DoesNotContain("a", text);
    }

    [Fact]
    public void Log_RendersMarkupEntries()
    {
        var log = new Log();
        log.Write("[green]Hello[/]");
        log.Write("[red]World[/]");

        var text = ConsoleSnapshot.ToText(log, 20, 5);

        Assert.Contains("Hello", text);
        Assert.Contains("World", text);
    }
    #endregion
}
