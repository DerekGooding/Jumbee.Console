namespace Jumbee.Console;

/// <summary>
/// How the themed default focus cue is drawn on a focused control that isn't showing focus another way (no visible
/// frame border, and <c>Control.RendersOwnFocus</c> is false).
/// </summary>
/// <remarks>The colour comes from <see cref="IStyleTheme.Focus"/>; the mode from
/// <see cref="IStyleTheme.FocusStyle"/>.</remarks>
public enum FocusStyle
{
    /// <summary>Tint the whole control with the focus background — a solid, obvious cue (the default).</summary>
    Tint,

    /// <summary>Tint only the control's outer edge cells with the focus background — a subtler focus ring.</summary>
    Ring,

    /// <summary>Underline the control's bottom row — a minimal cue that leaves the content colours untouched.</summary>
    Underline
}