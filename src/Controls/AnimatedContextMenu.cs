using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AppRestorer.Controls;

/// <summary>
///   The purpose of this control is to provide a context menu with open AND close animations.
///   The standard WPF <see cref="ContextMenu"/> does not support a closing animation directly, 
///   since it only exposes a <see cref="ContextMenu.Closed"/> event, by then the visual tree 
///   is torn down and animations wouldn't be relevant.
/// </summary>
/// <remarks>
///   In this demo the <see cref="AnimatedContextMenu"/> is used in tandem with the 
///   <see cref="ActiveArrow"/> control, but it can be bound to any <see cref="UIElement"/>.
/// </remarks>
public class AnimatedContextMenu : Menu
{
    #region [Properties]
    bool _isClosing;
    bool _closeAfterClick = true;
    double _rootScale = 0.90;
    double _inMS = 150;
    double _outMS = 200;
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

    /// <summary>
    /// An <see cref="ItemsControl"/> wrapped inside a <see cref="Popup"/> with open and close animations.
    /// </summary>
    /// <param name="closeAfterClick">
    /// When an item is selected, if it is not bound to an <see cref="System.Windows.Input.ICommand"/>, 
    /// then the menu can remain open. Set this to <c>false</c> to always close after an item is clicked.
    /// </param>
    /// <param name="closeOnMouseLeave">
    /// Set this to <c>true</c> to auto-close the <see cref="Popup"/> when the mouse leaves the boundaries.
    /// </param>
    /// <param name="minWidth">The minimum width to render, no matter the content choice.</param>
    public AnimatedContextMenu(bool closeAfterClick = true, bool closeOnMouseLeave = false, double minWidth = 100, Color? shadowColor = null)
    {
        _closeAfterClick = closeAfterClick;

        // If a brush resource named "PopupMenuBrush" is defined we'll
        // use it, otherwise dark gray to black gradient is the default.
        var brsh = (Brush)Application.Current.TryFindResource("PopupMenuBrush");

        // Root for animation
        _scale = new ScaleTransform(_rootScale, _rootScale);
        _root = new Border
        {
            MinWidth = minWidth,
            //Background = (Brush)new BrushConverter().ConvertFrom("#FF2D2D30"),
            Background = brsh ?? Extensions.CreateGradientBrush(Color.FromRgb(50, 50, 50), Color.FromRgb(10, 10, 10)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(2),
            Margin = new Thickness(10), // for shadow only
            RenderTransformOrigin = new Point(0.5, 0),
            RenderTransform = _scale,
            Opacity = 0,
        };

        // Add shadow if requested
        if (shadowColor != null)
        {
            _root.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = (Color)shadowColor,
                Direction = 310,
                ShadowDepth = 6,
                Opacity = 0.5,
                BlurRadius = 7
            };
        }

        // IMPORTANT: Use an ItemsPresenter inside a ControlTemplate-like scope by setting the
        // TemplatedParent via VisualTreeHelper is not possible here, so we mirror items with
        // an ItemsControl bound to this.Items.
        var itemsHost = new ItemsControl
        {
            ItemsSource = Items,
            ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(StackPanel)))
        };
        _root.Child = itemsHost;

        // Host in a popup control
        _popup = new Popup
        {
            AllowsTransparency = true,
            Placement = PlacementMode.MousePoint,
            PopupAnimation = PopupAnimation.Fade,
            StaysOpen = closeOnMouseLeave,
            Child = _root
        };

        // Forward native popup closed to our routed Closed (when not our animated close)
        _popup.Closed += (_, __) =>
        {
            Debug.WriteLine("[INFO] Popup Closed");
            if (!_isClosing)
            {
                RaiseEvent(new RoutedEventArgs(ClosedEvent));
            }
        };

        // If Popup.StaysOpen=false, then this is not needed, but just in case.
        _popup.LostMouseCapture += (_, __) =>
        {
            Debug.WriteLine("[INFO] Popup LostMouseCapture");
            if (!_isClosing)
                CloseMenu();
        };

        if (closeOnMouseLeave)
        {
            _root.MouseLeave += (_, __) =>
            {
                Debug.WriteLine("[INFO] Border MouseLeave");
                if (!_isClosing)
                    CloseMenu();
            };
        }
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
        _scale.ScaleX = _rootScale;
        _scale.ScaleY = _rootScale;

        _popup.IsOpen = true;

        // Open animation
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(_inMS))
        {
            EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut }
        };
        var scaleUp = new DoubleAnimation(_rootScale, 1, TimeSpan.FromMilliseconds(_inMS*2d))
        {
            EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut }
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

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(_outMS))
        {
            EasingFunction = new PowerEase { EasingMode = EasingMode.EaseIn }
        };
        var scaleDown = new DoubleAnimation(1, _rootScale / 1.5d, TimeSpan.FromMilliseconds(_outMS))
        {
            EasingFunction = new PowerEase { EasingMode = EasingMode.EaseIn }
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
