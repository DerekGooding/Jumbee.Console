using Spectre.Console;
using Spectre.Console.Rendering;
using System.Globalization;

namespace Jumbee.Console;

public partial class BarChart
{
    /// <summary>A single labelled, coloured item in a <see cref="BarChart"/>.</summary>
    public class BarChartItem
    {
        /// <summary>Initializes a new <see cref="BarChartItem"/> owned by <paramref name="chart"/> with the given index, label, value and colour.</summary>
        public BarChartItem(BarChart chart, int index, string label, double value, Color color)
        {
            Index = index;
            Label = label;
            Value = value;
            Color = color;
            Chart = chart;
        }

        /// <summary>The item's stable index within its chart.</summary>
        public readonly int Index;

        /// <summary>The chart that owns this item, or <see langword="null"/> once detached.</summary>
        public BarChart? Chart { get; private set; }

        /// <summary>
        /// Gets the item label.
        /// </summary>
        public string Label
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the item value.
        /// </summary>
        public double Value
        {
            get;
            set
            {
                field = value;
                Chart?.UpdateItemValue(Index, value);
            }
        }

        /// <summary>
        /// Gets the item color.
        /// </summary>
        public Color Color
        {
            get;
            set
            {
                field = value;
                Chart?.UpdateItemColor(Index, value);
            }
        }

        /// <summary>Detaches this item from its chart so further changes no longer update it.</summary>
        public void Detach() => Chart = null;

        /// <summary>Whether this item has been detached from its chart.</summary>
        public bool IsDetached => Chart is null;

        /// <summary>Requests a redraw of the owning chart.</summary>
        public void UpdateChart() => Chart?.Update();
    }

    /// <summary>A single bar renderable within a <see cref="BarChart"/>.</summary>
    protected interface IBarControl : IRenderable
    {
        /// <summary>The bar's value.</summary>
        double Value { get; set; }

        /// <summary>The value corresponding to a full bar.</summary>
        double MaxValue { get; set; }

        /// <summary>The bar's colour.</summary>
        Color Color { get; set; }
    }

    /// <summary>A vertically-drawn bar in a <see cref="BarChart"/>.</summary>
    protected class VerticalBar : Renderable, IBarControl
    {
        /// <summary>The bar's value.</summary>
        public double Value { get; set; }

        /// <summary>The value corresponding to a full-height bar.</summary>
        public double MaxValue { get; set; }

        /// <summary>The bar's height in rows.</summary>
        public int Height { get; set; }

        /// <summary>The bar's colour.</summary>
        public Color Color { get; set; }

        /// <summary>The glyph used to draw the bar in Unicode mode.</summary>
        public char UnicodeBar { get; set; } = '█';

        /// <summary>The glyph used to draw the bar in ASCII mode.</summary>
        public char AsciiBar { get; set; } = '|';

        /// <inheritdoc/>
        protected override Measurement Measure(RenderOptions options, int maxWidth) => new(1, maxWidth);

        /// <inheritdoc/>
        protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            var barChar = !options.Unicode ? AsciiBar : UnicodeBar;
            var ratio = MaxValue > 0 ? Math.Clamp(Value / MaxValue, 0, 1) : 0;
            var barHeight = (int)Math.Round(ratio * Height);
            var emptyHeight = Height - barHeight;

            var style = new Spectre.Console.Style(foreground: Color);

            for (var i = 0; i < emptyHeight; i++)
            {
                yield return new Segment(new string(' ', 1) + "\n"); // Or just " \n"
                                                                     // Actually Segment usually doesn't contain newline for Grid cells?
                                                                     // Grid handles newlines. If we return multiple segments, they are just concatenated?
                                                                     // No, IRenderable.Render usually returns a flow of segments.
                                                                     // In a Grid cell, if we want multiple lines, we must emit newlines.
                                                                     // However, Spectre.Console Grid cells can wrap or handle explicit newlines.
                                                                     // Let's try explicit newlines.
            }

            for (var i = 0; i < barHeight; i++)
            {
                // We render the bar character.
                // We should probably repeat it for width?
                // But VerticalBar is usually 1 char wide or full cell width?
                // Let's assume full cell width. But we don't know the exact width assigned by Grid here easily
                // unless we use maxWidth.
                // For a simple vertical bar, let's use 3 chars wide? Or just 1?
                // Let's use maxWidth to fill the column.
                // But Grid column width is determined by content... circular dependency?
                // No, Grid passes maxWidth.
                // Let's default to a fixed width if maxWidth is huge (which it is in Grid auto-sizing).
                // Let's say 3 chars wide.
                var w = Math.Min(3, maxWidth);
                var text = new string(barChar, w);

                // If it's not the last line, add newline
                // Actually, for the empty lines above, we also need width.
            }

