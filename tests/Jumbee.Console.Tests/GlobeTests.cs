namespace Jumbee.Console.Tests;

using System;

using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

public class GlobeTests
{
    public GlobeTests() => UiTestHarness.EnsureStopped();

    private static int NonBlank(string text)
    {
        var n = 0;
        foreach (var c in text)
            if (c is not ' ' and not '\n' and not '\r') n++;
        return n;
    }

    // The globe encodes the surface in per-cell COLOUR (a uniform half-block glyph), so image changes show up in the
    // foreground colours, not the glyphs ConsoleSnapshot.ToText sees. This signs a render by its foreground colours.
    private static string ColorSig(Globe g, int w, int h)
    {
        var buf = ConsoleSnapshot.Render(g, w, h);
        var sb = new System.Text.StringBuilder(w * h * 4);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var fg = buf[new Position(x, y)].Character.Foreground;
                sb.Append(fg is { } c ? $"{c.Red},{c.Green},{c.Blue};" : "_;");
            }
        return sb.ToString();
    }

    [Fact]
    public void Globe_DrawsACenteredDisc()
    {
        // The globe fills a centred disc: the middle is painted, the four corners fall outside the sphere.
        var g = new Globe { DisplayNight = false };
        var text = ConsoleSnapshot.ToText(g, 40, 20);
        var rows = text.Split('\n');

        Assert.True(NonBlank(text) > 200, "the disc should paint a large filled region");
        Assert.Equal(' ', rows[0][0]);            // top-left corner is outside the sphere
        Assert.NotEqual(' ', rows[10][20]);       // the centre is on the sphere
    }

    [Fact]
    public void Globe_RotationChangesTheImage()
    {
        var a = ColorSig(new Globe { DisplayNight = false, RotationAngle = 0.0 }, 40, 20);
        var b = ColorSig(new Globe { DisplayNight = false, RotationAngle = 1.5 }, 40, 20);
        Assert.NotEqual(a, b);   // spinning the globe re-maps the texture (different surface colours)
    }

    [Fact]
    public void Globe_SpinAdvancesRotation()
    {
        var g = new Globe { RotationAngle = 0 };
        g.Spin(0.1);
        Assert.Equal(0.1, g.RotationAngle, 5);
    }

    [Fact]
    public void Globe_Spin_ActuallyRotatesTheSurface()
    {
        // Regression: Spin must scroll the texture, not just bump the angle field. (A previous version also
        // counter-orbited the camera by half the angle, which exactly cancelled the scroll — the globe sat still
        // and only the shading moved.) Render, spin a few times, render again: the image must change.
        var g = new Globe { DisplayNight = false };
        var before = ColorSig(g, 56, 26);
        for (int i = 0; i < 10; i++) g.Spin(0.05);
        var after = ColorSig(g, 56, 26);
        Assert.NotEqual(before, after);
    }

    [Fact]
    public void Globe_SetLight_ChangesShading()
    {
        // The light direction drives the day/night shading, so moving it changes the rendered image.
        var a = new Globe { DisplayNight = true, RotationAngle = 1.4 };
        var before = ColorSig(a, 56, 26);
        a.SetLight(-1, 0.5, -0.5, 2.0);   // light swung to the opposite side
        var after = ColorSig(a, 56, 26);
        Assert.NotEqual(before, after);
    }

    [Fact]
    public void Globe_NightShadingDiffersFromFlat()
    {
        // Night shading darkens the shadow side (and blends the dark-side texture), so the globe differs from the
        // evenly-lit one. Compare the buffers' colours, not just glyphs: the shadow-side shade scales each cell's
        // colour even where the day/night glyph happens to match (ConsoleSnapshot.ToText only sees glyphs).
        const int w = 56, h = 26;
        var flat = ConsoleSnapshot.Render(new Globe { DisplayNight = false, RotationAngle = 1.4 }, w, h);
        var night = ConsoleSnapshot.Render(new Globe { DisplayNight = true, RotationAngle = 1.4 }, w, h);

        bool anyDiff = false;
        for (int y = 0; y < h && !anyDiff; y++)
            for (int x = 0; x < w && !anyDiff; x++)
            {
                var p = new ConsoleGUI.Space.Position(x, y);
                if (!Equals(flat[p].Character.Foreground, night[p].Character.Foreground)) anyDiff = true;
            }
        Assert.True(anyDiff, "night shading should darken the shadow side relative to the evenly-lit globe");
    }

    #region Interactive input
    [Fact]
    public void Interactive_EnablingMakesTheGlobeFocusable()
    {
        var g = new Globe();
        Assert.False(g.Focusable);        // display-only by default
        g.Interactive = true;
        Assert.True(g.Focusable);         // opts into keyboard navigation
    }

    [Fact]
    public void Interactive_DragRotatesAndTilts()
    {
        var g = new Globe { Interactive = true, RotationAngle = 0, CameraBeta = 0.35 };
        var m = (IMouseListener)g;

        m.OnMouseDown(new Position(0, 0));
        m.OnMouseMove(new Position(5, 3));   // 5 cells right, 3 down
        m.OnMouseUp(new Position(5, 3));

        Assert.Equal(0.15, g.RotationAngle, 6);   // dx 5 * 0.03
        Assert.Equal(0.44, g.CameraBeta, 6);      // 0.35 + dy 3 * 0.03
    }

    [Fact]
    public void Interactive_WheelZooms()
    {
        var g = new Globe { Interactive = true };   // Zoom default 1.35
        var w = (IMouseWheelListener)g;

        w.OnMouseWheel(new Position(0, 0), -1);     // wheel up → zoom in (smaller Zoom)
        Assert.Equal(1.25, g.Zoom, 6);
        w.OnMouseWheel(new Position(0, 0), 2);      // wheel down → zoom out
        Assert.Equal(1.45, g.Zoom, 6);
    }

    [Fact]
    public void Interactive_ArrowKeysSpinAndTilt()
    {
        var g = new Globe { Interactive = true, RotationAngle = 0, CameraBeta = 0 };

        UI.SendInput(g, ConsoleKey.RightArrow);
        Assert.Equal(0.15, g.RotationAngle, 6);
        UI.SendInput(g, ConsoleKey.LeftArrow);
        Assert.Equal(0.0, g.RotationAngle, 6);

        UI.SendInput(g, ConsoleKey.UpArrow);
        Assert.Equal(0.1, g.CameraBeta, 6);

        // Shift takes a larger step.
        UI.SendInput(g, ConsoleKey.RightArrow, shift: true);
        Assert.Equal(0.5, g.RotationAngle, 6);
    }

    [Fact]
    public void Interactive_PlusMinusZoom()
    {
        var g = new Globe { Interactive = true };   // Zoom 1.35
        UI.SendInput(g, ConsoleKey.OemMinus);       // '-' zooms out
        Assert.Equal(1.45, g.Zoom, 6);
        UI.SendInput(g, ConsoleKey.Add);            // '+' zooms in
        Assert.Equal(1.35, g.Zoom, 6);
    }

    [Fact]
    public void NonInteractive_IgnoresDragWheelAndKeys()
    {
        var g = new Globe { RotationAngle = 0 };    // Interactive false by default
        double zoom = g.Zoom;
        var m = (IMouseListener)g;

        m.OnMouseDown(new Position(0, 0));
        m.OnMouseMove(new Position(9, 9));
        m.OnMouseUp(new Position(9, 9));
        ((IMouseWheelListener)g).OnMouseWheel(new Position(0, 0), -3);
        UI.SendInput(g, ConsoleKey.RightArrow);

        Assert.Equal(0.0, g.RotationAngle, 6);
        Assert.Equal(zoom, g.Zoom, 6);              // wheel fell through to (absent) frame scroll, not zoom
    }
    #endregion
}
