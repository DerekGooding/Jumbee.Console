namespace Jumbee.Console.Drawing;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

/// <summary>The point density of a <see cref="WorldMap"/> — how finely the coastlines are sampled.</summary>
public enum MapResolution
{
    /// <summary>~1166 points — coarse; fine with a 1×1 marker such as <see cref="CanvasMarker.Dot"/>. The default.</summary>
    Low,
    /// <summary>~5125 points — detailed; best drawn with <see cref="CanvasMarker.Braille"/> for a crisp coastline.</summary>
    High,
}

/// <summary>
/// A world map: the Earth's coastlines as a cloud of longitude/latitude points (EPSG:4326), for drawing on a
/// <see cref="Jumbee.Console.Canvas"/> whose bounds are set to <c>X [-180, 180]</c> and <c>Y [-90, 90]</c> (a
/// sub-region zooms in). Ported from Ratatui's <c>Map</c>; the point data is Natural Earth coastline data.
/// </summary>
public sealed class WorldMap : IShape
{
    /// <param name="color">Colour of the map points.</param>
    /// <param name="resolution">Point density (see <see cref="MapResolution"/>). Default <see cref="MapResolution.Low"/>.</param>
    public WorldMap(Color color, MapResolution resolution = MapResolution.Low)
    {
        Color = color;
        Resolution = resolution;
    }

    public Color Color { get; }
    public MapResolution Resolution { get; }

    void IShape.Draw(Painter painter)
    {
        CColor color = Color;
        foreach (var (x, y) in WorldMapData.Get(Resolution))
            if (painter.GetPoint(x, y) is (int gx, int gy))
                painter.Paint(gx, gy, color);
    }
}

/// <summary>Loads and caches the embedded world-coastline point tables (one lon,lat pair per line).</summary>
internal static class WorldMapData
{
    public static (double X, double Y)[] Get(MapResolution resolution) =>
        resolution == MapResolution.High
            ? _high ??= Load("Jumbee.Console.Geo.world_high.txt")
            : _low ??= Load("Jumbee.Console.Geo.world_low.txt");

    private static (double X, double Y)[] Load(string resource)
    {
        using var stream = typeof(WorldMapData).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Embedded map data '{resource}' not found.");
        using var reader = new StreamReader(stream);

        var points = new List<(double, double)>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            int comma = line.IndexOf(',');
            if (comma < 0) continue;
            // Invariant culture: the data uses '.' as the decimal separator regardless of the current locale.
            double x = double.Parse(line.AsSpan(0, comma), CultureInfo.InvariantCulture);
            double y = double.Parse(line.AsSpan(comma + 1), CultureInfo.InvariantCulture);
            points.Add((x, y));
        }
        return [.. points];
    }

    private static (double X, double Y)[]? _low;
    private static (double X, double Y)[]? _high;
}
