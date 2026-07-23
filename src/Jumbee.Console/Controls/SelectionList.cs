namespace Jumbee.Console;
/// <summary>
/// A vertical list of independently-checkable options (multi-select).
/// </summary>
/// <remarks>
/// Navigate with Up/Down and press Space/Enter (or click a row) to toggle that option. Each row renders as
/// <c>[X]</c> when checked and <c>[ ]</c> otherwise.
/// </remarks>
public class SelectionList : ToggleList
{
    #region Constructors

    /// <summary>Initializes a new <see cref="SelectionList"/> with the given <paramref name="options"/>.</summary>
    public SelectionList(params string[] options) : base(options) => ApplyTheme();

    #endregion Constructors

    #region Events

    /// <summary>Raised when an option's checked state changes, with its index.</summary>
    public event EventHandler<int>? SelectionChanged;

    #endregion Events

    #region Properties

    /// <summary>The indices of the checked options, in ascending order.</summary>
    public IReadOnlyList<int> SelectedIndices => _checked.Order().ToList();

    /// <summary>The text of the checked options, in option order.</summary>
    public IReadOnlyList<string> SelectedValues =>
        Enumerable.Range(0, _options.Count).Where(_checked.Contains).Select(i => _options[i]).ToList();

    #endregion Properties

    #region Methods

    /// <summary>Sets the checked state of an option, raising <see cref="SelectionChanged"/> when it changes.</summary>
    public void SetChecked(int index, bool isChecked)
    {
        if (index < 0 || index >= _options.Count) return;
        var changed = isChecked ? _checked.Add(index) : _checked.Remove(index);
        if (!changed) return;
        Invalidate();
        SelectionChanged?.Invoke(this, index);
    }

    /// <summary>Whether the option at <paramref name="index"/> is checked.</summary>
    public bool IsCheckedAt(int index) => _checked.Contains(index);

    /// <inheritdoc/>
    protected override bool IsChecked(int index) => _checked.Contains(index);

    /// <inheritdoc/>
    protected override void Activate(int index) => SetChecked(index, !_checked.Contains(index));

    /// <inheritdoc/>
    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        SetGlyphs(UI.GlyphTheme.CheckboxChecked, UI.GlyphTheme.CheckboxUnchecked);
    }

    #endregion Methods

    #region Fields

    private readonly HashSet<int> _checked = [];

    #endregion Fields
}