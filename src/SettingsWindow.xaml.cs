using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AppRestorer.Controls;
using static AppRestorer.Extensions;

namespace AppRestorer;

/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    SettingsViewModel? _vm;
    AnimatedContextMenu? _menu;

    public SettingsWindow()
    {
        Debug.WriteLine($"{System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Name}__{System.Reflection.MethodBase.GetCurrentMethod()?.Name} [{DateTime.Now.ToString("hh:mm:ss.fff tt")}]");

        InitializeComponent();

        // We'll pass the MainWindow to the VM so common Window events will become simpler to work with.
        this.DataContext = new SettingsViewModel(this);

        // This simple app doesn't really need a VM, but it's good practice.
        _vm = DataContext as SettingsViewModel;

        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            Debug.WriteLine("[INFO] XAML system is in design mode.");
        else
            Debug.WriteLine("[INFO] XAML system is not in design mode.");

        #region [Menu Items]
        // Example items to show on candy button event
        _menu = new AnimatedContextMenu(shadowColor: Colors.Navy);
        _menu.Items.Add(new MenuItem { FontSize = 15d, Header = "Item 1", Command = _vm!.MenuCommand, Icon = MenuIconFactory.CreateRandom(fill: Extensions.ShiftSaturation(Extensions.CreateRandomLightBrush(), 0.1)) });
        _menu.Items.Add(new MenuItem { FontSize = 15d, Header = "Item 2", Command = _vm!.MenuCommand, Icon = MenuIconFactory.CreateRandom(fill: Extensions.ShiftSaturation(Extensions.CreateRandomLightBrush(), 0.2)) });
        _menu.Items.Add(new MenuItem { FontSize = 15d, Header = "Item 3", Command = _vm!.MenuCommand, Icon = MenuIconFactory.CreateRandom(fill: Extensions.ShiftSaturation(Extensions.CreateRandomLightBrush(), 0.3)) });
        _menu.Items.Add(new MenuItem { FontSize = 15d, Header = "Item 4", Command = _vm!.MenuCommand, Icon = MenuIconFactory.CreateRandom(fill: Extensions.ShiftSaturation(Extensions.CreateRandomLightBrush(), 0.4)) });
        _menu.Items.Add(new MenuItem { FontSize = 15d, Header = "Item 5", Command = _vm!.MenuCommand, Icon = MenuIconFactory.CreateRandom(fill: Extensions.ShiftSaturation(Extensions.CreateRandomLightBrush(), 0.5)) });
        _menu.Items.Add(new MenuItem { FontSize = 15d, Header = "Item 6", Command = _vm!.MenuCommand, Icon = MenuIconFactory.CreateRandom(fill: Extensions.ShiftSaturation(Extensions.CreateRandomLightBrush(), 0.6)) });
        _menu.Items.Add(new MenuItem { FontSize = 15d, Header = "Item 7", Command = _vm!.MenuCommand, Icon = MenuIconFactory.CreateRandom(fill: Extensions.ShiftSaturation(Extensions.CreateRandomLightBrush(), 0.7)) });
        _menu.Items.Add(new MenuItem { FontSize = 15d, Header = "Item 8", Command = _vm!.MenuCommand, Icon = MenuIconFactory.CreateRandom(fill: Extensions.ShiftSaturation(Extensions.CreateRandomLightBrush(), 0.8)) });
        _menu.Items.Add(new MenuItem { FontSize = 15d, Header = "Item 9", Command = _vm!.MenuCommand, Icon = MenuIconFactory.CreateRandom(fill: Extensions.ShiftSaturation(Extensions.CreateRandomLightBrush(), 0.9)) });
        #endregion

        // EventBus demonstration
        if (!App.RootEventBus.IsSubscribed(Constants.EB_ToSettings))
            App.RootEventBus.Subscribe(Constants.EB_ToSettings, EventBusMessageHandler);
    }

    void CandyButton_MouseUp(object sender, MouseButtonEventArgs e)
    {
        var cb = sender as CandyButton;
        if (cb == null) { return; }
        var tag = (string)cb.Tag;
        var name = (string)cb.Name;
        switch (tag)
        {
            case "cb1":
            case "cb2":
            case "cb3":
                _vm!.StatusText = $"Showing AnimatedContextMenu";
                var ctrl = sender as UIElement;
                if (ctrl == null || _menu == null) { return; }
                _menu.PlacementTarget = ctrl;
                _menu.IsOpen = true; // trigger the AnimatedContextMenu to open
                break;
            case "cb4":
            case "cb5":
                Task.Run(async () =>
                {
                    _vm!.IsBusy = true;
                    for (int i = 1; i < 31; i++)
                    {
                        _vm!.StatusText = $"{tag} step {i}";
                        await Task.Delay(100);
                    }
                    _vm!.IsBusy = false;
                });
                break;
            default:
                _vm!.StatusText = $"Closing window";
                this.Close();
                break;
        }
    }

    /// <summary>
    /// For <see cref="EventBus"/> demonstration. 
    /// Currently this is not used for any real functionality.
    /// </summary>
    void EventBusMessageHandler(object? sender, ObjectEventBusArgs e)
    {
        if (e.Payload == null)
            return;

        if (e.Payload.GetType() == typeof(string))
        {
            if (string.IsNullOrEmpty($"{e.Payload}"))
                return;

            _vm!.StatusText = $"{e.Payload}";
        }
        else
            Debug.WriteLine($"[EVENTBUS] Received event bus message of type '{e.Payload.GetType()}'");
    }
}
