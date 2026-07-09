namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.IO;

using ConsoleGUI.Data;
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
    public CColor Foreground
    {
        get => _foreground;
        set => SetAtomicProperty(ref _foreground, value);
    }
    #endregion

    #region Methods
    /// <summary>Advances the globe's own spin by <paramref name="delta"/> radians and counter-rotates the camera by
    /// half that, matching the reference screensaver so the world turns under a steady view. One invalidation.</summary>
    public void Spin(double delta = 0.01)
    {
        UI.Invoke(() =>
        {
            _angle += delta;
            _alpha -= delta / 2.0;
            Invalidate();
        });
    }

    // A globe fills its container and re-fits on resize; it must never be scrolled (inside a ControlFrame this hands
    // it the bounded viewport height instead of the unbounded scroll height, which would balloon it to the clamp).
    protected internal override bool FillsFrameViewport => true;

    protected override void Render()
    {
        int w = Size.Width, h = Size.Height;
        if (w <= 0 || h <= 0) return;

        consoleBuffer.Initialize();
        if (w < 2 || h < 2) return;

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
                    // Lambertian shade from a fixed overhead light; the terminator is where the surface turns away.
                    // light = (0, +∞, 0), so l points to +y; luminance = clamp(5·(n·l) + 0.5, 0, 1) = clamp(5·ny + .5).
                    double lum = 5.0 * ny + 0.5;
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
            }
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
