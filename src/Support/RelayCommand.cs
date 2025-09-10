using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace AppRestorer;

/// <summary>
/// A basic <see cref="ICommand"/> that runs an <see cref="Action"/>.
/// </summary>
public class RelayCommand : ICommand
{
    #region [Members]

    /// <summary>
    /// The action to run
    /// </summary>
    private Action mAction;

    #endregion

    #region [Public Events]

    /// <summary>
    /// The event thats fired when the <see cref="CanExecute(object)"/> value has changed
    /// </summary>
    public event EventHandler? CanExecuteChanged = (sender, e) => { };

    #endregion

    #region [Constructor]

    /// <summary>
    /// Default constructor
    /// </summary>
    public RelayCommand(Action action)
    {
        mAction = action;
    }

    #endregion

    #region [Command Methods]

    /// <summary>
    /// A relay command can always execute
    /// </summary>
    /// <param name="parameter"></param>
    /// <returns></returns>
    public bool CanExecute(object? parameter)
    {
        return true;
    }

    /// <summary>
    /// Executes the commands Action
    /// </summary>
    /// <param name="parameter"></param>
    public void Execute(object? parameter)
    {
        try
        {
            mAction();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"RelayCommand.Execute: {ex.Message}");
        }
    }

    #endregion

    public override string ToString() => $"RelayCommand<{mAction?.Target}> bound to event {mAction?.Method?.Name}";
}

#region [Generic RelayCommand]
/// <summary>
/// Modified <see cref="RelayCommand"/> class using generic types.
/// </summary>
public class RelayCommand<T> : ICommand
{
    Action<T> execute;
    Func<T, bool> canExecute;

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
    {
        this.execute = execute;
        this.canExecute = canExecute;
    }

    #region [Command Methods]
    public bool CanExecute(object? parameter)
    {
        if (parameter == null)
            Debug.WriteLine($"[WARNING] RelayCommand.CanExecute: {nameof(parameter)} is null!");
        return this.canExecute == null || this.canExecute((T)parameter);
    }

    public void Execute(object? parameter)
    {
        //if (parameter == null)
        //    Debug.WriteLine($"[WARNING] RelayCommand.Execute: {nameof(parameter)} is null!");
        //else if (parameter is System.Windows.Controls.TextBox obj1)
        //    System.Diagnostics.Debug.WriteLine($">> Object {obj1?.Name} is a TextBox");
        //else if (parameter is System.Windows.Controls.Button obj2)
        //    System.Diagnostics.Debug.WriteLine($">> Object {obj2?.Name} is a Button");
        try
        {
            this.execute((T)parameter);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"RelayCommand<T>.Execute: {ex.Message}");
        }
    }

    #endregion

    public override string ToString() => $"RelayCommand<{execute?.Target}> bound to event {execute?.Method?.Name}";
}
#endregion

#region [Asynchronous RelayCommand]
public class AsyncRelayCommand : AsyncCommandBase
{
    readonly Func<Task> _callback;

    public AsyncRelayCommand(Func<Task> callback, Action<Exception> onException) : base(onException)
    {
        _callback = callback;
    }

    protected override async Task ExecuteAsync(object? parameter)
    {
        await _callback();
    }
}

public abstract class AsyncCommandBase : ICommand
{
    readonly Action<Exception>? _onException;

    bool _isExecuting;
    public bool IsExecuting
    {
        get => _isExecuting;
        set
        {
            _isExecuting = value;
            CanExecuteChanged?.Invoke(this, new EventArgs());
        }
    }

    public event EventHandler? CanExecuteChanged;

    public AsyncCommandBase(Action<Exception> onException)
    {
        _onException = onException;
    }

    public bool CanExecute(object? parameter)
    {
        return !IsExecuting;
    }

    public async void Execute(object? parameter)
    {
        IsExecuting = true;

        try
        {
            await ExecuteAsync(parameter);
        }
        catch (Exception ex)
        {
            _onException?.Invoke(ex);
        }

        IsExecuting = false;
    }

    protected abstract Task ExecuteAsync(object? parameter);
}
#endregion

/// <summary>
/// A revamped <see cref="RelayCommand"/> class that offers a <see cref="bool"/> return type.
/// </summary>
public class RelayCommandResult : ICommand
{
    private readonly Action? _execute;                // Standard Action with no return
    private readonly Func<bool>? _executeWithResult;  // New Func<bool> to return a result
    private readonly Func<bool>? _canExecute;
    public event EventHandler? CanExecuteChanged;

    public RelayCommandResult(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public RelayCommandResult(Func<bool> executeWithResult, Func<bool> canExecute = null)
    {
        _executeWithResult = executeWithResult ?? throw new ArgumentNullException(nameof(executeWithResult));
        _canExecute = canExecute;
    }

    #region [Command Methods]
    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute();
    }

    public void Execute(object? parameter)
    {
        if (_execute != null)
        {
            try
            {
                _execute();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RelayCommandResult.Execute: {ex.Message}");
            }
        }
        else if (_executeWithResult != null)
        {
            try
            {
                bool result = _executeWithResult();
                // Optionally, handle the result (e.g., log it, trigger another action, etc.)
                Debug.WriteLine($"[INFO] {nameof(RelayCommandResult)} executed result: {result}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RelayCommandResult.ExecuteWithResult: {ex.Message}");
            }
        }
    }

    public bool ExecuteWithResult()
    {
        if (_executeWithResult != null)
        {
            return _executeWithResult();
        }
        else
        {
            Debug.WriteLine($"[ERROR] {nameof(RelayCommandResult)}: This command does not support returning a result.");
            throw new InvalidOperationException("This command does not support returning a result.");
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public override string ToString() => $"RelayCommandResult<{_execute?.Target}> bound to event {_execute?.Method.Name}";
    #endregion
}
