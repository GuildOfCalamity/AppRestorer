using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace AppRestorer.Controls;

/// <summary>
/// Ping-Pong style ProgressBar
/// </summary>
public partial class ProgressBarEx : UserControl
{
    Storyboard? _pingPongStoryboard;

    #region [Dependency Properties]
    public double AnimationWidth
    {
        get => (double)GetValue(AnimationWidthProperty);
        set => SetValue(AnimationWidthProperty, value);
    }
    public static readonly DependencyProperty AnimationWidthProperty = DependencyProperty.Register(
        nameof(AnimationWidth),
        typeof(double),
        typeof(ProgressBarEx),
new FrameworkPropertyMetadata(200.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public double BarWidth
    {
        get => (double)GetValue(BarWidthProperty);
        set => SetValue(BarWidthProperty, value);
    }
    public static readonly DependencyProperty BarWidthProperty = DependencyProperty.Register(
        nameof(BarWidth),
        typeof(double),
        typeof(ProgressBarEx),
new PropertyMetadata(50.0, OnBarWidthChanged));
    static void OnBarWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = d as ProgressBarEx;
        if (e.NewValue != null && e.NewValue is double dbl)
        {
            ctrl?.ApplyBarWidth(dbl);
        }
    }

    public double BarHeight
    {
        get => (double)GetValue(BarHeightProperty);
        set => SetValue(BarHeightProperty, value);
    }
    public static readonly DependencyProperty BarHeightProperty = DependencyProperty.Register(
        nameof(BarHeight),
        typeof(double),
        typeof(ProgressBarEx),
new PropertyMetadata(18.0, OnBarHeightChanged));
    static void OnBarHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = d as ProgressBarEx;
    }

    public Color BarColor
    {
        get => (Color)GetValue(BarColorProperty);
        set => SetValue(BarColorProperty, value);
    }
    public static readonly DependencyProperty BarColorProperty = DependencyProperty.Register(
        nameof(BarColor),
        typeof(Color),
        typeof(ProgressBarEx),
        new PropertyMetadata(Colors.DodgerBlue, OnBarColorChanged));
    static void OnBarColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = d as ProgressBarEx;
    }

    public TimeSpan Duration
    {
        get => (TimeSpan)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }
    public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
        nameof(Duration),
        typeof(TimeSpan),
        typeof(ProgressBarEx),
        new PropertyMetadata(TimeSpan.FromSeconds(1.5), OnDurationChanged));
    static void OnDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = d as ProgressBarEx;
        if (e.NewValue != null && e.NewValue is TimeSpan ts) 
        {
            ctrl?.ApplyPingPongAnimation(ts);
        }
    }
    #endregion

    public ProgressBarEx()
    {
        InitializeComponent();
    }

    void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        //pbar.ApplyTemplate(); // Be sure the instance has been applied.
        ApplyPingPongAnimation(Duration);
    }

    void ApplyBarWidth(double newWidth)
    {
        var indicator = (Rectangle)pbar.Template.FindName("PART_Indicator", pbar);
        if (indicator == null)
        {
            Debug.WriteLine("[WARNING] UserControl: Template part not found — check style/template names.");
            return;
        }
        indicator.Width = newWidth;
    }

    void ApplyPingPongAnimation(TimeSpan ts)
    {
        if (_pingPongStoryboard != null)
        {
            _pingPongStoryboard?.Stop();
            _pingPongStoryboard?.Children?.Clear();
        }

        //var canvas = FindName("PART_Canvas") as Canvas;
        var canvas = (Canvas)pbar.Template.FindName("PART_Canvas", pbar);
        //var indicator = FindName("PART_Indicator") as Rectangle;
        var indicator = (Rectangle)pbar.Template.FindName("PART_Indicator", pbar);
        if (canvas == null || indicator == null)
        {
            Debug.WriteLine("[WARNING] UserControl: Template parts not found — check style/template names.");
            return;
        }

        indicator.Width = BarWidth;
        double target = pbar.ActualWidth - indicator.Width - 10; // Padding margin
        double target2 = AnimationWidth - indicator.Width;
        if (target2 <= 0)
            target2 = 10;

        var animation = new DoubleAnimation
        {
            From = 0,
            To = target2,
            Duration = ts,
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };
        Storyboard.SetTarget(animation, indicator);
        Storyboard.SetTargetProperty(animation, new PropertyPath("(Canvas.Left)"));
        _pingPongStoryboard = new Storyboard();
        _pingPongStoryboard.Children.Add(animation);
        _pingPongStoryboard.Begin();
    }


    void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _pingPongStoryboard?.Stop();
    }
}
