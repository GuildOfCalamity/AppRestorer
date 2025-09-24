using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;


namespace AppRestorer.Controls;

public class AutoExpander : Expander
{
    readonly DispatcherTimer? _collapseTimer;
    bool _contextMenuOpen = false;

    static AutoExpander()
    {
        //DefaultStyleKeyProperty.OverrideMetadata(typeof(AutoExpander), new FrameworkPropertyMetadata(typeof(AutoExpander)));
    }

    public AutoExpander()
    {
        _collapseTimer = new DispatcherTimer();
        _collapseTimer.Tick += CollapseTimer_Tick;

        this.MouseEnter += AutoExpander_MouseEnter;
        this.MouseLeave += AutoExpander_MouseLeave;
        this.ContextMenuOpening += AutoExpander_ContextMenuOpening;
        this.ContextMenuClosing += AutoExpander_ContextMenuClosing;

        UpdateTimerInterval();
    }

    #region [Dependency Properties]
    public static readonly DependencyProperty CollapseDelayProperty = DependencyProperty.Register(
            nameof(CollapseDelay),
            typeof(TimeSpan),
            typeof(AutoExpander),
            new PropertyMetadata(TimeSpan.FromSeconds(3), OnCollapseDelayChanged));
    public TimeSpan CollapseDelay
    {
        get => (TimeSpan)GetValue(CollapseDelayProperty);
        set => SetValue(CollapseDelayProperty, value);
    }
    static void OnCollapseDelayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AutoExpander expander)
        {
            expander?.UpdateTimerInterval();
        }
    }
    void UpdateTimerInterval()
    {
        if (_collapseTimer != null)
            _collapseTimer.Interval = CollapseDelay;
    }

    public static readonly DependencyProperty CollapseOnLeaveProperty = DependencyProperty.Register(
            nameof(CollapseOnLeave),
            typeof(bool),
            typeof(AutoExpander),
            new PropertyMetadata(true));
    public bool CollapseOnLeave
    {
        get => (bool)GetValue(CollapseOnLeaveProperty);
        set => SetValue(CollapseOnLeaveProperty, value);
    }
    #endregion

    #region [Events]
    void CollapseTimer_Tick(object? sender, EventArgs e)
    {
        _collapseTimer?.Stop();

        // Double-check that user is not in control before collapsing
        if (!this.IsMouseOver /* && !this.IsKeyboardFocusWithin */)
        {
            this.IsExpanded = false;
        }
    }

    void AutoExpander_MouseEnter(object sender, MouseEventArgs e)
    {
        _collapseTimer?.Stop();   // cancel any pending collapse
        this.IsExpanded = true;   // expand immediately
    }

    void AutoExpander_MouseLeave(object sender, MouseEventArgs e)
    {
        if (CollapseOnLeave && !_contextMenuOpen)
        {
            // Only collapse if focus is not inside
            //if (!this.IsKeyboardFocusWithin)
            //{
                _collapseTimer?.Stop();
                if (this.IsExpanded)
                    _collapseTimer?.Start();  // start countdown to collapse
            //}
        }
    }

    void AutoExpander_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        _contextMenuOpen = true;
        _collapseTimer?.Stop();
    }

    void AutoExpander_ContextMenuClosing(object sender, ContextMenuEventArgs e)
    {
        _contextMenuOpen = false;

        if (CollapseOnLeave && !this.IsMouseOver && !this.IsKeyboardFocusWithin)
        {
            _collapseTimer?.Stop();
            _collapseTimer?.Start();
        }
    }
    #endregion
}

