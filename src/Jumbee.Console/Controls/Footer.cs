using Spectre.Console.Rendering;

namespace Jumbee.Console;
/// <summary>A single key-binding hint shown in a <see cref="Footer"/>: the key chord and what it does.</summary>
public readonly record struct FooterHint(string Key, string Label);

/// <summary>
/// A one-row key-hints bar (e.g. <c>^j Send  ^t Method  ^c Quit  f1 Help</c>), filling the available width.
/// </summary>
/// <remarks>
/// The key chord is drawn in an accent style and the label in the normal text style. Non-interactive; hints are set
/// by the app (typically mirroring its global hotkeys).
/// </remarks>
public class Footer : RenderableControl
{
    #region Constructors

    /// <summary>Initializes a new <see cref="Footer"/> with the given key-binding hints.</summary>
    public Footer(params FooterHint[] hints)
    {
        Focusable = false;
        if (hints is { Length: > 0 }) _hints.AddRange(hints);
        ApplyTheme();
    }

    #endregion Constructors

    #region Properties

    /// <summary>Spaces between adjacent hints. Defaults to 2.</summary>
    public int Gap
    {
        get;
        set => SetAtomicProperty(ref field, Math.Max(1, value));
    } = 2;

    /// <summary>Style of the key chord (e.g. <c>^j</c>). Defaults to <see cref="IStyleTheme.TextAccent"/>.</summary>
    public Style KeyStyle { get => _keyStyle; set => SetAtomicProperty(ref _keyStyle, value, themeOverride: true); }

    /// <summary>Style of the label text. Defaults to <see cref="IStyleTheme.Text"/>.</summary>
    public Style LabelStyle { get => _labelStyle; set => SetAtomicProperty(ref _labelStyle, value, themeOverride: true); }

    #endregion Properties

    #region Methods

    /// <summary>Replaces the hints shown.</summary>
    public void SetHints(params FooterHint[] hints) => UI.Invoke(() =>
    {
        _hints.Clear();
        if (hints is { Length: > 0 }) _hints.AddRange(hints);
        Invalidate();
    });

    /// <summary>Appends a single hint.</summary>
    public void Add(string key, string label) => UI.Invoke(() =>
    {
        _hints.Add(new FooterHint(key, label));
        Invalidate();
    });

    /// <inheritdoc/>
    protected override bool RendersInteractiveState => false;

    /// <inheritdoc/>
    protected override void ApplyTheme()
    {
        if (!IsThemeOverridden(nameof(KeyStyle))) _keyStyle = UI.StyleTheme.TextAccent;
        if (!IsThemeOverridden(nameof(LabelStyle))) _labelStyle = UI.StyleTheme.Text;
    }

    /// <inheritdoc/>
    protected override int IntrinsicHeight() => 1;

    /// <inheritdoc/>
    protected override int IntrinsicWidth() => 0;   // fill the width

    /// <inheritdoc/>
    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        if (maxWidth <= 0) yield break;
        var key = _keyStyle.SpectreConsoleStyle;
        var label = _labelStyle.SpectreConsoleStyle;
        var gap = new string(' ', Gap);

        var used = 0;
        for (var i = 0; i < _hints.Count; i++)
        {
            var h = _hints[i];
            // width this hint contributes: leading gap (except first) + key + space + label
            var sep = i == 0 ? 0 : Gap;
            var cost = sep + h.Key.Length + 1 + h.Label.Length;
            if (used + cost > maxWidth) break;   // stop cleanly rather than clip mid-hint

            if (i > 0) yield return new Segment(gap, label);
            yield return new Segment(h.Key, key);
            yield return new Segment(" ", label);
            yield return new Segment(h.Label, label);
            used += cost;
        }
    }

    #endregion Methods

    #region Fields

    private readonly List<FooterHint> _hints = [];
    private Style _keyStyle;
    private Style _labelStyle;

    #endregion Fields
}