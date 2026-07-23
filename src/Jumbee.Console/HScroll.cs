
using System;

namespace Jumbee.Console;
/// <summary>
/// An opt-in horizontal-scroll offset for controls that render a fixed-width buffer wider than their viewport and
/// window it in <c>Blit</c> (the <see cref="ControlFrame"/> only scrolls vertically).
/// </summary>
/// <remarks>
/// Hold one as a mutable field; call <see cref="Clamp"/> when blitting and <see cref="Pan"/> from left/right key
/// handling. The content and viewport widths are passed per call rather than stored, since they change with
/// layout/resize.
/// </remarks>
/// <example>
/// <code>
/// private HScroll _hscroll;
/// // in Blit:   var left = _hscroll.Clamp(content.Width, viewportWidth); ... src[x + left, y]
/// // in OnInput: if (_hscroll.Pan(±step, content.Width, viewportWidth)) Invalidate();
/// // on new content / Home: _hscroll.Reset();
/// </code>
/// </example>
public struct HScroll
{
    private int _left;

    /// <summary>The current offset — the leftmost visible content column.</summary>
    public readonly int Offset => _left;

    /// <summary>The largest valid offset for the given widths (0 when the content fits).</summary>
    public static int Max(int contentWidth, int viewportWidth) => Math.Max(0, contentWidth - viewportWidth);

    /// <summary>Pans by <paramref name="delta"/> columns, clamped to the content/viewport. Returns whether it moved.</summary>
    public bool Pan(int delta, int contentWidth, int viewportWidth) => SetOffset(_left + delta, contentWidth, viewportWidth);

    /// <summary>Sets the offset, clamped to <c>[0, Max]</c>. Returns whether it changed.</summary>
    public bool SetOffset(int value, int contentWidth, int viewportWidth)
    {
        var clamped = Math.Clamp(value, 0, Max(contentWidth, viewportWidth));
        if (clamped == _left) return false;
        _left = clamped;
        return true;
    }

    /// <summary>Re-clamps to the current widths (e.g. a resize widened the viewport) and returns the offset. Call in Blit.</summary>
    public int Clamp(int contentWidth, int viewportWidth)
    {
        _left = Math.Clamp(_left, 0, Max(contentWidth, viewportWidth));
        return _left;
    }

    /// <summary>Scrolls back to the left edge (new content, Home).</summary>
    public void Reset() => _left = 0;
}