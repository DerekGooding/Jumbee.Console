namespace Jumbee.Console.Tests;

using System;
using System.Collections.Generic;

using Jumbee.Console;

using Xunit;

/// <summary>
/// <see cref="Color"/> is a value type, so it owes callers value equality. Beyond the obvious, this is what keeps
/// colour property setters cheap: <c>Control.SetAtomicProperty</c> compares via <see cref="EqualityComparer{T}.Default"/>,
/// which without <see cref="IEquatable{T}"/> falls back to a comparer that boxes both operands and compares
/// reflectively — 48 bytes of garbage per set, for every colour property on every control.
/// </summary>
public class ColorEqualityTests
{
    [Fact]
    public void EqualColours_AreEqual_ByEveryRoute()
    {
        var a = new Color(10, 20, 30);
        var b = new Color(10, 20, 30);

        Assert.True(a == b);
        Assert.False(a != b);
        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Theory]
    [InlineData(11, 20, 30)]   // R differs
    [InlineData(10, 21, 30)]   // G differs
    [InlineData(10, 20, 31)]   // B differs
    public void DifferentColours_AreNotEqual(byte r, byte g, byte b)
    {
        var colour = new Color(10, 20, 30);
        var other = new Color(r, g, b);

        Assert.False(colour == other);
        Assert.True(colour != other);
        Assert.False(colour.Equals(other));
    }

    [Fact]
    public void NotEqualToOtherTypesOrNull()
    {
        var colour = new Color(10, 20, 30);

        Assert.False(colour.Equals(null));
        Assert.False(colour.Equals("not a colour"));
    }

    [Fact]
    public void WorksAsADictionaryKey()
    {
        var map = new Dictionary<Color, string> { [new Color(1, 2, 3)] = "first" };
        map[new Color(1, 2, 3)] = "second";   // same key — replaces rather than adds

        Assert.Single(map);
        Assert.Equal("second", map[new Color(1, 2, 3)]);
    }

    // Distinct RGB triples pack into 24 bits, so the hash is perfect — no collisions to degrade a hash lookup.
    [Fact]
    public void HashIsCollisionFreeAcrossChannels()
    {
        var hashes = new HashSet<int>();
        for (var r = 0; r < 256; r += 5)
            for (var g = 0; g < 256; g += 5)
                for (var b = 0; b < 256; b += 5)
                    Assert.True(hashes.Add(new Color((byte)r, (byte)g, (byte)b).GetHashCode()));
    }

    // The regression guard: if IEquatable<Color> is ever dropped, the runtime silently reverts to the boxing
    // comparer and every colour assignment starts allocating again — with nothing else failing.
    [Fact]
    public void ImplementsIEquatable_SoGenericComparisonsDoNotBox()
    {
        Assert.True(typeof(IEquatable<Color>).IsAssignableFrom(typeof(Color)));

        var a = new Color(1, 2, 3);
        var b = new Color(1, 2, 4);
        var comparer = EqualityComparer<Color>.Default;
        for (var i = 0; i < 1000; i++) comparer.Equals(a, b);   // warm up

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++) comparer.Equals(a, b);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }
}
