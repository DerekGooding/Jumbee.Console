namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Linq;

using Spectre.Console;
using Spectre.Console.Rendering;

/// <summary>
/// An append-only, scrolling log of Spectre <see cref="IRenderable"/> entries — markup strings, tables, rules,
/// etc. — that always shows the most recent entries that fit (a "tail" view). Each entry is a renderable, so log
/// lines can be styled/coloured. <see cref="Write(string)"/>/<see cref="Write(IRenderable)"/> are safe to call
/// from any thread.
/// </summary>
public class Log : RenderableControl
{
    #region Properties
    /// <summary>The maximum number of entries retained; older entries are discarded. Defaults to 1000.</summary>
    public int MaxEntries
    {
        get => _maxEntries;
        set => SetAtomicProperty(ref _maxEntries, Math.Max(1, value), watch: (_, _) => UI.Invoke(Trim));
    }
    #endregion

    #region Methods
    /// <summary>Appends a markup string as a styled entry. Safe to call from any thread.</summary>
    public void Write(string markup) => Write(new Markup(markup ?? ""));

    /// <summary>Appends a renderable entry. Safe to call from any thread.</summary>
    public void Write(IRenderable renderable)
    {
        UI.Invoke(() =>
        {
            _entries.Add(renderable);
            Trim();
            Initialize();       // re-measure the content height so a wrapping frame updates its scroll range
            ScrollToBottom();   // tail: keep the newest entry in view
        });
    }

    public void Clear() => UI.Invoke(() =>
    {
        _entries.Clear();
        Initialize();
        if (Frame is not null) Frame.Top = 0;
    });

    // Content-only render (never reads focus/hover): reuse the cached buffer on interactive-state changes.
    protected override bool RendersInteractiveState => false;

    // The Log is content-sized: it reports the full height of all entries so a wrapping ControlFrame windows and
    // scrolls it (mouse wheel / Alt+Up-Down / scrollbar), auto-tailing to the bottom on each Write. A short log is
    // sized to its few rows (no spurious scrollbar); a long one overflows and scrolls. Consulted only when the frame
    // leaves the height unbounded (see Control.CalculateSize).
    protected override int MeasureHeight(int width)
    {
        if (_entries.Count == 0) return 1;
        var options = new RenderOptions(ansiConsole.Profile.Capabilities, new Spectre.Console.Size(Math.Max(1, width), 1));
        var segments = ((IRenderable)new Rows(_entries)).Render(options, Math.Max(1, width));
        return Math.Max(1, Segment.SplitLines(segments).Count);
    }

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        if (_entries.Count == 0) return [];

        // Framed: render every entry into the content-tall buffer; the frame windows the visible portion and tails
        // to the bottom on Write. Unframed (a fixed-height cell with no scroll): render only the most recent entries
        // that fit, so the newest stays visible rather than the buffer clipping the bottom.
        IEnumerable<IRenderable> entries = _entries;
        if (Frame is null)
        {
            var rows = Math.Max(1, ActualHeight);
            entries = _entries.Skip(Math.Max(0, _entries.Count - rows));
        }
        return ((IRenderable)new Rows(entries)).Render(options, maxWidth);
    }

    // Scroll the wrapping frame so the last rendered row is in view (the log's default "tail" behaviour). No-op when
    // the log is not framed or fits within the viewport.
    private void ScrollToBottom()
    {
        if (Frame is null) return;
        var viewport = Frame.ViewportSize.Height;
        if (viewport <= 0) return;
        Frame.Top = Math.Max(0, ActualHeight - viewport);
    }

    private void Trim()
    {
        if (_entries.Count > _maxEntries)
            _entries.RemoveRange(0, _entries.Count - _maxEntries);
    }
    #endregion

    #region Fields
    private readonly List<IRenderable> _entries = new();
    private int _maxEntries = 1000;
    #endregion
}
