namespace Jumbee.Console.Examples;

using System;
using System.Collections.Generic;
using System.Threading;

using Jumbee.Console;
using Jumbee.Console.Drawing;

/// <summary>
/// A live drawing on the <see cref="Canvas"/> control — a braille <see cref="Circle"/> track, a rotating rose curve
/// traced with <see cref="Points"/>, a radar <see cref="Line"/> sweep, and an orbiting blip, all redrawn each tick
/// from a <see cref="Control.Feed"/>. Demonstrates the retained shape API (clear + re-add per frame) and sub-cell
/// braille rendering in an arbitrary coordinate system (origin bottom-left, y up).
/// </summary>
public sealed class CanvasExample : Canvas, IExample, IActivatable
{
    #region Constructors
    public CanvasExample()
    {
        WithMarker(CanvasMarker.Braille).WithXBounds(-1.1, 1.1).WithYBounds(-1.1, 1.1);
        Draw();   // a first static frame before the feed starts
    }
    #endregion

    #region Live feed
    // Advance the phase and redraw while shown; stop when hidden. The tick is posted onto the UI thread by Feed, so it
    // touches the retained shape list directly.
    public void OnActivated() => _feed = Feed(() => { _t += 0.04; Draw(); }, 33);

    public void OnDeactivated()
    {
        _feed?.Cancel();
        _feed = null;
    }

    private void Draw()
    {
        double a = _t;
        Clear();

        // The circular track the sweep rides, dim.
        Add(new Circle(0, 0, 1.0, Track));

        // A 3-petal rose r = cos(3θ), spun by the phase — the braille dots trace a smooth curve.
        var rose = new List<(double X, double Y)>(RosePoints);
        for (int i = 0; i < RosePoints; i++)
        {
            double th = i * (2 * Math.PI / RosePoints);
            double r = Math.Cos(3 * th);
            rose.Add((r * Math.Cos(th + a), r * Math.Sin(th + a)));
        }
        Add(new Points(rose, Rose));

        // Radar sweep from the centre to the rim.
        Add(new Line(0, 0, Math.Cos(a), Math.Sin(a), Sweep));

        // A blip orbiting the other way, drawn as a small filled-ish circle.
        double bx = 0.85 * Math.Cos(-a * 1.7), by = 0.85 * Math.Sin(-a * 1.7);
        Add(new Circle(bx, by, 0.07, Blip));
    }
    #endregion

    #region IExample
    public bool FillsPane => true;
    public string Category => "Controls";
    public string Title => "Canvas";
    public string Description =>
        "The Canvas control (ported from Ratatui): shapes drawn in an arbitrary coordinate system with sub-cell braille — a live rose curve, radar sweep and orbiting blip.";
    #endregion

    #region Fields
    private const int RosePoints = 480;
    private static readonly Color Track = new(45, 70, 70);
    private static readonly Color Rose = new(120, 205, 230);
    private static readonly Color Sweep = new(120, 240, 140);
    private static readonly Color Blip = new(240, 180, 90);

    private double _t;
    private CancellationTokenSource? _feed;
    #endregion
}
