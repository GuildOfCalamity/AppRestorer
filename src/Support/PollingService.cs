using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace AppRestorer;

/// <summary>
/// - A <c>ManualResetEvent</c> is a specialized subclass of EventWaitHandle that is always manual-reset mode.
///   - Once signaled, <c>Set()</c>, it stays signaled until you explicitly call <c>Reset()</c>.
///   - All waiting threads are released when it’s signaled.
/// </summary>
public class PollingService : IDisposable
{
    Thread? _thread;
    TimeSpan _interval = TimeSpan.FromMinutes(5);
    readonly EventWaitHandle _shutdown = new EventWaitHandle(false, EventResetMode.ManualReset); // false = initially unsignaled
    readonly ConcurrentQueue<Action> _workQueue = new ConcurrentQueue<Action>();

    /// <summary>
    /// The default <see cref="TimeSpan"/> interval will be 5 minutes.
    /// </summary>
    /// <param name="interval">how often to poll for work</param>
    public void Start(TimeSpan interval = default)
    {
        if (interval == default || interval == TimeSpan.Zero || interval == TimeSpan.MinValue || interval == TimeSpan.MaxValue)
            _interval = TimeSpan.FromMinutes(5);
        else
            _interval = interval;

        _thread = new Thread(PollLoop) { IsBackground = true, Priority = ThreadPriority.BelowNormal };
        _thread.Start();
    }

    public void Stop()
    {
        _shutdown?.Set(); // Signal shutdown
        _thread?.Join();
    }

    /// <summary>
    /// Queue work to be executed as soon as possible.
    /// </summary>
    public void AddWork(Action action)
    {
        if (action == null)
            return;

        _workQueue.Enqueue(action);
        //_workSignal.Set(); // Wake the loop early
    }


    void PollLoop()
    {
        DateTime nextRun = DateTime.UtcNow;
        while (!_shutdown.WaitOne(TimeSpan.Zero, false))
        {
            // Calculate remaining time until next run
            TimeSpan remaining = nextRun - DateTime.UtcNow;
            if (remaining < TimeSpan.Zero)
                remaining = TimeSpan.Zero;

            Debug.WriteLine($"[POLLING] Checking for work in {remaining.ToReadableTime()}");

            // Wait for either shutdown signal or the remaining time
            if (_shutdown.WaitOne(remaining, false))
                break; // shutdown was signaled

            // Do the work
            CheckForWork();

            // Schedule next run
            nextRun = DateTime.UtcNow + _interval;
        }
    }

    void CheckForWork()
    {
        Debug.WriteLine($"[POLLING] Checking for work on {DateTime.Now.ToString("ddd MMM dd, yyyy")} at {DateTime.Now.ToString("hh:mm:ss tt")}");

        // Process any queued work
        while (_workQueue.TryDequeue(out var workItem))
        {
            try
            {
                workItem();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[POLLING] Work item failed: {ex}");
            }
        }
    }

    public void Dispose()
    {
        Stop();
        _shutdown?.Dispose();
    }
}

/// <summary>
/// - A <c>ManualResetEvent</c> is a specialized subclass of EventWaitHandle that is always manual-reset mode.
///   - Once signaled, <c>Set()</c>, it stays signaled until you explicitly call <c>Reset()</c>.
///   - All waiting threads are released when it’s signaled.
/// </summary>
public class PollingServiceAsync : IAsyncDisposable
{
    Thread? _thread;
    TimeSpan _interval = TimeSpan.FromMinutes(5);
    readonly EventWaitHandle _shutdown = new EventWaitHandle(false, EventResetMode.ManualReset); // false = initially unsignaled
    readonly ConcurrentQueue<Func<Task>> _workQueue = new ConcurrentQueue<Func<Task>>();


    /// <summary>
    /// The default <see cref="TimeSpan"/> interval will be 5 minutes.
    /// </summary>
    /// <param name="interval">how often to poll for work</param>
    public void Start(TimeSpan interval = default)
    {
        if (interval == default || interval == TimeSpan.Zero || interval == TimeSpan.MinValue || interval == TimeSpan.MaxValue)
            _interval = TimeSpan.FromMinutes(5);
        else
            _interval = interval;

        _thread = new Thread(() => PollLoopAsync().GetAwaiter().GetResult()) { IsBackground = true, Priority = ThreadPriority.BelowNormal };
        _thread.Start();
    }

    public void Stop()
    {
        _shutdown?.Set(); // Signal shutdown
        _thread?.Join();
    }

