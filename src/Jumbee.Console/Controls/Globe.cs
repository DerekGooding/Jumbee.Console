namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.IO;

using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;

using CColor = ConsoleGUI.Data.Color;

/// <summary>
/// A ray-traced ASCII globe rendered into the control's buffer — a shaded, texture-mapped sphere of the Earth. A ray
/// is shot through every character cell; where it hits the sphere the surface point is mapped back to a lat/long in
/// the built-in Earth texture to pick a glyph, and (with <see cref="DisplayNight"/>) shaded by a fixed light so the
/// day/night terminator sweeps across as it turns. Spin it by advancing <see cref="RotationAngle"/> (or the
/// <see cref="Spin"/> helper) from a frame/timer feed; tilt and zoom the camera with <see cref="CameraAlpha"/>,
/// <see cref="CameraBeta"/> and <see cref="Zoom"/>. Display-only.
///
/// Ported from the C++/Rust "globe" ASCII generator (DinoZ1729 / adamsky).
/// </summary>
public class Globe : Control
{
    #region Constructors
    public Globe() => Focusable = false;   // display-only
    #endregion

    #region Properties
    /// <summary>Rotation of the globe about its polar axis, in radians. Advance it each tick to spin the world.</summary>
    public double RotationAngle
    {
        get => _angle;
        set => SetAtomicProperty(ref _angle, value);
    }

    /// <summary>Camera longitude angle (radians) in the equatorial plane — orbits the globe left/right.</summary>
    public double CameraAlpha
    {
        get => _alpha;
        set => SetAtomicProperty(ref _alpha, value);
    }

    /// <summary>Camera latitude angle (radians) — tilts the view up/down towards the poles. Clamped to ±1.5.</summary>
    public double CameraBeta
    {
        get => _beta;
        set => SetAtomicProperty(ref _beta, Math.Clamp(value, -1.5, 1.5));
    }

    /// <summary>Camera distance from the centre; smaller is closer (a bigger globe). Clamped to ≥ 1.05.</summary>
    public double Zoom
    {
        get => _zoom;
        set => SetAtomicProperty(ref _zoom, Math.Max(1.05, value));
    }

    /// <summary>When <see langword="true"/>, shade the sphere by a fixed light so a day/night terminator is drawn;
    /// otherwise the whole globe is lit evenly. Default <see langword="true"/>.</summary>
    public bool DisplayNight
    {
        get => _displayNight;
        set => SetAtomicProperty(ref _displayNight, value);
    }

    /// <summary>When <see langword="true"/> (the default) the surface is coloured from a blue-ocean → green-land ramp;
    /// when <see langword="false"/> the globe is drawn in a single <see cref="Foreground"/> tone (classic monochrome).</summary>
    public bool Colored
    {
        get => _colored;
        set => SetAtomicProperty(ref _colored, value);
    }

    /// <summary>Base glyph colour used when <see cref="Colored"/> is <see langword="false"/> (default a soft cyan).</summary>
    public Color Foreground
    {
        get => _foreground;
        set => SetAtomicProperty(ref _foreground, value);
    }

    /// <summary>When <see langword="true"/> (the default), the globe reports only its drawn disc as damaged each
    /// frame so the compositor skips the blank margins around it (opt-in partial redraw). Set <see langword="false"/>
    /// to fall back to reporting the whole control rect — used to A/B the optimization.</summary>
    public bool DamageTracking
    {
        get => _damageTracking;
        set => SetAtomicProperty(ref _damageTracking, value);
    }

    /// <summary>
    /// When <see langword="true"/>, the globe responds to user input: <b>drag</b> to rotate/tilt, the <b>mouse wheel</b>
    /// to zoom, and (while focused) the <b>arrow keys</b> to spin/tilt and <b>+/-</b> to zoom (Shift = larger step).
    /// Enabling it makes the globe focusable (it joins keyboard navigation); the default (<see langword="false"/>)
    /// leaves it display-only.
    /// </summary>
    public bool Interactive
    {
        get => _interactive;
        set
        {
            if (_interactive == value) return;
            _interactive = value;
            Focusable = value;   // interactive globes take focus for keyboard + wheel; display-only ones stay out of nav
            Invalidate();
        }
    }
    #endregion

    #region Methods
    /// <summary>Spins the globe about its polar axis by <paramref name="delta"/> radians — scrolling the texture
    /// under a fixed camera and light, so the world turns and the day/night terminator stays put on screen (the
    /// natural "rotating earth, fixed sun" look). One invalidation.</summary>
    /// <remarks>Only the texture rotation (<see cref="RotationAngle"/>) is advanced. The reference generator also
    /// counter-orbited the camera by half the angle each tick, but that <em>exactly cancels</em> the texture scroll
    /// (at screen centre <c>theta ≈ alpha/π + angle/2π</c>), leaving the globe fixed with only the shading moving —
    /// so the camera coupling is deliberately omitted here.</remarks>
    public void Spin(double delta = 0.01)
    {
        UI.Invoke(() =>
        {
            _angle += delta;
            Invalidate();
        });
    }

