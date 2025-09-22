using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AppRestorer.Controls
{
    public partial class CandyButton : UserControl
    {
        bool hasAppliedTemplate = false;
        Color previousColor = Colors.DodgerBlue;
        Color disabledColor = Color.FromRgb(120, 120, 120);

        #region [Dependency Properties]
        public Color ButtonColor
        {
            get => (Color)GetValue(ButtonColorProperty);
            set => SetValue(ButtonColorProperty, value);
        }
        public static readonly DependencyProperty ButtonColorProperty = DependencyProperty.Register(
            nameof(ButtonColor),
            typeof(Color),
            typeof(CandyButton),
            new PropertyMetadata(Colors.DodgerBlue, OnButtonColorChanged));
        static void OnButtonColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as CandyButton;
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(CandyButton),
            new PropertyMetadata(string.Empty, OnTextChanged));
        static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as CandyButton;
        }

        public SolidColorBrush TextColor
        {
            get => (SolidColorBrush)GetValue(TextColorProperty);
            set => SetValue(TextColorProperty, value);
        }
        public static readonly DependencyProperty TextColorProperty = DependencyProperty.Register(
            nameof(TextColor),
            typeof(SolidColorBrush),
            typeof(CandyButton),
            new PropertyMetadata(Brushes.White, OnTextColorChanged));
        static void OnTextColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as CandyButton;
        }

        public double TextSize
        {
            get => (double)GetValue(TextSizeProperty);
            set => SetValue(TextSizeProperty, value);
        }
        public static readonly DependencyProperty TextSizeProperty = DependencyProperty.Register(
            nameof(TextSize),
            typeof(double),
            typeof(CandyButton),
            new PropertyMetadata(12d, OnTextSizeChanged));
        static void OnTextSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as CandyButton;
        }

        public double ButtonSize
        {
            get => (double)GetValue(ButtonSizeProperty);
            set => SetValue(ButtonSizeProperty, value);
        }
        public static readonly DependencyProperty ButtonSizeProperty = DependencyProperty.Register(
            nameof(ButtonSize),
            typeof(double),
            typeof(CandyButton),
            new PropertyMetadata(40d, OnButtonSizeChanged));
        static void OnButtonSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as CandyButton;
        }

        /// <summary>
        /// A "tap" is the same thing as a "click".
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
        /// </summary>
        public static readonly RoutedEvent MouseDownEvent = EventManager.RegisterRoutedEvent(
            nameof(MouseDown),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(ColorPreview));

        /// <summary>
        /// Provide CLR accessor for the event
        /// </summary>
        public event RoutedEventHandler MouseDown
        {
            add { AddHandler(MouseDownEvent, value); }
            remove { RemoveHandler(MouseDownEvent, value); }
        }

        /// <summary>
        /// Raise the <see cref="MouseDown"/> event.
        /// </summary>
        void RaiseMouseDownEvent()
        {
            var newEventArgs = new RoutedEventArgs(MouseDownEvent);
            RaiseEvent(newEventArgs);
        }

        /// <summary>
        /// If inheriting from <see cref="Button"/>
        /// </summary>
        //protected override void OnClick() => RaiseTapEvent();

        /// <summary>
        /// If inheriting from <see cref="UserControl"/>
        /// </summary>
        //protected override void OnMouseDown(MouseButtonEventArgs e)
        //{
        //    //base.OnMouseDown(e);
        //    RaiseTapEvent();
        //}

        /// <summary>
        /// I've added a hard MouseDown event to <see cref="System.Windows.Shapes.Ellipse"/>.
        /// </summary>
        void Control_MouseDown(object sender, MouseButtonEventArgs e)
        {
            RaiseMouseDownEvent();
        }

        public static readonly RoutedEvent MouseUpEvent = EventManager.RegisterRoutedEvent(
            nameof(MouseUp),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(ColorPreview));

        /// <summary>
        /// Provide CLR accessor for the event
        /// </summary>
        public event RoutedEventHandler MouseUp
        {
            add { AddHandler(MouseUpEvent, value); }
            remove { RemoveHandler(MouseUpEvent, value); }
        }

        /// <summary>
        /// Raise the <see cref="MouseUp"/> event.
        /// </summary>
        void RaiseMouseUpEvent()
        {
            var newEventArgs = new RoutedEventArgs(MouseUpEvent);
            RaiseEvent(newEventArgs);
        }

        void Control_MouseUp(object sender, MouseButtonEventArgs e)
        {
            RaiseMouseUpEvent();
        }
        #endregion

        public CandyButton()
        {
            InitializeComponent();
            this.IsEnabledChanged += CandyButtonIsEnabledChanged;
        }

        void CandyButtonIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //Debug.WriteLine($"[INFO] {nameof(CandyButton)} IsEnabled: {e.NewValue}");
            if (ButtonColor != disabledColor) { previousColor = ButtonColor; }
            if (e.NewValue is bool ena && ena == false) { ButtonColor = disabledColor; }
            else { ButtonColor = previousColor; }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            hasAppliedTemplate = true;
            Debug.WriteLine($"[INFO] {nameof(CandyButton)} template has been applied.");
            // If the user has no text, reclaim space from grid so vertical centering works normally.
            if (string.IsNullOrWhiteSpace(Text))
            {
                userText.Visibility = Visibility.Collapsed;
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            // NOTE: Some visual properties can only be updated AFTER the control's measurement.
            Debug.WriteLine($"[INFO] {nameof(CandyButton)} is measured to be {availableSize}");
            return base.MeasureOverride(availableSize);
        }

    }
}
