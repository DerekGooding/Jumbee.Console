namespace Jumbee.Console.DocumentViewers;

using System;
using System.Collections.Generic;
using System.Linq;

using Mermaider.Models;

using CColor = ConsoleGUI.Data.Color;

/// <summary>
/// Lays out and renders a parsed Mermaid <see cref="SequenceDiagram"/> directly in cell space (Mermaider has no
/// public sequence layout, and sequence layout is simple): actor boxes across the top, dashed lifelines down, and
/// messages stacked top-to-bottom as horizontal arrows, plus block frames (loop/alt/…) and notes.
/// </summary>
internal sealed class MermaidSequenceRenderer
{
    #region Constructors
    public MermaidSequenceRenderer(MermaidStyles styles) => _s = styles;
    #endregion

    #region Methods
    public CellCanvas Render(SequenceDiagram diagram)
    {
        var actors = diagram.Actors.ToList();
        if (actors.Count == 0) return new CellCanvas(1, 1);
        var col = new Dictionary<string, int>();
        for (var i = 0; i < actors.Count; i++) col[actors[i].Id] = i;

        var msgs = diagram.Messages;

        // ── Horizontal: actor box widths, then centre X per column (gap widened by adjacent-actor message labels) ──
        var boxW = actors.Select(a => Math.Max(MinActorW, a.Label.Length + 4)).ToArray();
        var gap = new int[Math.Max(0, actors.Count - 1)];
        for (var p = 0; p < gap.Length; p++) gap[p] = ActorGap;
        foreach (var m in msgs)
        {
            if (!col.TryGetValue(m.From, out var a) || !col.TryGetValue(m.To, out var b) || a == b) continue;
            var lo = Math.Min(a, b);
            if (Math.Abs(a - b) == 1) gap[lo] = Math.Max(gap[lo], m.Label.Length + 3);
        }
        var left = new int[actors.Count];
        var cx = new int[actors.Count];
        var x = LeftMargin;
        for (var i = 0; i < actors.Count; i++)
        {
            left[i] = x;
            cx[i] = x + boxW[i] / 2;
            x += boxW[i] + (i < gap.Length ? gap[i] : 0);
        }
        var width = left[^1] + boxW[^1] + RightMargin + SelfLoopWidth;   // room for a self-loop off the last actor

        // ── Vertical: assign a row to every event (block start/divider/message/note/block end), in message order ──
        var startsAt = Bucket(diagram.Blocks, b => b.StartIndex);
        var endsAt = Bucket(diagram.Blocks, b => b.EndIndex);
        var notesAt = Bucket(diagram.Notes, n => Math.Clamp(n.AfterIndex, 0, Math.Max(0, msgs.Count - 1)));

        var msgY = new int[msgs.Count];
        var blockTop = new Dictionary<SequenceBlock, int>();
        var blockBot = new Dictionary<SequenceBlock, int>();
        var noteY = new Dictionary<SequenceNote, int>();
        var dividerY = new List<(int y, string label, SequenceBlock block)>();

        var y = ActorBoxH + 1;
        for (var i = 0; i < msgs.Count; i++)
        {
            foreach (var b in startsAt(i)) { blockTop[b] = y; y += 2; }
            foreach (var b in diagram.Blocks)
                foreach (var d in b.Dividers)
                    if (d.Index == i) { dividerY.Add((y, d.Label, b)); y += 1; }

            var self = msgs[i].From == msgs[i].To;
            msgY[i] = y + 1;                         // arrow row (label sits on the row above)
            y += self ? 4 : 2;

            foreach (var n in notesAt(i)) { noteY[n] = y; y += NoteHeight; }
            foreach (var b in endsAt(i)) { blockBot[b] = y; y += 1; }
        }
        var lifelineBottom = y;
        var height = y + 1;

        var canvas = new CellCanvas(width, height);

        // ── Draw: lifelines → block frames → messages → notes → actor boxes (opaque, on top) ──
        foreach (var c in cx.Distinct())
            for (var yy = ActorBoxH; yy <= lifelineBottom; yy++)
                canvas.SetChar(c, yy, '┆', _s.Edge);

        foreach (var b in diagram.Blocks)
            DrawBlock(canvas, b, cx, blockTop, blockBot, width);
        foreach (var (dy, label, _) in dividerY)
            DrawDivider(canvas, dy, label, width);

        for (var i = 0; i < msgs.Count; i++)
            DrawMessage(canvas, msgs[i], col, cx, msgY[i]);

        foreach (var (note, ny) in noteY)
            DrawNote(canvas, note, col, cx, ny);

        for (var i = 0; i < actors.Count; i++)
        {
            canvas.Box(left[i], 0, left[i] + boxW[i] - 1, ActorBoxH - 1, rounded: actors[i].Type == SequenceActorType.Actor, _s.NodeBorder);
            Centered(canvas, actors[i].Label, cx[i], 1, _s.NodeLabel);
        }

        return canvas;
    }

