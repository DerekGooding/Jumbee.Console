
using ConsoleGUI.Data;
using ConsoleGUI.Space;
using CColor = ConsoleGUI.Data.Color;
using CPlot = ConsolePlot.Plot;

namespace Jumbee.Console;
/// <summary>
/// A <see cref="ConsolePlot.Plot"/> whose <see cref="Render"/> blits the rendered image into a
/// <see cref="ConsoleBuffer"/> instead of writing ANSI to <see cref="System.Console"/>, so the Jumbee
/// <see cref="Plot"/> control can draw it. Used internally by <see cref="Plot"/>.
/// </summary>
internal sealed class PlotImage : CPlot
{
    #region Constructors

    public PlotImage(int width, int height, ConsoleBuffer buffer) : base(width, height) => _buffer = buffer;

    #endregion Constructors

    #region Properties

    /// <summary>Background colour written behind every cell, or <see langword="null"/> for transparent.</summary>
    public CColor? Background { get; set; }

    #endregion Properties

    #region Methods

    /// <summary>
    /// Copies the drawn image into the target buffer. ConsolePlot's image has y = 0 at the bottom, so its rows are
    /// flipped vertically onto the buffer's top-down rows.
    /// </summary>
    public override void Render()
    {
        var image = GetImage();
        for (int row = 0; row < image.Height; row++)
        {
            int y = image.Height - 1 - row;
            for (int x = 0; x < image.Width; x++)
            {
                var pixel = image.buffer[y, x];
                var ch = pixel.Character == '\0' ? ' ' : pixel.Character;
                // A pixel's own background (e.g. an annotation label) wins; otherwise the plot's overall Background.
                var bg = pixel.BackgroundColor ?? Background;
                _buffer.Write(new Position(x, row), new Character(ch, pixel.ForegroundColor, bg));
            }
        }
    }

    #endregion Methods

    #region Fields

    private readonly ConsoleBuffer _buffer;

    #endregion Fields
}