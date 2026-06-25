namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Threading;

using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;

/// <summary>
/// Base class for all Jumbee.Console controls.
/// </summary>
public abstract class Control : CControl, IFocusable, IDisposable, IMouseListener
{
    #region Constructors
    public Control() : base()
    {
        consoleBuffer = new ConsoleBuffer();
        ansiConsole = new AnsiConsoleBuffer(consoleBuffer);
        UI.Paint += OnPaint;
        OnInitialization += Control_OnInitialization;
        OnFocus += Control_OnFocus;
        OnLostFocus += Control_OnLostFocus;
    }

   
    #endregion

    #region Indexers    
    public override Cell this[Position position]
    {
        get
        {            
            if (position.X >= Size.Width || position.Y >= Size.Height)
            {
                return emptyCell;
            }
            else
            {
                var cell = consoleBuffer[position];
                // Attach this control as the cell's mouse listener so mouse events inside it are routed here
                // (click-to-focus, hover, click). ConsoleGUI's mouse system hit-tests via the cell's listener.
                return Focusable || WantsMouse ? cell.WithMouseListener(this, position) : cell;
            }
        }
    }
    #endregion

    #region Properties
    public virtual int Width
    {
        get => field;
        set
        {
            UI.Invoke(() => 
            {                
                field = value;            
                Resize(new Size(value, Height));
            });
        }
    }

    public int ActualWidth => Size.Width;
    
    public virtual int Height
    {
        get => field;
        set
        {
            UI.Invoke(() => 
            {
                field = value;
                Resize(new Size(Width, value));
            });
        }
    }

    public int ActualHeight => Size.Height;

    public bool HasLayout => ActualWidth > 0 && ActualHeight > 0;

    public ControlFrame? Frame { get; set; }

    public bool HasFrame => Frame is not null;

    public bool Focusable { get; set; } = true;

