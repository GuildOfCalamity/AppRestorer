using System.Collections.Concurrent;
using System.Diagnostics;

namespace AppRestorer
{
    /// <summary>
    /// Basic flavor - without a result.
    /// </summary>
    public class TimedTask
    {
        static readonly ConcurrentBag<TimedTask> AllTasks = new ConcurrentBag<TimedTask>();
        readonly Action _action;
        readonly DateTime _scheduledTime;
        readonly CancellationTokenSource _cts;
        bool _hasRun = false;

        public event Action OnStarted;
        public event Action OnCompleted;
        public event Action OnCanceled;

        /// <summary>
        /// Private constructor to initialize a TimedTask.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="scheduledTime"></param>
        /// <exception cref="ArgumentNullException"></exception>
        private TimedTask(Action action, DateTime scheduledTime)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _scheduledTime = scheduledTime;
            _cts = new CancellationTokenSource();
            _hasRun = false;

            AllTasks.Add(this);
            StartTask();
        }

        /// <summary>
        /// Schedules a task to run at a specific DateTime.
        /// </summary>
        public static TimedTask Schedule(Action action, DateTime runAt)
        {
            return new TimedTask(action, runAt);
        }

        /// <summary>
        /// Schedules a task to run after a delay.
        /// </summary>
        public static TimedTask Schedule(Action action, TimeSpan delay)
        {
            return new TimedTask(action, DateTime.Now.Add(delay));
        }

        void StartTask()
        {
            Task.Run(async () =>
            {
                var delay = _scheduledTime - DateTime.Now;
                if (delay < TimeSpan.Zero)
                    delay = TimeSpan.Zero;

                try
                {
                    await Task.Delay(delay, _cts.Token);
                    if (!_cts.Token.IsCancellationRequested)
                    {
                        OnStarted?.Invoke();
                        _action();
                        _hasRun = true;
                        OnCompleted?.Invoke();
                    }
                    else
                    {
                        OnCanceled?.Invoke();
                    }
                }
                catch (TaskCanceledException)
                {
                    // Task was canceled before execution
                    OnCanceled?.Invoke();
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Cancels the pending task if it has not run yet.
        /// </summary>
        public void Cancel()
        {
            if (!_hasRun)
            {
                Debug.WriteLine("[WARNING] Canceling task!");
                _cts?.Cancel();
            }
        }

        public bool HasRun => _hasRun;
        public bool IsPending => !_hasRun && !_cts.IsCancellationRequested;
        public static int GetCompletedCount() => AllTasks.Count(t => t._hasRun);
        public static int GetPendingCount() => AllTasks.Count(t => t.IsPending);
    }

    /// <summary>
    /// Support for action with a result ⇒ Func<TResult>
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public class TimedTask<TResult>
    {
        private static readonly ConcurrentBag<TimedTask<TResult>> AllTasks = new ConcurrentBag<TimedTask<TResult>>();

        readonly Func<CancellationToken, Task<TResult>> _asyncFunc;
        readonly DateTime _scheduledTime;
        readonly CancellationTokenSource _cts;
        bool _hasRun;
        TResult _result;
        readonly TaskCompletionSource<TResult> _tcs;

        public event Action OnStarted;
        public event Action<TResult> OnCompleted;
        public event Action OnCanceled;

        private TimedTask(Func<CancellationToken, Task<TResult>> asyncFunc, DateTime scheduledTime)
        {
            _asyncFunc = asyncFunc ?? throw new ArgumentNullException(nameof(asyncFunc));
            _scheduledTime = scheduledTime;
            _cts = new CancellationTokenSource();
            _tcs = new TaskCompletionSource<TResult>();
            _hasRun = false;

            AllTasks.Add(this);
            StartTask();
        }

        /// <summary>
        /// Sync Func<TResult> overload
        /// </summary>
        public static TimedTask<TResult> Schedule(Func<TResult> func, DateTime runAt)
        {
            return new TimedTask<TResult>(_ => Task.FromResult(func()), runAt);
        }

        /// <summary>
        /// Sync Func<TResult> overload
        /// </summary>
        public static TimedTask<TResult> Schedule(Func<TResult> func, TimeSpan delay)
        {
            return new TimedTask<TResult>(_ => Task.FromResult(func()), DateTime.Now.Add(delay));
        }

        /// <summary>
        /// Async Func<Task<TResult>> overload
        /// </summary>
        public static TimedTask<TResult> Schedule(Func<Task<TResult>> func, DateTime runAt)
        {
            return new TimedTask<TResult>(_ => func(), runAt);
        }

        /// <summary>
        /// Async Func<Task<TResult>> overload
        /// </summary>
        public static TimedTask<TResult> Schedule(Func<Task<TResult>> func, TimeSpan delay)
        {
            return new TimedTask<TResult>(_ => func(), DateTime.Now.Add(delay));
        }

        void StartTask()
        {
            Task.Run(async () =>
            {
                var delay = _scheduledTime - DateTime.Now;
                if (delay < TimeSpan.Zero)
                    delay = TimeSpan.Zero;

                try
                {
                    await Task.Delay(delay, _cts.Token);
                    if (!_cts.Token.IsCancellationRequested)
                    {
                        OnStarted?.Invoke();
                        _result = await _asyncFunc(_cts.Token);
                        _hasRun = true;
                        _tcs.SetResult(_result);
                        OnCompleted?.Invoke(_result);
                    }
                    else
                    {
                        _tcs.SetCanceled();
                        OnCanceled?.Invoke();
                    }
                }
                catch (TaskCanceledException)
                {
                    _tcs.SetCanceled();
                    OnCanceled?.Invoke();
                }
                catch (Exception ex)
                {
                    _tcs.SetException(ex);
                }
            });
        }

        public void Cancel()
        {
            if (!_hasRun)
            {
                Debug.WriteLine("[WARNING] Canceling task!");
                _cts.Cancel();
            }
        }

        public TResult Result => _result;
        public Task<TResult> ResultTask => _tcs.Task;
        public bool HasRun => _hasRun;
        public bool IsPending => !_hasRun && !_cts.IsCancellationRequested;
        public static int GetCompletedCount() => AllTasks.Count(t => t._hasRun);
        public static int GetPendingCount() => AllTasks.Count(t => t.IsPending);
    }
}
