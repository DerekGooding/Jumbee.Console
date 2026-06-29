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
            Invalidate();
        });
    }

    public void Clear() => UI.Invoke(() =>
    {
        _entries.Clear();
        Invalidate();
    });

    // Content-only render (never reads focus/hover): reuse the cached buffer on interactive-state changes.
    protected override bool RendersInteractiveState => false;

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        if (_entries.Count == 0) return [];

        // Show the most recent entries that plausibly fit the viewport (one row per entry is the common case).
        var rows = Math.Max(1, ActualHeight);
        var start = Math.Max(0, _entries.Count - rows);
        var visible = _entries.Skip(start);
        return ((IRenderable)new Rows(visible)).Render(options, maxWidth);
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
