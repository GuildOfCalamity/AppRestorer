using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace AppRestorer.Controls;

/**
 **   🛠️🛠️ THE ULTIMATE REUSABLE SPINNER CONTROL 🛠️🛠️
 **
 **           Copyright © The Guild 2024-2025
 **/

public enum SpinnerRenderMode
{
    RotateCanvas,    // if using single color with dot circle (simpler mode, but less versatile)
    AnimatePositions // if using gradient brush and versatile animations/shapes
}

public enum SpinnerRenderShape
{
    Dots,   // for standard/classic spinner
    Polys,  // for spinner with more complex shapes
    Snow,   // for raining/snowing animation
    Wind,   // for horizontal animation
    Wave,   // for sine wave animation
    Space,  // for starfield animation
    Line,   // for line warp animation
    Stripe, // for exaggerated line animation
    Bounce, // for dot bouncing animation
}

/// <summary>
/// If mode is set to <see cref="SpinnerRenderMode.RotateCanvas"/> then some of<br/>
/// the more advanced animations will not render correctly, it's<br/>
/// recommended to keep the mode set to <see cref="SpinnerRenderMode.AnimatePositions"/><br/>
/// which employs the <see cref="CompositionTarget.Rendering"/> surface event.<br/>
/// Visibility determines if animation runs.
/// </summary>
/// <remarks>
/// Most render methods have their own data elements, however some are shared,<br/>
/// e.g. the Snow/Wind/Space modes all use the _rain arrays.<br/>
/// </remarks>
public partial class Spinner : UserControl
{
    bool hasAppliedTemplate = false;
    bool _renderHooked = false;
    double _angle = 0d;
    const double Tau = 2 * Math.PI;
    const double Epsilon = 0.000000000001;

    public int DotCount { get; set; } = 10;
    public double DotSize { get; set; } = 8;
    public Brush DotBrush { get; set; } = Brushes.DodgerBlue;
    public SpinnerRenderMode RenderMode { get; set; } = SpinnerRenderMode.AnimatePositions; // more versatile
    public SpinnerRenderShape RenderShape { get; set; } = SpinnerRenderShape.Wave;

    public Spinner()
    {
        InitializeComponent();
        Loaded += Spinner_Loaded;
        IsVisibleChanged += Spinner_IsVisibleChanged;
    }

