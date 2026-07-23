using Mermaider.Models;
using CColor = ConsoleGUI.Data.Color;

namespace Jumbee.Console.Documents;

/// <summary>
/// Lays out and renders a parsed Mermaid <see cref="SequenceDiagram"/> directly in cell space (Mermaider has no
/// public sequence layout, and sequence layout is simple): actor boxes across the top, dashed lifelines down, and
/// messages stacked top-to-bottom as horizontal arrows, plus block frames (loop/alt/…), notes, activation bars,
/// participant boxes (actor grouping) and create/destroy markers.
/// </summary>
internal sealed class MermaidSequenceRenderer(MermaidStyles styles)
{
    #region Methods

    public CellCanvas Render(SequenceDiagram diagram)
    {
        var actors = diagram.Actors.ToList();
        if (actors.Count == 0) return new CellCanvas(1, 1);
        var col = new Dictionary<string, int>();
        for (var i = 0; i < actors.Count; i++) col[actors[i].Id] = i;

        var msgs = diagram.Messages;

        // Participant boxes reserve a title row above the actor boxes; without any, the top band is zero.
        var actorTop = diagram.Boxes.Count > 0 ? 1 : 0;

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
        _cx = new int[actors.Count];
        var x = LeftMargin;
        for (var i = 0; i < actors.Count; i++)
        {
            left[i] = x;
            _cx[i] = x + (boxW[i] / 2);
            x += boxW[i] + (i < gap.Length ? gap[i] : 0);
        }
        var width = left[^1] + boxW[^1] + RightMargin + SelfLoopWidth;   // room for a self-loop off the last actor

        // A right-of / over note on an edge actor can extend past the actor columns — widen to fit it.
        foreach (var n in diagram.Notes)
        {
            var (_, nx1) = NoteRect(n, col);
            width = Math.Max(width, nx1 + RightMargin);
        }

        // ── Vertical: assign a row to every event (block start/divider/message/note/block end), in message order ──
        var startsAt = Bucket(diagram.Blocks, b => b.StartIndex);
        var endsAt = Bucket(diagram.Blocks, b => b.EndIndex);
        var notesAt = Bucket(diagram.Notes, n => Math.Clamp(n.AfterIndex, 0, Math.Max(0, msgs.Count - 1)));

        var msgY = new int[msgs.Count];
        var blockTop = new Dictionary<SequenceBlock, int>();
        var blockBot = new Dictionary<SequenceBlock, int>();
        var noteY = new Dictionary<SequenceNote, int>();
        var dividerY = new List<(int y, string label, SequenceBlock block)>();

        var y = actorTop + ActorBoxH + 1;
        for (var i = 0; i < msgs.Count; i++)
        {
            foreach (var b in startsAt(i)) { blockTop[b] = y; y += 2; }
            foreach (var b in diagram.Blocks)
            {
                foreach (var d in b.Dividers)
                {
                    if (d.Index == i) { dividerY.Add((y, d.Label, b)); y++; }
                }
            }

            var self = msgs[i].From == msgs[i].To;
            msgY[i] = y + 1;                         // arrow row (label sits on the row above)
            y += self ? 4 : 2;

            foreach (var n in notesAt(i)) { noteY[n] = y; y += NoteHeight; }
            foreach (var b in endsAt(i)) { blockBot[b] = y; y++; }
        }
        var lifelineBottom = y;
        var height = y + 1;

        // ── Per-actor lifeline extents: created actors' lifelines start at the create message; destroyed ones end
        //    at the destroy marker (with an ✗). AtMessageIndex maps through msgY to a row. ──
        var lifeStart = new int[actors.Count];
        var lifeEnd = new int[actors.Count];
        var createY = new int[actors.Count];
        var destroyY = new int[actors.Count];
        Array.Fill(createY, -1);
        Array.Fill(destroyY, -1);
        for (var i = 0; i < actors.Count; i++) { lifeStart[i] = actorTop + ActorBoxH; lifeEnd[i] = lifelineBottom; }
        foreach (var c in diagram.Creates)
            if (col.TryGetValue(c.ActorId, out var i) && (uint)c.AtMessageIndex < (uint)msgs.Count)
            { createY[i] = msgY[c.AtMessageIndex]; lifeStart[i] = createY[i] + 2; }
        foreach (var d in diagram.Destroys)
            if (col.TryGetValue(d.ActorId, out var i) && (uint)d.AtMessageIndex < (uint)msgs.Count)
            { destroyY[i] = msgY[d.AtMessageIndex]; lifeEnd[i] = destroyY[i]; }

        // ── Activation bars: walk messages tracking an open-activation stack per actor (Activate targets the
        //    receiver, Deactivate the sender). Nested bars offset right by their depth. ──
        _bars = BuildActivations(msgs, msgY, col, lifeEnd);

        var canvas = new CellCanvas(width, height);

        // ── Draw: participant boxes → lifelines → activation bars → block frames → messages → notes → actor boxes ──
        foreach (var b in diagram.Boxes)
            DrawParticipantBox(canvas, b, col, left, boxW, actorTop, width);

        for (var i = 0; i < actors.Count; i++)
            for (var yy = lifeStart[i]; yy <= lifeEnd[i]; yy++)
                canvas.SetChar(_cx[i], yy, '┆', _s.Edge);

        foreach (var bar in _bars)
            DrawActivation(canvas, bar);

        foreach (var b in diagram.Blocks)
            DrawBlock(canvas, b, _cx, blockTop, blockBot, width);
        foreach (var (dy, label, _) in dividerY)
            DrawDivider(canvas, dy, label, width);

        for (var i = 0; i < msgs.Count; i++)
            DrawMessage(canvas, msgs[i], col, msgY[i]);

        foreach (var (note, ny) in noteY)
            DrawNote(canvas, note, col, ny);

        for (var i = 0; i < actors.Count; i++)
        {
            var top = createY[i] >= 0 ? createY[i] - 1 : actorTop;
            canvas.Box(left[i], top, left[i] + boxW[i] - 1, top + ActorBoxH - 1,
                rounded: actors[i].Type == SequenceActorType.Actor, _s.NodeBorder);
            Centered(canvas, actors[i].Label, _cx[i], top + 1, _s.NodeLabel);
            if (destroyY[i] >= 0) canvas.SetChar(_cx[i], destroyY[i], '✗', _s.Arrow);
        }

        return canvas;
    }

