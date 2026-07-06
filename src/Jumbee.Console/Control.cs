namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;

/// <summary>
/// Base class for all Jumbee.Console controls.
/// </summary>
public abstract class Control : CControl, IFocusable, IDisposable, IMouseListener, IMouseWheelListener
{
    #region Constructors
    public Control() : base()
    {
        consoleBuffer = new ConsoleBuffer();
        ansiConsole = new AnsiConsoleBuffer(consoleBuffer);
        UI.Paint += OnPaint;
        UI.ThemeChanged += OnThemeChanged;
        OnInitialization += Control_OnInitialization;
        OnFocus += Control_OnFocus;
        OnLostFocus += Control_OnLostFocus;
        CaptureFocusCue();
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
                // Default focus cue: make keyboard focus visible on a control that isn't showing it another way.
                // Drawn here at composite time — no reflow, no re-render — like the composite/dialog background fills.
                if (ShowsDefaultFocusCue) cell = ApplyFocusCue(cell, position);
                // Attach this control as the cell's mouse listener so mouse events inside it are routed here
                // (click-to-focus, hover, click). ConsoleGUI's mouse system hit-tests via the cell's listener.
                return Focusable || WantsMouse ? cell.WithMouseListener(this, position) : cell;
            }
        }
    }

    // True when this control should paint the themed default focus cue: it is focused, doesn't render its own focus
    // indication, and no frame is already showing the cue — either there's no frame, or the frame is borderless (a
    // visible-border frame recolours its border on focus, so we defer to it).
    private bool ShowsDefaultFocusCue =>
        IsFocused && !RendersOwnFocus && (Frame is null || !Frame.ShowsFocusCue);

    // Applies the themed focus cue to a cell, per the captured FocusStyle: Tint fills every unpainted cell, Ring
    // fills only the outer-edge cells, Underline underlines the bottom row. Tint/Ring need a focus colour; Underline
    // needs none. Preserves the cell's mouse listener.
    private Cell ApplyFocusCue(Cell cell, Position position)
    {
        var ch = cell.Character;
        switch (_focusStyle)
        {
            case FocusStyle.Underline:
                if (position.Y == Size.Height - 1)
                    ch = new Character(ch.Content, ch.Foreground, ch.Background,
                        (ch.Decoration ?? Decoration.None) | Decoration.Underline, ch.IsCursor);
                break;
            case FocusStyle.Ring:
                var onEdge = position.X == 0 || position.Y == 0 || position.X == Size.Width - 1 || position.Y == Size.Height - 1;
                if (onEdge && ch.Background is null && _focusTint is { } ring) ch = ch.WithBackground(ring);
                break;
            default:   // Tint
                if (ch.Background is null && _focusTint is { } tint) ch = ch.WithBackground(tint);
                break;
        }
        return new Cell(ch, cell.MouseListener);
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

    public ControlFrame? Frame
    {
        get => field;
        set
        {
            if (ReferenceEquals(field, value)) return;
            // The frame participates in runtime theme switches via its own UI.ThemeChanged subscription; manage
            // it here so the lifecycle follows attachment (and a replaced frame is detached).
            if (field is not null) UI.ThemeChanged -= field.OnThemeChanged;
            field = value;
            if (value is not null) UI.ThemeChanged += value.OnThemeChanged;
        }
    }

    public bool HasFrame => Frame is not null;

    /// <summary>The composite that owns this control as one of its children (set by <see cref="CompositeControl"/>),
    /// or null for a top-level control. Lets focus resolve to the composite — the navigable unit — so clicking or
    /// navigating to a child focuses the composite, which then delegates focus back to the child.</summary>
    internal CompositeControl? Owner { get; set; }

    /// <summary>The outermost navigable focus unit for this control: the topmost owning composite, or itself.</summary>
    internal Control FocusRoot => Owner?.FocusRoot ?? this;

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
                InvalidateInteractive();
            }
        }
    }

    public IFocusable FocusableControl => this.Frame is not null ? this.Frame : this;

    /// <summary>
    /// The focused control within this one — itself (<em>not</em> its <see cref="FocusableControl"/>/frame, so a
    /// frame forwarding input inward doesn't loop back to itself) when focused, otherwise <see langword="null"/>.
    /// A composite (<see cref="CompositeControl"/>) overrides this to return its focused descendant, letting
    /// keyboard input route through the composite to the right child.
    /// </summary>
    public virtual IFocusable? FocusedControl => Focusable && IsFocused ? this : null;

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

    /// <summary>
    /// When <see langword="true"/>, this control indicates keyboard focus in its own way (e.g. a button's fill
    /// change, a tab's underline, an editor's cursor), so the base class does <em>not</em> paint the themed default
    /// focus tint over it. Override and return <see langword="true"/> on controls with their own focus styling; the
    /// default (<see langword="false"/>) gives unstyled focusable controls an automatic, always-visible focus cue.
    /// </summary>
    protected virtual bool RendersOwnFocus => false;

    #region Mouse hooks (override to react; defaults are no-ops)
    protected virtual void OnMouseEnter() {}
    protected virtual void OnMouseLeave() {}
    protected virtual void OnMouseMove(Position position) {}
    protected virtual void OnMousePress(Position position) {}
    protected virtual void OnMouseRelease(Position position) {}
    protected virtual void OnClick(Position position) {}
    protected virtual void OnDoubleClick(Position position) {}

    /// <summary>
    /// Handles a wheel notch over the control (<paramref name="delta"/>: negative up, positive down). Default
    /// scrolls the surrounding <see cref="Frame"/> if there is one; override to consume the wheel directly.
    /// </summary>
    protected virtual void OnMouseWheel(Position position, int delta) => Frame?.Scroll(delta);

    /// <summary>
    /// Grabs the mouse so this control receives all subsequent move/press/release (in its own frame) until
    /// <see cref="ReleaseMouse"/>, even when the pointer leaves its cells — for drags (a splitter divider, a
    /// scrollbar thumb, a slider). Call from <see cref="OnMousePress"/>; pair with <see cref="ReleaseMouse"/> in
    /// <see cref="OnMouseRelease"/>.
    /// </summary>
    protected void CaptureMouse() => ConsoleGUI.ConsoleManager.CaptureMouse(this);

    /// <summary>Releases a capture taken by <see cref="CaptureMouse"/>.</summary>
    protected void ReleaseMouse() => ConsoleGUI.ConsoleManager.ReleaseMouseCapture();
    #endregion

    #region IMouseListener (dispatch sink: ConsoleManager calls these on the UI thread)
    void IMouseListener.OnMouseEnter()
    {
        IsMouseOver = true;
        OnMouseEnter();
        MouseEntered?.Invoke(this, EventArgs.Empty);
        InvalidateInteractive();
    }

    void IMouseListener.OnMouseLeave()
    {
        IsMouseOver = false;
        IsMousePressed = false;
        OnMouseLeave();
        MouseLeft?.Invoke(this, EventArgs.Empty);
        InvalidateInteractive();
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
        InvalidateInteractive();
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
        InvalidateInteractive();
    }
    #endregion

    #region IMouseWheelListener
    void IMouseWheelListener.OnMouseWheel(Position position, int delta)
    {
        OnMouseWheel(position, delta);
        MouseWheeled?.Invoke(this, delta);
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
        UI.ThemeChanged -= OnThemeChanged;
        if (Frame is not null) UI.ThemeChanged -= Frame.OnThemeChanged;
    }

    /// <summary>
    /// Re-captures this control's themed colours/glyphs from the current <see cref="UI.StyleTheme"/>/
    /// <see cref="UI.GlyphTheme"/>. Called by themed controls from their constructor and again on a runtime theme
    /// switch (<see cref="UI.SetTheme"/>). Must read the themes <em>only here</em> (and in the constructor), never
    /// on the render path. The default is a no-op for controls that don't use the theme.
    /// </summary>
    protected virtual void ApplyTheme() {}

    /// <summary>
    /// <see langword="true"/> if <paramref name="property"/> was explicitly set by the caller (a themeable token
    /// setter passed it to <see cref="SetAtomicProperty"/>). A control's <see cref="ApplyTheme"/> guards each
    /// themed field with this so a runtime theme switch re-themes only the properties the caller left at default.
    /// </summary>
    protected bool IsThemeOverridden(string property) => _themeOverrides.IsOverridden(property);

    // Runtime theme switch: re-capture themed fields (clobbering any explicit overrides) and repaint. Glyph-width
    // changes flow through the control's own ApplyTheme (which re-measures and resizes), so a relayout follows.
    private void OnThemeChanged(object? sender, EventArgs e)
    {
        CaptureFocusCue();
        ApplyTheme();
        Invalidate();
    }

    // Capture the themed default focus cue (colour + mode) once, off the render path. Read in the base constructor
    // and on a runtime theme switch, like other themed values.
    private void CaptureFocusCue()
    {
        _focusTint = UI.StyleTheme.Focus.BackgroundColor?.ToConsoleGUIColor();
        _focusStyle = UI.StyleTheme.FocusStyle;
    }
    
    // Move focus here *exclusively*, the same as click-to-focus. Setting IsFocused directly would leave any
    // previously-focused control focused too (only UI.SetFocus clears the others), and Layout input routing then
    // delivers keys to every focused control. Always go through UI.SetFocus so single-focus is preserved.
    public void Focus() => UI.SetFocus(this);

    public void UnFocus() => IsFocused = false;

    /// <summary>
    /// The help shown for this control in the global help dialog (F1), or <see langword="null"/> for no help.
    /// Override to describe the control and its keys. The result is deduplicated across the UI by
    /// <see cref="HelpInfo.Name"/>, so give controls of the same kind the same name. <see cref="OnHelp"/> handlers
    /// can further modify (or create) it.
    /// </summary>
    protected internal virtual HelpInfo? GetHelpInfo() => null;

    /// <summary>The effective help for this control: <see cref="GetHelpInfo"/> with any <see cref="OnHelp"/>
    /// handlers applied (they mutate it in place, and may supply help even when <see cref="GetHelpInfo"/> returned
    /// <see langword="null"/> — a blank entry named after the type is created first). <see langword="null"/> when the
    /// control has no help.</summary>
    public HelpInfo? CompileHelp()
    {
        var info = GetHelpInfo();
        if (OnHelp is { } handlers)
        {
            info ??= new HelpInfo(GetType().Name);
            handlers(info);
        }
        return info;
    }

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
    /// Requests a repaint in response to an <em>interactive-state</em> change (focus gained/lost, mouse
    /// enter/leave/press/release) rather than a content change. Defaults to <see cref="Invalidate"/>, so controls
    /// behave exactly as before. <see cref="RenderableControl"/> overrides this to skip the (expensive) re-render
    /// of its wrapped renderable when that renderable's output does not depend on interactive state.
    /// </summary>
    protected virtual void InvalidateInteractive() => Invalidate();

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
    /// <param name="themeOverride">
    /// When <see langword="true"/>, marks the calling property (captured automatically via
    /// <paramref name="propertyName"/>) as an explicit theme override, so a later runtime theme switch through
    /// <see cref="ApplyTheme"/> leaves it alone. Themeable token setters pass <c>themeOverride: true</c>.
    /// </param>
    /// <param name="propertyName">
    /// The calling member's name, supplied automatically by the compiler (<see cref="CallerMemberNameAttribute"/>).
    /// Inside a property setter this is the property name (e.g. <c>AccentStyle</c>), so it matches the
    /// <c>nameof(AccentStyle)</c> used by <see cref="ApplyTheme"/>. Do not pass it explicitly.
    /// </param>
    /// <returns>The resulting field value (the coerced new value when changed, otherwise the existing one).</returns>
    protected T SetAtomicProperty<T>(ref T field, T value, bool updatesLayout = false, Func<T, T>? validate = null, Action<T, T>? watch = null, bool themeOverride = false, [CallerMemberName] string? propertyName = null)
    {
        if (themeOverride && propertyName is not null) _themeOverrides.Mark(propertyName);
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

        // Width first (an intrinsic height may depend on it, e.g. wrapped text). An intrinsic width — a genuine
        // fixed extent reported by an adornment control (e.g. a vertical TextLabel that is one column wide) — is
        // honored even under a finite parent, ahead of the fill-to-parent default, so the control doesn't balloon
        // to fill the space a docking/layout parent offers.
        var intrinsicWidth = IntrinsicWidth();
        var preferredWidth = Width > 0 ? Width
            : intrinsicWidth > 0 ? intrinsicWidth
            : Size.Width > 0 ? Size.Width
            : maxWidth;
        var width = Math.Clamp(preferredWidth, minWidth, maxWidth);

        // An intrinsic height is likewise authoritative even under a finite parent (a horizontal TextLabel is one
        // row tall and must stay one row when docked, not eat the whole panel). Distinct from MeasureHeight below,
        // which is a content height honored only when the parent is unbounded (for scrolling).
        var intrinsicHeight = IntrinsicHeight();
        // When the parent leaves the height unbounded — a scrolling ControlFrame passes int.MaxValue so the child
        // can grow and be scrolled — size to the control's intrinsic content height (MeasureHeight) instead of
        // filling to the 1000 clamp. That makes the frame's scrollbar and scroll range reflect real content. A
        // finite parent (e.g. a Grid cell) still fills, exactly as before.
        var unbounded = MaxSize.Height >= UnboundedHeight;
        var contentHeight = unbounded ? MeasureHeight(width) : 0;
        var preferredHeight = Height > 0 ? Height
            : intrinsicHeight > 0 ? intrinsicHeight
            : contentHeight > 0 ? contentHeight
            : Size.Height > 0 ? Size.Height
            : maxHeight;

        var height = Math.Clamp(preferredHeight, minHeight, maxHeight);
        return (width, height);
    }

    /// <summary>
    /// The control's intrinsic content height in rows at the given <paramref name="width"/>, or 0 when it has no
    /// intrinsic height and should fill the space its parent gives it (the default). Consulted by
    /// <see cref="CalculateSize"/> only when a parent leaves the height unbounded — i.e. inside a scrolling
    /// <see cref="ControlFrame"/> — so the frame can size the control to its content and show an accurate
    /// scrollbar instead of a tiny thumb over ~1000 empty rows. Override on content controls (lists, editors,
    /// logs). A content change that alters the height must re-lay-out (<see cref="Initialize"/>, not merely
    /// <see cref="Invalidate"/>) so the frame re-measures.
    /// </summary>
    protected virtual int MeasureHeight(int width) => 0;

    /// <summary>
    /// When <see langword="true"/>, a wrapping <see cref="ControlFrame"/> sizes this control to its visible
    /// viewport (a bounded height) instead of the frame's usual unbounded scroll height — so the control fills the
    /// frame and the frame never scrolls it. For controls that manage their own scrolling internally (e.g. a
    /// terminal emulator, which owns its scrollback); ballooning them to the scroll height would oversize them and
    /// push live content out of view. Default <see langword="false"/> (normal frame-scrolling behavior).
    /// </summary>
    protected internal virtual bool FillsFrameViewport => false;

    /// <summary>
    /// An intrinsic, fixed width in cells this control always wants regardless of the space its parent offers, or
    /// 0 (the default) to fill the parent's width. Unlike <see cref="MeasureHeight"/> — a content height honored
    /// only when the parent is unbounded — an intrinsic size is authoritative even under a finite parent. Override
    /// on adornment controls with a genuine fixed extent (e.g. a vertical <see cref="TextLabel"/>, one column
    /// wide) so a docking/layout parent can't stretch them to fill the region.
    /// </summary>
    protected virtual int IntrinsicWidth() => 0;

    /// <summary>The intrinsic, fixed height counterpart of <see cref="IntrinsicWidth"/> (e.g. a horizontal
    /// <see cref="TextLabel"/> is one row tall). Returns 0 to fill the parent's height (the default).</summary>
    protected virtual int IntrinsicHeight() => 0;

    // A height limit this large only comes from a scrolling ControlFrame (which passes int.MaxValue); any real
    // viewport is far smaller, so it cleanly distinguishes "unbounded for scrolling" from a finite parent.
    private const int UnboundedHeight = 100_000;

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
            // Fractional ms: most controls repaint in well under 1 ms; ElapsedMilliseconds would truncate them to 0.
            UI.controlPaintTimes[this][UI.paintTimeIndex] = timer.Elapsed.TotalMilliseconds;
            // Report this control's own area as damaged. The ConsoleGUI base Control.Update bubbles the rect up the
            // DrawingContext tree, translating it to screen coordinates, so the next frame's flush re-composites only
            // this control's region instead of the whole screen. MarkDirty then ensures that flush is scheduled.
            if (Size.Width > 0 && Size.Height > 0) Update(ConsoleGUI.Space.Rect.OfSize(Size));
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

    /// <summary>Raised while the global help dialog is compiled, letting code add or modify this control's help
    /// without subclassing. The handler receives a mutable <see cref="HelpInfo"/> (created if
    /// <see cref="GetHelpInfo"/> returned <see langword="null"/>) and edits it in place.</summary>
    public event Action<HelpInfo>? OnHelp;

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
    /// <summary>Raised on a wheel notch over the control (negative up, positive down).</summary>
    public event EventHandler<int>? MouseWheeled;
    #endregion

    #region Fields
    public static Character emptyChar = new Character();
    protected static readonly Cell emptyCell = new Cell(Character.Empty);    
    protected internal uint paintRequests;
    protected readonly ConsoleBuffer consoleBuffer;
    protected readonly AnsiConsoleBuffer ansiConsole;
    // The themed default focus cue (IStyleTheme.Focus background + IStyleTheme.FocusStyle mode), captured off the
    // render path; applied to a focused control that shows focus no other way. Tint null = theme sets no focus bg.
    private ConsoleGUI.Data.Color? _focusTint;
    private FocusStyle _focusStyle;
    // Tracks which theme-token properties the caller explicitly set, so ApplyTheme re-themes only the rest.
    private readonly ThemeOverrides _themeOverrides = new();

    /// <summary>Maximum gap (ms) between two clicks for them to register as a double-click.</summary>
    protected const long DoubleClickMs = 400;
    private long _lastClickMs;
    private Position _lastClickPos;
    #endregion

    #region Types
    public delegate void InitializationHandler();
    #endregion
}
