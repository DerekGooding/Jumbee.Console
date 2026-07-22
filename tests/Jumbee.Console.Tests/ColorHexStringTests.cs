namespace Jumbee.Console.Tests;

using Jumbee.Console;

using Xunit;

/// <summary><see cref="Color.FromHexString"/>/<see cref="Color.TryFromHexString"/> parse hex colours (with or without
/// a leading '#', full and 3-digit forms), so callers don't have to drop down to Spectre.Console.</summary>
public class ColorHexStringTests
{
    [Theory]
    [InlineData("#4080C0")]
    [InlineData("4080C0")]
    public void FromHexString_ParsesSixDigit_WithOrWithoutHash(string hex)
    {
        Assert.Equal(new Color(0x40, 0x80, 0xC0), Color.FromHexString(hex));
    }

    [Fact]
    public void FromHexString_ExpandsThreeDigitShortForm()
    {
        // "#48C" expands to "#4488CC".
        Assert.Equal(new Color(0x44, 0x88, 0xCC), Color.FromHexString("#48C"));
    }

    [Fact]
    public void TryFromHexString_ReturnsFalse_OnMalformedInput()
    {
        Assert.False(Color.TryFromHexString("nope", out var color));
        Assert.Equal(default, color);

        Assert.True(Color.TryFromHexString("#FF0000", out var red));
        Assert.Equal(new Color(0xFF, 0, 0), red);
    }
}
