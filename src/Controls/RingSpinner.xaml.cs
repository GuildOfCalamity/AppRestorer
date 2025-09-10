using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AppRestorer.Controls;

public partial class RingSpinner : UserControl
{
    bool _hasAppliedTemplate = false;

    /// <summary>
    ///   Define our rectangle height property
    /// </summary>
    public static readonly DependencyProperty HeightProperty = DependencyProperty.Register(
        nameof(Height),
        typeof(double),
        typeof(RingSpinner),
        new PropertyMetadata(50d, OnHeightChanged));
    public double Height
    {
        get { return (double)this.GetValue(HeightProperty); }
        set { this.SetValue(HeightProperty, value); }
    }
    static void OnHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var rs = d as RingSpinner;
        if (rs != null && e.NewValue != null)
        {
            Debug.WriteLine($"[INFO] Height changed to {e.NewValue}");
        }
    }

    /// <summary>
    ///   Define our rectangle width property
    /// </summary>
    public static readonly DependencyProperty WidthProperty = DependencyProperty.Register(
        nameof(Width),
        typeof(double),
        typeof(RingSpinner),
        new PropertyMetadata(50d, OnWidthChanged));
    public double Width
    {
        get { return (double)this.GetValue(WidthProperty); }
        set { this.SetValue(WidthProperty, value); }
    }
    static void OnWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var rs = d as RingSpinner;
        if (rs != null && e.NewValue != null)
        {
            Debug.WriteLine($"[INFO] Width changed to {e.NewValue}");
        }
    }

    /// <summary>
    ///   Define our stroke thickness property
    /// </summary>
    public static readonly DependencyProperty RingThicknessProperty = DependencyProperty.Register(
        nameof(RingThickness),
        typeof(double),
        typeof(RingSpinner),
        new PropertyMetadata(6d, OnRingThicknessChanged));
    public double RingThickness
    {
        get { return (double)this.GetValue(RingThicknessProperty); }
        set { this.SetValue(RingThicknessProperty, value); }
    }
    static void OnRingThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var rs = d as RingSpinner;
        if (rs != null && e.NewValue != null)
        {
            Debug.WriteLine($"[INFO] RingThickness changed to {e.NewValue}");
        }
    }

    /// <summary>
    ///   Define our ring brush property
    /// </summary>
    public static readonly DependencyProperty RingBrushProperty = DependencyProperty.Register(
        nameof(RingBrush),
        typeof(Brush),
        typeof(RingSpinner),
        new PropertyMetadata(new SolidColorBrush(Colors.DodgerBlue)));
    public Brush RingBrush
    {
        get { return (Brush)this.GetValue(RingBrushProperty); }
        set { this.SetValue(RingBrushProperty, value); }
    }

    /// <summary>
    ///   Define our width vs height ratio property
    /// </summary>
    public static readonly DependencyProperty RingRatioProperty = DependencyProperty.Register(
        nameof(RingRatio),
        typeof(double),
        typeof(RingSpinner),
        new PropertyMetadata(2d, OnRingRatioChanged));
    public double RingRatio
    {
        get { return (double)this.GetValue(RingRatioProperty); }
        set { this.SetValue(RingRatioProperty, value); }
    }
    static void OnRingRatioChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var rs = d as RingSpinner;
        if (rs != null && e.NewValue != null)
        {
            Debug.WriteLine($"[INFO] RingRatio changed to {e.NewValue}");
            if (rs.RingRatio > 0)
            {
                //rs.Ring1.Height = rs.Width / rs.RingRatio;
                //rs.Ring2.Height = rs.Width / rs.RingRatio;
                //rs.Ring3.Height = rs.Width / rs.RingRatio;
                //rs.Ring4.Height = rs.Width / rs.RingRatio;
            }
        }
    }

    /// <summary>
    ///   Define our rectangle radius property
    /// </summary>
    public static readonly DependencyProperty RingRadiusProperty = DependencyProperty.Register(
        nameof(RingRadius),
        typeof(double),
        typeof(RingSpinner),
        new PropertyMetadata(2d, OnRingRatioChanged));
    public double RingRadius
    {
        get { return (double)this.GetValue(RingRadiusProperty); }
        set { this.SetValue(RingRadiusProperty, value); }
    }
    static void OnRingRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var rs = d as RingSpinner;
        if (rs != null && e.NewValue != null)
        {
            Debug.WriteLine($"[INFO] RingRadius changed to {e.NewValue}");
            rs.Rect1.RadiusX = rs.Rect1.RadiusY = rs.RingRadius;
            rs.Rect2.RadiusX = rs.Rect2.RadiusY = rs.RingRadius;
            rs.Rect3.RadiusX = rs.Rect3.RadiusY = rs.RingRadius;
            rs.Rect4.RadiusX = rs.Rect4.RadiusY = rs.RingRadius;
        }
    }

    public RingSpinner() => this.InitializeComponent();

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _hasAppliedTemplate = true;
        //Ring1.Height = Width / RingRatio;
        //Ring2.Height = Width / RingRatio;
        //Ring3.Height = Width / RingRatio;
        //Ring4.Height = Width / RingRatio;
        Debug.WriteLine($"[INFO] {nameof(RingSpinner)} template has been applied.");
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Debug.WriteLine($"[INFO] {nameof(RingSpinner)} is measured to be {availableSize}");
        return base.MeasureOverride(availableSize);
    }
}
