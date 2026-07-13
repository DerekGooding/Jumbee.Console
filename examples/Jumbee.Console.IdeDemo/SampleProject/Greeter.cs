namespace SampleApp;

/// <summary>Builds a friendly greeting for the given application name.</summary>
public sealed class Greeter(string appName)
{
    public string Greet() => $"Hello from {appName}! Edit me, then run `dotnet run` in the terminal below.";
}
