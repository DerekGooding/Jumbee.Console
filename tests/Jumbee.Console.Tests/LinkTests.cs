namespace Jumbee.Console.Tests;

using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

public class LinkTests
{
    private static readonly Position Origin = new(0, 0);

    private static void Click(Control c, Position p)
    {
        var m = (IMouseListener)c;
        m.OnMouseDown(p);
        m.OnMouseUp(p);
    }

    [Fact]
    public void Link_RendersText()
    {
        var link = new Link("Docs", url: null);
        Assert.Contains("Docs", ConsoleSnapshot.ToText(link, 10, 1));
    }

    [Fact]
    public void Link_Click_RaisesActivated()
    {
        var link = new Link("Docs", url: null);   // null URL -> no browser launch, just the event
        var activated = false;
        link.Activated += (_, _) => activated = true;

        Click(link, Origin);

        Assert.True(activated);
    }

    [Theory]
    [InlineData(ConsoleKey.Enter)]
    [InlineData(ConsoleKey.Spacebar)]
    public void Link_EnterOrSpace_Activates(ConsoleKey key)
    {
        var link = new Link("Docs", url: null);
        var activated = false;
        link.Activated += (_, _) => activated = true;

        UI.SendInput(link, key);

        Assert.True(activated);
    }

    [Fact]
    public void Link_WidthMatchesText()
    {
        var link = new Link("Open docs", url: null);
        Assert.Equal("Open docs".Length, link.Width);
    }
}
