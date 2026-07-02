namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ConsoleGUI.Data;
using ConsoleGUI.Space;
using Spectre.Console;
using Spectre.Console.Interop;
using Spectre.Console.Rendering;

/// <summary>
/// An implementation of Spectre.Console.IAnsiConsole that writes to a ConsoleBuffer.
/// </summary>
public class AnsiConsoleBuffer : IAnsiConsole, IAnsiConsoleInput, IAnsiConsoleOutput, IExclusivityMode, IDisposable
{
    #region Constructors
    public AnsiConsoleBuffer(ConsoleBuffer console)
    {
        _console = console;
        _cursor = new AnsiConsoleBufferCursor(this);
        _input = this;
        _exclusivityMode = this;
        _pipeline = new RenderPipeline();
        _profile = new Profile(this, Encoding.UTF8);
        _profile.Capabilities.Ansi = AnsiConsole.Profile.Capabilities.Ansi;
        _profile.Capabilities.ColorSystem = AnsiConsole.Profile.Capabilities.ColorSystem;
        _profile.Capabilities.Interactive = AnsiConsole.Profile.Capabilities.Interactive;
        _profile.Capabilities.Unicode = AnsiConsole.Profile.Capabilities.Unicode;
        _cursorX = 0;
        _cursorY = 0;
    }

    #endregion

    #region Properties
    public Profile Profile => _profile;
    public IAnsiConsoleCursor Cursor => _cursor;
    /// <summary>The concrete buffer cursor, exposing Jumbee-specific <see cref="AnsiConsoleBufferCursor.Style"/>/<see cref="AnsiConsoleBufferCursor.Color"/>.</summary>
    internal AnsiConsoleBufferCursor BufferCursor => _cursor;
    public IAnsiConsoleInput Input => _input;
    public IExclusivityMode ExclusivityMode => _exclusivityMode;
    public RenderPipeline Pipeline => _pipeline;
    internal int CursorX => _cursorX;
    internal int CursorY => _cursorY;
    #endregion

    #region Methods
    public void Clear(bool home)
    {
        if (marshal)
        {
            UI.Invoke(() => _Clear(home));
        }
        else
        {
            _Clear(home);
        }
    }

    private void _Clear(bool home)
    {
        bool wasVisible = _cursor.IsVisible;
        _cursor.Forget();
        _console.Initialize();
        if (home)
        {
            _cursorX = 0;
            _cursorY = 0;
        }

        if (wasVisible)
        {
            _cursor.Show(true);
        }
        
    }

    public void Write(IRenderable renderable)
    {
        // Render to segments on the calling thread (which owns/mutates the renderable), then apply them to the
        // buffer. When marshaling, only the apply runs on the UI thread — so the UI thread never enumerates a
        // renderable the producer is concurrently mutating (e.g. a LiveDisplay Table rebuilt each tick).
        // GetSegments already returns a fully-materialized private List (via Segment.Merge), so reuse it directly
        // instead of copying the whole segment set again; the fallback only fires if that ever changes.
        var produced = renderable.GetSegments(this);
        var segments = produced as List<Segment> ?? [.. produced];
        if (marshal)
        {
            UI.Invoke(() => _Write(segments));
        }
        else
        {
            _Write(segments);
        }
    }

    private void _Write(IReadOnlyList<Segment> segments)
    {
        bool wasVisible = _cursor.Hide();

        foreach (var segment in segments)
        {
            if (segment.IsControlCode)
            {
                // Iterate the span, not .Text: segments are often ReadOnlyMemory<char> slices of a source string
                // (Split/Truncate, Paragraph word slices), and reading .Text would materialize a substring here.
                foreach (var c in segment.TextSpan)
                {
                    var position = new Position(_cursorX, _cursorY);
                    if (IsValidPosition(position))
                    {
                        if (c == '\r')
                        {
                            _cursorX = 0;
                            continue;
                        }
                        else if (c == '\n')
                        {
                            _cursorX = 0;
                            _cursorY++;
                            continue;
                        }
                        else
                        {
                            //_console.Write(position, new Character(c, isControl: true));
                            continue;
                        }
                    }
                    
                }
            }
            else
            {
                var style = segment.Style;
                var fg = style.Foreground.ToConsoleGUIColor();
                var bg = style.Background.ToConsoleGUIColor();
                var decoration = (ConsoleGUI.Data.Decoration)style.Decoration;
                foreach (char c in segment.Text)
                {
                    if (c == '\n')
                    {
                        _cursorY++;
                        _cursorX = 0;
                        continue;
                    }
                    else if (c == '\r')
                    {
                        _cursorX = 0;
                        continue;
                    }
                    else
                    {
                        var width = c.GetCellWidth();
                        if (width <= 0) continue; // Skip zero-width chars
                        // Character-level soft wrap: when the glyph won't fit on the current row, drop to the next
                        // row instead of clipping it. Opt-in (used by TextEditor, which disables Spectre's own
                        // word-wrap so this is the single, deterministic wrap the caret math can mirror exactly).
                        if (wrap && _cursorX > 0 && _cursorX + width > _console.Size.Width)
                        {
                            _cursorX = 0;
                            _cursorY++;
                        }
                        var position = new Position(_cursorX, _cursorY);
                        if (IsValidPosition(position))
                        {
                            _console.Write(position, new Character(c, fg, bg, decoration));             
                            _cursorX += width;
                        }
                    }
                    
                }
            }
        }

        if (wasVisible)
        {
            _cursor.Show(true);
        }
        
    }

