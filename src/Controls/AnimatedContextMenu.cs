using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;


namespace AppRestorer.Controls;

/// <summary>
/// The purpose of this control is to provide a context menu with open AND close animations.
/// The standard WPF <c>ContextMenu</c> does not support a close animation directly, 
/// since it only exposes a <c>Closed</c> event, by then the visual tree is torn down 
/// and animations wouldn't be relevant.
/// </summary>
public class AnimatedContextMenu : Menu
{
    #region [Properties]
    bool _isClosing;
    bool _closeAfterClick = true;
    readonly Popup _popup;
    readonly Border _root;
    readonly ScaleTransform _scale;

    public UIElement PlacementTarget
    {
        get => _popup.PlacementTarget;
        set => _popup.PlacementTarget = value;
    }

    public bool IsOpen
    {
        get => _popup.IsOpen;
        set
        {
            if (value)
                OpenMenu();
            else
                CloseMenu();
        }
    }
    #endregion

    #region [Routed events]
    public static readonly RoutedEvent OpenedEvent =
        EventManager.RegisterRoutedEvent(
            "Opened",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(AnimatedContextMenu));

    public event RoutedEventHandler Opened
    {
        add => AddHandler(OpenedEvent, value);
        remove => RemoveHandler(OpenedEvent, value);
    }

    public static readonly RoutedEvent ClosedEvent =
        EventManager.RegisterRoutedEvent(
            "Closed",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(AnimatedContextMenu));


    public event RoutedEventHandler Closed
    {
        add => AddHandler(ClosedEvent, value);
        remove => RemoveHandler(ClosedEvent, value);
    }
    #endregion

    public AnimatedContextMenu(bool closeAfterClick = true)
    {
        _closeAfterClick = closeAfterClick;

        var brsh = (Brush)Application.Current.TryFindResource("PopupMenuBrush");

        // Root for animation
        _scale = new ScaleTransform(0.95, 0.95);
        _root = new Border
        {
            //Background = (Brush)new BrushConverter().ConvertFrom("#FF2D2D30"),
            Background = brsh ?? Extensions.CreateGradientBrush(Color.FromRgb(50, 50, 50), Color.FromRgb(10, 10, 10)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(2),
            Margin = new Thickness(10), // for shadow only
            RenderTransformOrigin = new Point(0.5, 0),
            RenderTransform = _scale,
            Opacity = 0,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Navy,
                Direction = 310,
                ShadowDepth = 6,
                Opacity = 0.6,
                BlurRadius = 7
            }
        };

        // IMPORTANT: Use an ItemsPresenter inside a ControlTemplate-like scope by setting the
        // TemplatedParent via VisualTreeHelper is not possible here, so we mirror items with
        // an ItemsControl bound to this.Items.
        var itemsHost = new ItemsControl
        {
            ItemsSource = Items,
            ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(StackPanel)))
        };
        _root.Child = itemsHost;

        // Host in Popup
        _popup = new Popup
        {
            AllowsTransparency = true,
            Placement = PlacementMode.MousePoint,
            StaysOpen = false,
            Child = _root
        };

        // Forward native popup closed to our routed Closed (when not our animated close)
        _popup.Closed += (_, __) =>
        {
            if (!_isClosing)
            {
                RaiseEvent(new RoutedEventArgs(ClosedEvent));
            }
        };
    }

    void OpenMenu()
    {
        if (_popup.IsOpen) 
            return;

        // Sync ItemsControl with latest items
        ((ItemsControl)_root.Child).ItemsSource = null;
        ((ItemsControl)_root.Child).ItemsSource = Items;

        if (_closeAfterClick)
        {   // Be sure to close when any item is clicked
            AttachCloseOnClickHandlers();
        }

        _root.Opacity = 0;
        _scale.ScaleX = 0.90;
        _scale.ScaleY = 0.90;

        _popup.IsOpen = true;

        // Open animation
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        var scaleUp = new DoubleAnimation(0.80, 1, TimeSpan.FromMilliseconds(350))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        _root.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        _scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUp);
        _scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUp);

        // Fire Opened after starting animations
        RaiseEvent(new RoutedEventArgs(OpenedEvent));
    }

    void CloseMenu()
    {
        if (!_popup.IsOpen) 
            return;

        _isClosing = true;

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        var scaleDown = new DoubleAnimation(1, 0.6, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };

        fadeOut.Completed += (_, __) =>
        {
            _popup.IsOpen = false;
            _isClosing = false;
            RaiseEvent(new RoutedEventArgs(ClosedEvent));
        };

        _root.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        _scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleDown);
        _scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleDown);
    }


    void AttachCloseOnClickHandlers()
    {
        foreach (var item in Items)
        {
            if (item is MenuItem mi)
            {
                mi.Click -= MenuItem_Click_Close;
                mi.Click += MenuItem_Click_Close;
                // Also attach to submenus
                AttachCloseOnClickHandlersRecursive(mi);
            }
        }
    }

    void AttachCloseOnClickHandlersRecursive(MenuItem parent)
    {
        foreach (var subItem in parent.Items)
        {
            if (subItem is MenuItem mi)
            {
                mi.Click -= MenuItem_Click_Close;
                mi.Click += MenuItem_Click_Close;
                if (mi.HasItems)
                    AttachCloseOnClickHandlersRecursive(mi);
            }
        }
    }

    void MenuItem_Click_Close(object sender, RoutedEventArgs e)
    {
        // Only close if it's not a parent opening a submenu
        if (sender is MenuItem mi && !mi.HasItems)
        {
            CloseMenu();
        }
    }
}
