using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using System.Diagnostics;

namespace AppRestorer.Controls;

/*
 *  NOTE: This must be accompanied by the <Style TargetType="{x:Type ctrl:TrackRing}"> in your App.xaml resource dictionary.
 *       <ResourceDictionary.MergedDictionaries>
 *             <ResourceDictionary Source = "/Controls/TrackRing.xaml"/>
 *       </ResourceDictionary.MergedDictionaries>
 */
[TemplateVisualState(GroupName = VisualStates.GroupActive, Name = VisualStates.StateActive)]
[TemplateVisualState(GroupName = VisualStates.GroupActive, Name = VisualStates.StateInactive)]
public partial class TrackRing : Control
{
    bool _hasAppliedTemplate;

    #region [Dependency Properties]
    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive),
        typeof(bool),
        typeof(TrackRing),
        new PropertyMetadata(false, IsActiveChanged));
    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public static readonly DependencyProperty TemplateSettingsProperty = DependencyProperty.Register(
        nameof(TemplateSettings),
        typeof(TemplateSettingValues),
        typeof(TrackRing),
        new PropertyMetadata(null));
    public TemplateSettingValues TemplateSettings
    {
        get => (TemplateSettingValues)GetValue(TemplateSettingsProperty);
        set => SetValue(TemplateSettingsProperty, value);
    }
    #endregion

    public TrackRing()
    {
        DefaultStyleKey = typeof(TrackRing);
        TemplateSettings = new TemplateSettingValues(60);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _hasAppliedTemplate = true;
        UpdateState(IsActive);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 40D;
        var height = 40D;
        if (DesignerProperties.GetIsInDesignMode(this) == false)
        {
            width = double.IsNaN(Width) == false ? Width : availableSize.Width;
            height = double.IsNaN(Height) == false ? Height : availableSize.Height;
        }
        TemplateSettings = new TemplateSettingValues(Math.Min(width, height));
        return base.MeasureOverride(availableSize);
    }

    static void IsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
    {
        var tr = d as TrackRing;
        if (tr == null || args.NewValue == null) { return; }
        var isActive = (bool)args.NewValue;
        tr.UpdateState(isActive);
    }

    void UpdateState(bool isActive)
    {
        if (_hasAppliedTemplate)
        {
            var state = isActive ? VisualStates.StateActive : VisualStates.StateInactive;
            VisualStateManager.GoToState(this, state, true);
        }
    }
}

public class TemplateSettingValues : DependencyObject
{
    #region [Dependency Properties]
    public static readonly DependencyProperty EllipseDiameterProperty = DependencyProperty.Register(
        nameof(EllipseDiameter),
        typeof(double),
        typeof(TemplateSettingValues),
        new PropertyMetadata(60D));
    public double EllipseDiameter
    {
        get => (double)GetValue(EllipseDiameterProperty);
        set => SetValue(EllipseDiameterProperty, value);
    }

    public static readonly DependencyProperty EllipseOffsetProperty = DependencyProperty.Register(
        nameof(EllipseOffset),
        typeof(Thickness),
        typeof(TemplateSettingValues),
        new PropertyMetadata(default(Thickness)));
    public Thickness EllipseOffset
    {
        get => (Thickness)GetValue(EllipseOffsetProperty);
        set => SetValue(EllipseOffsetProperty, value);
    }

    public static readonly DependencyProperty MaxSideLengthProperty = DependencyProperty.Register(
        nameof(MaxSideLength),
        typeof(double),
        typeof(TemplateSettingValues),
        new PropertyMetadata(0D));
    public double MaxSideLength
    {
        get => (double)GetValue(MaxSideLengthProperty);
        set => SetValue(MaxSideLengthProperty, value);
    }
    #endregion

    public TemplateSettingValues(double width)
    {
        if (width <= 0.1)
            width = 1.0;

        if (width <= 40D)
            EllipseDiameter = width / 9D + 1D;
        else
            EllipseDiameter = width / 9D;

        MaxSideLength = width - EllipseDiameter; // max grid size (TrackRing.xaml)
        EllipseOffset = new Thickness(0, EllipseDiameter * 1.5D, 0, 0);
    }
}
