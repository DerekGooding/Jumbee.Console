namespace Jumbee.Console.Examples;

using Jumbee.Console;

/// <summary>
/// A hand-driven ASCII Earth: the <see cref="Globe"/> control with <see cref="Globe.Interactive"/> enabled.
/// Demonstrates opting a display-only control into mouse + keyboard input.
/// </summary>
public sealed class InteractiveGlobeExample : Globe, IExample
{
    public InteractiveGlobeExample()
    {
        Interactive = true;
        RotationAngle = 2.2;    // start on a recognisable face rather than the date line
        CameraBeta = 0.35;
    }

    #region IExample
    string IExample.Category => "Flexibility";
    string IExample.Title => "Interactive Globe";
    string IExample.Description =>
        "A mouse- and keyboard-driven ASCII Earth — drag to rotate, scroll to zoom, arrows to spin/tilt, +/- to zoom.";
    #endregion
}
