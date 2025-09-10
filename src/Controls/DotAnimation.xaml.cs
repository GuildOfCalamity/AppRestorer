using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AppRestorer.Controls;

public partial class DotAnimation : UserControl
{
    double minimumScale = 0.4;
    const double defaultDuration = 0.6;
    bool hasAppliedTemplate = false;
    Storyboard? dotAnimationStoryboard;

    #region [Dependency Properties]
    /// <summary>
    /// DotRadius Dependency Property
    /// </summary>
    public double DotRadius
    {
        get { return (double)GetValue(DotRadiusProperty); }
        set { SetValue(DotRadiusProperty, value); }
    }
    public static readonly DependencyProperty DotRadiusProperty = DependencyProperty.Register(
        nameof(DotRadius),
        typeof(double),
        typeof(DotAnimation),
        new PropertyMetadata(4d, OnDotRadiusChanged));

    /// <summary>
    /// DotMinimum Dependency Property. 
    /// A percentage of how small the resting size should be in relation to the expanded size.
    /// </summary>
    public double DotMinimum
    {
        get { return (double)GetValue(DotMinimumProperty); }
        set { SetValue(DotMinimumProperty, value); }
    }
    public static readonly DependencyProperty DotMinimumProperty = DependencyProperty.Register(
        nameof(DotMinimum),
        typeof(double),
        typeof(DotAnimation),
        new PropertyMetadata(0.4d, OnDotMinimumChanged));

    /// <summary>
    /// DotSpacing Dependency Property
    /// </summary>
    public double DotSpacing
    {
        get { return (double)GetValue(DotSpacingProperty); }
        set { SetValue(DotSpacingProperty, value); }
    }
    public static readonly DependencyProperty DotSpacingProperty = DependencyProperty.Register(
        nameof(DotSpacing),
        typeof(double),
        typeof(DotAnimation),
        new PropertyMetadata(12d, OnDotSpacingChanged));

    /// <summary>
    /// DotSize Dependency Property
    /// </summary>
    public double DotSize
    {
        get { return (double)GetValue(DotSizeProperty); }
        set { SetValue(DotSizeProperty, value); }
    }
    public static readonly DependencyProperty DotSizeProperty = DependencyProperty.Register(
        nameof(DotSize),
        typeof(double),
        typeof(DotAnimation),
        new PropertyMetadata(18d, OnDotSizeChanged));

    /// <summary>
    /// DotDuration Dependency Property
    /// </summary>
    public double DotDuration
    {
        get { return (double)GetValue(DotDurationProperty); }
        set { SetValue(DotDurationProperty, value); }
    }
    public static readonly DependencyProperty DotDurationProperty = DependencyProperty.Register(
        nameof(DotDuration),
        typeof(double),
        typeof(DotAnimation),
        new PropertyMetadata(defaultDuration, OnDotDurationChanged));


    /// <summary>
    /// DotOpacity Dependency Property
    /// </summary>
    public double DotOpacity
    {
        get { return (double)GetValue(DotOpacityProperty); }
        set { SetValue(DotOpacityProperty, value); }
    }
    public static readonly DependencyProperty DotOpacityProperty = DependencyProperty.Register(
        nameof(DotOpacity),
        typeof(double),
        typeof(DotAnimation),
        new PropertyMetadata(0.9d, OnDotOpacityChanged));

    /// <summary>
    /// IsRunning Dependency Property
    /// </summary>
    public bool IsRunning
    {
        get { return (bool)GetValue(IsRunningProperty); }
        set { SetValue(IsRunningProperty, value); }
    }
    public static readonly DependencyProperty IsRunningProperty = DependencyProperty.Register(
        nameof(IsRunning),
        typeof(bool),
        typeof(DotAnimation),
        new PropertyMetadata(false, OnIsRunningChanged));

