namespace Jumbee.Console;

using ConsoleGUI;
using ConsoleGUI.Common;
using ConsoleGUI.Data;
using ConsoleGUI.Space;

/// <summary>
/// A modal scrim that shows the layer beneath it, dimmed. For each cell the popup does not cover, it reads the cell
/// from the layer below and blends its colours toward <see cref="_tint"/> by <see cref="_factor"/> (0 = fully
/// see-through, 1 = a solid tint fill — the classic opaque modal), emitting it with <em>no</em> mouse listener so
/// outside clicks are still swallowed (modality preserved). The popup is composited on top with its own listeners,
/// so it stays interactive.
/// </summary>
/// <remarks>
/// This is how a terminal fakes alpha: there is no transparent colour to emit (ANSI SGR has no alpha channel), so
/// overlapping layers are flattened to opaque colours here. 
/// <see cref="ConsoleGUI.Data.Color.Mix"/> is the per-channel linear blend.
/// </remarks>
internal sealed class DimScrim : ConsoleGUI.Common.Control, IDrawingContextListener
{
    #region Constructors
    public DimScrim(IControl below, IControl popup, Color tint, float factor)
    {
        _below = below;
        _tint = tint;
        _factor = factor;
        _popupContext = new DrawingContext(this, popup);
    }
    #endregion

    #region Indexers
    public override Cell this[Position position]
    {
        get
        {
            // The popup on top wherever it has real content — kept verbatim so it carries its own mouse listener
            // and stays clickable/interactive.
            if (_popupContext.Contains(position))
            {
                var cell = _popupContext[position];
                if (cell.Character != Character.Empty) return cell;
            }

            // Otherwise the layer beneath, dimmed and listener-less (so the scrim swallows the click → modal).
            return Dim(position);
        }
    }
    #endregion

    #region Methods
    private Cell Dim(in Position position)
    {
        // Read the layer below directly (it is positioned at the origin, aligned with this scrim); guard its extent.
        var ch = position.X >= 0 && position.Y >= 0 && position.X < _below.Size.Width && position.Y < _below.Size.Height
            ? _below[position].Character
            : Character.Empty;

        Color? fg = ch.Foreground.HasValue ? ch.Foreground.Value.Mix(_tint, _factor) : null;
        // A cell with no background becomes solid tint (empty areas stay fully obscured, as the old scrim did); a
        // real cell's background blends toward the tint so the content shows through, dimmed. The cursor is dropped
        // (the focused popup owns it now), and no listener is attached.
        Color bg = _tint;
        if (ch.Background.HasValue) bg = ch.Background.Value.Mix(_tint, _factor);
        return new Cell(new Character(ch.Content, fg, bg, ch.Decoration));
    }

    protected override void Initialize()
    {
        using (Freeze())
        {
            _popupContext.SetLimits(MinSize, MaxSize);
            Resize(_popupContext.Size);   // fill the available area (the popup layer centres the popup within it)
        }
    }

    void IDrawingContextListener.OnRedraw(DrawingContext drawingContext) => Initialize();

    void IDrawingContextListener.OnUpdate(DrawingContext drawingContext, Rect rect) => Update(rect);
    #endregion

    #region Fields
    private readonly IControl _below;
    private readonly DrawingContext _popupContext;
    private readonly Color _tint;
    private readonly float _factor;
    #endregion
}
