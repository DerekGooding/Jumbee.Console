namespace Jumbee.Console;
/// <summary>
/// Supplies <see cref="TerminalInputEvent"/>s (keys, mouse, paste, focus) to the UI input loop.
/// </summary>
/// <remarks>
/// The default reads the real console; tests (or scripted/headless scenarios) can supply their own to inject events
/// deterministically.
/// </remarks>
public interface IInputSource
{
    /// <summary>
    /// Returns the next available input event without blocking. Returns <see langword="false"/> when none is ready.
    /// </summary>
    bool TryRead(out TerminalInputEvent? evt);
}

/// <summary>
/// The default <see cref="IInputSource"/>, reading keys from <see cref="Console"/> and wrapping them as
/// <see cref="KeyInputEvent"/>s.
/// </summary>
/// <remarks>
/// Returns no event when console input is redirected or unavailable. (Mouse/paste/focus require the raw VT input
/// source — a later step; this keyboard-only source keeps existing behavior.)
/// </remarks>
public sealed class ConsoleInputSource : IInputSource
{
    /// <inheritdoc/>
    public bool TryRead(out TerminalInputEvent? evt)
    {
        try
        {
            if (System.Console.KeyAvailable)
            {
                evt = KeyInputEvent.From(System.Console.ReadKey(intercept: true));
                return true;
            }
        }
        catch (InvalidOperationException)
        {
            // Console input is redirected/unavailable.
        }

        evt = null;
        return false;
    }
}