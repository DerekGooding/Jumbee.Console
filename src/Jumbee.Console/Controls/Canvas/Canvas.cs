namespace Jumbee.Console;

using System;
using System.Collections.Generic;

using ConsoleGUI.Space;

using Jumbee.Console.Drawing;

using Character = ConsoleGUI.Data.Character;

/// <summary>
/// A blank drawing surface on which you paint <see cref="IShape"/>s (<see cref="Line"/>, <see cref="Rectangle"/>,
/// <see cref="Circle"/>, <see cref="Points"/>, <see cref="FilledLine"/>) in an arbitrary coordinate system, rendered
/// with sub-cell markers (braille by default). Ported from Ratatui's canvas widget.
///
/// <para>Shapes accumulate in a retained list: <see cref="Add"/> appends to the current layer, <see cref="Layer()"/>
/// starts a new one (composited top-down per property, so a higher layer's glyph/colour wins each cell while lower
/// layers show through where it doesn't paint), and <see cref="Clear"/> empties the surface. Layers may use different
/// markers via <see cref="Layer(CanvasMarker)"/> — e.g. a <see cref="CanvasMarker.Block"/> backdrop showing through a
/// <see cref="CanvasMarker.Braille"/> overlay; <see cref="Marker"/> sets the starting marker. Set the visible window
/// with <see cref="XBounds"/>/<see cref="YBounds"/> — the canvas origin is the bottom-left corner. Display-only; it
/// fills its container and re-fits on resize.</para>
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
    public Color? Background
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
    public Canvas WithBackground(Color? background)
    {
        Background = background;
        return this;
    }

    /// <summary>Appends a shape to the current layer and redraws. Fluent.</summary>
    public Canvas Add(IShape shape)
    {
        UI.Invoke(() =>
        {
            _ops.Add(Op.Draw(shape));
            Rebuild();
        });
        return this;
    }

    /// <summary>Starts a new layer (keeping the current marker) — shapes added after this compose on top of (and can
    /// show through) earlier layers. Fluent.</summary>
    public Canvas Layer()
    {
        UI.Invoke(() =>
        {
            _ops.Add(Op.LayerBreak(null));
            Rebuild();
        });
        return this;
    }

    /// <summary>Starts a new layer drawn with <paramref name="marker"/> (which becomes the current marker for this and
    /// subsequent layers until changed again). Lets layers mix resolutions — e.g. a <see cref="CanvasMarker.Block"/>
    /// backdrop under a <see cref="CanvasMarker.Braille"/> overlay, where the block's background shows through the
    /// braille glyphs. Custom markers use the current <see cref="CustomMarker"/>. Fluent.</summary>
    public Canvas Layer(CanvasMarker marker)
    {
        UI.Invoke(() =>
        {
            _ops.Add(Op.LayerBreak(marker));
            Rebuild();
        });
        return this;
    }

    /// <summary>
    /// Prints <paramref name="text"/> at canvas coordinate (<paramref name="x"/>, <paramref name="y"/>), with its
    /// first character at that point and running right. Labels are always drawn on top of every layer (they are not
    /// part of a layer and are unaffected by the marker), and clipped at the right edge. <paramref name="fg"/> defaults
    /// to white; <paramref name="bg"/> is transparent when null. Fluent.
    /// </summary>
    public Canvas Print(double x, double y, string text, Color? fg = null, Color? bg = null)
    {
        UI.Invoke(() =>
        {
            _labels.Add(new Label(x, y, text ?? "", fg ?? Color.White, bg));
            Invalidate();   // labels are drawn each render from _labels; no layer rebuild needed
        });
        return this;
    }

    /// <summary>Removes every shape, layer and label, leaving a blank canvas. Fluent.</summary>
    public Canvas Clear()
    {
        UI.Invoke(() =>
        {
            _ops.Clear();
            _labels.Clear();
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
        DrawLabels(w, h);
    }

    private List<Layer> BuildLayers(int width, int height)
    {
        var context = new CanvasContext(width, height, _xBounds, _yBounds, _marker, _customMarker);
        foreach (var op in _ops)
        {
            if (op.Shape is not null) context.Draw(op.Shape);
            else if (op.NewMarker is CanvasMarker m) context.Marker(m, _customMarker);
            else context.Layer();
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

    // Draws the retained labels on top of the composited layers. A label maps to a cell with the same y-flip as the
    // shapes but at cell resolution (w-1, h-1) — matching ratatui — and its text runs right from that cell, clipped
    // to the buffer's right edge.
    private void DrawLabels(int width, int height)
    {
        if (_labels.Count == 0) return;
        var (left, right) = _xBounds;
        var (bottom, top) = _yBounds;
        double spanX = Math.Abs(right - left), spanY = Math.Abs(top - bottom);
        if (spanX <= 0 || spanY <= 0) return;
        double resX = width - 1, resY = height - 1;

        foreach (var label in _labels)
        {
            if (label.X < left || label.X > right || label.Y < bottom || label.Y > top) continue;
            int x = (int)((label.X - left) * resX / spanX);
            int y = (int)((top - label.Y) * resY / spanY);
            if (y < 0 || y >= height) continue;
            for (int col = x, k = 0; k < label.Text.Length; k++, col++)
            {
                if (col < 0) continue;
                if (col >= width) break;
                consoleBuffer.Write(new Position(col, y), new Character(label.Text[k], label.Fg, label.Bg));
            }
        }
    }
    #endregion

    #region Child types
    // An entry in the retained op list: a shape to draw on the current layer, a plain layer break, or a layer break
    // that also switches the marker for the new layer.
    private readonly struct Op
    {
        private Op(IShape? shape, CanvasMarker? newMarker)
        {
            Shape = shape;
            NewMarker = newMarker;
        }

        public IShape? Shape { get; }                 // non-null => draw this shape
        public CanvasMarker? NewMarker { get; }       // set (with Shape null) => layer break switching to this marker

        public static Op Draw(IShape shape) => new(shape, null);
        public static Op LayerBreak(CanvasMarker? marker) => new(null, marker);
    }

    // A text label anchored at a canvas coordinate, drawn on top of all layers (see DrawLabels).
    private readonly struct Label
    {
        public Label(double x, double y, string text, CColor fg, CColor? bg)
        {
            X = x;
            Y = y;
            Text = text;
            Fg = fg;
            Bg = bg;
        }

        public double X { get; }
        public double Y { get; }
        public string Text { get; }
        public CColor Fg { get; }
        public CColor? Bg { get; }
    }
    #endregion

    #region Fields
    // The ordered op list of shapes and layer breaks (see Op); replayed each rebuild.
    private readonly List<Op> _ops = [];
    // Text labels drawn on top of every layer each render (not part of the layer/marker system).
    private readonly List<Label> _labels = [];
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
