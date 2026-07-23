namespace Jumbee.Console.Documents;

using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using CColor = ConsoleGUI.Data.Color;

/// <summary>
/// Adapts a <see cref="CellCanvas"/> to Spectre.Console's <see cref="IRenderable"/>: each row is emitted as
/// foreground-coloured <see cref="Segment"/>s (consecutive same-colour cells merged into one segment) followed by a
/// line break. This lets a rasterized diagram drop into any renderable tree — a <see cref="Panel"/>, table cell or
/// stacked document — so viewers built on renderable composition can embed cell-grid graphics. The canvas is treated
/// as opaque foreground-only: cell backgrounds are the terminal default.
/// </summary>
internal sealed class CellCanvasRenderable : IRenderable
{
    #region Constructors

    public CellCanvasRenderable(CellCanvas canvas) => _canvas = canvas;

    #endregion Constructors

    #region Methods

    public Measurement Measure(RenderOptions options, int maxWidth)
    {
        var w = Math.Min(_canvas.Width, maxWidth);
        return new Measurement(w, w);   // intrinsic width; clips (never wraps) if the diagram is wider than allotted
    }

    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var w = Math.Min(_canvas.Width, maxWidth);
        for (var y = 0; y < _canvas.Height; y++)
        {
            var x = 0;
            while (x < w)
            {
                var color = _canvas.ColorAt(x, y);
                var sb = new StringBuilder();
                // Merge the run of cells sharing this colour into a single styled segment.
                while (x < w && SameColor(_canvas.ColorAt(x, y), color))
                {
                    sb.Append(_canvas.GlyphAt(x, y));
                    x++;
                }
                yield return new Segment(sb.ToString(), StyleFor(color));
            }
            yield return Segment.LineBreak;
        }
    }

    private static Style StyleFor(CColor? color) =>
        color is { } c ? new Style(foreground: new Color(c.Red, c.Green, c.Blue)) : Style.Plain;

    private static bool SameColor(CColor? a, CColor? b)
    {
        if (a is { } ca && b is { } cb) return ca.Red == cb.Red && ca.Green == cb.Green && ca.Blue == cb.Blue;
        return !a.HasValue && !b.HasValue;
    }

    #endregion Methods

    #region Fields

    private readonly CellCanvas _canvas;

    #endregion Fields
}