    /// <summary>
    /// Queue work to be executed as soon as possible.
    /// </summary>
    //public void AddWork(Action action)
    //{
    //    if (action == null) 
    //        throw new ArgumentNullException(nameof(action));
    //    _workQueue.Enqueue(action);
    //    //_workSignal.Set(); // Wake the loop early
    //}

    public void AddWork(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        _workQueue.Enqueue(() => { action(); return Task.CompletedTask; });
        //_workSignal.Set(); // Wake the loop early
    }

    public void AddWork(Func<Task> asyncAction)
    {
        if (asyncAction == null) throw new ArgumentNullException(nameof(asyncAction));
        _workQueue.Enqueue(asyncAction);
        //_workSignal.Set(); // Wake the loop early
    }

    async Task PollLoopAsync()
    {
        DateTime nextRun = DateTime.UtcNow;
        while (!_shutdown.WaitOne(TimeSpan.Zero, false))
        {
            // Calculate remaining time until next run
            TimeSpan remaining = nextRun - DateTime.UtcNow;
            if (remaining < TimeSpan.Zero)
                remaining = TimeSpan.Zero;

            Debug.WriteLine($"[POLLING] Checking for work in {remaining.ToReadableTime()}");

            // Wait for either shutdown signal or the remaining time
            if (_shutdown.WaitOne(remaining, false))
                break; // shutdown was signaled

            // Process any queued work
            while (_workQueue.TryDequeue(out var workItem))
            {
                try
                {
                    await workItem();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[POLLING] Work item failed: {ex}");
                }
            }

            // Schedule next run
            nextRun = DateTime.UtcNow + _interval;
        }

        if (_workQueue.Count > 0)
        {
            Debug.WriteLine($"[WARNING] The work queue still contains pending items.");
        }
    }

    void CheckForWork()
    {
        Debug.WriteLine($"[POLLING] Checking for work on {DateTime.Now.ToString("ddd MMM dd, yyyy")} at {DateTime.Now.ToString("hh:mm:ss tt")}");

        // Process any queued work
        while (_workQueue.TryDequeue(out var workItem))
        {
            try
            {
                workItem();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[POLLING] Work item failed: {ex}");
            }
        }
    }

    public void Dispose()
    {
        Stop();
        _shutdown?.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Stop();
        _shutdown.Dispose();
        return ValueTask.CompletedTask;
    }
}


public enum WorkPriority
{
    High,
    Normal
}

/// <summary>
/// - A <c>ManualResetEvent</c> is a specialized subclass of EventWaitHandle that is always manual-reset mode.
///   - Once signaled, <c>Set()</c>, it stays signaled until you explicitly call <c>Reset()</c>.
///   - All waiting threads are released when it’s signaled.
/// </summary>
public class PriorityPollingServiceAsync : IAsyncDisposable
{
    Thread? _thread;
    TimeSpan _interval = TimeSpan.FromMinutes(5);
    readonly EventWaitHandle _shutdown = new EventWaitHandle(false, EventResetMode.ManualReset);
    readonly EventWaitHandle _workSignal = new EventWaitHandle(false, EventResetMode.AutoReset);
    readonly ConcurrentQueue<Func<Task>> _highPriorityQueue = new();
    readonly ConcurrentQueue<Func<Task>> _normalPriorityQueue = new();

    /// <summary>
    /// The default <see cref="TimeSpan"/> interval will be 5 minutes.
    /// </summary>
    /// <param name="interval">how often to poll for work</param>
    public void Start(TimeSpan interval = default)
    {
        if (interval == default || interval == TimeSpan.Zero || interval == TimeSpan.MinValue || interval == TimeSpan.MaxValue)
            _interval = TimeSpan.FromMinutes(5);
        else
            _interval = interval;

        _thread = new Thread(() => PollLoopAsync().GetAwaiter().GetResult())
        {
            IsBackground = true, 
            Priority = ThreadPriority.BelowNormal,
        };
        _thread.Start();
    }

    public void Stop()
    {
        _shutdown.Set();
        _workSignal.Set();
        _thread?.Join();
    }

