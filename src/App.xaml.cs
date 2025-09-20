using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AppRestorer;

public partial class App : Application
{
    public static string AssemblyInfo { get; set; } = string.Empty;
    public static string RuntimeInfo { get; set; } = string.Empty;
    public static string BuildConfig { get; set; } = string.Empty;
    public static string BaseDirectory { get; set; } = string.Empty;
    public static string AppDataDirectory { get; set; } = string.Empty;
    public static EventBus RootEventBus { get; set; } = new();
    public static PriorityPollingServiceAsync PollService { get; set; } = new();

    #region [Overrides]
    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
        //Debug.WriteLine($"[INFO] AppDomainFullTrust: {AppDomain.CurrentDomain.IsFullyTrusted}");

        AppDataDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), GetCurrentAssemblyName());
        BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        RuntimeInfo = $"{Extensions.GetRuntimeInfo()}";
        BuildConfig = $"{typeof(App).GetBuildConfig()}";
        AssemblyInfo = $"{typeof(App).ReflectAssemblyFramework()}";

        base.OnStartup(e);

        //Current.MainWindow = new MainWindow { Visibility = Visibility.Hidden };

        PollService?.Start(TimeSpan.FromMinutes(5));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Debug.WriteLine($"[INFO] App OnExit Event: {e.ApplicationExitCode}");

        PollService?.Stop();

        base.OnExit(e);
    }
    #endregion

    #region [Public Methods]
    public static bool ShowMessage(string message, string title = "Notice", string yes = "Yes", string no = "No", Window? owner = null)
    {
        var msgBox = new MessageBoxWindow(message, title, yes, no, true);
        if (owner != null) 
        { 
            msgBox.Owner = owner;
            msgBox.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            // Apply light blur effect on background/owner window
            owner.Effect = new System.Windows.Media.Effects.BlurEffect()
            {
                KernelType = System.Windows.Media.Effects.KernelType.Gaussian,
                Radius = 5
            };
        }
        else 
        { 
            msgBox.WindowStartupLocation = WindowStartupLocation.CenterScreen; 
        }
        bool? result = msgBox.ShowDialog();

        // Remove blur once closed
        if (owner != null)
            owner.Effect = null;

        return result == true;
    }

    /// <summary>
    /// Returns the executing assembly's codebase/location. Since this will reference the linked library, we'll replace the DLL extension with EXE.
    /// </summary>
    public static string GetCurrentLocation() => $"{System.Reflection.Assembly.GetExecutingAssembly().Location}".Replace(".dll", ".exe");

    /// <summary>
    /// Returns the declaring type's namespace.
    /// </summary>
    public static string GetCurrentNamespace() => System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Namespace ?? "AppRestorer";

    /// <summary>
    /// Returns the declaring type's full name: "AppRestorer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
    /// </summary>
    public static string GetCurrentFullName() => System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Assembly?.FullName ?? "AppRestorer";

    /// <summary>
    /// Returns the declaring type's assembly name.
    /// </summary>
    public static string GetCurrentAssemblyName() => System.Reflection.Assembly.GetExecutingAssembly()?.GetName()?.Name ?? "AppRestorer";

    /// <summary>
    /// Returns the declaring type's assembly name with extension.
    /// </summary>
    public static string GetCurrentAssemblyNameWithExtension() => $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.exe";

    /// <summary>
    /// Returns the AssemblyVersion, not the FileVersion.
    /// </summary>
    public static Version GetCurrentAssemblyVersion() => System.Reflection.Assembly.GetExecutingAssembly()?.GetName()?.Version ?? new Version();

    #endregion

    #region [Domain Exceptions]
    void CurrentDomain_FirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Exception.Message) &&
            !e.Exception.Message.Contains("A task was canceled", StringComparison.OrdinalIgnoreCase) &&
            !e.Exception.Message.Contains("'local' does not map to a namespace", StringComparison.OrdinalIgnoreCase) &&
            !e.Exception.Message.Contains($"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.XmlSerializers"))
        {
            var str = $"[WARNING] First chance exception: {e.Exception.Message}";
            Debug.WriteLine(str);
            str.WriteToLog();
        }
    }

    void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            var str = $"[ERROR] Unhandled exception: {((Exception)e.ExceptionObject).Message}";
            Debug.WriteLine(str);
            str.WriteToLog();
            //MessageBox.Show(((Exception)e.ExceptionObject).Message, "AppRestore UnhandledException");
            //System.Diagnostics.EventLog.WriteEntry(SystemTitle, $"Unhandled exception thrown:\r\n{((Exception)e.ExceptionObject).ToString()}");
        }
        catch (Exception) { }
    }

    void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var str = $"[ERROR] Unhandled exception: {e.Exception.Message}";
        Debug.WriteLine(str);
        e.Handled = true; // Prevent crash
        str.WriteToLog();
    }
    #endregion

    /// <summary>
    /// Can be called from any thread.
    /// </summary>
    public static void ShowDialog(string msg, string caption = "Notice") => ShowDialog(msg, caption, isWarning: false, shadows: true, modal: false, autoClose: default, autoFocus: false, assetName: "", assetOpacity: 0.5, owner: null);
    public static void ShowDialog(string msg, string caption = "Notice", bool isWarning = false, bool shadows = true, bool modal = false, TimeSpan autoClose = default, bool autoFocus = false, string assetName = "", double assetOpacity = 0.5, Window? owner = null)
    {
        try
        {
            System.Threading.Thread thread = new System.Threading.Thread(() =>
            {
                // Set this thread's sync context to the current dispatcher's context.
                System.Threading.SynchronizationContext.SetSynchronizationContext(new System.Windows.Threading.DispatcherSynchronizationContext(System.Windows.Threading.Dispatcher.CurrentDispatcher));

                // Border can have only one child element, so we'll use a Canvas to add
                // child elements, like a StackPanel, and then add that to the Border's child.
                Border border = new Border();
                border.Width = 600;
                border.Height = 220;
                if (isWarning)
                    border.Background = Extensions.CreateGradientBrush(Color.FromRgb(100, 20, 0), Color.FromRgb(10, 10, 10));
                else
                    border.Background = Extensions.CreateGradientBrush(Color.FromRgb(40, 40, 40), Color.FromRgb(10, 10, 10));
                border.BorderThickness = new Thickness(2);
                border.BorderBrush = new SolidColorBrush(Colors.LightGray);
                border.CornerRadius = new CornerRadius(8);
                border.HorizontalAlignment = HorizontalAlignment.Stretch;
                border.VerticalAlignment = VerticalAlignment.Stretch;

                // Canvas setup
                Canvas cnvs = new Canvas();
                cnvs.VerticalAlignment = VerticalAlignment.Stretch;
                cnvs.HorizontalAlignment = HorizontalAlignment.Stretch;

                // StackPanel setup
                var sp = new StackPanel
                {
                    Background = Brushes.Transparent,
                    Orientation = Orientation.Vertical,
                    Height = border.Height,
                    Width = border.Width,
                };

                // TextBox setup
                var tbx = new TextBox()
                {
                    Background = sp.Background,
                    FontSize = 20,
                    AcceptsReturn = true,
                    BorderThickness = new Thickness(0),
                    MaxHeight = border.Height / 2,
                    MinHeight = border.Height / 2,
                    MaxWidth = border.Width / 1.111,
                    MinWidth = border.Width / 1.111,
                    Margin = new Thickness(20, 30, 20, 10),
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                    FontWeight = FontWeights.Regular,
                    Text = msg
                };

                if (shadows)
                {
                    tbx.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = new Color { A = 255, R = 40, G = 40, B = 40 },
                        Direction = 310,
                        ShadowDepth = 6,
                        Opacity = 0.8,
                        BlurRadius = 8
                    };
                }

                // Insert your style key here if you want to override the default button.
                var ct = (ControlTemplate)Application.Current.TryFindResource("ReformedButton");

                // Button setup
                var btn = new Button()
                {
                    Template = ct, // Comment out this line if you want to use the method's button style.
                    Width = 130,
                    Height = 45,
                    Content = "Close",
                    OverridesDefaultStyle = true,
                    IsEnabled = true,
                    IsDefault = true,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                    BorderThickness = new Thickness(1),
                    FontSize = 16,
                    FontWeight = FontWeights.Regular,
                    Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                    Margin = new Thickness(4, 4, 4, 2),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Background = Extensions.CreateGradientBrush(Color.FromRgb(10, 10, 10), Color.FromRgb(30, 30, 30)) /* Background = sp.Background */
                };

                if (shadows)
                {
                    btn.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = new Color { A = 255, R = 10, G = 10, B = 10 },
                        Direction = 310,
                        ShadowDepth = 2,
                        Opacity = 0.8,
                        BlurRadius = 4
                    };
                }

                // Add image asset
                if (!string.IsNullOrEmpty(assetName))
                {
                    #region [option 1 - less flexible]
                    //var img = new Image()
                    //{
                    //    Width = 30, 
                    //    Opacity = 0.4,
                    //    Margin = new Thickness(5, 5, 0, -50),
                    //    VerticalAlignment = VerticalAlignment.Center,
                    //    HorizontalAlignment = HorizontalAlignment.Left,
                    //    Source = Extensions.ReturnImageSource($"pack://application:,,,/Assets/{assetName}"),
                    //};
                    //sp.Children.Add(img);
                    #endregion

                    #region [option 2 - more flexible]
                    var bi = new System.Windows.Media.Imaging.BitmapImage(new Uri($"pack://application:,,,/Assets/{assetName}"));
                    if (bi != null)
                    {
                        // An ImageBrush won’t take a raw width or height in pixels
                        // because it's designed as a paint rather than an Image control.
                        // ⚠️ Use a `Viewbox` to control what portion of the image is displayed.
                        // ⚠️ Use a `Viewport` to control how big the image is.
                        var ib = new System.Windows.Media.ImageBrush(bi);
                        ib.Opacity = assetOpacity;
                        ib.Stretch = Stretch.Fill;
                        ib.TileMode = TileMode.None;
                        ib.AlignmentX = AlignmentX.Left;
                        ib.AlignmentY = AlignmentY.Bottom;
                        //ib.ViewboxUnits = BrushMappingMode.RelativeToBoundingBox;
                        //ib.Viewbox = new Rect(0, 0, 1.25, 1.25); // 125% image scale
                        ib.ViewportUnits = BrushMappingMode.Absolute;
                        //ib.Viewport = new Rect(4, border.Height / 1.9, bi.PixelWidth, bi.PixelHeight);
                        ib.Viewport = new Rect(4, border.Height / 1.9, 80, 80);
                        sp.Background = ib;
                        /*
                        <!-- ⚠️ Fixed size in pixels XAML example ⚠️ -->
                         <StackPanel>
                            <StackPanel.Background>
                                <ImageBrush ImageSource="photo.png"
                                            Viewbox="0,0,1,1"        <!-- whole image -->
                                            ViewboxUnits="RelativeToBoundingBox"
                                            Viewport="0,0,100,50"    <!-- 100x50 px -->
                                            ViewportUnits="Absolute"
                                            TileMode="None"
                                            Stretch="Fill"/>
                            </StackPanel.Background>
                        </StackPanel>

                        <!-- ⚠️ Fixed relative size XAML example ⚠️ -->
                        <StackPanel>
                            <StackPanel.Background>
                                <ImageBrush ImageSource="photo.png"
                                    Viewport="0,0,0.25,0.25"   <!-- 25% width & height of element -->
                                    ViewportUnits="RelativeToBoundingBox"
                                    TileMode="Tile"
                                    Stretch="Fill"/>
                            </StackPanel.Background>
                        </StackPanel>
                        */
                    }
                    #endregion
                }

                sp.Children.Add(tbx);
                sp.Children.Add(btn);
                cnvs.Children.Add(sp);
                border.Child = cnvs;
                if (shadows)
                {
                    border.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = new Color { A = 255, R = 40, G = 40, B = 40 },
                        Direction = 310,
                        ShadowDepth = 6,
                        Opacity = 0.8,
                        BlurRadius = 8
                    };
                }

                // Create window to hold content.
                var w = new Window();
                if (owner != null)
                {
                    try
                    {
                        // A cross-thread violation may occur as InvalidOperationException.
                        w.Owner = owner;
                        w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    }
                    catch (Exception)
                    {
                        w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    }
                }
                else // No owner, just use CenterScreen
                    w.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                w.WindowStyle = WindowStyle.None;
                w.AllowsTransparency = true;
                //w.Topmost = true;
                w.Background = Brushes.Transparent;
                w.VerticalAlignment = VerticalAlignment.Center;
                w.HorizontalAlignment = HorizontalAlignment.Center;
                w.Height = border.Height + 20; // add padding for shadow effect
                w.Width = border.Width + 20; // add padding for shadow effect

                // Apply content to new window.
                w.Content = border;

                if (string.IsNullOrEmpty(caption))
                    caption = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name ?? "AppRestorer";

                w.Title = caption;
                // Setup a delegate for the window loaded event.
                w.Loaded += (s, e) =>
                {
                    if (autoFocus)
                    {
                        w.Activate(); 
                        w.Focus();
                    }

                    // Check timer here to auto-close.
                    if (autoClose != default && autoClose != TimeSpan.Zero && autoClose.TotalMilliseconds > 0)
                    {
                        var t = new DispatcherTimer { Interval = autoClose };
                        t.Tick += (s2, e2) => { t?.Stop(); w?.Close(); };
                        t?.Start();
                    }
                };
                // Setup a delegate for the window closed event.
                w.Closed += (s, e) =>
                {
                    // Once we call Dispatcher.Run() on a thread, that thread won’t
                    // exit until we call Dispatcher.CurrentDispatcher.InvokeShutdown().
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown();
                };
                // Setup a delegate for the close button click event.
                btn.Click += (s, e) =>
                {
                    w.Close();
                };
                // Setup a delegate for the window mouse-down event (drag/move the dialog).
                w.MouseDown += (s, e) =>
                {
                    if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                    {
                        w.DragMove();
                    }
                    else if (e.RightButton == System.Windows.Input.MouseButtonState.Pressed)
                    {
                        // There could be a formatting sitch where the close button
                        // is pushed off the window, so provide a backup close method.
                        w.Close();
                    }
                };

                // Show our constructed window. We're not on the
                // main UI thread, so we shouldn't use "w.ShowDialog()".
                // Use the blocking flag for modal/non-modal behavior.
                w.Show();

                // Start a message loop on this thread that doesn’t already have one.
                // Without it, a thread with a Dispatcher will never process Invoke/BeginInvoke calls.
                System.Windows.Threading.Dispatcher.Run();
            });

            // You can only show a dialog in a STA thread.
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
            if (modal)
                thread.Join(); // wait until thread completes (window closed)
        }
        catch (Exception ex) { Debug.WriteLine($"[ERROR] Couldn't show dialog: {ex.Message}"); }
    }
}
