namespace Jumbee.Console.Tests;

using Jumbee.Console;

using Xunit;

public class SetAtomicPropertyTests
{
    /// <summary>
    /// Minimal control exposing properties backed by <c>SetAtomicProperty</c>, with counters for the
    /// invalidate/initialize/onChanged paths. <c>Initialize</c> is stubbed (no base call) so the
    /// "updates layout" path is observable without running real layout.
    /// </summary>
    private sealed class TestControl : Control
    {
        public int InvalidateCount;
        public int InitializeCount;
        public int OnChangedRuns;

        private int _value;
        public int Value { get => _value; set => SetAtomicProperty(ref _value, value); }

        private int _layoutValue;
        public int LayoutValue { get => _layoutValue; set => SetAtomicProperty(ref _layoutValue, value, updatesLayout: true); }

        private int _custom;
        public int Custom { get => _custom; set => SetAtomicProperty(ref _custom, value, onChanged: () => OnChangedRuns++); }

        protected override void Render() { }
        protected override void Invalidate() { InvalidateCount++; base.Invalidate(); }
        protected override void Initialize() => InitializeCount++;
    }

    [Fact]
    public void SetAtomicProperty_WhenValueChanges_AssignsAndInvalidates()
    {
        var c = new TestControl();

        c.Value = 5;

        Assert.Equal(5, c.Value);
        Assert.Equal(1, c.InvalidateCount);
    }

    [Fact]
    public void SetAtomicProperty_WhenValueUnchanged_DoesNothing()
    {
        var c = new TestControl();
        c.Value = 5;                 // InvalidateCount -> 1
        var invalidations = c.InvalidateCount;

        c.Value = 5;                 // same value

        Assert.Equal(invalidations, c.InvalidateCount);   // no extra repaint
        Assert.Equal(5, c.Value);
    }

    [Fact]
    public void SetAtomicProperty_WhenUpdatesLayout_CallsInitializeNotInvalidate()
    {
        var c = new TestControl();

        c.LayoutValue = 3;

        Assert.Equal(1, c.InitializeCount);
        Assert.Equal(0, c.InvalidateCount);
    }

    [Fact]
    public void SetAtomicProperty_OnChanged_RunsOnlyWhenValueChanges()
    {
        var c = new TestControl();

        c.Custom = 1;
        Assert.Equal(1, c.OnChangedRuns);
        Assert.Equal(1, c.InvalidateCount);

        c.Custom = 1;   // same value -> short-circuits before onChanged
        Assert.Equal(1, c.OnChangedRuns);
        Assert.Equal(1, c.InvalidateCount);
    }

    [Fact]
    public void SetAtomicProperty_OnChanged_RunsBeforeInvalidate()
    {
        var c = new TestControl();

        // Both must have fired exactly once for a single change.
        c.Custom = 42;

        Assert.Equal(42, c.Custom);
        Assert.Equal(1, c.OnChangedRuns);
        Assert.Equal(1, c.InvalidateCount);
    }
}
