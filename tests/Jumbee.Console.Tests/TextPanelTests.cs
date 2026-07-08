namespace Jumbee.Console.Tests;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

public class TextPanelTests
{
    public TextPanelTests() => UiTestHarness.EnsureStopped();

    [Fact]
    public void TextPanel_RendersMultipleLines()
    {
        var p = new TextPanel("line one\nline two\nline three");
        var text = ConsoleSnapshot.ToText(p, 20, 4);

        Assert.Contains("line one", text);
        Assert.Contains("line two", text);
        Assert.Contains("line three", text);
    }

    [Fact]
    public void TextPanel_RendersMarkupStyling()
    {
        var p = new TextPanel("[green]OK[/] [red]FAIL[/]");
        var text = ConsoleSnapshot.ToText(p, 20, 2);

        Assert.Contains("OK", text);
        Assert.Contains("FAIL", text);
    }

    [Fact]
    public void TextPanel_Escape_ShowsLiteralBrackets()
    {
        var p = new TextPanel(TextPanel.Escape(@"  \ /  [rain]"));
        var text = ConsoleSnapshot.ToText(p, 24, 2);

        Assert.Contains("[rain]", text);   // brackets shown verbatim, not parsed as markup
    }

    [Fact]
    public void TextPanel_UpdatingContent_Rerenders()
    {
        var p = new TextPanel("before");
        Assert.Contains("before", ConsoleSnapshot.ToText(p, 20, 2));

        p.Markup = "after";
        Assert.Contains("after", ConsoleSnapshot.ToText(p, 20, 2));
    }

    [Fact]
    public void TextPanel_Framed_DoesNotBalloon()
    {
        // Regression guard: a framed content control must report a viewport-bounded height (its MeasureHeight feeds
        // the frame's scroll range), never the 1000-row scroll clamp that draws a broken scrollbar.
        var p = new TextPanel("a\nb\nc\nd");
        p.WithRoundedBorder();
        ConsoleSnapshot.Render(p, 20, 12);

        Assert.True(p.ActualHeight is > 0 and <= 12, $"expected a viewport-bounded height, got {p.ActualHeight}");
    }
}