    private void DrawMessage(CellCanvas canvas, SequenceMessage m, Dictionary<string, int> col, int[] cx, int y)
    {
        if (!col.TryGetValue(m.From, out var a) || !col.TryGetValue(m.To, out var b)) return;
        var style = m.LineStyle == SequenceLineStyle.Dashed ? CellCanvas.LineStyle.Dashed : CellCanvas.LineStyle.Normal;
        var filled = m.ArrowHead == SequenceArrowHead.Filled;

        if (a == b)   // self-message: a small loop off the right of the lifeline
        {
            int x0 = cx[a], r = cx[a] + SelfLoopWidth - 1;
            for (var xx = x0; xx < r; xx++) canvas.Link((xx, y), (xx + 1, y), _s.Edge, style);
            canvas.Link((r, y), (r, y + 1), _s.Edge, style);
            canvas.Link((r, y + 1), (r, y + 2), _s.Edge, style);
            for (var xx = r; xx > x0; xx--) canvas.Link((xx, y + 2), (xx - 1, y + 2), _s.Edge, style);
            canvas.SetChar(x0, y + 2, filled ? '◀' : '◁', _s.Arrow);
            Centered(canvas, m.Label, x0 + SelfLoopWidth / 2 + 1, y - 1, _s.EdgeLabel);
            return;
        }

        int x1 = cx[a], x2 = cx[b], lo = Math.Min(x1, x2), hi = Math.Max(x1, x2);
        for (var xx = lo; xx < hi; xx++) canvas.Link((xx, y), (xx + 1, y), _s.Edge, style);
        var head = x2 > x1 ? (filled ? '▶' : '▷') : (filled ? '◀' : '◁');
        canvas.SetChar(x2, y, head, _s.Arrow);
        Centered(canvas, m.Label, (x1 + x2) / 2, y - 1, _s.EdgeLabel);
    }

    private void DrawBlock(CellCanvas canvas, SequenceBlock b, int[] cx,
        Dictionary<SequenceBlock, int> top, Dictionary<SequenceBlock, int> bot, int width)
    {
        if (!top.TryGetValue(b, out var y0) || !bot.TryGetValue(b, out var y1)) return;
        // Span from the leftmost to rightmost actor centre, padded (blocks usually wrap the whole interaction).
        int minX = Math.Max(0, cx.Min() - 2), maxX = Math.Min(width - 1, cx.Max() + 2);
        if (maxX <= minX + 1) return;

        canvas.LinkRect(minX, y0, maxX, y1, _s.GroupBorder);
        var tag = $" {b.Type.ToString().ToLowerInvariant()}{(string.IsNullOrEmpty(b.Label) ? "" : " " + b.Label)} ";
        canvas.Text(minX + 2, y0, Clip(tag, maxX - minX - 3), _s.GroupLabel);
    }

    private void DrawDivider(CellCanvas canvas, int y, string label, int width)
    {
        // A dashed horizontal divider (alt/else) across the diagram width, with the label at the left.
        for (var xx = 1; xx < width - 1; xx++) canvas.SetChar(xx, y, '┄', _s.GroupBorder);
        if (!string.IsNullOrEmpty(label)) canvas.Text(3, y, Clip($"[{label}]", width - 6), _s.GroupLabel);
    }

    private void DrawNote(CellCanvas canvas, SequenceNote note, Dictionary<string, int> col, int[] cx, int y)
    {
        var centres = note.ActorIds.Where(col.ContainsKey).Select(id => cx[col[id]]).DefaultIfEmpty(cx[0]).ToList();
        int lo = centres.Min(), hi = centres.Max();
        int w = Math.Max(note.Text.Length + 2, 6);
        int x0 = note.Position switch
        {
            SequenceNotePosition.Left => Math.Max(0, lo - 2 - w),
            SequenceNotePosition.Right => hi + 2,
            _ => (lo + hi) / 2 - w / 2,   // Over: centred across the actor span
        };
        x0 = Math.Max(0, x0);
        int x1 = x0 + w;
        canvas.Box(x0, y, x1, y + NoteHeight - 1, rounded: false, _s.NodeBorder);
        Centered(canvas, note.Text, (x0 + x1) / 2, y + 1, _s.Member);
    }

    private static Func<int, IEnumerable<T>> Bucket<T>(IEnumerable<T> items, Func<T, int> key)
    {
        var map = new Dictionary<int, List<T>>();
        foreach (var it in items) (map.TryGetValue(key(it), out var l) ? l : map[key(it)] = []).Add(it);
        return i => map.TryGetValue(i, out var l) ? l : Enumerable.Empty<T>();
    }

    private static void Centered(CellCanvas canvas, string text, int cx, int cy, CColor? color)
    {
        var t = text.Replace('\n', ' ');
        canvas.Text(cx - t.Length / 2, cy, t, color);
    }

    private static string Clip(string s, int maxW)
    {
        if (maxW <= 0) return string.Empty;
        if (s.Length <= maxW) return s;
        return maxW == 1 ? "…" : string.Concat(s.AsSpan(0, maxW - 1), "…");
    }
    #endregion

    #region Fields
    private const int ActorBoxH = 3;
    private const int LeftMargin = 2;
    private const int RightMargin = 2;
    private const int MinActorW = 7;
    private const int ActorGap = 4;
    private const int SelfLoopWidth = 5;
    private const int NoteHeight = 3;

    private readonly MermaidStyles _s;
    #endregion
}
