using System;
using System.Reflection.Metadata;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;


namespace AppRestorer.Controls;

public enum RenderMode
{
    RotateCanvas,    // if using single color
    AnimatePositions // if using gradient brushes
}

public enum RenderShape
{
    Dots,
    Polys,
}


/// <summary>
/// Visibility determines if animation runs.
/// </summary>
public partial class Spinner : UserControl
{
    public int DotCount { get; set; } = 10;
    public double DotSize { get; set; } = 6;
    public Brush DotBrush { get; set; } = Brushes.DodgerBlue;
    public double RotationDuration { get; set; } = 1.0; // seconds
    public RenderMode Mode { get; set; } = RenderMode.AnimatePositions; // more versatile
    public RenderShape Shape { get; set; } = RenderShape.Dots;
    public bool SineWave { get; set; } = false;
    public double WaveAmplitude { get; set; } = 12; // pixels
    public double WaveFrequency { get; set; } = 1;  // cycles across width

    public Spinner()
    {
        InitializeComponent();
        Loaded += Spinner_Loaded;
        IsVisibleChanged += Spinner_IsVisibleChanged;
    }

    void Spinner_Loaded(object sender, RoutedEventArgs e)
    {
        if (Shape == RenderShape.Dots)
            CreateDots();
        else
            CreatePolys();

        if (IsVisible)
        {
            if (Mode == RenderMode.RotateCanvas)
                StartAnimationStandard();
            else
                StartAnimationCompositionTarget();
        }
    }

