namespace Jumbee.Console.Tests;

using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>Headless tests for <see cref="Footer"/>: hint rendering, gaps, and width-bounded truncation.</summary>
public class FooterTests
{
    [Fact]
    public void Renders_AllHints_WhenRoom()
    {
        var f = new Footer(new FooterHint("^j", "Send"), new FooterHint("^c", "Quit"));
        var text = ConsoleSnapshot.ToText(f, 40, 1);
        Assert.Contains("^j Send", text);
        Assert.Contains("^c Quit", text);
    }

    [Fact]
    public void Truncates_TrailingHints_WhenNarrow()
    {
        var f = new Footer(new FooterHint("^j", "Send"), new FooterHint("^c", "Quit"));
        var text = ConsoleSnapshot.ToText(f, 8, 1);   // only "^j Send" (7) fits; the gap + "^c Quit" does not
        Assert.Contains("^j Send", text);
        Assert.DoesNotContain("Quit", text);
    }

    [Fact]
    public void SetHints_ReplacesContent()
    {
        var f = new Footer(new FooterHint("^a", "Alpha"));
        ConsoleSnapshot.Render(f, 40, 1);
        f.SetHints(new FooterHint("^b", "Beta"));
        var text = ConsoleSnapshot.ToText(f, 40, 1);
        Assert.Contains("^b Beta", text);
        Assert.DoesNotContain("Alpha", text);
    }
}
