namespace Jumbee.Console.Examples;

using System;
using System.Collections.Generic;
using System.Threading;

using Jumbee.Console;
using Jumbee.Console.Drawing;

/// <summary>
/// The same live drawing — a ring with a rotating cross and an orbiting blip — rendered side by side in every
/// <see cref="CanvasMarker"/>, so the sub-cell resolution of each is directly comparable: Braille (2×4) and Quadrant
/// (2×2) resolve the curve finely, HalfBlock (1×2) adds per-half colour, and Block/Bar/Dot (1×1) are the coarse
/// single-glyph markers. A grid of framed <see cref="Canvas"/> panes driven by one <see cref="Control.Feed"/>.
/// </summary>
public sealed class CanvasMarkersExample : CompositeControl, IExample, IActivatable
{
    #region Constructors
    public CanvasMarkersExample()
    {
        Control Pane(CanvasMarker marker, string title)
        {
            var canvas = new Canvas().WithMarker(marker).WithXBounds(-1.15, 1.15).WithYBounds(-1.15, 1.15);
            _canvases.Add(canvas);
            return canvas.WithFrame(BorderStyle.Rounded, title: title);
        }

        var grid = new Grid([13, 13], [30, 30, 30],
        [
            [Pane(CanvasMarker.Braille, "Braille 2×4"), Pane(CanvasMarker.HalfBlock, "HalfBlock 1×2"), Pane(CanvasMarker.Quadrant, "Quadrant 2×2")],
            [Pane(CanvasMarker.Block, "Block 1×1"), Pane(CanvasMarker.Bar, "Bar 1×1"), Pane(CanvasMarker.Dot, "Dot 1×1")],
        ]);
        SetContent(grid);

        Draw();   // a first static frame before the feed starts
    }
    #endregion

    #region Live feed
    public void OnActivated() => _feed = Feed(() => { _t += 0.03; Draw(); }, 50);

    public void OnDeactivated()
    {
        _feed?.Cancel();
        _feed = null;
    }

    // Redraws the identical scene into every pane; only the marker differs, so the panes read as a resolution ladder.
    private void Draw()
    {
        double a = _t;
        double cx = Math.Cos(a), cy = Math.Sin(a);
        double px = Math.Cos(a + Math.PI / 2), py = Math.Sin(a + Math.PI / 2);
        double bx = 0.75 * Math.Cos(-a * 1.6), by = 0.75 * Math.Sin(-a * 1.6);

        foreach (var canvas in _canvases)
        {
            canvas.Clear();
            canvas.Add(new Circle(0, 0, 1.0, Ring));            // fixed ring
            canvas.Add(new Line(-cx, -cy, cx, cy, Spoke));      // rotating diameter
            canvas.Add(new Line(-px, -py, px, py, Spoke));      // perpendicular diameter
            canvas.Add(new Circle(bx, by, 0.08, Blip));         // counter-orbiting blip
        }
    }
    #endregion

    #region IExample
    public bool FillsPane => true;
    public string Category => "Controls";
    public string Title => "Canvas Markers";
    public string Description =>
        "The same live drawing in every Canvas marker, side by side — a direct comparison of sub-cell resolution (Braille/Quadrant/HalfBlock vs Block/Bar/Dot).";
    #endregion

    #region Fields
    private static readonly Color Ring = new(70, 95, 105);
    private static readonly Color Spoke = new(120, 205, 230);
    private static readonly Color Blip = new(240, 180, 90);

    private readonly List<Canvas> _canvases = [];
    private double _t;
    private CancellationTokenSource? _feed;
    #endregion
}
