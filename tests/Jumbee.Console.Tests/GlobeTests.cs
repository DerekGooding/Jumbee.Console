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
    public void Globe_NightShadingDiffersFromFlat()
    {
        // Night shading blends in the dark-side texture, so the terminator side differs from the evenly-lit globe.
        var flat = ConsoleSnapshot.ToText(new Globe { DisplayNight = false }, 40, 20);
        var night = ConsoleSnapshot.ToText(new Globe { DisplayNight = true }, 40, 20);
        Assert.NotEqual(flat, night);
    }
}
