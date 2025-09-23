using System;
using System.Reflection.Metadata;
using System.Windows;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using static System.Formats.Asn1.AsnWriter;


namespace AppRestorer.Controls;

public enum RenderMode
{
    RotateCanvas,    // if using single color with dot circle
    AnimatePositions // if using gradient brush and versatile animation shape
}

public enum RenderShape
{
    Dots,  // for standard spinner
    Polys, // for spinner with more complex shapes
    Snow,  // for raining animation
    Wind,  // for horizontal animation
    Wave,  // for sine wave animation
    Space, // for starfield animation
    Line,  // for line warp animation
    Test   // RFU
}

/// <summary>
/// If mode is set to <see cref="RenderMode.RotateCanvas"/> then some of<br/>
/// the more advanced animations will not render correctly, it's<br/>
/// recommended to keep the mode set to <see cref="RenderMode.AnimatePositions"/><br/>
/// which employs the <see cref="CompositionTarget.Rendering"/> surface event.<br/>
/// </summary>
/// <remarks>
/// Visibility determines if animation runs.
/// </remarks>
public partial class Spinner : UserControl
{
    const double Tau = 2 * Math.PI;
    public int DotCount { get; set; } = 10;
    public double DotSize { get; set; } = 8;
    public Brush DotBrush { get; set; } = Brushes.DodgerBlue;
    public RenderMode Mode { get; set; } = RenderMode.AnimatePositions; // more versatile
    public RenderShape Shape { get; set; } = RenderShape.Wave;

    public Spinner()
    {
        InitializeComponent();
        Loaded += Spinner_Loaded;
        IsVisibleChanged += Spinner_IsVisibleChanged;
    }

