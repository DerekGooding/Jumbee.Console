namespace Jumbee.Console.DocumentViewers;

using System;
using System.Collections.Generic;

using Mermaider.Models;

using CColor = ConsoleGUI.Data.Color;

/// <summary>
/// Rasterizes a Mermaider <see cref="PositionedGraph"/> (flowchart / state) to a <see cref="CellCanvas"/>: subgraph
/// groups, then rectilinear edges with arrowheads and labels, then node boxes on top. Coordinates are SVG pixels
/// scaled to cells via <see cref="MermaidStyles.ScaleX"/>/<see cref="MermaidStyles.ScaleY"/>.
/// </summary>
internal sealed class MermaidFlowchartRenderer
{
    #region Constructors
    public MermaidFlowchartRenderer(MermaidStyles styles) => _s = styles;
    #endregion

    #region Methods
    public CellCanvas Render(PositionedGraph graph)
    {
        _xDiv = _s.ScaleX <= 0 ? 9.0 : _s.ScaleX;
        _yDiv = _s.ScaleY <= 0 ? 18.0 : _s.ScaleY;

        var w = (int)Math.Ceiling(graph.Width / _xDiv) + 3;
        var h = (int)Math.Ceiling(graph.Height / _yDiv) + 3;
        var canvas = new CellCanvas(w, h);

        foreach (var group in graph.Groups)
            DrawGroup(canvas, group);

        foreach (var edge in graph.Edges)
            if (edge.Style != EdgeStyle.Invisible && edge.Points.Count >= 2)
                DrawEdge(canvas, edge);

        foreach (var edge in graph.Edges)
            if (edge.Style != EdgeStyle.Invisible && edge.LabelPosition is { } lp && !string.IsNullOrEmpty(edge.Label))
                DrawCentered(canvas, edge.Label!, CX(lp.X), CY(lp.Y), _s.EdgeLabel);

        foreach (var node in graph.Nodes)
            DrawNode(canvas, node);

        return canvas;
    }

    private void DrawGroup(CellCanvas canvas, PositionedGroup group)
    {
        int x0 = CX(group.X), y0 = CY(group.Y);
        int x1 = CX(group.X + group.Width), y1 = CY(group.Y + group.Height);
        if (x1 <= x0 + 1 || y1 <= y0 + 1) return;

        // Outline only (no interior clear) via the line-mask system, so edges that enter/cross the subgraph render
        // as clean junctions rather than punching holes in the border. Drawn before edges/nodes. Label on the top edge.
        canvas.LinkRect(x0, y0, x1, y1, _s.GroupBorder);
        if (!string.IsNullOrEmpty(group.Label))
            canvas.Text(x0 + 2, y0, Clip(group.Label, x1 - x0 - 3), _s.GroupLabel);

        foreach (var child in group.Children)
            DrawGroup(canvas, child);
    }

    private void DrawEdge(CellCanvas canvas, PositionedEdge edge)
    {
        var path = BuildPath(edge.Points);
        if (path.Count == 0) return;

        CColor? color = _s.Edge;
        var style = edge.Style switch
        {
            EdgeStyle.Thick => CellCanvas.LineStyle.Heavy,
            EdgeStyle.Dotted => CellCanvas.LineStyle.Dashed,
            _ => CellCanvas.LineStyle.Normal,
        };
        for (var k = 0; k < path.Count - 1; k++)
            canvas.Link(path[k], path[k + 1], color, style);

        if (edge.HasArrowEnd && path.Count >= 2)
        {
            // Back off one cell from the endpoint so the arrowhead sits beside the target node rather than under
            // the box border that gets painted on top.
            var tip = path.Count >= 3 ? path[^2] : path[^1];
            canvas.SetChar(tip.x, tip.y, Arrow(Dir(path[^2], path[^1])), _s.Arrow);
        }
        if (edge.HasArrowStart && path.Count >= 2)
        {
            var tip = path.Count >= 3 ? path[1] : path[0];
            canvas.SetChar(tip.x, tip.y, Arrow(Dir(path[1], path[0])), _s.Arrow);
        }
    }

