namespace Jumbee.Console.Tests;

using System.Threading.Tasks;

using ConsoleGUI;
using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

using CColor = ConsoleGUI.Data.Color;
using CCharacter = ConsoleGUI.Data.Character;
using CCell = ConsoleGUI.Data.Cell;
using CDecoration = ConsoleGUI.Data.Decoration;

/// <summary>
/// Tests of the real ANSI render path (ConsoleManager → escape sequences → parsed screen) via
/// <see cref="AnsiConsoleSnapshot"/>. These exercise the encoding/diff/cursor + serialized async output that the
/// logical <see cref="ConsoleSnapshot"/> compose path can't see.
/// </summary>
public class AnsiSnapshotTests
{
    public AnsiSnapshotTests() => UiTestHarness.EnsureStopped();

    [Fact]
    public async Task Ansi_RoundTrips_FlatButton_MatchesComposeText()
    {
        var compose = ConsoleSnapshot.ToText(new Button("Save"), 18, 1);
        var ansi = ConsoleSnapshot.ToText((await AnsiConsoleSnapshot.RenderAsync(new Button("Save"), 18, 1)).Buffer);

        Assert.Contains("Save", ansi);
        Assert.Equal(compose, ansi);   // the ANSI encode→parse round-trip reproduces the composed glyphs
    }

    [Fact]
    public async Task Ansi_RoundTrips_TruecolorBackground()
    {
        // A Modern button's middle row is the solid fill colour. Assert the 24-bit SGR background survived
        // encode→parse — something ToText can't check, and the whole point of the ANSI path.
        var btn = new Button("OK") { Style = ButtonStyle.Primary.WithShape(ButtonShape.Modern) };

        var screen = await AnsiConsoleSnapshot.RenderAsync(btn, 18, 3);
        var bg = screen.Buffer[2, 1].Character.Background;   // middle row, inside the fill

        Assert.NotNull(bg);
        Assert.Equal(40, (int)bg!.Value.Red);
        Assert.Equal(70, (int)bg.Value.Green);
        Assert.Equal(120, (int)bg.Value.Blue);
    }

    [Fact]
    public async Task Ansi_ModernButton_IsThreeRowsWithCenteredLabel()
    {
        var btn = new Button("OK") { Style = ButtonStyle.Primary.WithShape(ButtonShape.Modern) };

        var text = ConsoleSnapshot.ToText((await AnsiConsoleSnapshot.RenderAsync(btn, 18, 3)).Buffer);
        var lines = text.Split('\n');

        Assert.Contains("OK", lines[1]);                 // label on the middle row
        Assert.DoesNotContain("OK", lines[0]);           // not on the top edge
        Assert.Contains("▔", lines[0]);                  // bevel edges survived the ANSI path
        Assert.Contains("▁", lines[2]);
    }

    [Fact]
    public async Task Ansi_LivePressRelease_DiffReturnsToCleanState()
    {
        // Reproduce a click on the live render path (successive frames sharing ConsoleManager's diff buffer, output
        // serialized). The bevel should invert under press and return cleanly on release — no stale/garbled cells.
        var btn = new Button("OK") { Style = ButtonStyle.Primary.WithShape(ButtonShape.Modern) };
        using var session = await AnsiConsoleSession.StartAsync((IControl)btn.FocusableControl, 18, 3);

        AssertBg(session, 2, 1, 40, 70, 120);     // initial: Normal fill on the middle row

        ((IMouseListener)btn).OnMouseDown(new Position(3, 1));
        await session.FrameAsync();
        AssertBg(session, 2, 1, 90, 130, 200);    // pressed: Press fill
        AssertFg(session, 2, 0, 63, 91, 140);     // pressed: bevel inverted (darker edge on top)

        ((IMouseListener)btn).OnMouseUp(new Position(3, 1));
        await session.FrameAsync();
        AssertBg(session, 2, 1, 40, 70, 120);     // released: back to Normal fill (no stale pressed cells)
        AssertFg(session, 2, 0, 104, 125, 160);   // released: bevel un-inverted (lighter edge on top)

        var lines = ConsoleSnapshot.ToText(session.Screen.Buffer).Split('\n');
        Assert.Contains("OK", lines[1]);
        Assert.Contains("▔", lines[0]);
        Assert.Contains("▁", lines[2]);
    }