    // Resolves activation spans from Activate/Deactivate flags. Each actor keeps a stack of open bars; a `+` message
    // pushes one on its receiver (depth = current stack size), a `-` pops one off its sender. Bars left open at the
    // end run to the actor's lifeline bottom.
    private static List<Bar> BuildActivations(IReadOnlyList<SequenceMessage> msgs, int[] msgY,
        Dictionary<string, int> col, int[] lifeEnd)
    {
        var bars = new List<Bar>();
        var open = new Dictionary<int, Stack<Bar>>();
        Stack<Bar> StackFor(int a) => open.TryGetValue(a, out var s) ? s : open[a] = new Stack<Bar>();

        for (var i = 0; i < msgs.Count; i++)
        {
            var m = msgs[i];
            if (m.Activate && col.TryGetValue(m.To, out var t))
            {
                var stack = StackFor(t);
                var bar = new Bar { Actor = t, Depth = stack.Count, Y0 = msgY[i], Y1 = lifeEnd[t] };
                stack.Push(bar);
                bars.Add(bar);
            }
            if (m.Deactivate && col.TryGetValue(m.From, out var f) && open.TryGetValue(f, out var fs) && fs.Count > 0)
                fs.Pop().Y1 = msgY[i];
        }
        return bars;
    }

    private void DrawActivation(CellCanvas canvas, Bar bar)
    {
        var (x0, x1) = BarExtentX(bar);
        var y1 = Math.Max(bar.Y0 + 1, bar.Y1);
        canvas.Box(x0, bar.Y0, x1, y1, rounded: false, _s.NodeBorder);
    }

    private void DrawMessage(CellCanvas canvas, SequenceMessage m, Dictionary<string, int> col, int y)
    {
        if (!col.TryGetValue(m.From, out var a) || !col.TryGetValue(m.To, out var b)) return;
        var style = m.LineStyle == SequenceLineStyle.Dashed ? CellCanvas.LineStyle.Dashed : CellCanvas.LineStyle.Normal;
        var filled = m.ArrowHead == SequenceArrowHead.Filled;

        if (a == b)   // self-message: a small loop off the right of the lifeline
        {
            int x0 = ActiveEdge(a, y, _cx[a] + 1), r = x0 + SelfLoopWidth - 1;
            for (var xx = x0; xx < r; xx++) canvas.Link((xx, y), (xx + 1, y), _s.Edge, style);
            canvas.Link((r, y), (r, y + 1), _s.Edge, style);
            canvas.Link((r, y + 1), (r, y + 2), _s.Edge, style);
            for (var xx = r; xx > x0; xx--) canvas.Link((xx, y + 2), (xx - 1, y + 2), _s.Edge, style);
            canvas.SetChar(x0, y + 2, filled ? '◀' : '◁', _s.Arrow);
            Centered(canvas, m.Label, x0 + (SelfLoopWidth / 2) + 1, y - 1, _s.EdgeLabel);
            return;
        }

        // Endpoints meet the near edge of any active bar on each actor, not the lifeline centre.
        int x1 = ActiveEdge(a, y, _cx[b]), x2 = ActiveEdge(b, y, _cx[a]);
        int lo = Math.Min(x1, x2), hi = Math.Max(x1, x2);
        for (var xx = lo; xx < hi; xx++) canvas.Link((xx, y), (xx + 1, y), _s.Edge, style);
        var head = x2 > x1 ? (filled ? '▶' : '▷') : (filled ? '◀' : '◁');
        canvas.SetChar(x2, y, head, _s.Arrow);
        Centered(canvas, m.Label, (_cx[a] + _cx[b]) / 2, y - 1, _s.EdgeLabel);
    }

