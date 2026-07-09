namespace Jumbee.Console;

using System;
using System.Collections.Generic;

using ConsoleGUI.Data;
using ConsoleGUI.Space;

using Jumbee.Console.Drawing;

/// <summary>
/// A blank drawing surface on which you paint <see cref="IShape"/>s (<see cref="Line"/>, <see cref="Rectangle"/>,
/// <see cref="Circle"/>, <see cref="Points"/>, <see cref="FilledLine"/>) in an arbitrary coordinate system, rendered
/// with sub-cell markers (braille by default). Ported from Ratatui's canvas widget.
///
/// <para>Shapes accumulate in a retained list: <see cref="Add"/> appends to the current layer, <see cref="Layer"/>
/// starts a new one (composited top-down per property, so a higher layer's glyph/colour wins each cell while lower
/// layers show through where it doesn't paint), and <see cref="Clear"/> empties the surface. All layers share the
/// single <see cref="Marker"/>. Set the visible window with <see cref="XBounds"/>/<see cref="YBounds"/> — the canvas
/// origin is the bottom-left corner. Display-only; it fills its container and re-fits on resize.</para>
/// </summary>
public class Canvas : Control
{
    #region Constructors
    public Canvas() => Focusable = false;   // display-only
    #endregion

    #region Properties
    /// <summary>The horizontal window (left, right) of the coordinate system mapped across the control's width. Default (0, 0).</summary>
    public (double Min, double Max) XBounds
    {
        get => _xBounds;
        set => SetAtomicProperty(ref _xBounds, value, watch: (_, _) => _dirty = true);
    }

    /// <summary>The vertical window (bottom, top) of the coordinate system mapped across the control's height. Default (0, 0). The origin is bottom-left.</summary>
    public (double Min, double Max) YBounds
    {
        get => _yBounds;
        set => SetAtomicProperty(ref _yBounds, value, watch: (_, _) => _dirty = true);
    }

    /// <summary>The marker (and thus sub-cell resolution) shapes are drawn with. Default <see cref="CanvasMarker.Braille"/>.</summary>
    public CanvasMarker Marker
    {
        get => _marker;
        set => SetAtomicProperty(ref _marker, value, watch: (_, _) => _dirty = true);
    }

    /// <summary>The glyph used when <see cref="Marker"/> is <see cref="CanvasMarker.Custom"/>. Default <c>•</c>.</summary>
    public char CustomMarker
    {
        get => _customMarker;
        set => SetAtomicProperty(ref _customMarker, value, watch: (_, _) => _dirty = true);
    }

    /// <summary>Background colour painted behind every cell, or <see langword="null"/> (the default) for transparent.</summary>
    public CColor? Background
    {
        get => _background;
        set => SetAtomicProperty(ref _background, value, watch: (_, _) => _dirty = true);
    }
    #endregion

    #region Methods
    /// <summary>Sets the <see cref="XBounds"/> and returns this canvas, for fluent chaining.</summary>
    public Canvas WithXBounds(double min, double max)
    {
        XBounds = (min, max);
        return this;
    }

    /// <summary>Sets the <see cref="YBounds"/> and returns this canvas, for fluent chaining.</summary>
    public Canvas WithYBounds(double min, double max)
    {
        YBounds = (min, max);
        return this;
    }

    /// <summary>Sets the <see cref="Marker"/> and returns this canvas, for fluent chaining.</summary>
    public Canvas WithMarker(CanvasMarker marker)
    {
        Marker = marker;
        return this;
    }

    /// <summary>Sets the <see cref="Background"/> and returns this canvas, for fluent chaining.</summary>
    public Canvas WithBackground(CColor? background)
    {
        Background = background;
        return this;
    }

    /// <summary>Appends a shape to the current layer and redraws. Fluent.</summary>
    public Canvas Add(IShape shape)
    {
        UI.Invoke(() =>
        {
            _ops.Add(shape);
            Rebuild();
        });
        return this;
    }