    public void Dispose()
    {
    }

    public int CursorDistance
    {
        get => _cursorY * _console.Size.Width + _cursorX;
        set => SetCursorPosition(_console.GetPosition(value));
    }
    
    internal void SetCursorPosition(int x, int y)
    {        
        _cursorX = x;
        _cursorY = y;        
    }

    internal void SetCursorPosition(Position position)
    {      
        _cursorX = position.X;
        _cursorY = position.Y;
    }

    internal void MoveCursor(int dx, int dy)
    {       
        _cursorX += dx;
        _cursorY += dy;      
    }

    private bool IsValidPosition(Position position) =>
       position.X >= 0 && position.X < _console.Size.Width && position.Y >= 0  && position.Y < _console.Size.Height;
    
    #region IAnsiConsoleInput implementation 
    bool IAnsiConsoleInput.IsKeyAvailable() => throw new NotSupportedException();

    ConsoleKeyInfo? IAnsiConsoleInput.ReadKey(bool intercept) => throw new NotSupportedException();

    Task<ConsoleKeyInfo?> IAnsiConsoleInput.ReadKeyAsync(bool intercept, CancellationToken cancellationToken) => throw new NotSupportedException();
    #endregion

    #region IAnsiConsoleOutput implementation
    TextWriter IAnsiConsoleOutput.Writer => throw new NotSupportedException();
    
    bool IAnsiConsoleOutput.IsTerminal => true;
    
    int IAnsiConsoleOutput.Width => _console.Size.Width;
    
    int IAnsiConsoleOutput. Height => _console.Size.Height;
    
    void IAnsiConsoleOutput.SetEncoding(Encoding encoding) {}
    #endregion

    #region IExclusivityMode implementation
    T IExclusivityMode.Run<T>(Func<T> func)
    {
        // Try acquiring the exclusivity semaphore
        if (!_semaphore.Wait(0))
        {
            throw new InvalidOperationException("AnsiConsole exclusivity lock is already held.");
        }

        try
        {
            return func();
        }
        finally
        {
            _semaphore.Release(1);
        }
    }

    async Task<T> IExclusivityMode.RunAsync<T>(Func<Task<T>> func)
    {
        // Try acquiring the exclusivity semaphore
        if (!await _semaphore.WaitAsync(0).ConfigureAwait(false))
        {
            throw new InvalidOperationException("AnsiConsole exclusivity lock is already held.");
        }

        try
        {
            return await func().ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release(1);
        }
    }
    #endregion

    #endregion

    #region Fields
    /// <summary>
    /// When <see langword="true"/>, <see cref="Write"/> and <see cref="Clear"/> are marshaled onto the UI thread
    /// via <see cref="UI.Invoke"/> so their buffer mutations are serialized with rendering and resizing. Set this
    /// for controls whose wrapped Spectre widget refreshes from its own thread (e.g. <see cref="SpectreLiveDisplay"/>,
    /// <see cref="SpectreTaskProgress"/>). Defaults to <see langword="false"/> to preserve the original
    /// synchronous IAnsiConsole behavior for existing Spectre.Console controls.
    /// </summary>
    public bool marshal;
    /// <summary>When <see langword="true"/>, <see cref="Write"/> wraps glyphs to the next row at the buffer's
    /// right edge instead of clipping them. See the wrap note in <see cref="_Write"/>.</summary>
    public bool wrap;
    internal readonly ConsoleBuffer _console;
    internal readonly AnsiConsoleBufferCursor _cursor;
    private readonly IAnsiConsoleInput _input;
    private readonly IExclusivityMode _exclusivityMode;
    private readonly RenderPipeline _pipeline;
    private readonly Profile _profile;
    private int _cursorX;
    private int _cursorY;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    #endregion
}

