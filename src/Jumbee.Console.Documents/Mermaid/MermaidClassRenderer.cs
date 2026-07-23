
using Mermaider.Models;
using CColor = ConsoleGUI.Data.Color;

namespace Jumbee.Console.Documents;
/// <summary>
/// Rasterizes a Mermaider <see cref="PositionedClassDiagram"/> to a <see cref="CellCanvas"/>: relationships
/// (polylines with UML end-markers — inheritance/realization triangles, composition/aggregation diamonds,
/// association/dependency arrows, lollipop circles) first, then three-compartment class boxes (header + annotation,
/// attributes, methods) on top. Coordinates are SVG pixels scaled to cells like the flowchart renderer.
/// </summary>
internal sealed class MermaidClassRenderer(MermaidStyles styles)
{


    #region Methods

    public CellCanvas Render(PositionedClassDiagram diagram)
    {
        _xDiv = _s.ScaleX <= 0 ? 9.0 : _s.ScaleX;
        // Class boxes are dense: each border and each of the two compartment separators costs a full cell row that
        // the pixel layout doesn't budget for, so a member-per-24px height needs a finer vertical scale than the
        // flowchart's ~18 to avoid clipping members. Cap at 14 (still respect a smaller user ScaleY).
        _yDiv = Math.Min(_s.ScaleY <= 0 ? 18.0 : _s.ScaleY, 14.0);

        var w = (int)Math.Ceiling(diagram.Width / _xDiv) + 3;
        var h = (int)Math.Ceiling(diagram.Height / _yDiv) + 3;
        var canvas = new CellCanvas(w, h);

        foreach (var rel in diagram.Relationships)
            DrawRelationship(canvas, rel);

        foreach (var cls in diagram.Classes)
            DrawClass(canvas, cls);

        return canvas;
    }

    private void DrawClass(CellCanvas canvas, PositionedClassNode c)
    {
        int x0 = CX(c.X), y0 = CY(c.Y), x1 = CX(c.X + c.Width), y1 = CY(c.Y + c.Height);
        if (x1 < x0 + 2) x1 = x0 + 2;
        if (y1 < y0 + 2) y1 = y0 + 2;

        CColor? border = _s.NodeBorder;
        canvas.Box(x0, y0, x1, y1, rounded: false, border);

        // Compartment separators from the layout's compartment pixel heights.
        var sep1 = Math.Clamp(y0 + (int)Math.Round(c.HeaderHeight / _yDiv), y0 + 1, y1 - 1);
        var sep2 = Math.Clamp(y0 + (int)Math.Round((c.HeaderHeight + c.AttrHeight) / _yDiv), sep1, y1 - 1);
        Separator(canvas, x0, x1, sep1, border);
        if (sep2 > sep1) Separator(canvas, x0, x1, sep2, border);

        // Header: optional «annotation» then the class name, centred.
        var hy = y0 + 1;
        if (c.Annotation is { Length: > 0 } annot && hy < sep1)
            Centered(canvas, $"«{annot}»", (x0 + x1) / 2, hy++, _s.Annotation);
        if (hy < sep1 || hy == sep1 - 0)   // place the name on the last header row available
            Centered(canvas, c.Label, (x0 + x1) / 2, Math.Min(hy, sep1 - 1), _s.NodeLabel);

        // Members, left-aligned, one per row, clipped to the box.
        FillMembers(canvas, c.Attributes, x0, sep1 + 1, x1, sep2);
        FillMembers(canvas, c.Methods, x0, sep2 + 1, x1, y1);
    }

    private void FillMembers(CellCanvas canvas, IReadOnlyList<ClassMember> members, int x0, int top, int x1, int bottom)
    {
        var y = top;
        foreach (var m in members)
        {
            if (y >= bottom) break;
            canvas.Text(x0 + 1, y, Clip(FormatMember(m), x1 - x0 - 1), _s.Member);
            y++;
        }
    }