    /// <summary>Sets the directional light (world space) used for day/night shading and its
    /// <paramref name="softness"/> (terminator sharpness; higher = harder edge). The direction is normalized.</summary>
    public void SetLight(double x, double y, double z, double softness)
    {
        double len = Math.Sqrt(x * x + y * y + z * z);
        if (len <= 0) return;
        UI.Invoke(() =>
        {
            _lx = x / len; _ly = y / len; _lz = z / len; _softness = softness;
            Invalidate();
        });
    }

    #region Input (active only when Interactive)
    // Route mouse events to the globe when interactive (Focusable already does this too, but be explicit).
    protected override bool WantsMouse => _interactive;

    // Receive keyboard input (arrow keys / +-) only when interactive; otherwise keys pass through for navigation.
    public override bool HandlesInput => _interactive;

    protected override void OnMousePress(Position position)
    {
        if (!_interactive) return;
        _dragging = true;
        _lastDrag = position;
        CaptureMouse();   // keep receiving move/up even when the pointer leaves the disc
    }

    protected override void OnMouseMove(Position position)
    {
        if (!_dragging) return;
        int dx = position.X - _lastDrag.X;
        int dy = position.Y - _lastDrag.Y;
        _lastDrag = position;
        // Horizontal drag spins the globe about its pole; vertical drag tilts the camera (CameraBeta clamps itself).
        RotationAngle += dx * DragSpinPerCell;
        CameraBeta += dy * DragTiltPerCell;
    }

    protected override void OnMouseRelease(Position position)
    {
        if (!_dragging) return;
        _dragging = false;
        ReleaseMouse();
    }

    protected override void OnMouseWheel(Position position, int delta)
    {
        if (!_interactive) { base.OnMouseWheel(position, delta); return; }
        // Wheel up (delta < 0) decreases Zoom → closer/bigger globe; down zooms out. Zoom clamps to >= 1.05.
        Zoom += delta * ZoomPerNotch;
    }

    protected override void OnInput(InputEvent inputEvent)
    {
        if (!_interactive) return;
        bool shift = (inputEvent.Key.Modifiers & ConsoleModifiers.Shift) != 0;
        double spin = shift ? 0.5 : 0.15;
        double tilt = shift ? 0.3 : 0.1;
        double zoom = shift ? 0.3 : 0.1;
        switch (inputEvent.Key.Key)
        {
            case ConsoleKey.LeftArrow: RotationAngle -= spin; break;
            case ConsoleKey.RightArrow: RotationAngle += spin; break;
            case ConsoleKey.UpArrow: CameraBeta += tilt; break;
            case ConsoleKey.DownArrow: CameraBeta -= tilt; break;
            case ConsoleKey.Add or ConsoleKey.OemPlus: Zoom -= zoom; break;   // + zooms in
            case ConsoleKey.Subtract or ConsoleKey.OemMinus: Zoom += zoom; break;   // - zooms out
            default: return;   // leave other keys unhandled for navigation
        }
        inputEvent.Handled = true;
    }
    #endregion

    // A globe fills its container and re-fits on resize; it must never be scrolled (inside a ControlFrame this hands
    // it the bounded viewport height instead of the unbounded scroll height, which would balloon it to the clamp).
    protected internal override bool FillsFrameViewport => true;

    // Opt into partial redraw: the disc is inscribed in (and usually narrower than) the pane, so reporting just the
    // drawn disc lets the compositor skip the blank margins. The disc changes almost everywhere as it spins, so the
    // win is the margin area, not the disc itself — most valuable in panes wider than 2:1 where the margins are large.
    protected override bool TracksDamage => _damageTracking;

