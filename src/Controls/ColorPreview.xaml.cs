using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;


namespace AppRestorer.Controls
{
    public partial class ColorPreview : UserControl, INotifyPropertyChanged
    {
        static bool useReflectivePropertyChange = false;
        public event PropertyChangedEventHandler? PropertyChanged;
        void NotifyPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public ColorPreview()
        {
            InitializeComponent();
            //DataContext = this;
        }

        public SolidColorBrush Brush
        {
            get => (SolidColorBrush)GetValue(BrushProperty);
            set => SetValue(BrushProperty, value);
        }
        public static readonly DependencyProperty BrushProperty = DependencyProperty.Register(
            nameof(Brush), 
            typeof(SolidColorBrush), 
            typeof(ColorPreview),
            new PropertyMetadata(Brushes.Transparent, OnBrushChanged));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), 
            typeof(string), 
            typeof(ColorPreview),
            new PropertyMetadata(string.Empty, OnTitleChanged));

        public bool ShowAlpha
        {
            get => (bool)GetValue(ShowAlphaProperty);
            set => SetValue(ShowAlphaProperty, value);
        }
        public static readonly DependencyProperty ShowAlphaProperty = DependencyProperty.Register(
            nameof(ShowAlpha), 
            typeof(bool), 
            typeof(ColorPreview),
            new PropertyMetadata(false));


        public string HexCode => Brush != null
            ? ShowAlpha
                ? $"#{Brush.Color.A:X2}{Brush.Color.R:X2}{Brush.Color.G:X2}{Brush.Color.B:X2}"
                : $"#{Brush.Color.R:X2}{Brush.Color.G:X2}{Brush.Color.B:X2}"
            : "#000000";

        static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var preview = d as ColorPreview;
            if (useReflectivePropertyChange)
                preview?.OnPropertyChanged(nameof(Title));
            else
                preview?.NotifyPropertyChanged(nameof(Title));
        }

        static void OnBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var preview = d as ColorPreview;
            if (useReflectivePropertyChange)
                preview?.OnPropertyChanged(nameof(HexCode));
            else
                preview?.NotifyPropertyChanged(nameof(HexCode));
        }

        /// <summary>
        /// Create a custom routed event by first registering a RoutedEventID. 
        /// We're overriding the Button.OnClick event for this example, so a
        /// "tap" is the same thing as a "click".
        ///
        /// RoutingStrategy.Tunnel: The routed event uses a tunneling strategy, where the 
        ///     event instance routes downwards through the tree, from root to source element.
        ///
        /// RoutingStrategy.Bubble: The routed event uses a bubbling strategy, where the 
        ///     event instance routes upwards through the tree, from event source to root.
        ///
        /// RoutingStrategy.Direct: The routed event does not route through an element tree, 
        ///     but does support other routed event capabilities such as class handling, 
        ///     System.Windows.EventTrigger or System.Windows.EventSetter.
        ///
        /// </summary>
        public static readonly RoutedEvent TapEvent = EventManager.RegisterRoutedEvent(
            nameof(Tap),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(ColorPreview));

        /// <summary>
        /// Provide CLR accessor for the event
        /// </summary>
        public event RoutedEventHandler Tap
        {
            add { AddHandler(TapEvent, value); }
            remove { RemoveHandler(TapEvent, value); }
        }

        /// <summary>
        /// This method raises the Tap event
        /// </summary>
        void RaiseTapEvent()
        {
            var newEventArgs = new RoutedEventArgs(TapEvent);
            RaiseEvent(newEventArgs);
        }

        /// <summary>
        /// If inheriting from <see cref="Button"/>
        /// </summary>
        //protected override void OnClick() => RaiseTapEvent();

        /// <summary>
        /// If inheriting from <see cref="UserControl"/>
        /// </summary>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            //base.OnMouseDown(e);
            RaiseTapEvent();
        }

        #region [Reflection Helpers]
        /// <summary>
        /// If not using <see cref="INotifyPropertyChanged"/>.
        /// </summary>
        /// <param name="name">"HexCode"</param>
        void OnPropertyChanged(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            var dp = GetLocalDependencyProperty(typeof(ColorPreview), name);
            if (dp != null)
            {
                var descriptor = DependencyPropertyDescriptor.FromProperty(dp, typeof(ColorPreview));
                descriptor?.AddValueChanged(this, (s, e) => { });
                // After triggering the add value, remove it so we avoid possible memory leaks.
                descriptor?.RemoveValueChanged(this, (s, e) => { });
            }
        }

        /// <summary>
        /// Assumes that you're naming your local <see cref="DependencyProperty"/>s like "ColorProperty", "TextProperty", etc.
        /// </summary>
        /// <param name="ownerType">typeof(ColorPreview)</param>
        /// <param name="propertyName">HexCode</param>
        /// <returns><see cref="DependencyProperty"/></returns>
        public static DependencyProperty? GetLocalDependencyProperty(Type ownerType, string propertyName)
        {
            var fieldName = propertyName + "Property";
            var field = ownerType.GetField(fieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            return field?.GetValue(null) as DependencyProperty;
        }
        #endregion
    }
}
