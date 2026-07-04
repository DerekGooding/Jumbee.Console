namespace Jumbee.Console.Examples;

using System;
using System.Linq;

/// <summary>
/// The <c>--verify</c> smoke check: builds every catalogued example and confirms each declared source file is
/// embedded. Prints a line per example and returns a non-zero exit code on any failure — a CI guard that every
/// example is wired and its source ships, with no test project (and AOT-safe).
/// </summary>
internal static class Verify
{
    public static int Run()
    {
        var failures = 0;
        foreach (var example in ExampleCatalog.All)   // the catalogue constructing them already exercised each ctor
        {
            var missing = example.SourceFiles.Where(f => !SourceLoader.Exists(f)).ToArray();
            if (missing.Length > 0)
            {
                Console.WriteLine($"FAIL  {example.Title}: missing source [{string.Join(", ", missing)}]");
                failures++;
            }
            else
            {
                Console.WriteLine($"PASS  {example.Category} › {example.Title}");
            }
        }

        Console.WriteLine($"\n{ExampleCatalog.All.Count} examples, {failures} failure(s).");
        return failures == 0 ? 0 : 1;
    }
}