    protected override void Render()
    {
        int w = Size.Width, h = Size.Height;
        if (w <= 0 || h <= 0) return;

        consoleBuffer.Initialize();
        if (w < 2 || h < 2) return;

        // Track the bounding box of the cells we actually draw, to report as damage below.
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;

        var tex = EarthTexture.Instance;

        // Camera basis + position for (zoom, alpha, beta). Only the forward transform is needed to turn a screen ray
        // into a world-space direction (the reference also builds an inverse it never reads).
        double sinA = Math.Sin(_alpha), cosA = Math.Cos(_alpha);
        double sinB = Math.Sin(_beta), cosB = Math.Cos(_beta);
        double ox = _zoom * cosA * cosB, oy = _zoom * sinA * cosB, oz = _zoom * sinB;
        // 3×3 rotation columns (right, up, forward) of the camera matrix; translation is the camera position.
        double m0 = -sinA, m1 = cosA, m2 = 0;
        double m4 = cosA * sinB, m5 = sinA * sinB, m6 = -cosB;
        double m8 = cosA * cosB, m9 = sinA * cosB, m10 = sinB;

        // Screen half-extents in cells, keeping the terminal's ~2:1 cell aspect so the globe stays circular: one
        // vertical cell spans twice the world of one horizontal cell. `unit` is chosen to fit the limiting dimension
        // and centre the globe in the pane.
        double unit = Math.Min(w / 2.0, h);
        double denomX = unit, denomY = unit / 2.0;
        double halfW = w / 2.0, halfH = h / 2.0;
        double r2 = Radius * Radius;
        double dotOO = ox * ox + oy * oy + oz * oz;

        for (int yi = 0; yi < h; yi++)
        {
            double sy = ((yi - halfH) + 0.5) / denomY;
            for (int xi = 0; xi < w; xi++)
            {
                double sx = -(((xi - halfW) + 0.5) / denomX);

                // Screen ray (sx, sy, -1) rotated into world space, then made a direction from the camera origin.
                double dx = sx * m0 + sy * m4 - m8;
                double dy = sx * m1 + sy * m5 - m9;
                double dz = sx * m2 + sy * m6 - m10;
                double invLen = 1.0 / Math.Sqrt(dx * dx + dy * dy + dz * dz);
                dx *= invLen; dy *= invLen; dz *= invLen;

                // Ray/sphere intersection (sphere centred at origin, radius `Radius`).
                double dotUO = dx * ox + dy * oy + dz * oz;
                double disc = dotUO * dotUO - dotOO + r2;
                if (disc < 0) continue;   // ray misses the globe

                double dist = -Math.Sqrt(disc) - dotUO;
                double ix = ox + dist * dx, iy = oy + dist * dy, iz = oz + dist * dz;

                // Surface point → lat/long → texture cell.
                double nInv = 1.0 / Math.Sqrt(ix * ix + iy * iy + iz * iz);
                double nx = ix * nInv, ny = iy * nInv, nz = iz * nInv;

                double phi = -iz / Radius / 2.0 + 0.5;
                double theta = Math.Atan(iy / ix) / Math.PI + 0.5 + _angle / 2.0 / Math.PI;
                theta -= Math.Floor(theta);

                int ex = (int)(theta * tex.Width);
                int ey = (int)(phi * tex.Height);
                if (ex < 0) ex = 0; else if (ex >= tex.Width) ex = tex.Width - 1;
                if (ey < 0) ey = 0; else if (ey >= tex.Height) ey = tex.Height - 1;

                int dayIdx = tex.DayIndex[ey][ex];

                char glyph;
                double shade;
                if (_displayNight)
                {
                    // Lambertian shade from a fixed directional light; the terminator is where the surface turns away.
                    // luminance = clamp(softness·(n·L) + 0.5, 0, 1). A lower softness spreads the day→night gradient
                    // across more of the face (a soft crescent) rather than a hard band; L is tilted toward the camera
                    // so the terminator sits off to one side instead of dead-centre.
                    double lum = _softness * (nx * _lx + ny * _ly + nz * _lz) + 0.5;
                    lum = lum < 0 ? 0 : lum > 1 ? 1 : lum;
                    int nightIdx = tex.NightIndex[ey][ex];
                    int blended = (int)((1.0 - lum) * nightIdx + lum * dayIdx);
                    if (blended < 0 || blended >= tex.Palette.Length) blended = 0;
                    glyph = tex.Palette[blended];
                    shade = 0.18 + 0.82 * lum;
                }
                else
                {
                    glyph = tex.Palette[dayIdx];
                    shade = 1.0;
                }

                if (glyph == ' ') glyph = '.';   // keep the disc solid even where a blank blend lands
                var color = _colored ? Shade(OceanLand(dayIdx, tex.MaxIndex), shade) : Shade(_foreground, shade);
                consoleBuffer.Write(new Position(xi, yi), new Character(glyph, color));

                if (xi < minX) minX = xi;
                if (xi > maxX) maxX = xi;
                if (yi < minY) minY = yi;
                if (yi > maxY) maxY = yi;
            }
        }

        if (_damageTracking)
        {
            // Report the drawn disc, unioned with last frame's, so a shrinking disc (zoom out / tilt) still erases
            // the cells it vacated. When the disc is stable (a plain spin) the union is just the disc box.
            var cur = maxX >= 0 ? new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1) : Rect.Empty;
            Damage(Rect.Surround(_prevDisc, cur));
            _prevDisc = cur;
        }
    }

    // Maps a day-texture palette index (low = ocean, high = land) to an Earth-like colour ramp.
    private static CColor OceanLand(int index, int maxIndex)
    {
        double t = maxIndex <= 0 ? 0 : (double)index / maxIndex;
        return Ramp(EarthStops, t);
    }

    private static CColor Shade(CColor c, double f) =>
        new((byte)(c.Red * f), (byte)(c.Green * f), (byte)(c.Blue * f));

    private static CColor Ramp(CColor[] stops, double t)
    {
        t = t < 0 ? 0 : t > 1 ? 1 : t;
        double scaled = t * (stops.Length - 1);
        int i = (int)Math.Floor(scaled);
        if (i >= stops.Length - 1) return stops[^1];
        double f = scaled - i;
        var a = stops[i];
        var b = stops[i + 1];
        return new CColor(
            (byte)(a.Red + (b.Red - a.Red) * f),
            (byte)(a.Green + (b.Green - a.Green) * f),
            (byte)(a.Blue + (b.Blue - a.Blue) * f));
    }
    #endregion

    #region Fields
    private const double Radius = 1.0;

    // Input sensitivities (tuned for a natural drag/zoom feel; radians per dragged cell, Zoom units per wheel notch).
    private const double DragSpinPerCell = 0.03;
    private const double DragTiltPerCell = 0.03;
    private const double ZoomPerNotch = 0.1;

    private bool _interactive;
    private bool _dragging;
    private Position _lastDrag;

    // Deep ocean → shallow → coast → lowland → land → highland.
    private static readonly CColor[] EarthStops =
    [
        new(12, 34, 92), new(20, 74, 150), new(42, 122, 150),
        new(70, 140, 72), new(120, 150, 78), new(180, 168, 128),
    ];

    private double _angle;
    private double _alpha;
    private double _beta = 0.35;
    private double _zoom = 1.35;
    private bool _displayNight = true;
    private bool _colored = true;
    private CColor _foreground = new(120, 210, 230);
    private bool _damageTracking = true;
    private Rect _prevDisc = Rect.Empty;   // last frame's drawn disc, unioned with this frame's to avoid ghosting
    // Light mostly overhead (+y) and north (+z) with a little toward the camera (+x). A purely-overhead light (0,1,0)
    // is perpendicular to the default ~+x camera, which puts the day/night terminator as a hard VERTICAL BAND down the
    // centre; the +z tilt swings it to a natural diagonal crescent along the lower/right rim (≈¼ of the disc in
    // shadow) and the small +x keeps it off the exact meridian. Normalized (0.2, 0.8, 0.5).
    private double _lx = 0.2074, _ly = 0.8296, _lz = 0.5185;
    private double _softness = 2.5;        // terminator sharpness: higher = harder day/night edge
    #endregion
}

