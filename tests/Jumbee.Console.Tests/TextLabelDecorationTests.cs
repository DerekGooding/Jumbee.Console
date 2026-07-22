namespace Jumbee.Console.Tests;

using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>Regression: <see cref="TextLabel"/> can carry a text decoration (e.g. bold), surfaced by the scope-tui
/// port's bold header cell. Decoration isn't captured by text snapshots, so assert on the rendered cell.</summary>
public class TextLabelDecorationTests
{
    [Fact]
    public void TextLabel_Bold_CarriesBoldDecorationInRenderedCells()
    {
        var bold = new TextLabel(TextLabelOrientation.Horizontal, "HI", decoration: Spectre.Console.Decoration.Bold);
        var deco = ConsoleSnapshot.Render(bold, 4, 1)[0, 0].Character.Decoration;
        Assert.NotNull(deco);
        Assert.True((deco!.Value & ConsoleGUI.Data.Decoration.Bold) != 0, "bold flag should be set on the cell");
    }

    [Fact]
    public void TextLabel_Default_HasNoDecoration()
    {
        var plain = new TextLabel(TextLabelOrientation.Horizontal, "HI");
        Assert.Null(ConsoleSnapshot.Render(plain, 4, 1)[0, 0].Character.Decoration);
    }
}
