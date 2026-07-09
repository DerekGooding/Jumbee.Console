namespace Jumbee.Console.Tests;

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
        var a = ConsoleSnapshot.ToText(new Globe { DisplayNight = false, RotationAngle = 0.0 }, 40, 20);
        var b = ConsoleSnapshot.ToText(new Globe { DisplayNight = false, RotationAngle = 1.5 }, 40, 20);
        Assert.NotEqual(a, b);   // spinning the globe re-maps the texture
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
        var before = ConsoleSnapshot.ToText(g, 56, 26);
        for (int i = 0; i < 10; i++) g.Spin(0.05);
        var after = ConsoleSnapshot.ToText(g, 56, 26);
        Assert.NotEqual(before, after);
    }

    [Fact]
    public void Globe_SetLight_ChangesShading()
    {
        // The light direction drives the day/night shading, so moving it changes the rendered image.
        var a = new Globe { DisplayNight = true, RotationAngle = 1.4 };
        var before = ConsoleSnapshot.ToText(a, 56, 26);
        a.SetLight(-1, 0.5, -0.5, 2.0);   // light swung to the opposite side
        var after = ConsoleSnapshot.ToText(a, 56, 26);
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
}
