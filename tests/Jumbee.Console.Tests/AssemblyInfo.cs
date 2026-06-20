using Xunit;

// The Jumbee.Console UI layer uses process-wide static state (UI.Paint subscriptions, the control list,
// ConsoleManager, the dirty flag), so tests that build/render controls are not safe to run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
