namespace Jumbee.Console;

using ConsoleGUI.Data;
using ConsoleGUI.Space;

using CPlot = ConsolePlot.Plot;

/// <summary>
/// A <see cref="ConsolePlot.Plot"/> whose <see cref="Render"/> blits the rendered image into a
/// <see cref="ConsoleBuffer"/> instead of writing ANSI to <see cref="System.Console"/>, so the Jumbee
/// <see cref="Plot"/> control can draw it. Used internally by <see cref="Plot"/>.
/// </summary>
internal sealed class PlotImage : CPlot
{
    #region Constructors
    public PlotImage(int width, int height, ConsoleBuffer buffer) : base(width, height) => _buffer = buffer;
    #endregion

    #region Properties
    /// <summary>Background colour written behind every cell, or <see langword="null"/> for transparent.</summary>
    public CColor? Background { get; set; }
    #endregion

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
                _buffer.Write(new Position(x, row), new Character(ch, pixel.ForegroundColor, Background));
            }
        }
    }
    #endregion

    #region Fields
    private readonly ConsoleBuffer _buffer;
    #endregion
}
