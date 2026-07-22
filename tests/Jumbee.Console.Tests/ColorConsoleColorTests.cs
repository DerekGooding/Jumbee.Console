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
}
