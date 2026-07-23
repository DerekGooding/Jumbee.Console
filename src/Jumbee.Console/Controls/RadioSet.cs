namespace Jumbee.Console;
/// <summary>
/// A vertical group of mutually-exclusive radio options, exactly one selected at a time.
/// </summary>
/// <remarks>
/// Navigating with Up/Down and pressing Space/Enter (or clicking a row) selects it. Each row renders as
/// <c>(●)</c> for the selected option and <c>( )</c> otherwise.
/// </remarks>
public class RadioSet : ToggleList
{
    #region Constructors

    /// <summary>Initializes a new <see cref="RadioSet"/> with the given <paramref name="options"/>.</summary>
    public RadioSet(params string[] options) : base(options) => ApplyTheme();

    #endregion Constructors

    #region Events

    /// <summary>Raised when the selected option changes, with its index.</summary>
    public event EventHandler<int>? SelectionChanged;

    #endregion Events

    #region Properties

    /// <summary>The index of the selected option, or -1 when nothing is selected. Setting it raises <see cref="SelectionChanged"/>.</summary>
    public int SelectedIndex
    {
        get;
        set
        {
            var clamped = value < 0 || _options.Count == 0 ? -1 : Math.Clamp(value, 0, _options.Count - 1);
            if (clamped == field) return;
            field = clamped;
            if (clamped >= 0) CursorIndex = clamped;
            Invalidate();
            SelectionChanged?.Invoke(this, field);
        }
    } = -1;

    /// <summary>The selected option text, or <see langword="null"/> when nothing is selected.</summary>
    public string? SelectedValue =>
        SelectedIndex >= 0 && SelectedIndex < _options.Count ? _options[SelectedIndex] : null;

    #endregion Properties

    #region Methods

    /// <inheritdoc/>
    protected override bool IsChecked(int index) => index == SelectedIndex;

    /// <inheritdoc/>
    protected override void Activate(int index) => SelectedIndex = index;

    /// <inheritdoc/>
    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        SetGlyphs(UI.GlyphTheme.RadioSelected, UI.GlyphTheme.RadioUnselected);
    }

    #endregion Methods

}