            // Re-thinking render strategy:
            // We want to return a block of text.
            // Empty lines first, then filled lines.

            var w2 = Math.Min(3, maxWidth); // Fixed width of 3 for now

            for (var i = 0; i < emptyHeight; i++)
            {
                yield return new Segment(new string(' ', w2));
                yield return Segment.LineBreak;
            }

            for (var i = 0; i < barHeight; i++)
            {
                yield return new Segment(new string(barChar, w2), style);
                if (i < barHeight - 1) yield return Segment.LineBreak;
            }
        }
    }

    /// <summary>A horizontally-drawn bar in a <see cref="BarChart"/>, optionally showing its value and remaining track.</summary>
    protected sealed class HorizontalBar : Renderable, IBarControl
    {
        /// <summary>The bar's value.</summary>
        public double Value { get; set; }

        /// <summary>The value corresponding to a full-width bar. Defaults to 100.</summary>
        public double MaxValue { get; set; } = 100;

        /// <summary>The bar's width in cells, or <see langword="null"/> to fill the available width.</summary>
        public int? Width { get; set; }

        /// <summary>Whether the remaining (unfilled) portion of the track is drawn. Defaults to <see langword="true"/>.</summary>
        public bool ShowRemaining { get; set; } = true;

        /// <summary>The glyph used to draw the bar in Unicode mode.</summary>
        public char UnicodeBar { get; set; } = '━';

        /// <summary>The glyph used to draw the bar in ASCII mode.</summary>
        public char AsciiBar { get; set; } = '-';

        /// <summary>Whether the formatted value is shown after the bar.</summary>
        public bool ShowValue { get; set; }

        /// <summary>The culture used to format the value, or <see langword="null"/> for the invariant culture.</summary>
        public CultureInfo? Culture { get; set; }

        /// <summary>An optional custom formatter for the value.</summary>
        public Func<double, CultureInfo, string>? ValueFormatter { get; set; }

        /// <summary>The bar's colour (sets both the completed and finished styles).</summary>
        public Color Color
        { get => CompletedStyle; set { CompletedStyle = value; FinishedStyle = value; } }

        /// <summary>The style of the filled portion while the bar is incomplete. Defaults to yellow.</summary>
        public Style CompletedStyle { get; set; } = Color.Yellow;

        /// <summary>The style of the filled portion once the bar is complete. Defaults to green.</summary>
        public Style FinishedStyle { get; set; } = Color.Green;

        /// <summary>The style of the remaining (unfilled) portion. Defaults to grey.</summary>
        public Style RemainingStyle { get; set; } = Color.Grey;

        /// <inheritdoc/>
        protected override Measurement Measure(RenderOptions options, int maxWidth)
        {
            var width = Math.Min(Width ?? maxWidth, maxWidth);
            return new Measurement(4, width);
        }

        /// <inheritdoc/>
        protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            var width = Math.Min(Width ?? maxWidth, maxWidth);
            var completedBarCount = Math.Min(MaxValue, Math.Max(0, Value));
            var isCompleted = completedBarCount >= MaxValue;

            var bar = !options.Unicode ? AsciiBar : UnicodeBar;
            var style = isCompleted ? FinishedStyle : CompletedStyle;
            var barCount = Math.Max(0, (int)(width * (completedBarCount / MaxValue)));

            // Show value?
            var value = ValueFormatter != null ? ValueFormatter(completedBarCount, Culture ?? CultureInfo.InvariantCulture) : completedBarCount.ToString(Culture ?? CultureInfo.InvariantCulture);
            if (ShowValue)
            {
                barCount = barCount - value.Length - 1;
                barCount = Math.Max(0, barCount);
            }

            yield return new Segment(new string(bar, barCount), style);

            if (ShowValue)
            {
                yield return barCount == 0
                    ? new Segment(value, style)
                    : new Segment(" " + value, style);
            }

            // More space available?
            if (barCount < width)
            {
                var diff = width - barCount;
                if (ShowValue)
                {
                    diff = diff - value.Length - 1;
                    if (diff <= 0)
                    {
                        yield break;
                    }
                }

                var legacy = options.ColorSystem is ColorSystem.NoColors or ColorSystem.Legacy;
                var remainingToken = ShowRemaining && !legacy ? bar : ' ';
                yield return new Segment(new string(remainingToken, diff), RemainingStyle);
            }
        }
    }
}