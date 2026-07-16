namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;

using CColor = ConsoleGUI.Data.Color;

/// <summary>
/// A ray-traced globe rendered into the control's buffer — a shaded, colour-mapped sphere of the Earth. Two rays are
/// shot through every character cell (upper/lower half, drawn with the ▀/▄ half-block glyphs for double vertical
/// resolution); where a ray hits the sphere the surface point is mapped to a lat/long and coloured from an
/// ocean-depth → land-elevation → polar-ice ramp, and (with <see cref="DisplayNight"/>) shaded by a fixed light so the
/// day/night terminator sweeps across as it turns. Spin it by advancing <see cref="RotationAngle"/> (or the
/// <see cref="Spin"/> helper) from a frame/timer feed; tilt and zoom the camera with <see cref="CameraAlpha"/>,
/// <see cref="CameraBeta"/> and <see cref="Zoom"/>. Display-only.
///
/// The land/ocean map is generated at runtime from public-domain Natural Earth land polygons
/// (<c>Geo/land_110m.txt</c>). The ray-traced-sphere idea is a common one (cf. the ASCII globes by DinoZ1729 /
/// adamsky) but this is an independent implementation — no code or data derives from a copyleft source.
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

    /// <summary>Your terminal's cell height-to-width ratio, used to keep the disc circular (a monospace cell is
    /// taller than it is wide). Default 2.0 suits most terminals; raise it if the globe looks vertically stretched,
    /// lower it if squashed. Clamped to ≥ 0.5.</summary>
    public double CellAspect
    {
        get => _cellAspect;
        set => SetAtomicProperty(ref _cellAspect, Math.Max(0.5, value));
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

    /// <summary>When <see langword="true"/> (the default) the surface is coloured from an ocean → land → ice ramp;
    /// when <see langword="false"/> the globe is drawn in a single <see cref="Foreground"/> tone (classic monochrome).</summary>
    public bool Colored
    {
        get => _colored;
        set => SetAtomicProperty(ref _colored, value);
    }

    /// <summary>Base colour used when <see cref="Colored"/> is <see langword="false"/> (default a soft cyan).</summary>
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
    /// <summary>Spins the globe about its polar axis by <paramref name="delta"/> radians — turning the world under a
    /// fixed camera and light, so the day/night terminator stays put on screen (the natural "rotating earth, fixed
    /// sun" look). One invalidation.</summary>
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
    protected override bool WantsMouse => _interactive;

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
    // drawn disc lets the compositor skip the blank margins.
    protected override bool TracksDamage => _damageTracking;

    protected override void Render()
    {
        int w = Size.Width, h = Size.Height;
        if (w <= 0 || h <= 0) return;

        consoleBuffer.Initialize();
        if (w < 2 || h < 2) return;

        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        var mask = EarthMask.Instance;

        // Orthographic camera: a unit sphere viewed from azimuth `alpha` / elevation `beta`. `forward` looks at the
        // origin; `right`/`up` are the screen axes in world space. Zoom scales the image extent (smaller = bigger disc).
        double sinA = Math.Sin(_alpha), cosA = Math.Cos(_alpha), sinB = Math.Sin(_beta), cosB = Math.Cos(_beta);
        double fx = -cosB * sinA, fy = -sinB, fz = -cosB * cosA;              // forward = -cameraDir
        // right = normalize(forward × worldUp), NOT worldUp × forward: that yields the left-handed basis which
        // mirrors the globe east-west (screen +x would sample decreasing longitude, putting the Americas the wrong
        // way round and reversing the drag direction).
        double rx = -fz, ry = 0, rz = fx;
        double rl = Math.Sqrt(rx * rx + rz * rz); if (rl < 1e-9) { rx = 1; rz = 0; rl = 1; }
        rx /= rl; rz /= rl;
        double ux = ry * fz - rz * fy, uy = rz * fx - rx * fz, uz = rx * fy - ry * fx;   // up = right × forward

        // Screen half-extents keep the disc circular for cells that are `CellAspect`× taller than wide (denomY is the
        // vertical radius in rows, denomX the horizontal radius in cells). At the default zoom the disc just fills the
        // limiting dimension; larger zoom shrinks it, smaller enlarges it.
        double aspect = _cellAspect;
        double unit = Math.Min(w / 2.0, h * aspect / 2.0);
        double zoomScale = _zoom / FitZoom;
        double denomX = unit, denomY = unit / aspect, halfW = w / 2.0, halfH = h / 2.0;

        // Samples one sub-cell ray; returns whether it hit the sphere and the shaded surface colour.
        (bool hit, CColor c) Sample(double sxw, double syw)
        {
            double r2 = sxw * sxw + syw * syw;
            if (r2 > 1.0) return (false, default);
            double z = Math.Sqrt(1.0 - r2);
            double px = sxw * rx + syw * ux - z * fx;        // surface point on the unit sphere (world space)
            double py = sxw * ry + syw * uy - z * fy;
            double pz = sxw * rz + syw * uz - z * fz;

            double lat = Math.Asin(Math.Clamp(py, -1.0, 1.0)) * (180.0 / Math.PI);
            double lon = (Math.Atan2(px, pz) - _angle) * (180.0 / Math.PI);
            mask.Sample(lat, lon, out bool land, out int elev, out int depth);

            double bright = 1.0;
            if (_displayNight)
            {
                double lum = _softness * (px * _lx + py * _ly + pz * _lz) + 0.5;
                bright = 0.15 + 0.85 * (lum < 0 ? 0 : lum > 1 ? 1 : lum);
            }
            var baseC = _colored ? Surface(lat, land, elev, depth) : _foreground;
            return (true, Shade(baseC, bright));
        }

        for (int yi = 0; yi < h; yi++)
        {
            for (int xi = 0; xi < w; xi++)
            {
                double sxw = ((xi + 0.5 - halfW) / denomX) * zoomScale;
                // Screen y grows downward, but +up (north) is toward the top, so negate: the row's upper half-cell
                // (higher on screen) must map to the higher latitude.
                var top = Sample(sxw, ((halfH - (yi + 0.25)) / denomY) * zoomScale);
                var bot = Sample(sxw, ((halfH - (yi + 0.75)) / denomY) * zoomScale);
                if (!top.hit && !bot.hit) continue;

                // ▀ paints the fg in the top half and the bg in the bottom; ▄ paints fg in the bottom half.
                Character ch = top.hit && bot.hit ? new('▀', top.c, bot.c)
                    : top.hit ? new('▀', top.c, null)
                    : new('▄', bot.c, null);
                consoleBuffer.Write(new Position(xi, yi), ch);

                if (xi < minX) minX = xi;
                if (xi > maxX) maxX = xi;
                if (yi < minY) minY = yi;
                if (yi > maxY) maxY = yi;
            }
        }

        if (_damageTracking)
        {
            var cur = maxX >= 0 ? new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1) : Rect.Empty;
            Damage(Rect.Surround(_prevDisc, cur));
            _prevDisc = cur;
        }
    }

    // Surface colour: ocean-depth blue → land-elevation green/tan/brown, whitened toward the poles by latitude.
    private static CColor Surface(double latDeg, bool land, int elev, int depth)
    {
        var c = land ? Ramp(LandStops, Math.Clamp(elev / 12.0, 0, 1)) : Ramp(OceanStops, Math.Clamp(depth / 10.0, 0, 1));
        double ice = Math.Clamp((Math.Abs(latDeg) - 66.0) / 14.0, 0, 1);   // sea-ice / ice-sheet toward the poles
        return ice <= 0 ? c : Mix(c, Ice, ice);
    }

    private static CColor Shade(CColor c, double f) =>
        new((byte)(c.Red * f), (byte)(c.Green * f), (byte)(c.Blue * f));

    private static CColor Mix(CColor a, CColor b, double t) => new(
        (byte)(a.Red + (b.Red - a.Red) * t),
        (byte)(a.Green + (b.Green - a.Green) * t),
        (byte)(a.Blue + (b.Blue - a.Blue) * t));

    private static CColor Ramp(CColor[] stops, double t)
    {
        t = t < 0 ? 0 : t > 1 ? 1 : t;
        double scaled = t * (stops.Length - 1);
        int i = (int)Math.Floor(scaled);
        if (i >= stops.Length - 1) return stops[^1];
        return Mix(stops[i], stops[i + 1], scaled - i);
    }
    #endregion

    #region Fields
    private const double DragSpinPerCell = 0.03;
    private const double DragTiltPerCell = 0.03;
    private const double ZoomPerNotch = 0.1;
    private const double FitZoom = 1.35;   // the zoom at which the disc just fills the limiting screen dimension

    private bool _interactive;
    private bool _dragging;
    private Position _lastDrag;

    // Deep ocean → shallow coastal water.
    private static readonly CColor[] OceanStops = [new(46, 112, 150), new(24, 74, 132), new(10, 32, 82)];
    // Coast lowland green → inland green → tan highland → brown mountains.
    private static readonly CColor[] LandStops = [new(74, 132, 78), new(92, 146, 74), new(150, 148, 96), new(126, 102, 74)];
    private static readonly CColor Ice = new(232, 238, 245);

    private double _angle;
    private double _alpha;
    private double _beta = 0.35;
    private double _zoom = 1.35;
    private double _cellAspect = 2.0;   // terminal cell height:width; keeps the disc circular
    private bool _displayNight = true;
    private bool _colored = true;
    private CColor _foreground = new(120, 210, 230);
    private bool _damageTracking = true;
    private Rect _prevDisc = Rect.Empty;   // last frame's drawn disc, unioned with this frame's to avoid ghosting
    // Light mostly overhead (+y) and north (+z) with a little toward the camera (+x), giving a diagonal terminator.
    private double _lx = 0.2074, _ly = 0.8296, _lz = 0.5185;
    private double _softness = 2.5;        // terminator sharpness: higher = harder day/night edge
    #endregion
}

