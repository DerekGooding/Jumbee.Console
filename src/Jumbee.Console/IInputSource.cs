namespace Jumbee.Console;

using System;

/// <summary>
/// Supplies key events to the UI input loop. The default reads the real console; tests (or scripted/headless
/// scenarios) can supply their own to inject keys deterministically.
/// </summary>
public interface IInputSource
{
    /// <summary>
    /// Returns the next available key without blocking. Returns <see langword="false"/> when no key is ready.
    /// </summary>
    bool TryReadKey(out ConsoleKeyInfo key);
}

/// <summary>
/// The default <see cref="IInputSource"/>, reading from <see cref="Console"/>. Returns no key when console
/// input is redirected or unavailable.
/// </summary>
public sealed class ConsoleInputSource : IInputSource
{
    /// <inheritdoc/>
    public bool TryReadKey(out ConsoleKeyInfo key)
    {
        try
        {
            if (Console.KeyAvailable)
            {
                key = Console.ReadKey(intercept: true);
                return true;
            }
        }
        catch (InvalidOperationException)
        {
            // Console input is redirected/unavailable.
        }

        key = default;
        return false;
    }
}