    private static string FormatMember(ClassMember m)
    {
        var vis = m.Visibility switch
        {
            ClassVisibility.Public => "+",
            ClassVisibility.Private => "-",
            ClassVisibility.Protected => "#",
            ClassVisibility.Package => "~",
            _ => " ",
        };
        if (m.IsMethod)
        {
            var sig = $"{vis}{m.Name}({m.Params ?? string.Empty})";
            return m.Type is { Length: > 0 } t ? $"{sig}: {t}" : sig;
        }
        return m.Type is { Length: > 0 } at ? $"{vis}{m.Name}: {at}" : $"{vis}{m.Name}";
    }

    private void DrawRelationship(CellCanvas canvas, PositionedClassRelationship rel)
    {
        var path = BuildPath(rel.Points);
        if (path.Count == 0) return;

        var dashed = rel.Type is ClassRelationType.Realization or ClassRelationType.Dependency;
        var style = dashed ? CellCanvas.LineStyle.Dashed : CellCanvas.LineStyle.Normal;
        CColor? line = _s.Edge;
        for (var k = 0; k < path.Count - 1; k++)
            canvas.Link(path[k], path[k + 1], line, style);

        // End-marker: which polyline end, and the direction pointing outward into that node.
        var atTo = rel.MarkerAt == ClassMarkerAt.To;
        var (tip, dir) = atTo
            ? (path.Count >= 3 ? path[^2] : path[^1], Dir(path[^2], path[^1]))
            : (path.Count >= 3 ? path[1] : path[0], Dir(path[1], path[0]));
        canvas.SetChar(tip.x, tip.y, Marker(rel.Type, dir), _s.Arrow);

        if (rel.LabelPosition is { } lp && !string.IsNullOrEmpty(rel.Label))
            Centered(canvas, rel.Label!, CX(lp.X), CY(lp.Y), _s.EdgeLabel);
        // Cardinalities sit a couple of cells in from each end, clear of the end-marker (which is at index 1 / ^2).
        if (rel.FromCardinality is { Length: > 0 } fc)
        {
            var (x, y) = path[Math.Min(2, path.Count - 1)];
            Centered(canvas, fc, x, y, _s.EdgeLabel);
        }
        if (rel.ToCardinality is { Length: > 0 } tc)
        {
            var (x, y) = path[Math.Max(0, path.Count - 3)];
            Centered(canvas, tc, x, y, _s.EdgeLabel);
        }
    }

    // UML end-marker glyph by relationship type (triangles/arrows orient to the edge; diamonds/circle are symmetric).
    private static char Marker(ClassRelationType type, int dir) => type switch
    {
        ClassRelationType.Inheritance or ClassRelationType.Realization => dir switch { 8 => '▷', 4 => '◁', 2 => '▽', _ => '△' },
        ClassRelationType.Composition => '◆',
        ClassRelationType.Aggregation => '◇',
        ClassRelationType.Lollipop => '○',
        _ => dir switch { 8 => '▶', 4 => '◀', 2 => '▼', _ => '▲' },   // Association / Dependency
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
        canvas.Text(cx - (t.Length / 2), cy, t, color);
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

    private static int Dir((int x, int y) a, (int x, int y) b) =>
        b.x > a.x ? 8 : b.x < a.x ? 4 : b.y > a.y ? 2 : 1;

    private static string Clip(string s, int maxW) => maxW <= 0 ? string.Empty : s.Length <= maxW ? s : maxW == 1 ? "…" : $"{s.AsSpan(0, maxW - 1)}…";

    private int CX(double px) => (int)Math.Round(px / _xDiv) + 1;

    private int CY(double px) => (int)Math.Round(px / _yDiv) + 1;

    #endregion Methods

    #region Fields

    private readonly MermaidStyles _s = styles;
    private double _xDiv = 9.0;
    private double _yDiv = 18.0;

    #endregion Fields
}