    private static void AssertBg(AnsiConsoleSession s, int x, int y, int r, int g, int b)
    {
        var c = s.Screen.Buffer[x, y].Character.Background;
        Assert.NotNull(c);
        Assert.Equal((r, g, b), ((int)c!.Value.Red, (int)c.Value.Green, (int)c.Value.Blue));
    }

    private static void AssertFg(AnsiConsoleSession s, int x, int y, int r, int g, int b)
    {
        var c = s.Screen.Buffer[x, y].Character.Foreground;
        Assert.NotNull(c);
        Assert.Equal((r, g, b), ((int)c!.Value.Red, (int)c.Value.Green, (int)c.Value.Blue));
    }

    [Fact]
    public async Task Ansi_ColourSurvivesDecorationReset()
    {
        // Two adjacent cells, same colours, but the decoration drops Bold→None. The None case emits ESC[m which
        // resets fg/bg in the terminal, so the encoder must re-emit the colours on the next cell even though they
        // "didn't change". Regression for that latent bug.
        var red = new CColor(200, 0, 0);
        var blue = new CColor(0, 0, 200);
        var grid = new CellGrid(new[,]
        {
            { Glyph('A', red, blue, CDecoration.Bold), Glyph('B', red, blue, CDecoration.None) },
        });

        var screen = await AnsiConsoleSnapshot.RenderAsync(grid, 2, 1);
        var b = screen.Buffer[1, 0].Character;

        Assert.Equal('B', b.Content);
        Assert.NotNull(b.Foreground);
        Assert.Equal((200, 0, 0), ((int)b.Foreground!.Value.Red, (int)b.Foreground.Value.Green, (int)b.Foreground.Value.Blue));
        Assert.NotNull(b.Background);
        Assert.Equal((0, 0, 200), ((int)b.Background!.Value.Red, (int)b.Background.Value.Green, (int)b.Background.Value.Blue));
    }

    [Fact]
    public async Task Ansi_DecorationsDoNotAccumulate()
    {
        // Bold then Italic on adjacent cells. SGR codes accumulate in a terminal, so the encoder must clear the old
        // decoration — otherwise the second cell becomes Bold+Italic. Regression for that latent bug.
        var grid = new CellGrid(new[,]
        {
            { Glyph('A', null, null, CDecoration.Bold), Glyph('B', null, null, CDecoration.Italic) },
        });

        var screen = await AnsiConsoleSnapshot.RenderAsync(grid, 2, 1);
        var b = screen.Buffer[1, 0].Character;

        Assert.Equal(CDecoration.Italic, b.Decoration);   // not Bold | Italic
    }

    [Fact]
    public async Task Ansi_ModernButton_BottomBevelKeepsFillBackground()
    {
        // The bottom bevel row follows the Bold label row (Bold→None), so it triggers the colour-after-reset bug:
        // its fill background must survive. Regression for the visible "black bottom edge" glitch.
        var btn = new Button("OK") { Style = ButtonStyle.Primary.WithShape(ButtonShape.Modern) };

        var screen = await AnsiConsoleSnapshot.RenderAsync(btn, 18, 3);
        var bg = screen.Buffer[2, 2].Character.Background;   // bottom row, inside the bevel

        Assert.NotNull(bg);
        Assert.Equal((40, 70, 120), ((int)bg!.Value.Red, (int)bg.Value.Green, (int)bg.Value.Blue));
    }

    private static CCell Glyph(char c, CColor? fg, CColor? bg, CDecoration deco) =>
        new(new CCharacter(c, fg, bg, deco));
}

/// <summary>A fixed-size control that returns caller-specified cells, for testing the ANSI encoder with precise
/// cell patterns. <c>cells[y, x]</c>.</summary>
internal sealed class CellGrid(CCell[,] cells) : ConsoleGUI.Common.Control
{
    private readonly int _height = cells.GetLength(0);
    private readonly int _width = cells.GetLength(1);

    public override CCell this[ConsoleGUI.Space.Position position] =>
        position.X >= 0 && position.X < _width && position.Y >= 0 && position.Y < _height
            ? cells[position.Y, position.X]
            : new CCell(CCharacter.Empty);

    protected override void Initialize() => Resize(new ConsoleGUI.Space.Size(_width, _height));
}
