namespace Jumbee.Console.DocumentViewers;

using System;
using System.Collections.Generic;

using Mermaider.Models;

using CColor = ConsoleGUI.Data.Color;

/// <summary>
/// Rasterizes a Mermaider <see cref="PositionedErDiagram"/> to a <see cref="CellCanvas"/>: relationships (polylines,
/// dashed when non-identifying, with cardinality labels at each end) first, then entity tables (name header + typed
/// attribute rows with PK/FK/UK keys) on top. Coordinates are SVG pixels scaled to cells like the other renderers.
/// </summary>
internal sealed class MermaidErRenderer
{
    #region Constructors
    public MermaidErRenderer(MermaidStyles styles) => _s = styles;
    #endregion

    #region Methods
    public CellCanvas Render(PositionedErDiagram diagram)
    {
        _xDiv = _s.ScaleX <= 0 ? 9.0 : _s.ScaleX;
        // Attribute rows are dense (borders + header separator cost full cell rows), so use a finer vertical scale
        // than the flowchart's ~18 to avoid clipping rows — same reasoning as the class renderer.
        _yDiv = Math.Min(_s.ScaleY <= 0 ? 18.0 : _s.ScaleY, 14.0);

        var w = (int)Math.Ceiling(diagram.Width / _xDiv) + 3;
        var h = (int)Math.Ceiling(diagram.Height / _yDiv) + 3;
        var canvas = new CellCanvas(w, h);

        foreach (var rel in diagram.Relationships)
            DrawRelationship(canvas, rel);

        foreach (var e in diagram.Entities)
            DrawEntity(canvas, e);

        return canvas;
    }

    private void DrawEntity(CellCanvas canvas, PositionedErEntity e)
    {
        int x0 = CX(e.X), y0 = CY(e.Y), x1 = CX(e.X + e.Width), y1 = CY(e.Y + e.Height);
        if (x1 < x0 + 2) x1 = x0 + 2;
        if (y1 < y0 + 2) y1 = y0 + 2;

        CColor? border = _s.NodeBorder;
        canvas.Box(x0, y0, x1, y1, rounded: false, border);

        var sep = Math.Clamp(y0 + (int)Math.Round(e.HeaderHeight / _yDiv), y0 + 1, y1 - 1);
        Separator(canvas, x0, x1, sep, border);
        Centered(canvas, e.Label, (x0 + x1) / 2, Math.Clamp(y0 + 1, y0 + 1, sep - 1 < y0 + 1 ? y0 + 1 : sep - 1), _s.NodeLabel);

        var y = sep + 1;
        foreach (var a in e.Attributes)
        {
            if (y >= y1) break;
            canvas.Text(x0 + 1, y, Clip(FormatAttr(a), x1 - x0 - 1), _s.Member);
            y++;
        }
    }

    private static string FormatAttr(ErAttributeInfo a)
    {
        var s = $"{a.Type} {a.Name}";
        if (a.Keys.Count > 0) s += " " + string.Join(",", a.Keys);   // PK / FK / UK
        return s;
    }

    private void DrawRelationship(CellCanvas canvas, PositionedErRelationship rel)
    {
        var path = BuildPath(rel.Points);
        if (path.Count == 0) return;

        // Non-identifying relationships are drawn dashed (mermaid's `..` connector).
        var style = rel.Identifying ? CellCanvas.LineStyle.Normal : CellCanvas.LineStyle.Dashed;
        CColor? line = _s.Edge;
        for (var k = 0; k < path.Count - 1; k++)
            canvas.Link(path[k], path[k + 1], line, style);

        // Compact crow's-foot cardinality glyphs sit right beside each entity; the verb label rides the midpoint, so
        // all three fit on a short relationship line without colliding.
        var e1 = path[Math.Min(1, path.Count - 1)];
        var e2 = path[Math.Max(0, path.Count - 2)];
        Centered(canvas, Cardinality(rel.Cardinality1), e1.x, e1.y, _s.EdgeLabel);
        Centered(canvas, Cardinality(rel.Cardinality2), e2.x, e2.y, _s.EdgeLabel);
        if (!string.IsNullOrEmpty(rel.Label))
        {
            var mid = path[path.Count / 2];
            Centered(canvas, rel.Label, mid.x, mid.y, _s.EdgeLabel);
        }
    }

    // Crow's-foot-style: ‖ exactly-one, o zero-or-one, « (crow's foot) many, o« zero-or-many.
    private static string Cardinality(ErCardinality c) => c switch
    {
        ErCardinality.One => "‖",
        ErCardinality.ZeroOne => "o",
        ErCardinality.Many => "«",
        ErCardinality.ZeroMany => "o«",
        _ => "",
    };

    private static void Separator(CellCanvas canvas, int x0, int x1, int y, CColor? color)
    {
        canvas.SetChar(x0, y, '├', color);
        canvas.SetChar(x1, y, '┤', color);
        for (var x = x0 + 1; x < x1; x++) canvas.SetChar(x, y, '─', color);
    }

    private static void Centered(CellCanvas canvas, string text, int cx, int cy, CColor? color)
    {
        var t = text.Replace('\n', ' ');
        canvas.Text(cx - t.Length / 2, cy, t, color);
    }

    private List<(int x, int y)> BuildPath(IReadOnlyList<Point> points)
    {
        var path = new List<(int x, int y)>();
        for (var i = 0; i < points.Count - 1; i++)
        {
            var (ax, ay) = (CX(points[i].X), CY(points[i].Y));
            var (bx, by) = (CX(points[i + 1].X), CY(points[i + 1].Y));
            foreach (var cell in CellCanvas.OrthoLine(ax, ay, bx, by))
                if (path.Count == 0 || path[^1] != cell)
                    path.Add(cell);
        }
        return path;
    }

    private static string Clip(string s, int maxW)
    {
        if (maxW <= 0) return string.Empty;
        if (s.Length <= maxW) return s;
        return maxW == 1 ? "…" : string.Concat(s.AsSpan(0, maxW - 1), "…");
    }

    private int CX(double px) => (int)Math.Round(px / _xDiv) + 1;
    private int CY(double px) => (int)Math.Round(px / _yDiv) + 1;
    #endregion

    #region Fields
    private readonly MermaidStyles _s;
    private double _xDiv = 9.0;
    private double _yDiv = 14.0;
    #endregion
}