/// <summary>
/// The built-in Earth day/night textures, parsed once into per-cell palette indices. Each texture row is stored
/// reversed (mirrored horizontally) to match the reference generator's orientation.
/// </summary>
internal sealed class EarthTexture
{
    #region Constructors
    private EarthTexture()
    {
        // Density ramp: blank → sparse punctuation → dense letters. Index into this = "brightness"/land-ness.
        Palette = [' ', '.', ':', ';', '\'', ',', 'w', 'i', 'o', 'g', 'O', 'L', 'X', 'H', 'W', 'Y', 'V', '@'];
        MaxIndex = Palette.Length - 1;

        var day = Load("Jumbee.Console.Textures.earth.txt");
        var night = Load("Jumbee.Console.Textures.earth_night.txt");

        // get_size() in the reference subtracts one, so theta/phi never index the final row/column.
        Width = day.Length > 0 ? day[0].Length - 1 : 0;
        Height = day.Length - 1;

        DayIndex = ToIndices(day);
        NightIndex = ToIndices(night);
    }
    #endregion

    #region Properties
    public static EarthTexture Instance => _instance ??= new EarthTexture();

    public char[] Palette { get; }
    public int MaxIndex { get; }
    public int Width { get; }
    public int Height { get; }
    public int[][] DayIndex { get; }
    public int[][] NightIndex { get; }
    #endregion

    #region Methods
    private int[][] ToIndices(char[][] rows)
    {
        var result = new int[rows.Length][];
        for (int y = 0; y < rows.Length; y++)
        {
            var row = rows[y];
            var line = new int[row.Length];
            for (int x = 0; x < row.Length; x++)
            {
                int idx = Array.IndexOf(Palette, row[x]);
                line[x] = idx < 0 ? 0 : idx;
            }
            result[y] = line;
        }
        return result;
    }

    private static char[][] Load(string resource)
    {
        using var stream = typeof(EarthTexture).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Embedded texture '{resource}' not found.");
        using var reader = new StreamReader(stream);

        var rows = new List<char[]>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0) continue;
            var chars = line.ToCharArray();
            Array.Reverse(chars);   // mirror each row, as the reference does
            rows.Add(chars);
        }
        return [.. rows];
    }
    #endregion

    #region Fields
    private static EarthTexture? _instance;
    #endregion
}
