namespace SampleApp;

// The bundled sample project the IDE demo opens. Edit these files in the editor pane, then run
// `dotnet build` / `dotnet run` in the terminal pane below.
public static class Program
{
    public static void Main()
    {
        var greeter = new Greeter("Jumbee IDE");
        Console.WriteLine(greeter.Greet());

        var calc = new Calculator();
        Console.WriteLine($"2 + 3 = {calc.Add(2, 3)}");
        Console.WriteLine($"6 * 7 = {calc.Multiply(6, 7)}");
        Console.WriteLine($"10 / 4 = {calc.Divide(10, 4):0.00}");
    }
}
