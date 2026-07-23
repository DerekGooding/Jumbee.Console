
using System;

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
        get => _selectedIndex;
        set
        {
            var clamped = value < 0 || _options.Count == 0 ? -1 : Math.Clamp(value, 0, _options.Count - 1);
            if (clamped == _selectedIndex) return;
            _selectedIndex = clamped;
            if (clamped >= 0) CursorIndex = clamped;
            Invalidate();
            SelectionChanged?.Invoke(this, _selectedIndex);
        }
    }

    /// <summary>The selected option text, or <see langword="null"/> when nothing is selected.</summary>
    public string? SelectedValue =>
        _selectedIndex >= 0 && _selectedIndex < _options.Count ? _options[_selectedIndex] : null;

    #endregion Properties

    #region Methods

    /// <inheritdoc/>
    protected override bool IsChecked(int index) => index == _selectedIndex;

    /// <inheritdoc/>
    protected override void Activate(int index) => SelectedIndex = index;

    /// <inheritdoc/>
    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        SetGlyphs(UI.GlyphTheme.RadioSelected, UI.GlyphTheme.RadioUnselected);
    }

    #endregion Methods

    #region Fields

    private int _selectedIndex = -1;

    #endregion Fields
}