    private void DrawNode(CellCanvas canvas, PositionedNode node)
    {
        int x0 = CX(node.X), y0 = CY(node.Y);
        int x1 = CX(node.X + node.Width), y1 = CY(node.Y + node.Height);
        if (x1 < x0 + 2) x1 = x0 + 2;
        if (y1 < y0 + 2) y1 = y0 + 2;

        var cx = (x0 + x1) / 2;
        var cy = (y0 + y1) / 2;

        switch (node.Shape)
        {
            case NodeShape.StateStart:
                canvas.SetChar(cx, cy, '●', _s.NodeBorder);
                return;
            case NodeShape.StateEnd:
                canvas.SetChar(cx, cy, '◉', _s.NodeBorder);
                return;
        }

        var rounded = node.Shape is NodeShape.Rounded or NodeShape.Stadium or NodeShape.Circle
            or NodeShape.DoubleCircle or NodeShape.StateStart or NodeShape.StateEnd;
        canvas.Box(x0, y0, x1, y1, rounded, _s.NodeBorder);

        // Diamonds (decisions) get angle markers on the side mid-cells to hint the rhombus shape.
        if (node.Shape is NodeShape.Diamond)
        {
            canvas.SetChar(x0, cy, '<', _s.NodeBorder);
            canvas.SetChar(x1, cy, '>', _s.NodeBorder);
        }

        DrawLabel(canvas, node.Label, x0 + 1, y0 + 1, x1 - 1, y1 - 1);
    }

    // Centres the (possibly multi-line) label within the interior box [ix0..ix1] x [iy0..iy1].
    private void DrawLabel(CellCanvas canvas, string label, int ix0, int iy0, int ix1, int iy1)
    {
        if (string.IsNullOrEmpty(label) || ix1 < ix0 || iy1 < iy0) return;
        var maxW = ix1 - ix0 + 1;
        var lines = label.Split('\n');
        var startY = iy0 + Math.Max(0, ((iy1 - iy0 + 1) - lines.Length) / 2);
        for (var i = 0; i < lines.Length; i++)
        {
            var y = startY + i;
            if (y > iy1) break;
            var text = Clip(lines[i], maxW);
            var x = ix0 + Math.Max(0, (maxW - text.Length) / 2);
            canvas.Text(x, y, text, _s.NodeLabel);
        }
    }

    private void DrawCentered(CellCanvas canvas, string text, int cx, int cy, CColor? color)
    {
        var t = text.Replace('\n', ' ');
        canvas.Text(cx - t.Length / 2, cy, t, color);
    }

    // Rasterizes the polyline into a contiguous run of cells (one step per cell), de-duplicating repeats.
    private List<(int x, int y)> BuildPath(IReadOnlyList<Point> points)
    {
        var path = new List<(int x, int y)>();
        for (var i = 0; i < points.Count - 1; i++)
        {
            var (ax, ay) = (CX(points[i].X), CY(points[i].Y));
            var (bx, by) = (CX(points[i + 1].X), CY(points[i + 1].Y));
            foreach (var cell in Line(ax, ay, bx, by))
                if (path.Count == 0 || path[^1] != cell)
                    path.Add(cell);
        }
        return path;
    }

    // Orthogonally-connected rasterization: every consecutive cell differs by exactly one axis, so a diagonal
    // segment becomes a connected staircase (proper ┌┐└┘ corners) instead of gaps. Mermaider's routing includes
    // gently-diagonal segments that a diagonal Bresenham would break apart. Steps are interleaved by a midpoint
    // comparison so the staircase tracks the ideal line (jogs distributed, not front-loaded).
    private static IEnumerable<(int x, int y)> Line(int x0, int y0, int x1, int y1)
    {
        int x = x0, y = y0;
        int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
        int sx = x0 <= x1 ? 1 : -1, sy = y0 <= y1 ? 1 : -1;
        int px = 0, py = 0;
        yield return (x, y);
        while (x != x1 || y != y1)
        {
            if (y == y1) x += sx;
            else if (x == x1) y += sy;
            else if ((2 * px + 1) * dy <= (2 * py + 1) * dx) { x += sx; px++; }
            else { y += sy; py++; }
            yield return (x, y);
        }
    }

    private static int Dir((int x, int y) a, (int x, int y) b) =>
        b.x > a.x ? 8 : b.x < a.x ? 4 : b.y > a.y ? 2 : 1;   // R,L,D,U

    private static char Arrow(int dir) => dir switch { 8 => '▶', 4 => '◀', 2 => '▼', _ => '▲' };

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
    private double _yDiv = 18.0;
    #endregion
}
