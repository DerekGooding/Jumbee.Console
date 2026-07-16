namespace Jumbee.Console.Tests;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>
/// The globe must actually look like Earth. These pin the two things that were silently wrong — the baked land/ocean
/// mask (whole continents rendered as sea) and the camera's handedness (the world mirrored east-west) — neither of
/// which any other test could see, because the rest compare a render only against another render.
/// <para>
/// Known limits of a 1:110m mask on a 400x200 grid (~0.9°/cell), asserted below so they aren't mistaken for bugs:
/// the Panama isthmus is narrower than a cell, and the Great Lakes are absent (they live in Natural Earth's lakes
/// layer, not its land layer).
/// </para>
/// </summary>
public class GlobeGeographyTests
{
    // Continental interiors — well inland, so a coarse mask can't be blamed for a near-coast disagreement.
    [Theory]
    [InlineData("Sahara", 15, 0)]
    [InlineData("Congo basin", -1, 23)]
    [InlineData("Sudan", 15, 30)]
    [InlineData("Amazon basin", -5, -63)]
    [InlineData("Kansas", 38, -98)]
    [InlineData("Siberia", 62, 95)]
    [InlineData("India", 22, 79)]
    [InlineData("Australian outback", -25, 133)]
    [InlineData("Antarctica", -80, 0)]
    [InlineData("Greenland", 72, -40)]
    // Islands: each is its own ring, so these catch a fill that only handles the big landmasses.
    [InlineData("Great Britain", 54, -2)]
    [InlineData("Ireland", 53, -8)]
    [InlineData("Iceland", 65, -19)]
    [InlineData("Madagascar", -20, 47)]
    [InlineData("Japan (Honshu)", 36, 138)]
    [InlineData("New Zealand (South)", -44, 170)]
    [InlineData("Cuba", 22, -79)]
    [InlineData("Sri Lanka", 7.5, 81)]
    [InlineData("Borneo", 0, 114)]
    [InlineData("New Guinea", -5, 141)]
    public void KnownLand_IsLand(string place, double lat, double lon)
    {
        EarthMask.Instance.Sample(lat, lon, out bool land, out _, out _);
        Assert.True(land, $"{place} ({lat}, {lon}) should be land");
    }

    [Theory]
    [InlineData("Mid-Atlantic", 0, -30)]
    [InlineData("Mid-Pacific", 0, -150)]
    [InlineData("Indian Ocean", -25, 75)]
    [InlineData("North Atlantic", 45, -40)]
    [InlineData("Southern Ocean", -55, 0)]
    // Enclosed seas: land polygons carry them as HOLES, so these catch a fill that ignores hole rings and floods
    // them with land (the even-odd rule handles it — an inner ring simply flips parity back).
    [InlineData("Caspian Sea", 42, 51)]
    [InlineData("Black Sea", 43, 34)]
    [InlineData("Mediterranean", 35, 18)]
    [InlineData("Hudson Bay", 60, -86)]
    [InlineData("Red Sea", 20, 38)]
    [InlineData("Persian Gulf", 27, 51)]
    [InlineData("Baltic Sea", 58, 20)]
    public void KnownOcean_IsOcean(string place, double lat, double lon)
    {
        EarthMask.Instance.Sample(lat, lon, out bool land, out _, out _);
        Assert.False(land, $"{place} ({lat}, {lon}) should be ocean");
    }

    // The mask's documented blind spots. Pinned deliberately: they are the data/resolution talking, not a defect,
    // and if a future change fixes them these should be flipped rather than deleted.
    [Fact]
    public void CoarseMask_MissesSubCellFeatures()
    {
        // The Panama isthmus (~50 km) is far narrower than a ~100 km cell, so the Americas read as separated.
        EarthMask.Instance.Sample(9, -80, out bool panama, out _, out _);
        Assert.False(panama);

        // Natural Earth's land layer doesn't cut out lakes, so the Great Lakes are land here.
        EarthMask.Instance.Sample(47.5, -87, out bool superior, out _, out _);
        Assert.True(superior);
    }

    // Orientation, end to end through the renderer: with no rotation or tilt the disc centre is (0°N, 0°E) — the Gulf
    // of Guinea — so equatorial AFRICA must appear to the RIGHT of centre (east) and the open Atlantic to the LEFT.
    // A left-handed camera basis mirrors the two, which is what put the Americas the wrong way round.
    [Fact]
    public void EastIsRight_TheGlobeIsNotMirrored()
    {
        // 80x40 with 2:1 cells puts the disc's horizontal radius at 40 cells, so ±30° of longitude on the equator
        // lands ±20 cells from the centre column.
        var globe = new Globe { DisplayNight = false, Colored = true, RotationAngle = 0, CameraAlpha = 0, CameraBeta = 0 };
        var buffer = ConsoleSnapshot.Render(globe, 80, 40);

        // Row 19's upper half-ray samples ~2°N; the fg of a ▀ cell is that upper sample.
        var east = buffer[59, 19].Character.Foreground!.Value;   // ~(2°N, 30°E) — equatorial Africa
        var west = buffer[20, 19].Character.Foreground!.Value;   // ~(2°N, 30°W) — mid-Atlantic

        // The ramps are unambiguous: land greens/browns have G > B, ocean blues have B > G.
        Assert.True(east.Green > east.Blue, $"30°E of centre should be African land, got rgb({east.Red},{east.Green},{east.Blue})");
        Assert.True(west.Blue > west.Green, $"30°W of centre should be Atlantic ocean, got rgb({west.Red},{west.Green},{west.Blue})");
    }
}