internal class AnsiConsoleBufferCursor : IAnsiConsoleCursor
{
    #region Constructors
    public AnsiConsoleBufferCursor(AnsiConsoleBuffer parent) => _parent = parent;
    #endregion

    #region Properties
    internal bool IsVisible => _isVisible;

    /// <summary>The cursor shape/blink, emitted by the renderer as DECSCUSR. Re-applies live if visible.</summary>
    public CursorStyle Style
    {
        get => _style;
        set
        {
            if (_style != value)
            {
                _style = value;
                if (_isVisible) { HideCursor(); ShowCursor(); }
            }
        }
    }

    /// <summary>Cursor colour (OSC 12), or <see langword="null"/> for the terminal default. Re-applies live if visible.</summary>
    public Color? Color
    {
        get => _color;
        set
        {
            if (!Nullable.Equals(_color, value))
            {
                _color = value;
                if (_isVisible) { HideCursor(); ShowCursor(); }
            }
        }
    }
    #endregion

    #region Methods
    public void Show(bool show)
    {        
        if (show)
        {
            if (_isVisible)
            {
                if (_savedPosition.HasValue && _savedPosition.Value.X == _parent.CursorX && _savedPosition.Value.Y == _parent.CursorY)
                {
                    return;
                }
                else
                {
                    HideCursor();
                }
            }
            ShowCursor();
        }
        else
        {
            if (_isVisible)
            {
                HideCursor();
            }
        }      
    }

    internal bool Hide()
    {       
        if (_isVisible)
        {
            HideCursor();
            return true;
        }
        return false;    
    }

    internal void Forget()
    {
        _isVisible = false;
        _savedPosition = null;
        _savedCell = default;
    }

    // Marks the cell at the cursor position so the renderer (ConsoleManager) positions the terminal's native
    // cursor there. The IsCursor flag plus the encoded Style (in the Decoration high bits) and optional Color
    // (in the cell's Foreground) ride the composited cell up to the renderer. The original cell is saved so
    // HideCursor can restore the glyph's real colour/decoration.
    private void ShowCursor()
    {
        var x = _parent.CursorX;
        var y = _parent.CursorY;

        if (x < 0 || y < 0 || x >= _parent._console.Size.Width || y >= _parent._console.Size.Height)
            return;
        var cell = _parent._console[x, y];
        _savedCell = cell;
        _savedPosition = new Position(x, y);

        var c = cell.Character;
        var decoration = CursorEncoding.WithColorFlag(
            CursorEncoding.EncodeStyle(c.Decoration ?? ConsoleGUI.Data.Decoration.None, (int)_style),
            _color.HasValue);
        var cursorChar = new Character(c.Content, _color ?? c.Foreground, c.Background, decoration, isCursor: true);
        _parent._console.Write(x, y, new Cell(cursorChar, cell.MouseListener));
        _isVisible = true;
    }

    private void HideCursor()
    {
        if (_savedPosition.HasValue)
        {
            var pos = _savedPosition.Value;
            if (pos.X >= 0 && pos.Y >= 0 && pos.X < _parent._console.Size.Width && pos.Y < _parent._console.Size.Height)
            {
                _parent._console.Write(pos, _savedCell);
            }
        }
        _isVisible = false;
        _savedPosition = null;
    }

    public void SetPosition(int column, int line)
    {       
        var wasVisible = Hide();
        _parent.SetCursorPosition(column, line);
        if (wasVisible) Show(true);   
    }

    public void Move(CursorDirection direction, int steps)
    {   
        var wasVisible = Hide();
        switch (direction)
        {
            case CursorDirection.Up:
                _parent.MoveCursor(0, -steps);
                break;
            case CursorDirection.Down:
                _parent.MoveCursor(0, steps);
                break;
            case CursorDirection.Left:
                _parent.MoveCursor(-steps, 0);
                break;
            case CursorDirection.Right:
                _parent.MoveCursor(steps, 0);
                break;
        }
        if (wasVisible) Show(true);    
    }    
    #endregion

    #region Fields
    private readonly AnsiConsoleBuffer _parent;
    private Position? _savedPosition;
    private Cell _savedCell;
    private bool _isVisible;
    private CursorStyle _style = CursorStyle.Default;
    private Color? _color;
    #endregion
}
