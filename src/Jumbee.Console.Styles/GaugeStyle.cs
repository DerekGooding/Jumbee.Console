namespace Jumbee.Console;

/// <summary>
/// The per-part <see cref="Style"/> a <c>Gauge</c> composes: the filled portion of the bar,
/// the empty track behind it, and the percent/value readout (and any inline label).
/// </summary>
/// <remarks>Only the foreground colour of <see cref="Fill"/>/<see cref="Track"/> is used — the bar is drawn as a
/// solid colour band.</remarks>
public readonly struct GaugeStyle : System.IEquatable<GaugeStyle>
{
    #region Constructors

    /// <summary>Initializes a new <see cref="GaugeStyle"/> from the fill, track, and text styles.</summary>
    public GaugeStyle(Style fill, Style track, Style text)
    {
        Fill = fill;
        Track = track;
        Text = text;
    }

    #endregion Constructors

    #region Properties

    /// <summary>The filled portion of the bar (its foreground colour fills the band).</summary>
    public Style Fill { get; init; }

    /// <summary>The empty track behind the fill (its foreground colour fills the band).</summary>
    public Style Track { get; init; }

    /// <summary>The percent/value readout and any inline label.</summary>
    public Style Text { get; init; }

    #endregion Properties

    #region Methods

    /// <summary>A copy with the fill recoloured (keeps the track and text).</summary>
    public GaugeStyle WithFill(Color fill) => new(fill, Track, Text);

    #endregion Methods

    #region Presets

    /// <summary>A blue fill on a dim dark-grey track, with grey text.</summary>
    public static GaugeStyle Default { get; } = new(
        fill: new Color(90, 160, 240),
        track: new Color(48, 48, 58),
        text: Style.Grey85);

    #endregion Presets

    #region Equality

    // Hand-written: Style holds a reference, so the runtime's default ValueType.Equals falls back to a reflective,
    // boxing field-by-field compare — which is what SetAtomicProperty would run on every assignment. See Color.
    /// <summary>Determines whether this <see cref="GaugeStyle"/> equals <paramref name="other"/>.</summary>
    public bool Equals(GaugeStyle other) => Fill == other.Fill && Track == other.Track && Text == other.Text;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is GaugeStyle other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => System.HashCode.Combine(Fill, Track, Text);

    /// <summary>Equality operator.</summary>
    public static bool operator ==(GaugeStyle a, GaugeStyle b) => a.Equals(b);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(GaugeStyle a, GaugeStyle b) => !a.Equals(b);

    #endregion Equality
}