    void Spinner_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        RunFade(IsVisible);
        if (IsVisible)
        {
            if (Mode == RenderMode.RotateCanvas)
                StartAnimationStandard();
            else
                StartAnimationCompositionTarget();
        }
        else
        {
            if (Mode == RenderMode.RotateCanvas)
                StopAnimationStandard();
            else
                StopAnimationCompositionTarget();
        }
    }

    void CreateDots(bool pulse = false)
    {
        if (PART_Canvas == null) 
            return;

        PART_Canvas.Children.Clear();
        double radius = Math.Min(ActualWidth, ActualHeight) / 2 - DotSize;

        // Fetch a brush from the local UserControl
        //Brush? brsh = (Brush)FindResource("DotBrush");

        for (int i = 0; i < DotCount; i++)
        {
            double angle = i * 360.0 / DotCount;
            double rad = angle * Math.PI / 180;
            double x = radius * Math.Cos(rad) + ActualWidth / 2 - DotSize / 2;
            double y = radius * Math.Sin(rad) + ActualHeight / 2 - DotSize / 2;
            //Rectangle dot = new Rectangle { Width = DotSize, Height = DotSize, RadiusX = 2, RadiusY = 2, Fill = DotBrush, Opacity = (double)i / DotCount };
            Ellipse dot = new Ellipse
            {
                Width = DotSize, Height = DotSize,
                Fill = DotBrush,
                Opacity = (double)i / DotCount // fade each consecutive dot
            };

            if (pulse)
            {   // Pulsing effect
                dot.RenderTransform = new RotateTransform(angle + 90, DotSize / 3, DotSize / 3);
            }
            
            Canvas.SetLeft(dot, x);
            Canvas.SetTop(dot, y);
            PART_Canvas.Children.Add(dot);
        }
    }

    /// <summary>
    /// Create path geometry instead of a standard <see cref="Ellipse"/>.
    /// </summary>
    /// <param name="pointOutward"></param>
    void CreatePolys(bool pointOutward = true)
    {
        if (PART_Canvas == null) 
            return;

        PART_Canvas.Children.Clear();
        double radius = Math.Min(ActualWidth, ActualHeight) / 2 - DotSize;

        for (int i = 0; i < DotCount; i++)
        {
            double angle = i * 360.0 / DotCount;
            double rad = angle * Math.PI / 180;

            double x = radius * Math.Cos(rad) + ActualWidth / 2 - DotSize / 2;
            double y = radius * Math.Sin(rad) + ActualHeight / 2 - DotSize / 2;

            var triangle = Geometry.Parse("M 0,0 L 6,0 3,6 Z");
            var equilateral = Geometry.Parse("M 0,1 L 0.5,0 1,1 Z");
            var diamond = Geometry.Parse("M 0.5,0 L 1,0.5 0.5,1 0,0.5 Z");
            var star = Geometry.Parse("M 0.5,0 L 0.61,0.35 1,0.35 0.68,0.57 0.81,0.91 0.5,0.7 0.19,0.91 0.32,0.57 0,0.35 0.39,0.35 Z");
            var circle = Geometry.Parse("M 0,0.5 A 0.5,0.5 0 1 0 1,0.5 A 0.5,0.5 0 1 0 0,0.5");
            var tick = Geometry.Parse("M 0,0 L 0,1");
            var chevronRight = Geometry.Parse("M 0,0 L 0.6,0.5 L 0,1 L 0.2,1 L 0.8,0.5 L 0.2,0 Z");
            var chevronLeft = Geometry.Parse("M 1,0 L 0.4,0.5 L 1,1 L 0.8,1 L 0.2,0.5 L 0.8,0 Z");
            var chevronUp = Geometry.Parse("M 0,1 L 0.5,0.4 L 1,1 L 1,0.8 L 0.5,0.2 L 0,0.8 Z");
            var chevronDown = Geometry.Parse("M 0,0 L 0.5,0.6 L 1,0 L 1,0.2 L 0.5,0.8 L 0,0.2 Z");
            var path = new System.Windows.Shapes.Path
            {
                Data = triangle,
                Fill = DotBrush,
                Width = DotSize,
                Stroke = DotBrush, // new SolidColorBrush(Colors.White),
                StrokeThickness = 2,
                Height = DotSize,
                Stretch = Stretch.Uniform,
                Opacity = (double)i / DotCount // fade each consecutive shape
            };
            
            if (pointOutward)
            {   // Keep the shape’s orientation consistent around the circle
                path.RenderTransform = new RotateTransform(angle + 90, DotSize / 2, DotSize / 2);
            }

            Canvas.SetLeft(path, x);
            Canvas.SetTop(path, y);
            PART_Canvas.Children.Add(path);
        }
    }


    void StartAnimationStandard()
    {
        // Always rebuild the animation fresh
        RotateTransform rotate = new RotateTransform();
        PART_Canvas.RenderTransform = rotate;
        PART_Canvas.RenderTransformOrigin = new Point(0.5, 0.5);
        DoubleAnimation anim = new DoubleAnimation
        {
            From = 0,
            To = 360,
            Duration = TimeSpan.FromSeconds(RotationDuration),
            RepeatBehavior = RepeatBehavior.Forever
        };
        rotate.BeginAnimation(RotateTransform.AngleProperty, anim);
    }

    void StopAnimationStandard()
    {
        PART_Canvas.RenderTransform?.BeginAnimation(RotateTransform.AngleProperty, null);
    }

    /// <summary>
    /// If <paramref name="fadeIn"/> is <c>true</c>, the <see cref="UserControl"/> will be animated to 1 opacity.<br/>
    /// If <paramref name="fadeIn"/> is <c>false</c>, the <see cref="UserControl"/> will be animated to 0 opacity.<br/>
    /// </summary>
    /// <remarks>animation will run for 250 milliseconds</remarks>
    void RunFade(bool fadeIn)
    {
        var anim = new DoubleAnimation
        {
            To = fadeIn ? 1.0 : 0.0,
            Duration = TimeSpan.FromMilliseconds(250),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };
        this.BeginAnimation(OpacityProperty, anim);
    }


    #region [CompositionTarget Rendering]

    double _angle = 0d;
    bool _renderHooked = false;

    void StartAnimationCompositionTarget()
    {
        if (_renderHooked) { return; }
        _renderHooked = true;
        if (SineWave)
            CompositionTarget.Rendering += OnSineWaveRendering;
        else
            CompositionTarget.Rendering += OnCircleRendering;
    }

    void StopAnimationCompositionTarget()
    {
        if (!_renderHooked) { return; }
        _renderHooked = false;
        if (SineWave)
            CompositionTarget.Rendering -= OnSineWaveRendering;
        else
            CompositionTarget.Rendering -= OnCircleRendering;
    }

    /// <summary>
    /// Keep each dot’s gradient fixed by moving the dots around the circle every frame. 
    /// This avoids rotating any gradients which can cause a wobble effect.
    /// </summary>
    void OnCircleRendering(object? sender, EventArgs e)
    {
        // 360 degrees per RotationDuration seconds
        double degPerSec = 360.0 / RotationDuration;

        // Use a steady clock
        _angle = (_angle + degPerSec * GetDeltaSeconds()) % 360.0;

        double radius = Math.Min(ActualWidth, ActualHeight) / 2 - DotSize;
        int count = PART_Canvas.Children.Count;

        for (int i = 0; i < count; i++)
        {
            double baseAngle = i * 360.0 / count;
            double a = (baseAngle + _angle) * Math.PI / 180.0;

            double x = radius * Math.Cos(a) + ActualWidth / 2 - DotSize / 2;
            double y = radius * Math.Sin(a) + ActualHeight / 2 - DotSize / 2;

            var dot = (UIElement)PART_Canvas.Children[i];
            Canvas.SetLeft(dot, x);
            Canvas.SetTop(dot, y);
        }
    }

    void OnSineWaveRendering(object? sender, EventArgs e)
    {
        double speed = ActualWidth / RotationDuration; // px/sec
        double delta = speed * GetDeltaSeconds();

        // Move the phase offset over time
        _angle = (_angle + delta) % ActualWidth;
        
        // Reverse direction
        //_angle = (_angle - delta) % ActualWidth;

        int count = PART_Canvas.Children.Count;
        double spacing = ActualWidth / count;

        for (int i = 0; i < count; i++)
        {
            double x = i * spacing;
            double phase = (x + _angle) / ActualWidth * WaveFrequency * 2 * Math.PI;
            double y = (ActualHeight - DotSize) / 2 + Math.Sin(phase) * WaveAmplitude;
            var dot = (UIElement)PART_Canvas.Children[i];
            Canvas.SetLeft(dot, x);
            Canvas.SetTop(dot, y);
        }
    }


    double _bounceOffset = 0d;
    bool _bounceForward = true;
    /// <summary>
    /// Shuffle from left to right and then back from right to left.
    /// </summary>
    void OnShuffleRendering(object sender, EventArgs e)
    {
        double speed = ActualWidth / RotationDuration; // px per second
        double delta = speed * GetDeltaSeconds();

        if (_bounceForward)
        {
            _bounceOffset += delta;
            if (_bounceOffset >= ActualWidth - DotSize)
                _bounceForward = false;
        }
        else
        {
            _bounceOffset -= delta;
            if (_bounceOffset <= 0)
                _bounceForward = true;
        }

        // Position dots in a line, staggered
        int count = PART_Canvas.Children.Count;
        double spacing = DotSize * 0.65;

        for (int i = 0; i < count; i++)
        {
            double x = _bounceOffset + i * spacing;
            double y = (ActualHeight - DotSize) / 2;

            var dot = (UIElement)PART_Canvas.Children[i];
            Canvas.SetLeft(dot, x % (ActualWidth - DotSize)); // wrap if needed
            Canvas.SetTop(dot, y);
        }
    }

    // Simple delta-time tracker
    DateTime _last = DateTime.MinValue;
    double GetDeltaSeconds()
    {
        var now = DateTime.UtcNow;
        if (_last == DateTime.MinValue) 
            _last = now;
        var dt = (now - _last).TotalSeconds;
        _last = now;
        return dt;
    }
    #endregion

}
