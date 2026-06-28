namespace Jumbee.Console.Tests;

using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

public class ButtonTests
{
    private static readonly Position Origin = new(0, 0);

    private static void Click(Control c, Position p)
    {
        var m = (IMouseListener)c;
        m.OnMouseDown(p);
        m.OnMouseUp(p);
    }

    [Fact]
    public void Button_Default_IsSingleRowText()
    {
        var text = ConsoleSnapshot.ToText(new Button("Save"), 18, 1);

        Assert.Contains("Save", text);
        Assert.DoesNotContain("▔", text);   // borderless by default — no bevel edges
    }

    [Fact]
    public void Button_Bevel_RendersRaisedTile()
    {
        var b = new Button("Save") { Style = ButtonStyle.Primary.WithShape(ButtonShape.Modern) };

        var text = ConsoleSnapshot.ToText(b, 18, 3);

        Assert.Contains("Save", text);
        Assert.Contains("▔", text);   // lighter top edge
        Assert.Contains("▁", text);   // darker bottom edge
    }

    [Fact]
    public void Button_FixedWidth_WidensBeyondLabel()
    {
        var bevel = ButtonStyle.Primary.WithShape(ButtonShape.Modern);
        var narrow = ConsoleSnapshot.ToText(new Button("Go") { Style = bevel }, 30, 3);
        var wide = ConsoleSnapshot.ToText(new Button("Go") { Style = bevel.WithWidth(24) }, 30, 3);

        // The fixed width widens the tile (and centres the label), so its bevel edge has more glyphs.
        Assert.True(CountChar(wide, '▔') > CountChar(narrow, '▔'));
    }

    private static int CountChar(string s, char c)
    {
        var n = 0;
        foreach (var ch in s) if (ch == c) n++;
        return n;
    }

    [Fact]
    public void Button_ShapeChange_RelaysOutSiblings()
    {
        var a = new Button("AAA");
        var b = new Button("BBB");
        var stack = new VerticalStackPanel(a, b);

        var before = ConsoleSnapshot.ToText(stack, 20, 9);
        var bRowBefore = RowOf(before, "BBB");

        a.Style = a.Style.WithShape(ButtonShape.Modern);   // a grows from 1 row to 3

        var after = ConsoleSnapshot.ToText(stack, 20, 9);
        var bRowAfter = RowOf(after, "BBB");

        Assert.Equal(1, bRowBefore);             // both flat: BBB sits directly under AAA
        Assert.Equal(3, bRowAfter);              // AAA now 3 rows tall, so BBB is pushed down
    }

    private static int RowOf(string text, string needle)
    {
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
            if (lines[i].Contains(needle)) return i;
        return -1;
    }

    [Fact]
    public void Button_Secondary_DiffersFromPrimaryFill()
    {
        var primary = new Button("X");
        var secondary = Button.Secondary("X");

        Assert.NotEqual(primary.Style.Normal, secondary.Style.Normal);
    }

    [Fact]
    public void Button_Click_RaisesActivated()
    {
        var b = new Button("Go");
        var activated = false;
        b.Activated += (_, _) => activated = true;

        Click(b, Origin);

        Assert.True(activated);
    }

    [Theory]
    [InlineData(ConsoleKey.Enter)]
    [InlineData(ConsoleKey.Spacebar)]
    public void Button_EnterOrSpace_Activates(ConsoleKey key)
    {
        var b = new Button("Go");
        var activated = false;
        b.Activated += (_, _) => activated = true;

        UI.SendInput(b, key);

        Assert.True(activated);
    }
}
