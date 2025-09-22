using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AppRestorer.Controls
{
    /// <summary>
    /// Interaction logic for ProgressBarEx.xaml
    /// </summary>
    public partial class ProgressBarEx : UserControl
    {
        Storyboard? _pingPongStoryboard;

        #region [Dependency Properties]
        public double AnimationWidth
        {
            get => (double)GetValue(AnimationWidthProperty);
            set => SetValue(AnimationWidthProperty, value);
        }
        public static readonly DependencyProperty AnimationWidthProperty = DependencyProperty.Register(
            nameof(AnimationWidth),
            typeof(double),
            typeof(ProgressBarEx),
    new FrameworkPropertyMetadata(200.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public Color BarColor
        {
            get => (Color)GetValue(BarColorProperty);
            set => SetValue(BarColorProperty, value);
        }
        public static readonly DependencyProperty BarColorProperty = DependencyProperty.Register(
            nameof(BarColor),
            typeof(Color),
            typeof(ProgressBarEx),
            new PropertyMetadata(Colors.DodgerBlue, OnBarColorChanged));
        static void OnBarColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as ProgressBarEx;
        }
        #endregion

        public ProgressBarEx()
        {
            InitializeComponent();
        }

        void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Get the ProgressBar instance
            //pbar.ApplyTemplate();

            //var canvas = FindName("PART_Canvas") as Canvas;
            var canvas = (Canvas)pbar.Template.FindName("PART_Canvas", pbar);
            //var indicator = FindName("PART_Indicator") as Rectangle;
            var indicator = (Rectangle)pbar.Template.FindName("PART_Indicator", pbar);


            if (canvas == null || indicator == null) 
            {
                MessageBox.Show("[UserControl] Template parts not found — check style/template names.");
                return; 
            }
            double target = pbar.ActualWidth - indicator.Width - 10; // Padding margin
            double target2 = AnimationWidth - indicator.Width;
            var animation = new DoubleAnimation
            {
                From = 0,
                To = target2,
                Duration = TimeSpan.FromSeconds(1.5),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            Storyboard.SetTarget(animation, indicator);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(Canvas.Left)"));
            _pingPongStoryboard = new Storyboard();
            _pingPongStoryboard.Children.Add(animation);
            _pingPongStoryboard.Begin();
        }

        void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _pingPongStoryboard?.Stop();
        }
    }
}
