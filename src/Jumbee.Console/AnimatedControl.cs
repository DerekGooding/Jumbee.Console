namespace Jumbee.Console;

using System;

public abstract class AnimatedControl : Control
{
    #region Constructors
    public AnimatedControl() : base() 
    {
        Invalidate();
    }
    #endregion
   
    #region Methods
    public void Start()
    {
        if (isRunning) return;
        isRunning = true;
        lastUpdate = DateTime.Now.Ticks;
        accumulated = 0L;
    }

    public void Stop()
    {
        if (!isRunning) return;
        isRunning = false;
    }

    public override void Dispose()
    {
        Stop();
        base.Dispose();        
    }
        
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
    protected override void Validate() {}
    #endregion

    #region Fields
    protected int frameCount = 0;
    protected int frameIndex = 0;    
    protected long lastUpdate;
    protected long accumulated;
    protected long interval;
    protected bool isRunning = false;
    #endregion
}
