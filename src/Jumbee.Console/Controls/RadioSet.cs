namespace Jumbee.Console;

using System;

/// <summary>
/// A vertical group of mutually-exclusive radio options. Exactly one option is selected at a time; navigating
/// with Up/Down and pressing Space/Enter (or clicking a row) selects it. Each row renders as <c>(●)</c> for the
/// selected option and <c>( )</c> otherwise.
/// </summary>
public class RadioSet : ToggleList
{
    #region Constructors
    public RadioSet(params string[] options) : base(options) => ApplyTheme();
    #endregion

    #region Events
    /// <summary>Raised when the selected option changes, with its index.</summary>
    public event EventHandler<int>? SelectionChanged;
    #endregion

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
    #endregion

    #region Methods
    protected override bool IsChecked(int index) => index == _selectedIndex;

    protected override void Activate(int index) => SelectedIndex = index;

    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        SetGlyphs(UI.GlyphTheme.RadioSelected, UI.GlyphTheme.RadioUnselected);
    }
    #endregion

    #region Fields
    private int _selectedIndex = -1;
    #endregion
}