    #region [Overrides]
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        hasAppliedTemplate = true;
        Debug.WriteLine($"[INFO] {nameof(Spinner)} template has been applied.");
    }

    protected override Size MeasureOverride(Size constraint)
    {
        // The width/height is used in render object calculations, so we must have some value.
        if (constraint.Width <= 0) { Width = 50; }
        if (constraint.Height <= 0) { Height = 50; }

        Debug.WriteLine($"[INFO] {nameof(Spinner)} is measured to be {constraint}");
        return base.MeasureOverride(constraint);
    }
    #endregion

    #region [Events]
    void Spinner_Loaded(object sender, RoutedEventArgs e)
    {
        if (RenderShape == SpinnerRenderShape.Dots || RenderShape == SpinnerRenderShape.Wave)
            CreateDots();
        else if (RenderShape == SpinnerRenderShape.Polys)
            CreatePolys();
        else if (RenderShape == SpinnerRenderShape.Snow || RenderShape == SpinnerRenderShape.Wind || RenderShape == SpinnerRenderShape.Space)
            CreateSnow();
        else if (RenderShape == SpinnerRenderShape.Line)
            CreateLines();
        else if (RenderShape == SpinnerRenderShape.Stripe)
            CreateStripe();
        else if (RenderShape == SpinnerRenderShape.Bounce)
            CreateBounce();
        else
            CreateDots();

        if (IsVisible)
        {
            if (RenderMode == SpinnerRenderMode.RotateCanvas)
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
            if (RenderMode == SpinnerRenderMode.RotateCanvas)
                StartAnimationStandard();
            else
                StartAnimationCompositionTarget();
        }
        else
        {
            if (RenderMode == SpinnerRenderMode.RotateCanvas)
                StopAnimationStandard();
            else
                StopAnimationCompositionTarget();
        }
    }
    #endregion

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
            Duration = TimeSpan.FromSeconds(WaveDuration), // RotationDuration
            RepeatBehavior = RepeatBehavior.Forever
        };
        rotate.BeginAnimation(RotateTransform.AngleProperty, anim);
    }

    void StopAnimationStandard()
    {
        PART_Canvas.RenderTransform?.BeginAnimation(RotateTransform.AngleProperty, null);
    }

    void StartAnimationCompositionTarget()
    {
        if (_renderHooked) { return; }
        _renderHooked = true;
        if (RenderShape == SpinnerRenderShape.Wave)
            CompositionTarget.Rendering += OnSineWaveRendering; // OnSpiralInOutRendering;
        else if (RenderShape == SpinnerRenderShape.Snow)
            CompositionTarget.Rendering += OnSnowRendering;
        else if (RenderShape == SpinnerRenderShape.Wind)
            CompositionTarget.Rendering += OnWindRendering;
        else if (RenderShape == SpinnerRenderShape.Space)
            CompositionTarget.Rendering += OnStarfieldRendering;
        else if (RenderShape == SpinnerRenderShape.Line)
            CompositionTarget.Rendering += OnLineRendering;
        else if (RenderShape == SpinnerRenderShape.Stripe)
            CompositionTarget.Rendering += OnStripeRendering;
        else if (RenderShape == SpinnerRenderShape.Bounce)
            CompositionTarget.Rendering += OnBounceRendering;
        else
            CompositionTarget.Rendering += OnCircleRendering;
    }

    void StopAnimationCompositionTarget()
    {
        if (!_renderHooked) { return; }
        _renderHooked = false;
        if (RenderShape == SpinnerRenderShape.Wave)
            CompositionTarget.Rendering -= OnSineWaveRendering; // OnSpiralInOutRendering;
        else if (RenderShape == SpinnerRenderShape.Snow)
            CompositionTarget.Rendering -= OnSnowRendering;
        else if (RenderShape == SpinnerRenderShape.Wind)
            CompositionTarget.Rendering -= OnWindRendering;
        else if (RenderShape == SpinnerRenderShape.Space)
            CompositionTarget.Rendering -= OnStarfieldRendering;
        else if (RenderShape == SpinnerRenderShape.Line)
            CompositionTarget.Rendering -= OnLineRendering;
        else if (RenderShape == SpinnerRenderShape.Stripe)
            CompositionTarget.Rendering -= OnStripeRendering;
        else if (RenderShape == SpinnerRenderShape.Bounce)
            CompositionTarget.Rendering -= OnBounceRendering;
        else
            CompositionTarget.Rendering -= OnCircleRendering;
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

    #region [Composition Rendering]

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
                Width = DotSize,
                Height = DotSize,
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

    /// <summary>
    /// Keep each dot’s gradient fixed by moving the dots around the circle every frame. 
    /// This avoids rotating any gradients which can cause a wobble effect.
    /// </summary>
    void OnCircleRendering(object? sender, EventArgs e)
    {
        // 360 degrees per RotationDuration seconds
        double degPerSec = 360.0 / WaveDuration;

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

    
    public double WaveDuration { get; set; } = 1.0; // seconds (A.K.A. Rotation Duration)
    public double WaveAmplitude { get; set; } = 14;     // pixels
    public double WaveFrequency { get; set; } = 1;      // cycles across width (shouldn't be less than 1)

    void OnSineWaveRendering(object? sender, EventArgs e)
    {
        double speed = ActualWidth / WaveDuration; // px/sec
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
            double phase = (x + _angle) / ActualWidth * WaveFrequency * Tau;
            double y = (ActualHeight - DotSize) / 2 + Math.Sin(phase) * WaveAmplitude;
            var dot = (UIElement)PART_Canvas.Children[i];
            Canvas.SetLeft(dot, x);
            Canvas.SetTop(dot, y);
        }
    }

    void OnSpiralRendering(object? sender, EventArgs e)
    {
        double dt = GetDeltaSeconds();
        _angle = (_angle + SpiralAngularSpeed * dt) % 360.0;

        int count = PART_Canvas.Children.Count;
        double centerX = ActualWidth / 2;
        double centerY = ActualHeight / 2;

        for (int i = 0; i < count; i++)
        {
            // Each dot’s time offset
            double t = i * 0.1 + _angle / SpiralAngularSpeed;

            // Spiral radius grows over time
            double radius = SpiralGrowthRate * t; // * 0.5;

            // Spiral angle
            double a = (SpiralAngularSpeed * t) * Math.PI / 180.0;
            double x = centerX + radius * Math.Cos(a) - DotSize / 2;
            double y = centerY + radius * Math.Sin(a) - DotSize / 2;

            var dot = (UIElement)PART_Canvas.Children[i];
            Canvas.SetLeft(dot, x);
            Canvas.SetTop(dot, y);
        }
    }

    // Spiral in/out phase tracker
    double _radiusPhase = 0.0;
    public double SpiralGrowthRate { get; set; } = 8;    // pixels/sec
    public double SpiralMaxRadius { get; set; } = 40;     // px
    public double SpiralAngularSpeed { get; set; } = 90; // deg/sec
    public double SpiralInOutSpeed { get; set; } = 0.75;   // cycles/sec

    void OnSpiralInOutRendering(object? sender, EventArgs e)
    {
        double dt = GetDeltaSeconds();

        // Angle for rotation
        _angle = (_angle + SpiralAngularSpeed * dt) % 360.0;

        // Phase for radius oscillation
        double phase = _radiusPhase + SpiralInOutSpeed * Tau * dt;
        _radiusPhase = phase;

        int count = PART_Canvas.Children.Count;
        double centerX = ActualWidth / 2;
        double centerY = ActualHeight / 2;

        for (int i = 0; i < count; i++)
        {
            // Offset phase per dot for staggered spiral arms
            double dotPhase = phase + i * (Math.PI / count);

            // Oscillating radius
            double radius = (SpiralMaxRadius / 2) * (1 + Math.Sin(dotPhase));

            // Dot angle offset
            double a = (_angle + i * (360.0 / count)) * Math.PI / 180.0;

            double x = centerX + radius * Math.Cos(a) - DotSize / 2;
            double y = centerY + radius * Math.Sin(a) - DotSize / 2;

            var dot = (UIElement)PART_Canvas.Children[i];
            Canvas.SetLeft(dot, x);
            Canvas.SetTop(dot, y);
        }
    }

    
    // Drift effect for snow/rain
    public double WindAmplitude { get; set; } = 5;   // max horizontal sway in px
    public double WindFrequency { get; set; } = 0.6; // cycles/sec
    public double WindBias { get; set; } = 2;        // constant drift px/sec

    public bool SnowSizeRandom { get; set; } = true;
    public double SnowBaseSpeed { get; set; } = 50;

    double[] _rainX;
    double[] _rainY;
    double[] _rainSpeed;
    double[] _rainPhase; // for wind sway offset
    double[] _rainSize;
    bool _fixedSnowSize = false;
    /// <summary>
    /// Creates an array of dots to apply wind/gravity pressure on.
    /// </summary>
    void CreateSnow()
    {
        if (PART_Canvas == null)
            return;

        _rainX = new double[DotCount];
        _rainY = new double[DotCount];
        _rainSpeed = new double[DotCount];
        _rainPhase = new double[DotCount];
        _rainSize = new double[DotCount];

        PART_Canvas.Children.Clear();

        for (int i = 0; i < DotCount; i++)
        {
            _rainX[i] = Random.Shared.NextDouble() * (ActualWidth - DotSize);
            _rainY[i] = Random.Shared.NextDouble() * ActualHeight;            // start at random vertical position
            _rainSpeed[i] = SnowBaseSpeed + Random.Shared.NextDouble() * 50;  // px/sec
            _rainPhase[i] = Random.Shared.NextDouble() * Tau;                 // random sway/drift start
            _rainSize[i] = SnowSizeRandom ? 1 + Random.Shared.NextDouble() * DotSize : DotSize;

            var dot = new Ellipse
            {
                Width = _rainSize[i],
                Height = _rainSize[i],
                Fill = DotBrush,
                Opacity = Random.Shared.NextDouble() + 0.09, // random opacity
                //Opacity = (double)i / DotCount, // ⇦ use this to fade each consecutive dot
            };

            Canvas.SetLeft(dot, _rainX[i]);
            Canvas.SetTop(dot, _rainY[i]);
            PART_Canvas.Children.Add(dot);
        }
    }


    void OnSnowRendering(object? sender, EventArgs e)
    {
        if (_rainX == null || _rainY == null) { return; }

        double dt = GetDeltaSeconds();

        for (int i = 0; i < DotCount; i++)
        {
            // move down
            _rainY[i] += _rainSpeed[i] * dt;

            if (_rainY[i] > ActualHeight)
            {
                // Re-spawn at top
                _rainY[i] = -DotSize;
                _rainX[i] = Random.Shared.NextDouble() * (ActualWidth - DotSize);
                _rainSpeed[i] = SnowBaseSpeed + Random.Shared.NextDouble() * 50;
                _rainPhase[i] = Random.Shared.NextDouble() * Tau;
            }

            // Advance sway phase
            _rainPhase[i] += WindFrequency * Tau * dt;

            // Horizontal sway + bias
            double sway = Math.Sin(_rainPhase[i]) * WindAmplitude;
            double x = _rainX[i] + sway + WindBias * (_rainY[i] / ActualHeight);

            //var dot = (Ellipse)PART_Canvas.Children[i]; // assumes the Canvas contains Ellipse elements
            var dot = (UIElement)PART_Canvas.Children[i];

            //Canvas.SetLeft(dot, _rainX[i]); // ⇦ use this if you want no sway/drift
            Canvas.SetLeft(dot, x); // place the sway/drift + bias
            Canvas.SetTop(dot, _rainY[i]);
        }
    }

    void OnWindRendering(object? sender, EventArgs e)
    {
        if (_rainX == null || _rainY == null) { return; }

        double dt = GetDeltaSeconds();

        for (int i = 0; i < DotCount; i++)
        {
            // move right
            _rainX[i] += _rainSpeed[i] * dt;

            if (_rainX[i] > ActualWidth)
            {
                // Re-spawn at side
                _rainY[i] = Random.Shared.NextDouble() * (ActualHeight - DotSize);
                _rainX[i] = -DotSize;
                _rainSpeed[i] = SnowBaseSpeed + Random.Shared.NextDouble() * 50;
                _rainPhase[i] = Random.Shared.NextDouble() * Tau;

            }

            // Advance sway/drift phase
            _rainPhase[i] += WindFrequency * Tau * dt;

            // Horizontal sway/drift + bias
            double sway = Math.Sin(_rainPhase[i]) * WindAmplitude;
            double x = _rainY[i] + sway + WindBias * (_rainX[i] / ActualWidth);

            //var dot = (Ellipse)PART_Canvas.Children[i]; // assumes the Canvas contains Ellipse elements
            var dot = (UIElement)PART_Canvas.Children[i];

            Canvas.SetLeft(dot, _rainX[i]);
            //Canvas.SetTop(dot, _rainY[i]); // ⇦ use this if you want no sway/drift
            Canvas.SetTop(dot, x); // place the sway/drift + bias
        }
    }

    void OnStarfieldRendering(object? sender, EventArgs e)
    {
        if (_rainX == null || _rainY == null) { return; }

        double dt = GetDeltaSeconds();
        double centerX = ActualWidth / 2;
        double centerY = ActualHeight / 2;

        for (int i = 0; i < DotCount; i++)
        {
            var dot = (Ellipse)PART_Canvas.Children[i]; // assumes the Canvas contains Ellipse elements

            // Direction vector from center
            double dx = _rainX[i] - centerX;
            double dy = _rainY[i] - centerY;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            // Normalize direction
            if (dist == 0) 
                dist = 0.0001;
            dx /= dist;
            dy /= dist;

            // Move outward
            _rainX[i] += dx * _rainSpeed[i] * dt;
            _rainY[i] += dy * _rainSpeed[i] * dt;

            // Scale size based on distance
            double scale = 1 + dist / (ActualWidth / 2);
            dot.Width = _rainSize[i] * scale;
            dot.Height = _rainSize[i] * scale;
            dot.Opacity = Math.Min(1.0, 0.4 + dist / (ActualWidth / 2));

            Canvas.SetLeft(dot, _rainX[i] - dot.Width / 2);
            Canvas.SetTop(dot, _rainY[i] - dot.Height / 2);

            // Re-spawn immediately when out of bounds
            if (_rainX[i] < -DotSize || _rainX[i] > ActualWidth + DotSize ||
                _rainY[i] < -DotSize || _rainY[i] > ActualHeight + DotSize)
            {
                double angle = Random.Shared.NextDouble() * Tau;
                _rainX[i] = centerX + Math.Cos(angle) * 2; // small offset so they don't overlap exactly
                _rainY[i] = centerY + Math.Sin(angle) * 2;
                _rainSpeed[i] = SnowBaseSpeed + Random.Shared.NextDouble() * 100;
                _rainSize[i] = 1 + Random.Shared.NextDouble() * DotSize;
            }
        }
    }


    double[] _starX;
    double[] _starY;
    double[] _starSpeed;
    double[] _starSize;
    double[] _starDirX;
    double[] _starDirY;
    /// <summary>
    /// Creates an array of dots to apply wind/gravity pressure on.
    /// </summary>
    void CreateLines()
    {
        if (PART_Canvas == null)
            return;

        _starX = new double[DotCount];
        _starY = new double[DotCount];
        _starSpeed = new double[DotCount];
        _starSize = new double[DotCount];
        _starDirX = new double[DotCount];
        _starDirY = new double[DotCount];

        PART_Canvas.Children.Clear();

        double centerX = ActualWidth / 2;
        double centerY = ActualHeight / 2;

        for (int i = 0; i < DotCount; i++)
        {
            double angle = Random.Shared.NextDouble() * Tau;
            _starX[i] = centerX;
            _starY[i] = centerY;
            _starDirX[i] = Math.Cos(angle);
            _starDirY[i] = Math.Sin(angle);
            _starSpeed[i] = 30 + Random.Shared.NextDouble() * 50;
            _starSize[i] = 1 + Random.Shared.NextDouble() * DotSize;

            var streak = new Line
            {
                Stroke = DotBrush,
                //Fill = new SolidColorBrush(Colors.SpringGreen),
                StrokeThickness = _starSize[i]/4,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Opacity = (double)i / DotCount // fade each consecutive
            };

            PART_Canvas.Children.Add(streak);
        }
    }

    public double LineBaseSpeed { get; set; } = 50;
    void OnLineRendering(object? sender, EventArgs e)
    {
        if (_starX == null || _starY == null) { return; }

        double dt = GetDeltaSeconds();
        double centerX = ActualWidth / 2;
        double centerY = ActualHeight / 2;

        for (int i = 0; i < DotCount; i++)
        {
            var streak = (Line)PART_Canvas.Children[i];

            // Move outward
            _starX[i] += _starDirX[i] * _starSpeed[i] * dt;
            _starY[i] += _starDirY[i] * _starSpeed[i] * dt;

            // Distance from center
            double dist = Math.Sqrt(Math.Pow(_starX[i] - centerX, 2) + Math.Pow(_starY[i] - centerY, 2));

            // Streak length scales with distance
            double length = dist * 0.2; // tweak multiplier for effect

            // End point is current position
            streak.X2 = _starX[i];
            streak.Y2 = _starY[i];

            // Start point is behind along velocity vector
            streak.X1 = _starX[i] - _starDirX[i] * length;
            streak.Y1 = _starY[i] - _starDirY[i] * length;

            // Opacity increases with distance
            streak.Opacity = Math.Min(1.0, 0.4 + dist / (ActualWidth / 2));

            // Re-spawn when out of bounds
            if (_starX[i] < -DotSize || _starX[i] > ActualWidth + DotSize ||
                _starY[i] < -DotSize || _starY[i] > ActualHeight + DotSize)
            {
                double angle = Random.Shared.NextDouble() * Tau;
                _starX[i] = centerX;
                _starY[i] = centerY;
                _starDirX[i] = Math.Cos(angle);
                _starDirY[i] = Math.Sin(angle);
                _starSpeed[i] = LineBaseSpeed + Random.Shared.NextDouble() * 50;
                _starSize[i] = 1 + Random.Shared.NextDouble() * DotSize;
                streak.StrokeThickness = _starSize[i]/4;
            }
        }
    }

    /// <summary>
    /// Creates an array of lines to apply horizontal movement on.
    /// </summary>
    void CreateStripe()
    {
        if (PART_Canvas == null)
            return;

        _starX = new double[DotCount];
        _starY = new double[DotCount];
        _starSpeed = new double[DotCount];
        _starSize = new double[DotCount];
        _starDirX = new double[DotCount];
        _starDirY = new double[DotCount];

        PART_Canvas.Children.Clear();

        double centerX = ActualWidth / 2;
        double centerY = ActualHeight / 2;

        for (int i = 0; i < DotCount; i++)
        {
            double angle = Random.Shared.NextDouble() * Tau;
            _starX[i] = Random.Shared.NextDouble() * (ActualWidth - DotSize);
            _starY[i] = Random.Shared.NextDouble() * (ActualHeight - DotSize);  // start at random vertical position

            _starDirX[i] = Math.Cos(angle); // not needed
            _starDirY[i] = Math.Sin(angle); // not needed
            _starSpeed[i] = StripeBaseSpeed + Random.Shared.NextDouble() * 100;
            _starSize[i] = 1 + Random.Shared.NextDouble() * DotSize;

            var streak = new Line
            {
                Stroke = DotBrush,
                X1 = _starX[i] * -0.05, // start outside left-most
                X2 = _starX[i] * 0.05,  // end outside right-most
                Y1 = _starY[i] / (DotSize), 
                Y2 = _starY[i] / (DotSize),
                StrokeThickness = _starSize[i] / 2.5,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Opacity = StripeLowOpacity ? RandomLowOpacity() : Random.Shared.NextDouble() + 0.09,
                //Fill = new SolidColorBrush(Colors.SpringGreen),
            };

            PART_Canvas.Children.Add(streak);
        }
    }

    public double StripeBaseSpeed { get; set; } = 50;
    public bool StripeLowOpacity { get; set; } = false; // for subtle backgrounds

    void OnStripeRendering(object? sender, EventArgs e)
    {
        if (_starX == null || _starY == null) { return; }

        double dt = GetDeltaSeconds();

        for (int i = 0; i < DotCount; i++)
        {
            // move from left to right
            _starX[i] += _starSpeed[i] * dt;

            if (_starX[i] > (ActualWidth + 2))
            {
                // Re-spawn at side
                _starY[i] = Random.Shared.NextDouble() * (ActualHeight - DotSize);
                _starX[i] = -DotSize;
                _starSpeed[i] = StripeBaseSpeed + Random.Shared.NextDouble() * 100;
            }

            var line = (UIElement)PART_Canvas.Children[i];
            Canvas.SetLeft(line, _starX[i]);
            Canvas.SetTop(line, _starY[i]);
        }
    }

    double[] _dotX;
    double[] _dotY;
    double[] _dotVX;
    double[] _dotVY;
    double[] _dotSize;
    /// <summary>
    /// Creates an array of dots to apply wind/gravity pressure on.
    /// </summary>
    void CreateBounce()
    {
        if (PART_Canvas == null)
            return;

        _dotX = new double[DotCount];
        _dotY = new double[DotCount];
        _dotVX = new double[DotCount];
        _dotVY = new double[DotCount];
        _dotSize = new double[DotCount];

        PART_Canvas.Children.Clear();

        for (int i = 0; i < DotCount; i++)
        {
            _dotX[i] = Random.Shared.NextDouble() * (ActualWidth - DotSize);
            _dotY[i] = Random.Shared.NextDouble() * (ActualHeight - DotSize);
            if (BounceSizeRandom)
                _dotSize[i] = 2 + Random.Shared.NextDouble() * DotSize;
            else
                _dotSize[i] = DotSize;

            // Random velocity between -BounceSpeed and +BounceSpeed px/sec
            _dotVX[i] = RandomSwing(BounceSpeed); // (Random.Shared.NextDouble() * 200 - 100);
            _dotVY[i] = RandomSwing(BounceSpeed); // (Random.Shared.NextDouble() * 200 - 100);

            var dot = new Ellipse
            {
                Width = _dotSize[i],
                Height = _dotSize[i],
                Fill = DotBrush,
                Opacity = Random.Shared.NextDouble() + 0.09,
            };

            Canvas.SetLeft(dot, _dotX[i]);
            Canvas.SetTop(dot, _dotY[i]);
            PART_Canvas.Children.Add(dot);
        }
    }

    public bool BounceSizeRandom { get; set; } = false;
    public bool BounceCollisions { get; set; } = true;
    public double BounceSpeed { get; set; } = 80;
    void OnBounceRendering(object? sender, EventArgs e)
    {
        if (_dotX == null || _dotY == null) { return; }

        // Restitution coefficient (0 to 1) makes collisions less bouncy.
        // Any values less than 1 will slowly absorb energy from the system
        // events over time, so the dots will eventually just slowly drift.
        double restitution = 1.0;

        double dt = GetDeltaSeconds();

        // Move dots
        for (int i = 0; i < DotCount; i++)
        {
            _dotX[i] += _dotVX[i] * dt;
            _dotY[i] += _dotVY[i] * dt;

            // Bounce off left/right walls
            if (_dotX[i] <= 0)
            {
                _dotX[i] = 0;
                _dotVX[i] = Math.Abs(_dotVX[i]) * restitution; // force rightward and apply restitution/friction
            }
            else if (_dotX[i] >= ActualWidth - DotSize)
            {
                _dotX[i] = ActualWidth - DotSize;
                _dotVX[i] = -Math.Abs(_dotVX[i]) * restitution; // force leftward and apply restitution/friction
            }

            // Bounce off top/bottom walls
            if (_dotY[i] <= 0)
            {
                _dotY[i] = 0;
                _dotVY[i] = Math.Abs(_dotVY[i]) * restitution; // force downward and apply restitution/friction
            }
            else if (_dotY[i] >= ActualHeight - DotSize)
            {
                _dotY[i] = ActualHeight - DotSize;
                _dotVY[i] = -Math.Abs(_dotVY[i]) * restitution; // force upward and apply restitution/friction
            }
        }

        // Handle collisions between dots
        if (BounceCollisions)
        {
            // If the time between frames is large, relative to the dot's speed, two dots
            // can "tunnel" through each other, they overlap deeply before we detect the
            // collision, or even skip past each other entirely. This can cause sticking,
            // jitter, or unnatural pushes. If sub-stepping is preferred then instead of
            // doing one big update per frame, we could break the frame's dt into smaller
            // slices and run multiple collision checks/updates.

            #region [Standard collision technique]
            for (int i = 0; i < DotCount; i++)
            {
                for (int j = i + 1; j < DotCount; j++)
                {
                    double dx = _dotX[j] - _dotX[i];
                    double dy = _dotY[j] - _dotY[i];
                    double distSq = dx * dx + dy * dy;
                    double minDist = DotSize;
            
                    if (distSq < minDist * minDist && distSq > Epsilon)
                    {
                        double dist = Math.Sqrt(distSq);
            
                        // Normal vector
                        double nx = dx / dist;
                        double ny = dy / dist;
            
                        // Tangent vector
                        double tx = -ny;
                        double ty = nx;
            
                        // Project velocities onto normal and tangent
                        double v1n = _dotVX[i] * nx + _dotVY[i] * ny;
                        double v1t = _dotVX[i] * tx + _dotVY[i] * ty;
                        double v2n = _dotVX[j] * nx + _dotVY[j] * ny;
                        double v2t = _dotVX[j] * tx + _dotVY[j] * ty;
            
                        // Swap normal components (equal mass, elastic)
                        double v1nAfter = v2n * restitution;
                        double v2nAfter = v1n * restitution;
            
                        // Recombine
                        _dotVX[i] = v1nAfter * nx + v1t * tx;
                        _dotVY[i] = v1nAfter * ny + v1t * ty;
                        _dotVX[j] = v2nAfter * nx + v2t * tx;
                        _dotVY[j] = v2nAfter * ny + v2t * ty;
            
                        // Minimum Translation Vector to separate them
                        double overlap = 0.5 * (minDist - dist);
                        _dotX[i] -= overlap * nx;
                        _dotY[i] -= overlap * ny;
                        _dotX[j] += overlap * nx;
                        _dotY[j] += overlap * ny;
                    }
                }
            }
            #endregion

            #region [Collision resolution with friction & restitution]
            /** This creates a "push each other out of the way" effect **/
            //double grip = 0.9; // tangential friction
            //for (int i = 0; i < DotCount; i++)
            //{
            //    for (int j = i + 1; j < DotCount; j++)
            //    {
            //        double dx = _dotX[j] - _dotX[i];
            //        double dy = _dotY[j] - _dotY[i];
            //        double minDist = DotSize;
            //        double distSq = dx * dx + dy * dy;
            //
            //        if (distSq < minDist * minDist && distSq > Epsilon)
            //        {
            //            double dist = Math.Sqrt(distSq);
            //
            //            // Normal and tangent
            //            double nx = dx / dist;
            //            double ny = dy / dist;
            //            double tx = -ny;
            //            double ty = nx;
            //
            //            // Overlap separation (MTV)
            //            double overlap = 0.5 * (minDist - dist);
            //            _dotX[i] -= overlap * nx;
            //            _dotY[i] -= overlap * ny;
            //            _dotX[j] += overlap * nx;
            //            _dotY[j] += overlap * ny;
            //
            //            // Project velocities
            //            double v1n = _dotVX[i] * nx + _dotVY[i] * ny;
            //            double v1t = _dotVX[i] * tx + _dotVY[i] * ty;
            //            double v2n = _dotVX[j] * nx + _dotVY[j] * ny;
            //            double v2t = _dotVX[j] * tx + _dotVY[j] * ty;
            //
            //            // Only resolve if approaching along the normal
            //            double relApproach = (v1n - v2n);
            //            if (relApproach < 0)
            //            {
            //                // Equal mass elastic exchange with restitution
            //                double v1nAfter = v2n * restitution;
            //                double v2nAfter = v1n * restitution;
            //
            //                // Apply tangential friction
            //                double v1tAfter = v1t * grip;
            //                double v2tAfter = v2t * grip;
            //
            //                // Recombine
            //                _dotVX[i] = v1nAfter * nx + v1tAfter * tx;
            //                _dotVY[i] = v1nAfter * ny + v1tAfter * ty;
            //                _dotVX[j] = v2nAfter * nx + v2tAfter * tx;
            //                _dotVY[j] = v2nAfter * ny + v2tAfter * ty;
            //            }
            //        }
            //    }
            //}
            #endregion
        }

        // Update visuals
        for (int i = 0; i < DotCount; i++)
        {
            var dot = (UIElement)PART_Canvas.Children[i];
            Canvas.SetLeft(dot, _dotX[i]);
            Canvas.SetTop(dot, _dotY[i]);
        }
    }


    double _bounceOffset = 0d;
    bool _bounceForward = true;
    /// <summary>
    /// Shuffle from left to right and then back from right to left.
    /// </summary>
    void OnShuffleRendering(object sender, EventArgs e)
    {
        double speed = ActualWidth / WaveDuration; // px per second (RotationDuration)
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

    #endregion

    DateTime _last = DateTime.MinValue;
    /// <summary>
    /// A simple delta-time tracker.
    /// </summary>
    /// <returns>
    /// How much time has elapsed since the last check.
    /// </returns>
    double GetDeltaSeconds()
    {
        var now = DateTime.UtcNow;
        if (_last == DateTime.MinValue || _last == DateTime.MaxValue) 
            _last = now;
        var dt = (now - _last).TotalSeconds;
        _last = now;
        return dt;
    }

    #region [Random Helpers]
    /// <summary>
    /// <see cref="Random.Shared"/>.NextDouble() gives [0.000 to 0.999], so scale to [-value to +value]
    /// </summary>
    /// <returns>negative <paramref name="value"/> to positive <paramref name="value"/></returns>
    static double RandomSwing(double value)
    {
        double factor = Random.Shared.NextDouble() * 2.0 - 1.0;
        return value * factor;
    }

    /// <summary>
    /// <see cref="Random.Shared"/>.Next() gives [min to max], so scale to [-value to +value]
    /// </summary>
    /// <returns>negative <paramref name="value"/> to positive <paramref name="value"/></returns>
    static int RandomSwing(int value)
    {
        // Returns a random int in [-value, +value]
        return Random.Shared.Next(-value, value + 1);
    }

    /// <summary>
    /// Returns a random opacity value between 0.1 and 0.41 (inclusive of 0.1, exclusive of 0.41).
    /// </summary>
    static double RandomLowOpacity()
    {
        return 0.1 + Random.Shared.NextDouble() * (0.41 - 0.1);
    }

    /// <summary>
    /// Returns a random opacity value between 0.5 and 0.99 (inclusive of 0.5, exclusive of 0.99).
    /// </summary>
    static double RandomHighOpacity()
    {
        return 0.1 + Random.Shared.NextDouble() * (0.99 - 0.5);
    }


    /// <summary>
    /// Returns a normally distributed random number using Box-Muller.
    /// mean = 0, stdDev = 1 by default.
    /// <code>
    ///   var noise = RandomGaussian(0, 10); // e.g. -6.2
    /// </code>
    /// </summary>
    static double RandomGaussian(double mean = 0, double stdDev = 1)
    {
        double u1 = 1.0 - Random.Shared.NextDouble(); // avoid 0
        double u2 = 1.0 - Random.Shared.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); // Box-Muller transform
        return mean + stdDev * randStdNormal;
    }

    /// <summary>
    /// Returns a Gaussian random clamped to [-maxAbs, +maxAbs].
    /// <code>
    ///   var clamped = RandomGaussianClamped(0, 20, 100); // e.g. +87.5
    /// </code>
    /// </summary>
    static double RandomGaussianClamped(double mean, double stdDev, double maxAbs)
    {
        double value = RandomGaussian(mean, stdDev);
        // Hard clamp if outside
        if (value > maxAbs) { return maxAbs; }
        if (value < -maxAbs) { return -maxAbs; }
        return value;
    }

    /// <summary>
    /// Returns a Gaussian random number, retrying until it falls within [-maxAbs, +maxAbs].
    /// Preserves the bell-curve distribution without flattening at the edges.
    /// <code>
    ///   var clamped = RandomGaussianBounded(0, 10, 50); // e.g. +24.1
    /// </code>
    /// </summary>
    static double RandomGaussianBounded(double mean, double stdDev, double maxAbs)
    {
        double value;
        // Retry until inside (no hard clamping)
        do { value = RandomGaussian(mean, stdDev); } 
        while (value < -maxAbs || value > maxAbs);
        return value;
    }

    /// <summary>
    /// Returns a Gaussian random number with directional bias.
    /// Bias > 0 skews right (positive), Bias < 0 skews left (negative).
    /// Bias magnitude ~0.0–1.0 (0 = no bias, 1 = strong bias).
    /// <code>
    ///   var biased = RandomGaussianBiased(0, 10, -0.3);
    /// </code>
    /// </summary>
    static double RandomGaussianBiased(double mean, double stdDev, double bias)
    {
        // Base Gaussian
        double g = RandomGaussian(mean, stdDev);

        // Apply bias: shift distribution toward one side
        // Bias is scaled by stdDev so it feels proportional
        double shift = bias * stdDev;

        return g + shift;
    }
    #endregion
}