    public bool IsFocused
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                Frame?.IsFocused = value;
                if (value)
                    OnFocus?.Invoke();
                else
                    OnLostFocus?.Invoke();
                // Repaint so RenderCursor runs for both the old and new focus: only the focused control owns the
                // terminal cursor, so the defocused one must clear its IsCursor cell.
                Invalidate();
            }
        }
    }

    public IFocusable FocusableControl => this.Frame is not null ? this.Frame : this;

    public virtual bool HandlesInput { get; } = false;
    
    public void OnInput(UI.InputEventArgs inputEventArgs)
    {
        // Input is dispatched on the UI thread (the input reader posts it there), so no lock is needed.
        if (HandlesInput)
        {
            this.OnInput(inputEventArgs.InputEvent!);
        }
    }

    protected virtual void OnInput(InputEvent inputEvent) {}

    /// <summary><see langword="true"/> while the pointer is over this control (between enter and leave).</summary>
    protected bool IsMouseOver { get; private set; }

    /// <summary><see langword="true"/> while a press started on this control and has not yet been released.</summary>
    protected bool IsMousePressed { get; private set; }

    /// <summary>
    /// When <see langword="true"/>, the control's cells are tagged with a mouse listener even if it is not
    /// <see cref="Focusable"/>, so it still receives hover/click (e.g. a non-focusable clickable Link).
    /// </summary>
    protected virtual bool WantsMouse => false;

    #region Mouse hooks (override to react; defaults are no-ops)
    protected virtual void OnMouseEnter() {}
    protected virtual void OnMouseLeave() {}
    protected virtual void OnMouseMove(Position position) {}
    protected virtual void OnMousePress(Position position) {}
    protected virtual void OnMouseRelease(Position position) {}
    protected virtual void OnClick(Position position) {}
    protected virtual void OnDoubleClick(Position position) {}
    #endregion

    #region IMouseListener (dispatch sink: ConsoleManager calls these on the UI thread)
    void IMouseListener.OnMouseEnter()
    {
        IsMouseOver = true;
        OnMouseEnter();
        MouseEntered?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    void IMouseListener.OnMouseLeave()
    {
        IsMouseOver = false;
        IsMousePressed = false;
        OnMouseLeave();
        MouseLeft?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    void IMouseListener.OnMouseMove(Position position)
    {
        OnMouseMove(position);
        MouseMoved?.Invoke(this, position);
    }

    void IMouseListener.OnMouseDown(Position position)
    {
        IsMousePressed = true;
        if (Focusable) UI.SetFocus(this);
        OnMousePress(position);
        MousePressed?.Invoke(this, position);
        Invalidate();
    }

    void IMouseListener.OnMouseUp(Position position)
    {
        OnMouseRelease(position);
        MouseReleased?.Invoke(this, position);
        if (!IsMousePressed) return;   // released without a matching press on us -> not a click
        IsMousePressed = false;

        var now = Environment.TickCount64;
        bool isDouble = now - _lastClickMs <= DoubleClickMs
            && _lastClickPos.X == position.X && _lastClickPos.Y == position.Y;
        _lastClickMs = isDouble ? 0 : now;   // reset so a triple-click isn't read as a second double
        _lastClickPos = position;

        if (isDouble)
        {
            OnDoubleClick(position);
            DoubleClicked?.Invoke(this, position);
        }
        else
        {
            OnClick(position);
            Clicked?.Invoke(this, position);
        }
        Invalidate();
    }
    #endregion

    /// <summary>
    /// Handles a bracketed-paste payload. Default replays it as character key events so existing text controls
    /// receive it; controls that can insert text in bulk (e.g. <see cref="TextEditor"/>) should override this.
    /// </summary>
    public virtual void OnPaste(string text)
    {
        if (!HandlesInput) return;
        foreach (var c in text)
            OnInput(new InputEvent(new ConsoleKeyInfo(c, (ConsoleKey)0, shift: false, alt: false, control: false)));
    }
    #endregion

    #region Methods
    public virtual void Dispose()
    {
        UI.Paint -= OnPaint;
    }
    
    public void Focus() => IsFocused = true;

    public void UnFocus() => IsFocused = false;

    /// <summary>
    /// Fired when a control's Initialize method is called. This method is always called inside UI.Invoke.
    /// </summary>
    protected virtual void Control_OnInitialization() {}

    protected virtual void Control_OnLostFocus() {}

    protected virtual void Control_OnFocus() {}

    /// <summary>
    /// This method renders the control's content to the console buffer.
    /// </summary>
    /// <remarks>Note that this does not actually draw the control on the console screen. 
    /// </remarks>
    protected abstract void Render();

    protected override void Initialize()
    {
        UI.Invoke((() => 
        {
            var (width, height) = CalculateSize();
            var size = new Size(width, height);
            Resize(size);
            consoleBuffer.Size = Size;
            Invalidate();
            OnInitialization?.Invoke();    
        }));
    }                 
            
    /// <summary>
    /// Invoked in the control's OnPaint event handler.
    /// </summary>
    protected virtual void Paint() => Render();

    /// <summary>
    /// Indicates the control should be repainted on the next UI update tick.
    /// </summary>
    protected virtual void Invalidate()
    {
        Interlocked.Increment(ref paintRequests);
        UI.MarkDirty();
    }

    /// <summary>
    /// Assigns a backing field and requests a redraw, but only when the value actually changes.
    /// Centralizes the (optional coerce) + equality-check + assign + (optional notify) + invalidate pattern
    /// required of visual property setters.
    /// </summary>
    /// <remarks>
    /// Only valid for atomically-assignable fields (a single value or reference store). State that cannot be
    /// written atomically (e.g. collections inside a wrapped control) must use a copy-on-write update instead.
    /// When supplied, <paramref name="validate"/> runs first, so the equality check and stored value use the
    /// coerced value; <paramref name="watch"/> then runs after assignment and before the invalidate/initialize.
    /// </remarks>
    /// <param name="field">The backing field to assign.</param>
    /// <param name="value">The new value.</param>
    /// <param name="updatesLayout">
    /// When <see langword="true"/>, the change affects layout and <see cref="Initialize"/> is called (which
    /// re-lays-out on the UI thread and invalidates). Otherwise <see cref="Invalidate"/> is called.
    /// </param>
    /// <param name="validate">Optional coercion (e.g. clamp) applied before the equality check and assignment.</param>
    /// <param name="watch">Optional change callback receiving the old and new values, run before the invalidate/initialize.</param>
    /// <returns>The resulting field value (the coerced new value when changed, otherwise the existing one).</returns>
    protected T SetAtomicProperty<T>(ref T field, T value, bool updatesLayout = false, Func<T, T>? validate = null, Action<T, T>? watch = null)
    {
        if (validate is not null) value = validate(value);
        if (EqualityComparer<T>.Default.Equals(field, value)) return field;

        var old = field;
        field = value;
        watch?.Invoke(old, value);

        if (updatesLayout)
            Initialize();
        else
            Invalidate();

        return field;
    }

    /// <summary>
    /// Indicates that any pending paint requests have been handled and the control does not need re-painting.
    /// </summary>
    protected virtual void Validate() => Interlocked.Exchange(ref paintRequests, 0u);

    /// <summary>
    /// Calculates the size of the control based on its own dimensions and the maximum and minimum size constraints set by its parent.
    /// </summary>
    /// <returns></returns>
    protected (int, int) CalculateSize()
    {
        // Handle the case when negative or overflow sizes may get passed down by parent containers
        int maxWidth = Math.Clamp(MaxSize.Width, 0 ,1000);
        int maxHeight = Math.Clamp(MaxSize.Height, 0, 1000);
        int minWidth = Math.Clamp(MinSize.Width, 0 ,1000);
        int minHeight = Math.Clamp(MinSize.Height, 0, 1000);

        // Use Width and Height as preferred if set (non-zero), otherwise default to MaxSize.Width and MaxSize.Height set by parents
        var preferredWidth = Width > 0 ? Width : Size.Width > 0 ? Size.Width : maxWidth;
        var preferredHeight = Height > 0 ? Height : Size.Height > 0 ? Size.Height : maxHeight;
        
        var width = Math.Clamp(preferredWidth, minWidth, maxWidth);
        var height = Math.Clamp(preferredHeight, minHeight, maxHeight);
        return (width, height);
    }

    public int ClampWidth(int width) => Math.Clamp(width, 0, Size.Width);

    public int ClampHeight(int height) => Math.Clamp(height, 0, Size.Height);
    /// <summary>
    /// Handles the paint event triggered by the UI timer. If one or more paint requests are pending, it runs the painting process and resets the paint request count.
    /// </summary>
    /// <remarks>
    /// Painting runs on the UI thread (driven by the dispatcher's frame), so no lock is required.
    /// </remarks>
    private void OnPaint(object? sender, UI.PaintEventArgs e)
    {
        if (paintRequests > 0)
        {
            var timer = UI.controlPaintTimers[this];
            timer.Restart();
            Paint();
            Validate();
            timer.Stop();
            UI.controlPaintTimes[this][UI.paintTimeIndex] = timer.ElapsedMilliseconds;            
            UI.MarkDirty();
        }
        else
        {
            UI.controlPaintTimes[this][UI.paintTimeIndex] = null;
        }
    }
    #endregion

    #region Events
    public event InitializationHandler OnInitialization;
    public event FocusableEventHandler? OnFocus;
    public event FocusableEventHandler? OnLostFocus;

    /// <summary>Raised when the pointer enters the control.</summary>
    public event EventHandler? MouseEntered;
    /// <summary>Raised when the pointer leaves the control.</summary>
    public event EventHandler? MouseLeft;
    /// <summary>Raised as the pointer moves within the control (relative position).</summary>
    public event EventHandler<Position>? MouseMoved;
    /// <summary>Raised when a button is pressed over the control (relative position).</summary>
    public event EventHandler<Position>? MousePressed;
    /// <summary>Raised when a button is released over the control (relative position).</summary>
    public event EventHandler<Position>? MouseReleased;
    /// <summary>Raised on a press+release on this control (relative position).</summary>
    public event EventHandler<Position>? Clicked;
    /// <summary>Raised on two clicks within <see cref="DoubleClickMs"/> at the same position.</summary>
    public event EventHandler<Position>? DoubleClicked;
    #endregion

    #region Fields
    public static Character emptyChar = new Character();
    protected static readonly Cell emptyCell = new Cell(Character.Empty);    
    protected internal uint paintRequests;
    protected readonly ConsoleBuffer consoleBuffer;
    protected readonly AnsiConsoleBuffer ansiConsole;

    /// <summary>Maximum gap (ms) between two clicks for them to register as a double-click.</summary>
    protected const long DoubleClickMs = 400;
    private long _lastClickMs;
    private Position _lastClickPos;
    #endregion

    #region Types
    public delegate void InitializationHandler();
    #endregion
}
