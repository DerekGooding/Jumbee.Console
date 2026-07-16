using System.Reflection;

using BenchmarkDotNet.Running;

// Run with:  dotnet run -c Release --project tests/Jumbee.Console.Benchmarks
// Filter with e.g.:  dotnet run -c Release --project tests/Jumbee.Console.Benchmarks -- --filter *Render*
// Map render/composite split (one mode per process, see MapPanelDiagnostics):
//   dotnet run -c Release --project tests/Jumbee.Console.Benchmarks -- --diag RebuildNoTracking
if (args.Length >= 1 && args[0] == "--diag")
{
    Jumbee.Console.Benchmarks.MapPanelDiagnostics.Diagnose(args.Length > 1 ? args[1] : null);
    return;
}

BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run(args);
