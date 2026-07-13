namespace SampleApp;

/// <summary>A tiny integer calculator used by the sample project.</summary>
public sealed class Calculator
{
    public int Add(int a, int b) => a + b;

    public int Subtract(int a, int b) => a - b;

    public int Multiply(int a, int b) => a * b;

    public double Divide(int a, int b) =>
        b == 0 ? throw new DivideByZeroException("Cannot divide by zero.") : (double)a / b;
}
