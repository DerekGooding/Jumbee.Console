namespace Jumbee.Console;

using System;

using ConsoleGUI;
using ConsoleGUI.Data;
using ConsoleGUI.Space;

using Spectre.Console.Rendering;

// The unqualified name Color in this namespace resolves to Jumbee.Console(.Styles).Color, which has static
// Red/Green/Blue and no Mix; use the project's CColor alias (Global.cs) for the alpha-capable ConsoleGUI colour.

/// <summary>
/// A translucent "glass" panel: a fixed-size overlay that shows the layer beneath it, tinted, with its own
/// <see cref="Content"/> drawn opaquely on top. Host it non-modally over the current UI with <see cref="Show"/>
/// (it floats via <see cref="Overlay.ShowPassive"/>, so the layer beneath keeps focus and keeps receiving input).
/// </summary>
/// <remarks>
/// A terminal cell has no real alpha (ANSI SGR has no alpha channel), so "transparency" is a software flatten of
/// the overlapping layers to opaque colours, done lazily in the indexer — only the panel's own cells are blended,
/// only when a frame redraws them. This is the same trick as <see cref="DimScrim"/> (the modal scrim), localised
/// to a panel and generalised: <see cref="_frosted"/> collapses each cell beneath to a single perceived colour
/// (accounting for glyph ink coverage) before the blend, and gamma-correct blending is available too.
/// </remarks>
public class GlassPanel : Control
{
    #region Constructors
    /// <param name="width">Panel width in cells.</param>
    /// <param name="height">Panel height in cells.</param>
    /// <param name="tint">Colour the layer beneath is blended toward (the glass colour).</param>
    /// <param name="factor">Blend strength: 0 = fully see-through, 1 = a solid <paramref name="tint"/> fill.</param>
    /// <param name="frosted">When <see langword="true"/>, cells beneath are collapsed to a perceived colour and
    /// frosted to the tint (content beneath becomes a faithful colour blur); when <see langword="false"/> the glyphs
    /// beneath show through, tinted (see-through glass).</param>
    /// <param name="gammaCorrect">Blend in linear light (gamma-correct) instead of gamma space.</param>
    public GlassPanel(int width, int height, Color tint, float factor = 0.6f, bool frosted = true, bool gammaCorrect = false)
    {
        _w = width;
        _h = height;
        _tint = tint;
        _factor = Math.Clamp(factor, 0f, 1f);
        _frosted = frosted;
        _gammaCorrect = gammaCorrect;
        Focusable = false;   // a HUD never takes focus; clicks pass through the glass to the layer beneath
    }
    #endregion

    #region Indexers
    // Composite: the panel's own rendered content (opaque, crisp) wherever it has ink, otherwise the glass backdrop
    // (the layer beneath, tinted). A content glyph with no background of its own sits on the glass so text never
    // punches a hole to the terminal default.
    public override Cell this[Position position]
    {
        get
        {
            if (position.X < 0 || position.Y < 0 || position.X >= Size.Width || position.Y >= Size.Height)
                return emptyCell;

            var backdrop = Backdrop(position);
            var content = consoleBuffer[position].Character;
            bool ink = (content.Content is char c && c != ' ') || content.Background.HasValue;
            if (!ink) return backdrop;

            var fg = content.Foreground ?? _textColor;
            var bg = content.Background ?? backdrop.Character.Background;
            return new Cell(new Character(content.Content, fg, bg, content.Decoration));
        }
    }
    #endregion

    #region Properties
    /// <summary>The Spectre renderable drawn opaquely over the glass (labels, values, a bordered <c>Panel</c>, …),
    /// or <see langword="null"/> for a bare glass pane.</summary>
    public IRenderable? Content
    {
        get => _content;
        set { _content = value; Invalidate(); }
    }

    /// <summary>Colour the layer beneath is blended toward.</summary>
    public Color Tint
    {
        get => _tint;
        set => SetAtomicProperty(ref _tint, value);
    }

