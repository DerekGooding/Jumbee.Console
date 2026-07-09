namespace Jumbee.Console.Tests;

using System.Threading.Tasks;

using ConsoleGUI;
using ConsoleGUI.Data;
using ConsoleGUI.Space;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;
using Xunit.Abstractions;

using CColor = ConsoleGUI.Data.Color;

/// <summary>
/// The opt-in partial-redraw path (<see cref="Control.TracksDamage"/> + <c>Damage()</c>): a control that changes
/// only part of its area reports just that sub-rect, so the compositor scans a fraction of the cells instead of the
/// whole control. Drives the real <see cref="ConsoleManager"/> via <see cref="AnsiConsoleSession"/> and reads
/// <see cref="ConsoleManager.LastFrameDirtyCells"/> — the cell count the last flush actually scanned.
/// </summary>
public class DamageTrackingTests
{
    private readonly ITestOutputHelper _out;
    public DamageTrackingTests(ITestOutputHelper output)
    {
        _out = output;
        UiTestHarness.EnsureStopped();
    }

    [Fact]
    public async Task OptIn_MovingSprite_RecompositesOnlyTheSpriteCells()
    {
        const int w = 60, h = 20;
        var sprite = new MovingSprite { Track = true };
        using var session = await AnsiConsoleSession.StartAsync(sprite, w, h);

        Assert.Equal((long)w * h, ConsoleManager.LastFrameDirtyCells);   // first frame is a full redraw

        sprite.Advance();
        await session.FrameAsync();

        // Only the vacated + new sprite cell should be scanned — a handful, not the whole 1200-cell surface.
        var dirty = ConsoleManager.LastFrameDirtyCells;
        _out.WriteLine($"opt-in dirty cells: {dirty} of {(long)w * h}");
        Assert.True(dirty > 0 && dirty <= 4, $"expected a tiny partial redraw, got {dirty}");

        // ...and the move actually landed on screen (the diff still emitted the moved glyph).
        Assert.Contains("@", ConsoleSnapshot.ToText(session.Screen.Buffer));
    }

    [Fact]
    public async Task Default_MovingSprite_RecompositesTheWholeControl()
    {
        const int w = 60, h = 20;
        var sprite = new MovingSprite { Track = false };   // default full-rect behaviour
        using var session = await AnsiConsoleSession.StartAsync(sprite, w, h);

        sprite.Advance();
        await session.FrameAsync();

        // Without opt-in the control reports its whole rect, so the flush scans every cell.
        Assert.Equal((long)w * h, ConsoleManager.LastFrameDirtyCells);
    }

    [Fact]
    public async Task Globe_OptIn_SkipsTheBlankMargins()
    {
        // A pane wider than 2:1 leaves blank margins around the (height-limited) disc; opt-in skips them.
        const int w = 120, h = 40;

        var tracked = new Globe { DamageTracking = true };
        using (var session = await AnsiConsoleSession.StartAsync(tracked, w, h))
        {
            tracked.Spin(0.03);
            await session.FrameAsync();
            var dirty = ConsoleManager.LastFrameDirtyCells;
            _out.WriteLine($"globe opt-in dirty cells: {dirty} of {(long)w * h}");
            Assert.True(dirty < (long)w * h, "opt-in globe should skip the blank margins");
        }

        var full = new Globe { DamageTracking = false };
        using (var session = await AnsiConsoleSession.StartAsync(full, w, h))
        {
            full.Spin(0.03);
            await session.FrameAsync();
            Assert.Equal((long)w * h, ConsoleManager.LastFrameDirtyCells);   // reports the whole pane
        }
    }

    [Fact]
    public async Task Globe_OptIn_StillRendersIdenticallyToDefault()
    {
        // The optimization must not change pixels: same spin, same output whether tracking damage or not.
        const int w = 80, h = 30;

        var tracked = new Globe { DamageTracking = true };
        using var s1 = await AnsiConsoleSession.StartAsync(tracked, w, h);
        tracked.Spin(0.1); await s1.FrameAsync();
        tracked.Spin(0.1); await s1.FrameAsync();
        var trackedText = ConsoleSnapshot.ToText(s1.Screen.Buffer);

        var full = new Globe { DamageTracking = false };
        using var s2 = await AnsiConsoleSession.StartAsync(full, w, h);
        full.Spin(0.1); await s2.FrameAsync();
        full.Spin(0.1); await s2.FrameAsync();
        var fullText = ConsoleSnapshot.ToText(s2.Screen.Buffer);

        Assert.Equal(fullText, trackedText);
    }

    // A screen-filling control with a static background and a one-cell "sprite" that steps right each frame. When
    // Track is on it reports only the vacated + new sprite cell; when off it uses the default full-rect path.
    private sealed class MovingSprite : Control
    {
        public bool Track = true;
        protected override bool TracksDamage => Track;

        public void Advance() { _x++; Invalidate(); }

        protected override void Render()
        {
            int w = Size.Width, h = Size.Height;
            if (w <= 0 || h <= 0) return;

            consoleBuffer.Initialize();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    consoleBuffer.Write(new Position(x, y), new Character('.', new CColor(40, 40, 40)));

            int sx = _x % w, sy = h / 2;
            consoleBuffer.Write(new Position(sx, sy), new Character('@', new CColor(230, 230, 90)));

            Damage(new Rect(sx, sy, 1, 1));
            if (_prevX >= 0) Damage(new Rect(_prevX, sy, 1, 1));
            _prevX = sx;
        }

        private int _x;
        private int _prevX = -1;
    }
}
