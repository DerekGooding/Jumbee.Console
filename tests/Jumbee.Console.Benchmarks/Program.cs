using System.Reflection;

using BenchmarkDotNet.Running;

// Run with:  dotnet run -c Release --project tests/Jumbee.Console.Benchmarks
// Filter with e.g.:  dotnet run -c Release --project tests/Jumbee.Console.Benchmarks -- --filter *Render*
BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run(args);