    static void OnIsRunningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (DotAnimation)d;
        control.UpdateAnimationState();
    }

    /// <summary>
    /// FillColor Dependency Property
    /// </summary>
    public Brush FillColor
    {
        get { return (Brush)GetValue(FillColorProperty); }
        set { SetValue(FillColorProperty, value); }
    }
    public static readonly DependencyProperty FillColorProperty = DependencyProperty.Register(
        nameof(FillColor),
        typeof(Brush),
        typeof(DotAnimation),
        new PropertyMetadata(Brushes.White));

    /// <summary>
    /// Easing Dependency Property
    /// </summary>
    public static readonly DependencyProperty EasingProperty = DependencyProperty.Register(
    nameof(Easing),
    typeof(IEasingFunction),
    typeof(DotAnimation),
    new PropertyMetadata(new QuadraticEase() { EasingMode = EasingMode.EaseInOut }, OnEasingChanged));
    public IEasingFunction Easing
    {
        get { return (IEasingFunction)this.GetValue(EasingProperty); }
        set { this.SetValue(EasingProperty, value); }
    }
    static void OnEasingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = d as DotAnimation;
        if (control == null)
            return;

        var easing = (IEasingFunction)e.NewValue;
        if (easing == null)
            return;

        control.Easing = easing;
        if (control.IsRunning)
        {
            control.dotAnimationStoryboard?.Stop(control);
            control.CreateDotAnimationStoryboard(control.Easing);
            control.dotAnimationStoryboard?.Begin(control, true);
        }
        else
        {
            control.CreateDotAnimationStoryboard(control.Easing);
        }
    }

    public static readonly DependencyProperty MouseDownCommandProperty =
       DependencyProperty.Register(
           "MouseDownCommand",
           typeof(ICommand),
           typeof(DotAnimation),
           new PropertyMetadata(null, OnMouseDownCommandChanged));

    public ICommand MouseDownCommand
    {
        get { return (ICommand)GetValue(MouseDownCommandProperty); }
        set { SetValue(MouseDownCommandProperty, value); }
    }
    #endregion

    #region [Exposing a custom MouseDown event]
    public static readonly RoutedEvent MouseDownEvent =
        EventManager.RegisterRoutedEvent(
            "MouseDown",
            RoutingStrategy.Bubble, // route upwards through the visual tree
            typeof(MouseButtonEventHandler),
            typeof(DotAnimation));
    public event MouseButtonEventHandler MouseDown
    {
        add { AddHandler(MouseDownEvent, value); }
        remove { RemoveHandler(MouseDownEvent, value); }
    }
    void Grid_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Raise the routed event
        RaiseEvent(new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, e.ChangedButton)
        {
            RoutedEvent = MouseDownEvent
        });
    }
    #endregion

    #region [Property Changed Callbacks]
    /// <summary>
    /// Updates the animation based on the IsRunning dependency property.
    /// </summary>
    void UpdateAnimationState()
    {
        // If you were fetching the storyboard from the XAML:
        //var sb = (Storyboard)Resources["DotAnimationStoryboard"];

        if (IsRunning)
        {
            Visibility = Visibility.Visible;
            dotAnimationStoryboard?.Begin(this, true);
        }
        else
        {
            dotAnimationStoryboard?.Stop(this);
            Visibility = Visibility.Collapsed;
        }
    }

    static void OnDotSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (DotAnimation)d;
        if (e.NewValue != null && e.NewValue is double cs)
        {
            control.UpdateDotSpacing(cs);
        }
        else
        {
            Debug.WriteLine($"[WARNING] e.NewValue is null or is not the correct type.");
        }
    }

    void UpdateDotSpacing(double space)
    {
        if (space != double.NaN && space > 0)
        {
            //cc1.Width = cc2.Width = cc3.Width = cc4.Width = new GridLength(space * 1.111d);
            hostGrid.Width = space * 5.5d;
        }
    }

    static void OnDotDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (DotAnimation)d;
        if (e.NewValue != null && e.NewValue is double cs)
        {
            if (control.IsRunning)
            {
                control.dotAnimationStoryboard?.Stop(control);
                control.CreateDotAnimationStoryboard(control.Easing, cs);
                control.dotAnimationStoryboard?.Begin(control, true);
            }
            else
            {
                control.CreateDotAnimationStoryboard(control.Easing, cs);
            }
        }
        else
        {
            Debug.WriteLine($"[WARNING] e.NewValue is null or is not the correct type.");
        }
    }

    static void OnDotOpacityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (DotAnimation)d;
        if (e.NewValue != null && e.NewValue is double cs)
        {
            control.UpdateOpacity(cs);
        }
        else
        {
            Debug.WriteLine($"[WARNING] e.NewValue is null or is not the correct type.");
        }
    }

    static void OnDotSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (DotAnimation)d;
        if (e.NewValue != null && e.NewValue is double cs)
        {
            control.UpdateDotSize(cs);
        }
        else
        {
            Debug.WriteLine($"[WARNING] e.NewValue is null or is not the correct type.");
        }
    }

    void UpdateDotSize(double size)
    {
        if (size != double.NaN && size > 0)
        {
            var corner = Math.Ceiling(size / 3d); // Math.Ceiling(Math.Sqrt(size / 3d));
            Dot1.Width = Dot1.Height = size;
            Dot1.RadiusX = Dot1.RadiusY = corner;
            Dot2.Width = Dot2.Height = size;
            Dot2.RadiusX = Dot2.RadiusY = corner;
            Dot3.Width = Dot3.Height = size;
            Dot3.RadiusX = Dot3.RadiusY = corner;
            Dot4.Width = Dot4.Height = size;
            Dot4.RadiusX = Dot4.RadiusY = corner;
            Dot5.Width = Dot5.Height = size;
            Dot5.RadiusX = Dot5.RadiusY = corner;
        }
    }

    static void OnDotMinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (DotAnimation)d;
        if (e.NewValue != null && e.NewValue is double cs)
        {
            control.minimumScale = cs;
            if (control.IsRunning)
            {
                control.dotAnimationStoryboard?.Stop(control);
                control.CreateDotAnimationStoryboard(control.Easing);
                control.dotAnimationStoryboard?.Begin(control, true);
            }
            else
            {
                control.CreateDotAnimationStoryboard(control.Easing);
            }
        }
        else
        {
            Debug.WriteLine($"[WARNING] e.NewValue is null or is not the correct type.");
        }
    }

    static void OnDotRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (DotAnimation)d;
        if (e.NewValue != null && e.NewValue is double cs)
        {
            #region [For designer's edification only]
            if (control.IsRunning)
                control.dotAnimationStoryboard?.Stop(control);
            
            control.Dot1.RadiusX = control.Dot1.RadiusY = cs;
            control.Dot2.RadiusX = control.Dot2.RadiusY = cs;
            control.Dot3.RadiusX = control.Dot3.RadiusY = cs;
            control.Dot4.RadiusX = control.Dot4.RadiusY = cs;
            control.Dot5.RadiusX = control.Dot5.RadiusY = cs;
            
            if (control.IsRunning)
                control.dotAnimationStoryboard?.Begin(control, true);
            #endregion
            Debug.WriteLine($"[INFO] New DotRadius is {cs}");
        }
        else
        {
            Debug.WriteLine($"[WARNING] e.NewValue is null or is not the correct type.");
        }
    }

    static void OnMouseDownCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DotAnimation control)
        {
            control.UpdateMouseDownCommandEvent();
        }
    }
    void UpdateMouseDownCommandEvent()
    {
        if (MouseDownCommand != null)
            hostGrid.MouseDown += HostGridOnMouseDown;
        else
            hostGrid.MouseDown -= HostGridOnMouseDown;
    }
    void HostGridOnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (MouseDownCommand != null && MouseDownCommand.CanExecute(e))
            MouseDownCommand.Execute(e);
    }
    #endregion

    public DotAnimation()
    {
        InitializeComponent();
        UpdateMouseDownCommandEvent(); // Ensure event handler is set initially
        CreateDotAnimationStoryboard(new QuadraticEase());
        
        Loaded += (s, e) =>
        {   
            UpdateAnimationState(); // Initialize state based on IsRunning property.
        };
        
        // If not using ICommand then wire-up basic event handler.
        if (MouseDownCommand == null)
            hostGrid.MouseDown += Grid_MouseDown;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        hasAppliedTemplate = true;
        Debug.WriteLine($"[INFO] {nameof(DotAnimation)} template has been applied.");
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Debug.WriteLine($"[INFO] {nameof(DotAnimation)} is measured to be {availableSize}");
        // NOTE: The radius can only be updated AFTER the control's measurement.
        Dot1.RadiusX = Dot1.RadiusY = DotRadius;
        Dot2.RadiusX = Dot2.RadiusY = DotRadius;
        Dot3.RadiusX = Dot3.RadiusY = DotRadius;
        Dot4.RadiusX = Dot4.RadiusY = DotRadius;
        Dot5.RadiusX = Dot5.RadiusY = DotRadius;
        return base.MeasureOverride(availableSize);
    }

    #region [Helpers]
    /// <summary>
    /// Create the DotAnimationStoryboard programmatically
    /// </summary>
    void CreateDotAnimationStoryboard(IEasingFunction easing, double seconds = defaultDuration)
    {
        if (dotAnimationStoryboard != null)
            dotAnimationStoryboard?.Children?.Clear();

        dotAnimationStoryboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };
        Duration duration = new Duration(TimeSpan.FromSeconds(seconds));
        AddDotAnimations(Dot1, TimeSpan.FromSeconds(0.0), duration, easing);
        AddDotAnimations(Dot2, TimeSpan.FromSeconds(0.2), duration, easing);
        AddDotAnimations(Dot3, TimeSpan.FromSeconds(0.4), duration, easing);
        AddDotAnimations(Dot4, TimeSpan.FromSeconds(0.6), duration, easing);
        AddDotAnimations(Dot5, TimeSpan.FromSeconds(0.8), duration, easing);
    }

    void UpdateOpacity(double opacity)
    {
        Debug.WriteLine($"[INFO] Updating {nameof(DotOpacity)} to {opacity}");
        // Each rectangle should inherit from the host's opacity, but just to be thorough:
        hostGrid.Opacity = Dot1.Opacity = Dot2.Opacity = Dot3.Opacity = Dot4.Opacity = Dot5.Opacity = opacity;
    }

    /// <summary>
    /// Method to add animations to a dot with a specific delay and duration
    /// </summary>
    /// <param name="dot"></param>
    /// <param name="beginTime"></param>
    /// <param name="duration"></param>
    void AddDotAnimations(UIElement dot, TimeSpan beginTime, Duration duration, IEasingFunction easing)
    {
        // Ensure each dot has a ScaleTransform applied
        var scaleTransform = new ScaleTransform(minimumScale, minimumScale);
        dot.RenderTransform = scaleTransform;
        dot.RenderTransformOrigin = new Point(0.5, 0.5);

        #region [ScaleX Animation]
        var scaleXAnimation = new DoubleAnimation
        {
            From = minimumScale,
            To = 1.09,
            Duration = duration,
            AutoReverse = true,
            BeginTime = beginTime,
            EasingFunction = easing
        };
        Storyboard.SetTarget(scaleXAnimation, dot);
        Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("RenderTransform.ScaleX"));
        dotAnimationStoryboard?.Children?.Add(scaleXAnimation);
        #endregion

        #region [ScaleY Animation]
        var scaleYAnimation = new DoubleAnimation
        {
            From = minimumScale,
            To = 1.09,
            Duration = duration,
            AutoReverse = true,
            BeginTime = beginTime,
            EasingFunction = easing
        };
        Storyboard.SetTarget(scaleYAnimation, dot);
        Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("RenderTransform.ScaleY"));
        dotAnimationStoryboard?.Children?.Add(scaleYAnimation);
        #endregion
    }

    /// <summary>
    /// Stops the storyboard and removes all contained animations
    /// </summary>
    void RemoveDotAnimations()
    {
        dotAnimationStoryboard?.Stop(this);
        dotAnimationStoryboard?.Children?.Clear();
    }
    #endregion
}