/// <summary>
/// A land/ocean + elevation grid baked once from public-domain Natural Earth 1:110m land polygons: each grid row is
/// scanline-filled by the even-odd rule against the polygon rings, then a distance transform gives land elevation
/// (distance inland) and ocean depth (distance from shore) for colouring. Sampled by lat/long.
/// </summary>
internal sealed class EarthMask
{
    #region Constructors
    private EarthMask()
    {
        // Filled from closed land RINGS, not from the coastline point cloud the map widgets scatter-plot: that cloud
        // carries no polyline structure, so the outline had to be guessed by joining nearby points and the result
        // flood-filled — and a single missed segment let the ocean into a whole continent (Africa, then Asia).
        // Rings make the fill exact: no seeds, no join threshold, nothing to leak through.
        var rings = LoadRings("Jumbee.Console.Geo.land_110m.txt");
        _ocean = new bool[H, W];

        // Even-odd scanline fill. For each row's centre latitude, collect where the rings cross it, sort, and fill
        // between alternate pairs — which also punches out holes (an inland sea's ring simply flips parity back).
        var xs = new List<double>();
        for (int y = 0; y < H; y++)
        {
            double lat = 90.0 - (y + 0.5) / H * 180.0;
            xs.Clear();
            foreach (var ring in rings)
                for (int i = 1; i < ring.Count; i++)
                {
                    var (aLon, aLat) = ring[i - 1];
                    var (bLon, bLat) = ring[i];
                    // Half-open test: a vertex exactly on the row counts once, so parity can't be corrupted.
                    if (aLat > lat == bLat > lat) continue;
                    xs.Add(aLon + (lat - aLat) / (bLat - aLat) * (bLon - aLon));
                }
            xs.Sort();
            for (int i = 0; i + 1 < xs.Count; i += 2)
            {
                int x0 = (int)Math.Ceiling((xs[i] + 180.0) / 360.0 * W - 0.5);
                int x1 = (int)Math.Floor((xs[i + 1] + 180.0) / 360.0 * W - 0.5);
                for (int x = Math.Max(x0, 0); x <= Math.Min(x1, W - 1); x++) _ocean[y, x] = true;   // inside a ring = land
            }
        }
        // The scan marked the INSIDE of the rings; invert so the field means what it says (ocean = outside all land).
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                _ocean[y, x] = !_ocean[y, x];

        // Distance transforms: elevation = steps inland from ocean; depth = steps offshore from land.
        _elev = Distance(land: true);
        _depth = Distance(land: false);
    }
    #endregion

