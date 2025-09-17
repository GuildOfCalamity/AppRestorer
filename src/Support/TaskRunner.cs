using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AppRestorer;

#region [TaskCompletedEvent Model]
/// <summary>
/// Holds our metadata about the completed task.
/// </summary>
public class TaskCompletedEventArgs : EventArgs
{
    public string TaskName { get; }
    public TimeSpan Duration { get; }
    public DateTime CompletedAt { get; }
    public bool Success { get; }
    public bool Canceled { get; }
    public Exception? Exception { get; }

    public TaskCompletedEventArgs(string taskName, TimeSpan duration, DateTime completedAt, bool success, bool canceled, Exception? exception = null)
    {
        TaskName = taskName;
        Duration = duration;
        CompletedAt = completedAt;
        Success = success;
        Canceled = canceled;
        Exception = exception;
    }
}
#endregion

//============================================================================================================
//  Without IProgress<T>
//============================================================================================================
public class TaskRunner
{
    readonly Action _action;
    readonly string _taskName;

    public event EventHandler<TaskCompletedEventArgs>? TaskCompleted;
    
    protected virtual void OnTaskCompleted(TaskCompletedEventArgs e) => TaskCompleted?.Invoke(this, e);

    public TaskRunner(Action action, string taskName)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _taskName = string.IsNullOrWhiteSpace(taskName) ? "Unnamed Task" : taskName;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        Exception? caughtException = null;
        bool success = true;
        bool canceled = false;

        try
        {
            await Task.Run(_action, cancellationToken);
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            success = false;
            canceled = true;
        }
        catch (AggregateException ex)
        {
            success = false;
            ex.Flatten().Handle((inner) =>
            {
                if (inner is TaskCanceledException || inner is OperationCanceledException)
                {
                    canceled = true;
                    return true;
                }
                caughtException = inner;
                return true;
            });
        }
        catch (Exception ex) // Catch any other exceptions
        {
            success = false;
            caughtException = ex;
        }
        finally
        {
            stopwatch.Stop();
            // Fire the completion event
            OnTaskCompleted(new TaskCompletedEventArgs(
                _taskName,
                stopwatch.Elapsed,
                DateTime.UtcNow,
                success,
                canceled,
                caughtException));
        }
    }
}

//============================================================================================================
//  With IProgress<T>
//============================================================================================================
public class TaskRunner<TProgress>
{
    readonly Action<IProgress<TProgress>>? _action;
    readonly Func<IProgress<TProgress>, Task>? _asyncAction;
    readonly string _taskName;
    readonly IProgress<TProgress>? _progress;

    public event EventHandler<TaskCompletedEventArgs>? TaskCompleted;
    
    protected virtual void OnTaskCompleted(TaskCompletedEventArgs e) => TaskCompleted?.Invoke(this, e);

    public TaskRunner(Action<IProgress<TProgress>> action, string taskName, IProgress<TProgress>? progress = null)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _taskName = string.IsNullOrWhiteSpace(taskName) ? "Unnamed Task" : taskName;
        _progress = progress;
    }

    public TaskRunner(Func<IProgress<TProgress>, Task> asyncAction, string taskName, IProgress<TProgress>? progress = null)
    {
        _asyncAction = asyncAction ?? throw new ArgumentNullException(nameof(asyncAction));
        _taskName = string.IsNullOrWhiteSpace(taskName) ? "Unnamed Task" : taskName;
        _progress = progress;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        Exception? caughtException = null;
        bool success = true;
        bool canceled = false;

        try
        {
            if (_action != null)
            {
                await Task.Run(() => _action(_progress), cancellationToken);
            }
            else if (_asyncAction != null)
            {
                await _asyncAction.Invoke(_progress);
            }
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            success = false;
            canceled = true;
        }
        catch (AggregateException ex)
        {
            success = false;
            ex.Flatten().Handle((inner) =>
            {
                if (inner is TaskCanceledException || inner is OperationCanceledException)
                {
                    canceled = true;
                    return true;
                }
                caughtException = inner;
                return true;
            });
        }
        catch (Exception ex) // Catch any other exceptions
        {
            success = false;
            caughtException = ex;
        }
        finally
        {
            stopwatch.Stop();
            // Fire the completion event
            OnTaskCompleted(new TaskCompletedEventArgs(
                _taskName,
                stopwatch.Elapsed,
                DateTime.UtcNow,
                success,
                canceled,
                caughtException));
        }
    }
}
