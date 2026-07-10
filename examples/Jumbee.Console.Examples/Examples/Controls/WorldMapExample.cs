namespace Jumbee.Console.Examples;

using System.Linq;

using Jumbee.Console;
using Jumbee.Console.Drawing;

/// <summary>
/// A braille <see cref="WorldMap"/> coastline with a <see cref="CanvasMarker.Dot"/> layer of labelled cities.
/// A capstone for the <see cref="Canvas"/> control combining per-layer markers, text labels and pan/zoom.
/// </summary>
public sealed class WorldMapExample : Canvas, IExample
{
    #region Constructors
    public WorldMapExample()
    {
        WithMarker(CanvasMarker.Braille).WithXBounds(-180, 180).WithYBounds(-90, 90);
        Interactive = true;   // drag to pan, wheel/±  to zoom, arrows to pan

        // Layer 1: the coastline in fine braille.
        Add(new WorldMap(Land, MapResolution.High));

        // Layer 2: city markers as bold dots over the braille, then their labels on top.
        Layer(CanvasMarker.Dot);
        Add(new Points([.. Cities.Select(c => (c.Lon, c.Lat))], City));
        foreach (var c in Cities)
            Print(c.Lon + 3, c.Lat, c.Name, LabelColor);   // nudge the text east of the marker
    }
    #endregion

    #region Fields
    private static readonly Color Land = new(70, 120, 95);
    private static readonly Color City = new(245, 185, 90);
    private static readonly Color LabelColor = new(232, 232, 236);

    private static readonly (double Lon, double Lat, string Name)[] Cities =
    [
        (-0.13, 51.51, "London"),
        (-74.01, 40.71, "New York"),
        (139.69, 35.69, "Tokyo"),
        (151.21, -33.87, "Sydney"),
        (31.24, 30.04, "Cairo"),
        (-43.17, -22.91, "Rio"),
        (37.62, 55.75, "Moscow"),
        (-58.38, -34.60, "Buenos Aires"),
    ];
    #endregion

    #region IExample
    string IExample.Category => "Controls";
    string IExample.Title => "World Map";
    string IExample.Description =>
        "An explorable braille world map with labelled cities — drag to pan, scroll to zoom. Combines the Canvas map, per-layer markers, text labels and pan/zoom.";
    #endregion
}
