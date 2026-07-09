namespace Jumbee.Console.Examples;

using System.Linq;

using Jumbee.Console;
using Jumbee.Console.Drawing;

/// <summary>
/// A braille world map with labelled cities — a capstone for the <see cref="Canvas"/> control that combines a
/// high-resolution <see cref="WorldMap"/> coastline, a second <see cref="CanvasMarker.Dot"/> layer of city markers
/// (per-layer markers: crisp dots over the fine braille coastline), and text labels drawn on top with
/// <see cref="Canvas.Print"/>. Display-only; the whole map re-fits when the pane is resized.
/// </summary>
public sealed class WorldMapExample : Canvas, IExample
{
    #region Constructors
    public WorldMapExample()
    {
        WithMarker(CanvasMarker.Braille).WithXBounds(-180, 180).WithYBounds(-90, 90);

        // Layer 1: the coastline in fine braille.
        Add(new WorldMap(Land, MapResolution.High));

        // Layer 2: city markers as bold dots over the braille, then their labels on top.
        Layer(CanvasMarker.Dot);
        Add(new Points([.. Cities.Select(c => (c.Lon, c.Lat))], City));
        foreach (var c in Cities)
            Print(c.Lon + 3, c.Lat, c.Name, LabelColor);   // nudge the text east of the marker
    }
    #endregion

    #region IExample
    public bool FillsPane => true;
    public string Category => "Controls";
    public string Title => "World Map";
    public string Description =>
        "A braille WorldMap coastline with a dot layer of labelled cities — the Canvas map, per-layer markers and text labels together.";
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
}
