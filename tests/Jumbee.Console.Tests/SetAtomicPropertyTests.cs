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
        public int Custom { get => _custom; set => SetAtomicProperty(ref _custom, value, watch: (_, _) => OnChangedRuns++); }

        public int WatchOld = -1;
        public int WatchNew = -1;
        public int WatchRuns;

        // Clamps to >= 0 (validate) and records the (old, new) pair (watch).
        private int _validated;
        public int Validated
        {
            get => _validated;
            set => SetAtomicProperty(ref _validated, value,
                validate: v => v < 0 ? 0 : v,
                watch: (oldValue, newValue) => { WatchRuns++; WatchOld = oldValue; WatchNew = newValue; });
        }

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

    [Fact]
    public void SetAtomicProperty_Validate_CoercesBeforeStoreAndEqualityCheck()
    {
        var c = new TestControl();
        c.Validated = 5;                 // baseline (so the coerced 0 is an actual change)

        c.Validated = -5;                // coerced to 0

        Assert.Equal(0, c.Validated);
        Assert.Equal(2, c.InvalidateCount);
        Assert.Equal(2, c.WatchRuns);
        Assert.Equal(0, c.WatchNew);     // watch sees the coerced value, not the raw -5
    }

    [Fact]
    public void SetAtomicProperty_Validate_NoOpAfterCoercionSkipsWatchAndInvalidate()
    {
        var c = new TestControl();
        c.Validated = 5;                 // baseline
        c.Validated = -3;                // coerced to 0, an actual change
        var runs = c.WatchRuns;
        var invalidations = c.InvalidateCount;

        c.Validated = -9;                // also coerces to 0 == current -> no-op

        Assert.Equal(0, c.Validated);
        Assert.Equal(runs, c.WatchRuns);
        Assert.Equal(invalidations, c.InvalidateCount);
    }

    [Fact]
    public void SetAtomicProperty_Watch_ReceivesOldAndNewValues()
    {
        var c = new TestControl();
        c.Validated = 3;
        Assert.Equal(0, c.WatchOld);   // old was default 0
        Assert.Equal(3, c.WatchNew);

        c.Validated = 7;
        Assert.Equal(3, c.WatchOld);   // old is the previous value
        Assert.Equal(7, c.WatchNew);
        Assert.Equal(2, c.WatchRuns);
    }
}