    /// <summary>Starts a new layer — shapes added after this compose on top of (and can show through) earlier layers. Fluent.</summary>
    public Canvas Layer()
    {
        UI.Invoke(() =>
        {
            _ops.Add(null);   // a null op is a layer break
            Rebuild();
        });
        return this;
    }

    /// <summary>Removes every shape and layer, leaving a blank canvas. Fluent.</summary>
    public Canvas Clear()
    {
        UI.Invoke(() =>
        {
            _ops.Clear();
            Rebuild();
        });
        return this;
    }

    private void Rebuild()
    {
        _dirty = true;
        Invalidate();
    }

    // A canvas fills its container and re-fits on resize; it must never be scrolled (inside a ControlFrame this hands
    // it the bounded viewport height instead of the unbounded scroll height, which would balloon it to the clamp).
    protected internal override bool FillsFrameViewport => true;

    protected override void Render()
    {
        int w = Size.Width, h = Size.Height;
        if (w <= 0 || h <= 0) return;

        // Rebuild the composited layers when the content changed or the control was resized.
        if (_dirty || _layers is null || _builtWidth != w || _builtHeight != h)
        {
            _dirty = false;
            _builtWidth = w;
            _builtHeight = h;
            _layers = BuildLayers(w, h);
        }

        consoleBuffer.Initialize();
        Blit(_layers, w, h);
    }

    private List<Layer> BuildLayers(int width, int height)
    {
        var context = new CanvasContext(width, height, _xBounds, _yBounds, _marker, _customMarker);
        foreach (var op in _ops)
        {
            if (op is null) context.Layer();
            else context.Draw(op);
        }
        context.Finish();
        return [.. context.Layers];
    }

    // Composites the layers into the buffer. Each of symbol/foreground/background is taken from the top-most layer
    // that supplies it (per-property merge, so a lower layer's background can show under a higher layer's glyph);
    // cells with nothing set fall back to the canvas Background, or stay blank when that is null.
    private void Blit(IReadOnlyList<Layer> layers, int width, int height)
    {
        int count = width * height;
        EnsureWorkArrays(count);
        Array.Clear(_sym, 0, count);
        Array.Clear(_fg, 0, count);
        Array.Clear(_bg, 0, count);

        foreach (var layer in layers)
        {
            var contents = layer.Contents;
            int n = Math.Min(count, contents.Length);
            for (int i = 0; i < n; i++)
            {
                var cell = contents[i];
                if (cell.Symbol.HasValue) _sym[i] = cell.Symbol;
                if (cell.Fg.HasValue) _fg[i] = cell.Fg;
                if (cell.Bg.HasValue) _bg[i] = cell.Bg;
            }
        }

        for (int i = 0; i < count; i++)
        {
            char? symbol = _sym[i];
            CColor? fg = _fg[i];
            CColor? bg = _bg[i] ?? _background;
            if (symbol is null && fg is null && bg is null) continue;   // leave the blank cell Initialize wrote
            consoleBuffer.Write(new Position(i % width, i / width), new Character(symbol ?? ' ', fg, bg));
        }
    }

    private void EnsureWorkArrays(int count)
    {
        if (_sym.Length >= count) return;
        _sym = new char?[count];
        _fg = new CColor?[count];
        _bg = new CColor?[count];
    }
    #endregion

    #region Fields
    // The ordered op list: a non-null entry is a shape on the current layer; a null entry is a layer break.
    private readonly List<IShape?> _ops = [];
    private List<Layer>? _layers;
    private (double Min, double Max) _xBounds;
    private (double Min, double Max) _yBounds;
    private CanvasMarker _marker = CanvasMarker.Braille;
    private char _customMarker = '•';
    private CColor? _background;
    private bool _dirty = true;
    private int _builtWidth = -1;
    private int _builtHeight = -1;

    // Reused per-property compositing scratch (grown to the largest buffer seen), cleared each blit.
    private char?[] _sym = [];
    private CColor?[] _fg = [];
    private CColor?[] _bg = [];
    #endregion
}
