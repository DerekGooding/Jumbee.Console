
using ConsoleGUI;
using ConsoleGUI.Api;
using ConsoleGUI.Data;
using ConsoleGUI.Space;
using System.Text;
using JControl = Jumbee.Console.Control;

namespace Jumbee.Console.Snapshot;
/// <summary>
/// Drives the <em>real</em> <see cref="ConsoleManager"/> ANSI render path headlessly, captures the emitted escape
/// sequences via <see cref="ConsoleManager.AnsiOutput"/>, and parses them back into an <see cref="AnsiScreen"/>.
/// </summary>
/// <remarks>
/// <para>
/// Where <see cref="ConsoleSnapshot"/> composes the logical cell grid through a DrawingContext, this exercises
/// ConsoleManager's actual ANSI encoding, diff, cursor handling, and serialized async output — the path that
/// previously could only be checked against a real terminal.
/// </para>
/// <para>
/// It mutates <see cref="ConsoleManager"/> global state, so a caller must ensure no UI loop is concurrently driving
/// it (the test suite disables parallelization and stops the UI first). The capture sink writes on a thread-pool
/// thread like production; <see cref="ConsoleManager.OutputIdle"/> is awaited so the result is deterministic.
/// </para>
/// </remarks>
public static class AnsiConsoleSnapshot
{
    #region Methods

    /// <summary>Renders <paramref name="content"/> through ConsoleManager's ANSI path and returns the parsed screen.</summary>
    public static async Task<AnsiScreen> RenderAsync(IControl content, int width, int height)
    {
        var capture = new StringBuilder();
        var prevOutput = ConsoleManager.AnsiOutput;
        var prevAnsi = ConsoleManager.AnsiEnabled;
        try
        {
            ConsoleManager.AnsiEnabled = true;
            // Mirror production: write off the UI thread (Task.Run), serialized by ConsoleManager so order is
            // deterministic and OutputIdle lets us await it — faithfully exercising the async output path.
            ConsoleManager.AnsiOutput = acsb =>
            {
                var s = acsb.ToString();
                return Task.Run(() => { lock (capture) capture.Append(s); });
            };
            ConsoleManager.Console = new HeadlessConsole { Size = new Size(width, height) };
            ConsoleManager.Setup();             // size the buffer to the console (resets the diff)
            ConsoleManager.Content = content;   // lay the control out (and emit it, if it renders eagerly)

            UI.PaintFrame();                    // controls that paint into their own buffers (Jumbee) render now
            ConsoleManager.Redraw();            // composite + emit the final frame (a diff; the parser accumulates)
            await ConsoleManager.OutputIdle.ConfigureAwait(false);   // wait for the serialized writes to land
        }
        finally
        {
            // Restore the bits that would affect other code; Console/Content are transient and overwritten by the
            // next render.
            ConsoleManager.AnsiOutput = prevOutput;
            ConsoleManager.AnsiEnabled = prevAnsi;
        }

        var screen = new AnsiScreen(width, height);
        lock (capture) screen.Feed(capture.ToString());
        return screen;
    }

    /// <summary>Renders a Jumbee control (using its frame when present) through the ANSI path.</summary>
    public static Task<AnsiScreen> RenderAsync(JControl control, int width, int height)
        => RenderAsync((IControl)control.FocusableControl, width, height);

    /// <summary>Renders a layout through the ANSI path.</summary>
    public static Task<AnsiScreen> RenderAsync(ILayout layout, int width, int height)
        => RenderAsync(layout.CControl, width, height);

    /// <summary>Renders a control through the ANSI path and returns the parsed screen as text (glyphs only).</summary>
    public static async Task<string> ToTextAsync(JControl control, int width, int height)
        => ConsoleSnapshot.ToText((await RenderAsync(control, width, height)).Buffer);

    /// <summary>Renders a layout through the ANSI path and returns the parsed screen as text (glyphs only).</summary>
    public static async Task<string> ToTextAsync(ILayout layout, int width, int height)
        => ConsoleSnapshot.ToText((await RenderAsync(layout, width, height)).Buffer);

    #endregion Methods
}

/// <summary>A no-op <see cref="IConsole"/> with a settable size, so <see cref="ConsoleManager"/> can run the ANSI
/// path with no real terminal. (The ANSI path emits through <see cref="ConsoleManager.AnsiOutput"/>, not
/// <see cref="IConsole.Write(Position, in Character)"/>, so the write members are unused.)</summary>
internal sealed class HeadlessConsole : IConsole
{
    public Size Size { get; set; }
    public bool KeyAvailable => false;

    public void Initialize()
    { }

    public void OnRefresh()
    { }

    public void Write(Position position, in Character character)
    { }

    public ConsoleKeyInfo ReadKey() => throw new System.NotSupportedException();
}