    public void AddWork(Action action, WorkPriority priority = WorkPriority.Normal)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        AddWork(() => { action(); return Task.CompletedTask; }, priority);
    }

    public void AddWork(Func<Task> asyncAction, WorkPriority priority = WorkPriority.Normal)
    {
        if (asyncAction == null) throw new ArgumentNullException(nameof(asyncAction));

        if (priority == WorkPriority.High)
            _highPriorityQueue.Enqueue(asyncAction);
        else
            _normalPriorityQueue.Enqueue(asyncAction);

        _workSignal.Set();
    }

    async Task PollLoopAsyncOld()
    {
        DateTime nextRun = DateTime.UtcNow;

        while (true)
        {
            TimeSpan remaining = nextRun - DateTime.UtcNow;
            if (remaining < TimeSpan.Zero)
                remaining = TimeSpan.Zero;

            Debug.WriteLine($"[POLLING] Checking for work in {remaining.ToReadableTime()}");

            int signaledIndex = WaitHandle.WaitAny(new WaitHandle[] { _shutdown, _workSignal }, remaining);

            if (signaledIndex == 0) // shutdown
                break;

            // Process high‑priority work first
            while (_highPriorityQueue.TryDequeue(out var highWork))
            {
                try { await highWork(); }
                catch (Exception ex) { Debug.WriteLine($"[High] Work failed: {ex.Message}"); }
            }

            // Then process normal‑priority work
            while (_normalPriorityQueue.TryDequeue(out var normalWork))
            {
                try { await normalWork(); }
                catch (Exception ex) { Debug.WriteLine($"[Normal] Work failed: {ex.Message}"); }
            }

            // If timeout expired, run scheduled poll
            if (signaledIndex == WaitHandle.WaitTimeout)
            {
                await CheckForWorkAsync();
                nextRun = DateTime.UtcNow + _interval;
            }
        }

        if (_highPriorityQueue.Count > 0)
        {
            Debug.WriteLine($"[WARNING] The high priority work queue still contains pending items.");
        }

        if (_normalPriorityQueue.Count > 0)
        {
            Debug.WriteLine($"[WARNING] The normal priority work queue still contains pending items.");
        }
    }

    async Task PollLoopAsync()
    {
        DateTime nextRun = DateTime.UtcNow;

        while (true)
        {
            TimeSpan remaining = nextRun - DateTime.UtcNow;
            if (remaining < TimeSpan.Zero)
                remaining = TimeSpan.Zero;

            int signaledIndex = WaitHandle.WaitAny(new WaitHandle[] { _shutdown, _workSignal }, remaining);

            if (signaledIndex == 0) // shutdown
                break;

            // Always drain high‑priority queue first
            while (_highPriorityQueue.TryDequeue(out var highWork))
            {
                try { await highWork(); }
                catch (Exception ex) { Console.WriteLine($"[High] Work failed: {ex.Message}"); }
            }

            // Process normal‑priority work, but check for high‑priority arrivals between each
            while (_normalPriorityQueue.TryDequeue(out var normalWork))
            {
                try { await normalWork(); }
                catch (Exception ex) { Console.WriteLine($"[Normal] Work failed: {ex.Message}"); }

                // If high‑priority work arrived mid‑processing, break and handle it immediately
                if (!_highPriorityQueue.IsEmpty)
                    break;
            }

            // If timeout expired, run scheduled poll
            if (signaledIndex == WaitHandle.WaitTimeout)
            {
                await CheckForWorkAsync();
                nextRun = DateTime.UtcNow + _interval;
            }
        }

        if (!_highPriorityQueue.IsEmpty)
        {
            Debug.WriteLine($"[WARNING] The high priority work queue still contains pending items.");
        }

        if (!_normalPriorityQueue.IsEmpty)
        {
            Debug.WriteLine($"[WARNING] The normal priority work queue still contains pending items.");
        }
    }


    Task CheckForWorkAsync()
    {
        Debug.WriteLine($"[POLLING] Checking for work at {DateTime.Now}");
        // Your scheduled async work here
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Stop();
        _shutdown.Dispose();
        _workSignal.Dispose();
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Original work-up model.
/// </summary>
public class PollingServiceSimple
{
    readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
    bool _running;

    public void Start()
    {
        _running = true;
        Thread pollingThread = new Thread(PollLoop)
        {
            IsBackground = true
        };
        pollingThread.Start();
    }

    public void Stop()
    {
        _running = false;
    }

    void PollLoop()
    {
        DateTime nextRun = DateTime.UtcNow;

        while (_running)
        {
            // Calculate remaining time until next run
            TimeSpan remaining = nextRun - DateTime.UtcNow;

            if (remaining > TimeSpan.Zero)
            {
                // If less than full interval, sleep only the remaining time
                Thread.Sleep(remaining);
            }

            // Do the work
            CheckForWork();

            // Schedule next run
            nextRun = DateTime.UtcNow + _interval;
        }
    }

    void CheckForWork()
    {
        Debug.WriteLine($"Checking for work at {DateTime.Now}");

        /** actual work here **/
    }
}