    #region Properties
    public static EarthMask Instance => _instance ??= new EarthMask();
    #endregion

    #region Methods
    public void Sample(double latDeg, double lonDeg, out bool land, out int elev, out int depth)
    {
        double u = (lonDeg + 180.0) / 360.0; u -= Math.Floor(u);
        double v = Math.Clamp((90.0 - latDeg) / 180.0, 0, 0.999999);
        int x = Math.Clamp((int)(u * W), 0, W - 1), y = Math.Clamp((int)(v * H), 0, H - 1);
        land = !_ocean[y, x];
        elev = _elev[y, x];
        depth = _depth[y, x];
    }

    // Multi-source BFS: seed every cell of the OPPOSITE terrain (distance 0) and count steps into the requested
    // terrain. land=true → elevation (steps inland from the shore); land=false → ocean depth (steps out from land).
    private int[,] Distance(bool land)
    {
        var dist = new int[H, W];
        var seen = new bool[H, W];
        var q = new Queue<(int y, int x)>();
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                if (!_ocean[y, x] != land) { seen[y, x] = true; q.Enqueue((y, x)); }
        while (q.Count > 0)
        {
            var (y, x) = q.Dequeue();
            foreach (var (dy, dx) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
            {
                int ny = y + dy; if (ny < 0 || ny >= H) continue;
                int nx = ((x + dx) % W + W) % W;
                if (!seen[ny, nx] && !_ocean[ny, nx] == land) { seen[ny, nx] = true; dist[ny, nx] = dist[y, x] + 1; q.Enqueue((ny, nx)); }
            }
        }
        return dist;
    }

    /// <summary>Reads the embedded land polygons: one <c>lon,lat</c> per line, a blank line ending each closed ring.</summary>
    private static List<List<(double lon, double lat)>> LoadRings(string resource)
    {
        using var stream = typeof(EarthMask).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Embedded map data '{resource}' not found.");
        using var reader = new StreamReader(stream);
        var rings = new List<List<(double lon, double lat)>>();
        var ring = new List<(double lon, double lat)>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            int c = line.IndexOf(',');
            if (c > 0
                && double.TryParse(line.AsSpan(0, c), CultureInfo.InvariantCulture, out double lon)
                && double.TryParse(line.AsSpan(c + 1), CultureInfo.InvariantCulture, out double lat))
            {
                ring.Add((lon, lat));
            }
            else if (ring.Count > 0)
            {
                rings.Add(ring);
                ring = [];
            }
        }
        if (ring.Count > 0) rings.Add(ring);
        return rings;
    }
    #endregion

    #region Fields
    private const int W = 400, H = 200;
    private static EarthMask? _instance;
    private readonly bool[,] _ocean;
    private readonly int[,] _elev;
    private readonly int[,] _depth;
    #endregion
}
