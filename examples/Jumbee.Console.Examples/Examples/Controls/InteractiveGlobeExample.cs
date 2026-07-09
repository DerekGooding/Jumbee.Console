namespace Jumbee.Console.Examples;

using Jumbee.Console;

/// <summary>
/// A hand-driven ASCII Earth: the <see cref="Globe"/> control with <see cref="Globe.Interactive"/> enabled and no
/// auto-spin. <b>Drag</b> to rotate and tilt, the <b>mouse wheel</b> to zoom, and (while focused) the <b>arrow keys</b>
/// to spin/tilt with <b>+/-</b> to zoom — Shift for larger steps. Demonstrates opting a display-only control into
/// mouse + keyboard input (<c>WantsMouse</c> + <c>CaptureMouse</c> drag + <c>OnInput</c>).
/// </summary>
public sealed class InteractiveGlobeExample : Globe, IExample
{
    public InteractiveGlobeExample()
    {
        Interactive = true;
        RotationAngle = 2.2;    // start on a recognisable face rather than the date line
        CameraBeta = 0.35;
    }

    public bool FillsPane => true;
    public string Category => "Flexibility";
    public string Title => "Interactive Globe";
    public string Description =>
        "A mouse- and keyboard-driven ASCII Earth — drag to rotate, scroll to zoom, arrows to spin/tilt, +/- to zoom.";
}