    /// <summary>Blend strength (0 = see-through, 1 = solid tint).</summary>
    public float Factor
    {
        get => _factor;
        set => SetAtomicProperty(ref _factor, Math.Clamp(value, 0f, 1f));
    }

    /// <summary>Colour used for content glyphs that carry no foreground of their own.</summary>
    public Color TextColor
    {
        get => _textColor;
        set => SetAtomicProperty(ref _textColor, value);
    }

    /// <summary><see langword="true"/> while the panel is floating over an overlay (between <see cref="Show"/> and
    /// <see cref="Hide"/>).</summary>
    public bool IsShown { get; private set; }
    #endregion

    #region Methods
    /// <summary>Floats the panel over <paramref name="overlay"/> (or the ambient <see cref="UI.Overlay"/>) with its
    /// top-left at (<paramref name="x"/>, <paramref name="y"/>). Non-capturing: the layer beneath keeps focus.</summary>
    public void Show(int x, int y, Overlay? overlay = null)
    {
        var ov = overlay ?? UI.Overlay
            ?? throw new InvalidOperationException("No overlay is available to host the glass panel; start the UI first.");
        _overlay = ov;
        _below = ov.Bottom.CControl;
        _anchor = new Position(x, y);
        ov.ShowPassive(this, x, y);
        IsShown = true;
    }

    /// <summary>Hides the panel (closes the passive overlay layer it was shown in).</summary>
    public void Hide()
    {
        _overlay?.Hide();
        IsShown = false;
    }

    /// <summary>Shows the panel if hidden, hides it if shown.</summary>
    public void Toggle(int x, int y, Overlay? overlay = null)
    {
        if (IsShown) Hide();
        else Show(x, y, overlay);
    }

    /// <summary>Configures the layer read through the glass and the panel's screen anchor without hosting it in an
    /// overlay — used by tests to composite the glass against a known backdrop.</summary>
    internal void ConfigureBackdrop(IControl below, Position anchor)
    {
        _below = below;
        _anchor = anchor;
    }

    protected override void Render()
    {
        ansiConsole.Clear(true);
        if (_content is not null) ansiConsole.Write(_content);
    }

    // A fixed extent so a docking/placement parent (the overlay's anchoring Box) sizes the panel snugly instead of
    // stretching it to fill the screen.
    protected override int IntrinsicWidth() => _w;
    protected override int IntrinsicHeight() => _h;

    // The glass backdrop for one cell: the layer beneath, tinted. Carries the beneath cell's mouse listener so a
    // click on the (non-text) glass passes through to whatever is under it.
    private Cell Backdrop(in Position position)
    {
        var below = ReadBelow(position);
        var ch = below.Character;

        if (_frosted)
        {
            // Collapse the cell beneath (glyph-on-background) to one perceived colour, then frost it toward the tint.
            var bg = GlassBlend.Blend(Perceived(ch), _tint, _factor, _gammaCorrect);
            return new Cell(new Character(' ', null, bg, null), below.MouseListener);
        }

        // See-through: keep the glyph beneath, blend its colours toward the tint (like the modal scrim).
        CColor? fg = ch.Foreground is { } f ? GlassBlend.Blend(f, _tint, _factor, _gammaCorrect) : null;
        CColor bg2 = ch.Background is { } b ? GlassBlend.Blend(b, _tint, _factor, _gammaCorrect) : _tint;
        return new Cell(new Character(ch.Content, fg, bg2, ch.Decoration), below.MouseListener);
    }

    private Cell ReadBelow(in Position position)
    {
        if (_below is null) return emptyCell;
        int bx = position.X + _anchor.X, by = position.Y + _anchor.Y;
        return bx >= 0 && by >= 0 && bx < _below.Size.Width && by < _below.Size.Height
            ? _below[new Position(bx, by)]
            : emptyCell;
    }