    void Spinner_Loaded(object sender, RoutedEventArgs e)
    {
        if (Shape == RenderShape.Dots || Shape == RenderShape.Wave)
            CreateDots();
        else if (Shape == RenderShape.Polys)
            CreatePolys();
        else if (Shape == RenderShape.Snow || Shape == RenderShape.Wind || Shape == RenderShape.Space)
            CreateSnow();
        else if (Shape == RenderShape.Line)
            CreateLines();
        else if (Shape == RenderShape.Test)
            CreateTest();
        else
            CreateDots();

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

    double _angle = 0d;
    bool _renderHooked = false;

    void StartAnimationCompositionTarget()
    {
        if (_renderHooked) { return; }
        _renderHooked = true;
        if (Shape == RenderShape.Wave)
            CompositionTarget.Rendering += OnSineWaveRendering; // OnSpiralInOutRendering;
        else if (Shape == RenderShape.Snow)
            CompositionTarget.Rendering += OnSnowRendering;
        else if (Shape == RenderShape.Wind)
            CompositionTarget.Rendering += OnWindRendering;
        else if (Shape == RenderShape.Space)
            CompositionTarget.Rendering += OnStarfieldRendering;
        else if (Shape == RenderShape.Line)
            CompositionTarget.Rendering += OnLineRendering;
        else if (Shape == RenderShape.Test)
            CompositionTarget.Rendering += OnTestRendering;
        else
            CompositionTarget.Rendering += OnCircleRendering;
    }

    void StopAnimationCompositionTarget()
    {
        if (!_renderHooked) { return; }
        _renderHooked = false;
        if (Shape == RenderShape.Wave)
            CompositionTarget.Rendering -= OnSineWaveRendering; // OnSpiralInOutRendering;
        else if (Shape == RenderShape.Snow)
            CompositionTarget.Rendering -= OnSnowRendering;
        else if (Shape == RenderShape.Wind)
            CompositionTarget.Rendering -= OnWindRendering;
        else if (Shape == RenderShape.Space)
            CompositionTarget.Rendering -= OnStarfieldRendering;
        else if (Shape == RenderShape.Line)
            CompositionTarget.Rendering -= OnLineRendering;
        else if (Shape == RenderShape.Test)
            CompositionTarget.Rendering -= OnTestRendering;
        else
            CompositionTarget.Rendering -= OnCircleRendering;
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
        _rainSize = new double[DotCount]; // for starfield only

        PART_Canvas.Children.Clear();

        for (int i = 0; i < DotCount; i++)
        {
            _rainX[i] = Random.Shared.NextDouble() * (ActualWidth - DotSize);
            _rainY[i] = Random.Shared.NextDouble() * ActualHeight;    // start at random vertical position
            _rainSpeed[i] = 50 + Random.Shared.NextDouble() * 100;    // px/sec
            _rainPhase[i] = Random.Shared.NextDouble() * Tau;         // random sway/drift start
            _rainSize[i] = 1 + Random.Shared.NextDouble() * (DotSize / 2);

            var dot = new Ellipse
            {
                Width = DotSize,
                Height = DotSize,
                Fill = DotBrush,
                Opacity = (double)i / DotCount // fade each consecutive dot
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
                _rainSpeed[i] = 50 + Random.Shared.NextDouble() * 100;
                _rainPhase[i] = Random.Shared.NextDouble() * Tau;

            }

            // Advance sway phase
            _rainPhase[i] += WindFrequency * Tau * dt;

            // Horizontal sway + bias
            double sway = Math.Sin(_rainPhase[i]) * WindAmplitude;
            double x = _rainX[i] + sway + WindBias * (_rainY[i] / ActualHeight);

            //var dot = (UIElement)PART_Canvas.Children[i];
            var dot = (Ellipse)PART_Canvas.Children[i]; // assumes the Canvas contains Ellipse elements

            if (!_fixedSnowSize)
            {
                // Mix up the particle size
                dot.Width = _rainSize[i] * 2;
                dot.Height = _rainSize[i] * 2;
            }

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
                _rainSpeed[i] = 50 + Random.Shared.NextDouble() * 100;
                _rainPhase[i] = Random.Shared.NextDouble() * Tau;

            }

            // Advance sway/drift phase
            _rainPhase[i] += WindFrequency * Tau * dt;

            // Horizontal sway/drift + bias
            double sway = Math.Sin(_rainPhase[i]) * WindAmplitude;
            double x = _rainY[i] + sway + WindBias * (_rainX[i] / ActualWidth);


            //var dot = (UIElement)PART_Canvas.Children[i];
            var dot = (Ellipse)PART_Canvas.Children[i]; // assumes the Canvas contains Ellipse elements

            if (!_fixedSnowSize)
            {
                // Mix up the particle size
                dot.Width = _rainSize[i] * 2;
                dot.Height = _rainSize[i] * 2;
            }

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
                _rainSpeed[i] = 50 + Random.Shared.NextDouble() * 150;
                _rainSize[i] = 1 + Random.Shared.NextDouble() * (DotSize / 2);
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
                _starSpeed[i] = 30 + Random.Shared.NextDouble() * 50;
                _starSize[i] = 1 + Random.Shared.NextDouble() * DotSize;
                streak.StrokeThickness = _starSize[i]/4;
            }
        }
    }

    /// <summary>
    /// Creates an array of dots to apply wind/gravity pressure on.
    /// </summary>
    void CreateTest()
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
            //_starX[i] = centerX; _starY[i] = centerY;
            _starX[i] = Random.Shared.NextDouble() * (ActualWidth - DotSize);
            _starY[i] = Random.Shared.NextDouble() * ActualHeight;    // start at random vertical position

            _starDirX[i] = Math.Cos(angle);
            _starDirY[i] = Math.Sin(angle);
            _starSpeed[i] = 30 + Random.Shared.NextDouble() * 50;
            _starSize[i] = 10 + Random.Shared.NextDouble() * DotSize;

            var streak = new Line
            {
                Stroke = DotBrush,
                X1 = _starX[i], X2 = _starX[i]* 1.111, 
                Y1 = _starX[i]/ DotSize, Y2 = _starX[i]/ DotSize,
                //Width = _starSize[i] * 12,
                //Height = DotSize / 2,
                Fill = new SolidColorBrush(Colors.SpringGreen),
                StrokeThickness = _starSize[i]/2,
                StrokeStartLineCap = PenLineCap.Square,
                StrokeEndLineCap = PenLineCap.Square,
                Opacity = (double)i / DotCount // fade each consecutive
            };

            PART_Canvas.Children.Add(streak);
        }
    }

    void OnTestRendering(object? sender, EventArgs e)
    {
        if (_starX == null || _starY == null) { return; }

        double dt = GetDeltaSeconds();

        for (int i = 0; i < DotCount; i++)
        {
            // move right
            _starX[i] += _starSpeed[i] * dt;

            if (_starX[i] > ActualWidth)
            {
                // Re-spawn at side
                _starY[i] = Random.Shared.NextDouble() * (ActualHeight - DotSize);
                _starX[i] = -DotSize;
                _starSpeed[i] = 50 + Random.Shared.NextDouble() * 100;
            }

            //var dot = (UIElement)PART_Canvas.Children[i];
            var line = (Line)PART_Canvas.Children[i]; // assumes the Canvas contains Line elements

            //if (!_fixedSnowSize)
            //{
            //    // Mix up the particle size
            //    line.Width = _starSize[i] * 10;
            //    line.Height = _starSize[i] * 2;
            //}

            Canvas.SetLeft(line, _starX[i]);
            Canvas.SetTop(line, _starY[i]);
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
}
