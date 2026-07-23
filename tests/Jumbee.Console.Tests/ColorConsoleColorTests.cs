namespace Jumbee.Console.Tests;

using System;

using Jumbee.Console;

using Xunit;

/// <summary><see cref="Color.ToConsoleColor"/> lets a Jumbee RGB colour reach the few 16-colour APIs (e.g. a plot's
/// axis/title colours). It's the inverse of <see cref="Color.FromSystemConsoleColor"/>, so the 16 canonical console
/// colours must round-trip.</summary>
public class ColorConsoleColorTests
{
    [Theory]
    [InlineData(ConsoleColor.Red)]
    [InlineData(ConsoleColor.Cyan)]
    [InlineData(ConsoleColor.Gray)]
    [InlineData(ConsoleColor.White)]
    [InlineData(ConsoleColor.Black)]
    [InlineData(ConsoleColor.DarkGreen)]
    public void CanonicalConsoleColours_RoundTrip(ConsoleColor c)
    {
        Assert.Equal(c, Color.FromSystemConsoleColor(c).ToConsoleColor());
    }

    // Regression: ToConsoleColor previously delegated to Spectre's ToConsoleColor, which runs a LINQ palette query
    // that allocates on every call — heavy when a render hot path converts a colour per frame (the scope-tui axis
    // colours did exactly this). The reimplementation is a direct nearest-of-16 scan and must not allocate.
    [Fact]
    public void ToConsoleColor_DoesNotAllocate()
    {
        var c = new Color(0x33, 0x99, 0xE0);   // a non-exact RGB (exercises the nearest-match path)
        for (var i = 0; i < 200; i++) _ = c.ToConsoleColor();   // warm up JIT + the one-time static palette build

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 20_000; i++) _ = c.ToConsoleColor();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(allocated == 0, $"ToConsoleColor allocated {allocated} bytes over 20k calls (expected 0)");
    }
}
