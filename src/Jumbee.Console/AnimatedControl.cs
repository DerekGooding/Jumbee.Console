namespace Jumbee.Console;
/// <summary>Base class for a control that advances through frames on a timer, repainting each frame while running.</summary>
public abstract class AnimatedControl : Control
{
    #region Constructors

    /// <summary>Initializes a new <see cref="AnimatedControl"/> and requests an initial paint.</summary>
    protected AnimatedControl() => Invalidate();

    #endregion Constructors

    #region Methods

    /// <summary>Starts the animation, resetting the frame timer.</summary>
    public void Start()
    {
        if (isRunning) return;
        isRunning = true;
        lastUpdate = DateTime.Now.Ticks;
        accumulated = 0L;
    }

    /// <summary>Stops the animation, freezing the current frame.</summary>
    public void Stop()
    {
        if (!isRunning) return;
        isRunning = false;
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        Stop();
        base.Dispose();
    }

    /// <summary>Advances the frame index once the interval elapses and renders the current frame.</summary>
    protected override void Paint()
    {
        if (!isRunning) return;
        var now = DateTime.Now.Ticks;
        var delta = now - lastUpdate;
        lastUpdate = now;
        accumulated += delta;
        if (accumulated >= interval)
        {
            accumulated = 0L;
            frameIndex = (frameIndex + 1) % frameCount;
        }
        // Render the current frame on every paint (not only when it advances) so the very first paint shows frame 0;
        // otherwise the control stays blank until a full interval elapses, which leaves it empty in a single static
        // frame and gives it no content/size to lay out inside a container.
        Render();
    }

    // Control should always repaint itself
    /// <summary>No-op so the control always repaints itself each frame.</summary>
    protected override void Validate()
    { }

    #endregion Methods

    #region Fields

    /// <summary>Total number of frames in the animation cycle.</summary>
    protected int frameCount = 0;

    /// <summary>Index of the currently displayed frame.</summary>
    protected int frameIndex = 0;

    /// <summary>Tick count of the previous paint, used to measure elapsed time.</summary>
    protected long lastUpdate;

    /// <summary>Ticks accumulated toward the next frame advance.</summary>
    protected long accumulated;

    /// <summary>Ticks between frame advances.</summary>
    protected long interval;

    /// <summary>Whether the animation is currently running.</summary>
    protected bool isRunning = false;

    #endregion Fields
}