    // A terminal cell is a foreground glyph painted over a background; its single "perceived" colour is the glyph
    // colour composited over the background by how much of the cell the glyph actually inks (see
    // GlassBlend.EstimateCoverage). Cells with no background are assumed to sit on a dark terminal default.
    private static CColor Perceived(in Character ch)
    {
        var bg = ch.Background ?? UnknownBackground;
        if (ch.Foreground is not { } fg || ch.Content is not { } content) return bg;
        var coverage = GlassBlend.EstimateCoverage(content);
        return coverage <= 0f ? bg : coverage >= 1f ? fg : bg.Mix(fg, coverage);
    }
    #endregion

    #region Fields
    private static readonly CColor UnknownBackground = new(12, 12, 16);
    private readonly int _w;
    private readonly int _h;
    private CColor _tint;
    private float _factor;
    private readonly bool _frosted;
    private readonly bool _gammaCorrect;
    private CColor _textColor = CColor.White;
    private IRenderable? _content;
    private IControl? _below;
    private Position _anchor;
    private Overlay? _overlay;
    #endregion
}

/// <summary>
/// Colour blending for <see cref="GlassPanel"/>: a gamma-space lerp (cheap, matches <see cref="CColor.Mix"/>) or a
/// gamma-correct blend in linear light via two lookup tables (no runtime <c>pow</c>), plus a rough estimate of how
/// much of a cell a glyph inks (for the perceived-colour collapse).
/// </summary>
public static class GlassBlend
{
    #region Constructors
    static GlassBlend()
    {
        for (int i = 0; i < 256; i++)
        {
            float s = i / 255f;
            _srgbToLinear[i] = s <= 0.04045f ? s / 12.92f : MathF.Pow((s + 0.055f) / 1.055f, 2.4f);
        }
        for (int i = 0; i <= LinearSteps; i++)
        {
            float l = i / (float)LinearSteps;
            float s = l <= 0.0031308f ? l * 12.92f : 1.055f * MathF.Pow(l, 1f / 2.4f) - 0.055f;
            _linearToSrgb[i] = (byte)Math.Clamp((int)MathF.Round(s * 255f), 0, 255);
        }
    }
    #endregion

    #region Methods
    /// <summary>Blends <paramref name="from"/> toward <paramref name="to"/> by <paramref name="factor"/> (0..1),
    /// in gamma space, or in linear light when <paramref name="gammaCorrect"/> is set.</summary>
    public static CColor Blend(in CColor from, in CColor to, float factor, bool gammaCorrect)
    {
        if (factor <= 0f) return from;
        if (factor >= 1f) return to;
        return gammaCorrect
            ? new CColor(Channel(from.Red, to.Red, factor), Channel(from.Green, to.Green, factor), Channel(from.Blue, to.Blue, factor))
            : from.Mix(to, factor);
    }

    // Blend one channel in linear light: sRGB -> linear (fwd LUT), lerp, linear -> sRGB (rev LUT).
    private static byte Channel(byte from, byte to, float factor)
    {
        float lin = _srgbToLinear[from] * (1f - factor) + _srgbToLinear[to] * factor;
        int idx = (int)(lin * LinearSteps + 0.5f);
        return _linearToSrgb[idx < 0 ? 0 : idx > LinearSteps ? LinearSteps : idx];
    }

    /// <summary>A rough fraction (0..1) of a cell inked by <paramref name="c"/>: blocks/shades by their fill, a
    /// space by nothing, ordinary text by a representative ink ratio. Used to collapse a cell to one colour.</summary>
    public static float EstimateCoverage(char c) => c switch
    {
        ' ' or '\0' => 0f,
        '█' => 1f,                                     // █ full block
        '▓' => 0.75f,                                 // ▓ dark shade
        '▒' => 0.5f,                                  // ▒ medium shade
        '░' => 0.25f,                                 // ░ light shade
        '▀' or '▄' or '▌' or '▐' => 0.5f, // ▀ ▄ ▌ ▐ half blocks
        '■' or '●' or '◆' => 1f,            // ■ ● ◆ solid marks
        _ => 0.38f,                                        // ordinary ink
    };
    #endregion

    #region Fields
    private const int LinearSteps = 4096;
    private static readonly float[] _srgbToLinear = new float[256];
    private static readonly byte[] _linearToSrgb = new byte[LinearSteps + 1];
    #endregion
}
