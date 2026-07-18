namespace Jumbee.Console;

using System.Linq;

using ConsoleGUI.Data;
using ConsoleGUI.Space;

/// <summary>The layout direction of a <see cref="TextLabel"/>.</summary>
public enum TextLabelOrientation
{
    /// <summary>Text runs left-to-right across a single row.</summary>
    Horizontal,
    /// <summary>Text runs top-to-bottom down a single column.</summary>
    Vertical
}

/// <summary>
/// Displays a single-line text label with a defined horizontal or vertical orientation and foreground and background color.
/// </summary>
public class TextLabel : Control
{
    #region Constructors
    /// <summary>Initializes a new <see cref="TextLabel"/> with the given <paramref name="orientation"/>, <paramref name="text"/>, and optional foreground/background colours.</summary>
    // Colours are nullable and default to transparent (null): an unset foreground inherits the terminal default and
    // an unset background lets whatever is behind show through. Passing the non-nullable default(Color) here would
    // paint an opaque BLACK background — invisible on a black terminal, but it dims to near-black under an overlay
    // scrim (and blocks compositing), which is rarely what a plain label wants.
    public TextLabel(TextLabelOrientation orientation, string text, Color? fgcolor = null, Color? bgcolor = null)
    {
        Focusable = false;   // a passive display label: never a focus/tab target, never owns the cursor
        _orientation = orientation;
        _text = text;
        _fgcolor = fgcolor;
        _bgcolor = bgcolor;
        chars = new Cell[_text.Length];
        size = orientation == TextLabelOrientation.Horizontal ? new Size(_text.Length, 1) :new Size(1, _text.Length);
        Resize(size);
    }
    #endregion

    #region Properties
    /// <summary>Foreground colour, or <see langword="null"/> for the terminal default.</summary>
    public Color? FgColor
    {
        get => _fgcolor;
        set => SetAtomicProperty(ref _fgcolor, value);
    }

    /// <summary>Background colour, or <see langword="null"/> for transparent (shows whatever is behind).</summary>
    public Color? BgColor
    {
        get => _bgcolor;
        set => SetAtomicProperty(ref _bgcolor, value);
    }

    /// <summary>The label text. Setting it re-sizes the control when the length changes.</summary>
    public string Text
    {
        get => _text;
        set => SetAtomicProperty(ref _text, value, watch: (old, @new) =>
        {
            chars = new Cell[_text.Length];
            // Only resize when the text length (the extent along the text axis) changes. A same-length update — e.g. a
            // "52%"→"54%" gauge tick — is a content change the following paint's own damage report already covers, so
            // an unconditional Resize here would report this label's whole area a second, redundant time every update.
            if ((old?.Length ?? 0) != (@new?.Length ?? 0))
            {
                size = _orientation == TextLabelOrientation.Horizontal ? new Size(_text.Length, 1) : new Size(1, _text.Length);
                Resize(size);
            }
        });
    }
    #endregion

    #region Indexers
    /// <summary>The rendered cell at <paramref name="position"/>, or an empty cell outside the text.</summary>
    public override Cell this[Position position]
    {
        get
        {
            if (string.IsNullOrEmpty(_text))
            {
                return emptyCell;
            }
            else if (_orientation == TextLabelOrientation.Horizontal)
            {
                if (position.Y >= 1 || position.X >= Text.Length)
                {
                    return emptyCell;
                }
                else
                {
                    return chars[position.X];
                }
            }
            else
            {
                if (position.X >= 1 || position.Y >= Text.Length)
                {
                    return emptyCell;
                }
                else
                {
                    return chars[position.Y];
                }
            }
        }
    }
    #endregion

    #region Methods
    /// <summary>Renders each character into the label's cell buffer with the configured colours.</summary>
    // We use a 1D buffer to render instead of the 2D consoleBuffer as it's more efficient to access.
    protected override void Render()
    {
        for (int i = 0; i < _text.Length; i++)
        {
            chars[i] = (Cell)new Character(_text[i], foreground: _fgcolor, background: _bgcolor);
        }
    }

    // A label is fixed in its minor axis (a horizontal label is 1 row tall, a vertical one is 1 column wide) and
    // fills along its text axis (returning 0 there). Reporting this as an intrinsic size keeps a label docked on a
    // DockPanel edge from ballooning to fill the panel and collapsing the fill region — see CalculateSize.
    /// <summary>1 for a vertical label (fixed one column wide), otherwise 0 (fills along the text axis).</summary>
    protected override int IntrinsicWidth() => _orientation == TextLabelOrientation.Vertical ? 1 : 0;

    /// <summary>1 for a horizontal label (fixed one row tall), otherwise 0 (fills along the text axis).</summary>
    protected override int IntrinsicHeight() => _orientation == TextLabelOrientation.Horizontal ? 1 : 0;

    #endregion

    #region Fields
    private TextLabelOrientation _orientation;
    private string _text = "";
    private Color? _fgcolor;
    private Color? _bgcolor;
    private Size size;
    private Cell[] chars = [];
    #endregion
}