    // The x where an arrow coming from `fromX` should meet `actor` at row `y`: the near edge of the widest active
    // bar covering that row, or the lifeline centre when the actor is idle there.
    private int ActiveEdge(int actor, int y, int fromX)
    {
        int lo = int.MaxValue, hi = int.MinValue;
        foreach (var bar in _bars)
        {
            if (bar.Actor != actor || y < bar.Y0 || y > Math.Max(bar.Y0 + 1, bar.Y1)) continue;
            var (x0, x1) = BarExtentX(bar);
            lo = Math.Min(lo, x0);
            hi = Math.Max(hi, x1);
        }
        return lo == int.MaxValue ? _cx[actor] : fromX <= _cx[actor] ? lo : hi;
    }

    // A bar is a 3-cell-wide box centred on the lifeline, shifted right by its nesting depth so nested bars overlap
    // like mermaid's stacked activations.
    private (int x0, int x1) BarExtentX(Bar bar)
    {
        var c = _cx[bar.Actor] + bar.Depth;
        return (c - 1, c + 1);
    }

    private void DrawParticipantBox(CellCanvas canvas, SequenceBox box, Dictionary<string, int> col,
        int[] left, int[] boxW, int actorTop, int width)
    {
        var idx = box.ActorIds.Where(col.ContainsKey).Select(id => col[id]).ToList();
        if (idx.Count == 0) return;
        int lo = idx.Min(), hi = idx.Max();
        var x0 = Math.Max(0, left[lo] - 1);
        var x1 = Math.Min(width - 1, left[hi] + boxW[hi]);
        var y1 = actorTop + ActorBoxH - 1;
        canvas.LinkRect(x0, 0, x1, y1, _s.GroupBorder);
        // The parser splits `box <color> <title>` greedily, so a title-only `box Frontend` lands in Color; fall back
        // to it when Title is empty.
        var title = string.IsNullOrEmpty(box.Title) ? box.Color : box.Title;
        if (!string.IsNullOrEmpty(title))
            canvas.Text(x0 + 2, 0, Clip($" {title} ", x1 - x0 - 3), _s.GroupLabel);
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

    private void DrawNote(CellCanvas canvas, SequenceNote note, Dictionary<string, int> col, int y)
    {
        var (x0, x1) = NoteRect(note, col);
        canvas.Box(x0, y, x1, y + NoteHeight - 1, rounded: false, _s.NodeBorder);
        Centered(canvas, note.Text, (x0 + x1) / 2, y + 1, _s.Member);
    }

    // Horizontal extent of a note box, shared by width sizing and drawing so they can't disagree.
    private (int x0, int x1) NoteRect(SequenceNote note, Dictionary<string, int> col)
    {
        var centres = note.ActorIds.Where(col.ContainsKey).Select(id => _cx[col[id]]).DefaultIfEmpty(_cx[0]).ToList();
        int lo = centres.Min(), hi = centres.Max();
        var w = Math.Max(note.Text.Length + 2, 6);
        var x0 = note.Position switch
        {
            SequenceNotePosition.Left => Math.Max(0, lo - 2 - w),
            SequenceNotePosition.Right => hi + 2,
            _ => ((lo + hi) / 2) - (w / 2),   // Over: centred across the actor span
        };
        x0 = Math.Max(0, x0);
        return (x0, x0 + w);
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
        canvas.Text(cx - (t.Length / 2), cy, t, color);
    }

    private static string Clip(string s, int maxW) => maxW <= 0 ? string.Empty : s.Length <= maxW ? s : maxW == 1 ? "…" : $"{s.AsSpan(0, maxW - 1)}…";

    #endregion Methods

    #region Fields

    private const int ActorBoxH = 3;
    private const int LeftMargin = 2;
    private const int RightMargin = 2;
    private const int MinActorW = 7;
    private const int ActorGap = 4;
    private const int SelfLoopWidth = 5;
    private const int NoteHeight = 3;

    private readonly MermaidStyles _s = styles;
    private int[] _cx = null!;         // per-render actor centre X (assigned at the top of Render)
    private List<Bar> _bars = null!;   // per-render activation bars (assigned at the top of Render)

    #endregion Fields

    #region Types

    private sealed class Bar
    {
        public int Actor;
        public int Depth;
        public int Y0;
        public int Y1;
    }

    #endregion Types
}