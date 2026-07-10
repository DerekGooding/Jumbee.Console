namespace Jumbee.Console.Examples;

using System;

/// <summary>
/// An OHLC candlestick chart — each period a candle (thin high/low wick + thick open/close body)
/// drawn with half-cell box glyphs, green for up periods and red for down.
/// </summary>
public sealed class CandlestickExample : Plot, IExample
{
    public CandlestickExample()
    {
        // A deterministic random-walk OHLC series (seeded) so the chart is stable.
        const int n = 40;
        var rng = new Random(7);
        double[] xs = new double[n], opens = new double[n], highs = new double[n], lows = new double[n], closes = new double[n];
        double price = 100;
        for (int i = 0; i < n; i++)
        {
            double open = price;
            double close = open + (rng.NextDouble() - 0.48) * 7;   // slight upward drift
            xs[i] = i;
            opens[i] = open;
            closes[i] = close;
            highs[i] = Math.Max(open, close) + rng.NextDouble() * 3;
            lows[i] = Math.Min(open, close) - rng.NextDouble() * 3;
            price = close;
        }

        AddCandles(xs, opens, highs, lows, closes);
        ConfigureGrid(g => g.IsVisible = true);
    }

    #region IExample
    string IExample.Category => "Controls";
    string IExample.Title => "Candlestick";
    string IExample.Description =>
        "OHLC candlesticks with half-cell wicks and bodies — green up, red down — the classic financial chart.";
    #endregion
}
