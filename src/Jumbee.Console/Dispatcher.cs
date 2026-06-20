namespace Jumbee.Console;

using System;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Owns a single UI thread and a serialized work queue. UI state mutation and rendering are intended to
/// run on this thread; other threads marshal work onto it via <see cref="Post"/>, <see cref="Invoke"/>,
/// or <see cref="InvokeAsync(Action)"/>.
/// </summary>
public sealed class Dispatcher
{
    #region Properties
    /// <summary>Gets a value indicating whether the UI loop is running.</summary>
    public bool IsRunning => _running;

    /// <summary>Gets the managed thread id of the UI thread, or -1 when not running.</summary>
    public int ThreadId => _uiThreadId;
    #endregion

    #region Methods
    /// <summary>
    /// Returns <see langword="true"/> when the caller is on the UI thread, or when no UI thread is running
    /// (so headless/inline callers are treated as having access).
    /// </summary>
    public bool CheckAccess() => _uiThreadId == NoThread || Environment.CurrentManagedThreadId == _uiThreadId;

    /// <summary>Throws when the caller is not on the UI thread.</summary>
    public void VerifyAccess()
    {
        if (!CheckAccess())
        {
            throw new InvalidOperationException(
                "This operation must run on the UI thread. Use Dispatcher.Post/Invoke/InvokeAsync to marshal it.");
        }
    }

    /// <summary>
    /// Starts the UI thread. <paramref name="frame"/> runs once per frame after the queue is drained;
    /// the loop also wakes every <paramref name="frameIntervalMs"/> so animations advance without input.
    /// </summary>
    public void Start(Action frame, int frameIntervalMs = 100)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (_running) return;

        _frame = frame;
        _frameIntervalMs = frameIntervalMs;
        _running = true;
        using var ready = new ManualResetEventSlim(false);
        _ready = ready;
        _thread = new Thread(UIFrameLoop) { IsBackground = true, Name = "Jumbee.UI" };
        _thread.Start();
        // Block until the loop has recorded its thread id, so CheckAccess() is correct the moment Start returns.
        ready.Wait();
        _ready = null;
    }

    /// <summary>Signals the UI thread to stop, waits briefly for it to exit, and clears any pending work.</summary>
    public void Stop()
    {
        if (!_running) return;

        _running = false;
        _wake.Set();
        _thread?.Join(1000);
        _thread = null;
        _uiThreadId = NoThread;

        while (_queue.TryDequeue(out _)) { }
    }

    /// <summary>Queues an action to run on the UI thread and wakes the loop.</summary>
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _queue.Enqueue(action);
        _wake.Set();
    }

    /// <summary>
    /// Runs an action on the UI thread, blocking until it completes. Runs inline when already on the UI
    /// thread (or when no UI thread is running). Exceptions are rethrown on the calling thread.
    /// </summary>
    public void Invoke(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (CheckAccess())
        {
            action();
            return;
        }

        using var done = new ManualResetEventSlim(false);
        ExceptionDispatchInfo? error = null;
        Post(() =>
        {
            try { action(); }
            catch (Exception ex) { error = ExceptionDispatchInfo.Capture(ex); }
            finally { done.Set(); }
        });
        done.Wait();
        error?.Throw();
    }

    /// <summary>Runs an action on the UI thread and returns a task that completes when it finishes.</summary>
    public Task InvokeAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (CheckAccess())
        {
            try { action(); return Task.CompletedTask; }
            catch (Exception ex) { return Task.FromException(ex); }
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(() =>
        {
            try { action(); tcs.SetResult(); }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }

    /// <summary>Runs a function on the UI thread and returns a task with its result.</summary>
    public Task<T> InvokeAsync<T>(Func<T> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (CheckAccess())
        {
            try { return Task.FromResult(func()); }
            catch (Exception ex) { return Task.FromException<T>(ex); }
        }

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(() =>
        {
            try { tcs.SetResult(func()); }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }

    /// <summary>
    /// Runs an async delegate on the UI thread and returns a task that completes when the delegate's task
    /// completes (unwrapped), so awaiting it waits for the whole operation and propagates its exceptions.
    /// </summary>
    public Task InvokeAsync(Func<Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (CheckAccess())
        {
            try { return func(); }
            catch (Exception ex) { return Task.FromException(ex); }
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(async () =>
        {
            try { await func(); tcs.SetResult(); }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }

    /// <summary>
    /// Runs an async function on the UI thread and returns a task with its (unwrapped) result.
    /// </summary>
    public Task<T> InvokeAsync<T>(Func<Task<T>> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (CheckAccess())
        {
            try { return func(); }
            catch (Exception ex) { return Task.FromException<T>(ex); }
        }

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(async () =>
        {
            try { tcs.SetResult(await func()); }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }

    private void UIFrameLoop()
    {
        _uiThreadId = Environment.CurrentManagedThreadId;
        // Install a SynchronizationContext so `await` in code running on the UI thread resumes on the UI thread.
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(this));
        _ready?.Set();
        while (_running)
        {
            _wake.WaitOne(_frameIntervalMs);
            ProcessQueue();
            if (!_running) break;

            try { _frame?.Invoke(); }
            catch { /* a frame error must not kill the UI thread */ }
        }
        ProcessQueue();
    }

    private void ProcessQueue()
    {
        while (_queue.TryDequeue(out var action))
        {
            try { action(); }
            catch { /* a posted action error must not kill the UI thread */ }
        }
    }
    #endregion

    #region Fields
    private const int NoThread = -1;
    private readonly ConcurrentQueue<Action> _queue = new();
    private readonly AutoResetEvent _wake = new(false);
    private ManualResetEventSlim? _ready;
    private Thread? _thread;
    private Action? _frame;
    private int _frameIntervalMs = 100;
    private volatile int _uiThreadId = NoThread;
    private volatile bool _running;
    #endregion
}

/// <summary>
/// A <see cref="SynchronizationContext"/> that marshals continuations onto a <see cref="Dispatcher"/>'s UI
/// thread, so <c>await</c> in code running on the UI thread resumes on the UI thread (the WPF/Avalonia model).
/// </summary>
internal sealed class DispatcherSynchronizationContext : SynchronizationContext
{
    private readonly Dispatcher _dispatcher;

    public DispatcherSynchronizationContext(Dispatcher dispatcher) => _dispatcher = dispatcher;

    public override void Post(SendOrPostCallback d, object? state) => _dispatcher.Post(() => d(state));

    public override void Send(SendOrPostCallback d, object? state) => _dispatcher.Invoke(() => d(state));

    public override SynchronizationContext CreateCopy() => new DispatcherSynchronizationContext(_dispatcher);
}
