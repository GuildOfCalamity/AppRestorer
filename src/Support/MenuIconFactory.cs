using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Shapes;
using static AppRestorer.Extensions;


namespace AppRestorer;

public static class MenuIconFactory
{
    const string _bannedSymbol = "F1 M8,1 A7,7 0 1 1 8,15 A7,7 0 1 1 8,1 Z M3,4 L4.5,2.5 L13.5,11.5 L12,13 Z";
    public static double DefaultSize { get; set; } = 12;
    public static Brush DefaultFill { get; set; } = new SolidColorBrush(Color.FromRgb(245, 245, 245)); // white smoke

    static readonly Dictionary<string, string> _icons = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Backward", "M 6 0 L 0 4 L 6 8 Z M 12 0 L 6 4 L 12 8 Z" },
        { "BatteryFull", "M 0 4 L 14 4 L 14 12 L 0 12 Z M 14 6 L 16 6 L 16 10 L 14 10 Z" },
        { "BatteryHalf", "M 0 4 L 14 4 L 14 12 L 0 12 Z M 0 4 L 7 4 L 7 12 L 0 12 Z M 14 6 L 16 6 L 16 10 L 14 10 Z" },
        { "BatteryLow", "M 0 4 L 14 4 L 14 12 L 0 12 Z M 0 4 L 4 4 L 4 12 L 0 12 Z M 14 6 L 16 6 L 16 10 L 14 10 Z" },
        { "Bell", "M 8 0 A 3 3 0 0 1 11 3 L 11 5 C 13 6 14 8 14 10 L 14 14 L 2 14 L 2 10 C 2 8 3 6 5 5 L 5 3 A 3 3 0 0 1 8 0 Z M 6 14 A 2 2 0 0 0 10 14" },
        { "BellOff", "M 8 0 A 3 3 0 0 1 11 3 L 11 5 C 13 6 14 8 14 10 L 14 14 L 2 14 L 2 10 C 2 8 3 6 5 5 L 5 3 A 3 3 0 0 1 8 0 Z M 6 14 A 2 2 0 0 0 10 14 M 0 0 L 16 16" },
        { "Bluetooth", "M 4 0 L 12 8 L 4 16 L 4 0 Z M 4 8 L 12 0 M 4 8 L 12 16" },
        { "Calendar", "M 2 2 L 14 2 L 14 14 L 2 14 Z M 2 5 L 14 5 M 5 2 L 5 0 M 11 2 L 11 0" },
        { "ChartBar", "M 0 14 L 0 8 L 3 8 L 3 14 Z M 5 14 L 5 4 L 8 4 L 8 14 Z M 10 14 L 10 0 L 13 0 L 13 14 Z" },
        { "ChartLine", "M 0 12 L 4 8 L 8 10 L 12 4 L 16 6" },
        { "ChartPie", "M 8 0 A 8 8 0 1 1 0 8 L 8 8 Z M 8 0 L 8 8 L 16 8 A 8 8 0 0 0 8 0 Z" },
        { "Clipboard", "M 5 0 L 11 0 L 11 2 L 14 2 L 14 16 L 2 16 L 2 2 L 5 2 Z M 5 0 L 5 2 L 11 2 L 11 0 Z" },
        { "Clock", "M 8 0 A 8 8 0 1 1 7.999 0.001 Z M 8 2 L 8 8 L 12 10" },
        { "Cloud", "M 4 10 A 4 4 0 0 1 4 2 A 6 6 0 0 1 14 6 A 4 4 0 0 1 14 14 L 4 14 A 4 4 0 0 1 4 10 Z" },
        { "CloudDownload", "M 4 10 A 4 4 0 0 1 4 2 A 6 6 0 0 1 14 6 A 4 4 0 0 1 14 14 L 4 14 A 4 4 0 0 1 4 10 Z M 8 6 L 8 12 M 6 10 L 8 12 L 10 10" },
        { "CloudUpload", "M 4 10 A 4 4 0 0 1 4 2 A 6 6 0 0 1 14 6 A 4 4 0 0 1 14 14 L 4 14 A 4 4 0 0 1 4 10 Z M 8 12 L 8 6 M 6 8 L 8 6 L 10 8" },
        { "Copy", "M 0 0 L 8 0 L 8 10 L 0 10 Z M 4 4 L 12 4 L 12 14 L 4 14 Z" },
        { "Cut", "M 2 2 A 2 2 0 1 1 2 6 L 8 12 M 2 10 A 2 2 0 1 1 2 14 L 8 8" },
        { "Dashboard", "M 0 0 L 7 0 L 7 7 L 0 7 Z M 9 0 L 16 0 L 16 5 L 9 5 Z M 9 7 L 16 7 L 16 16 L 9 16 Z M 0 9 L 7 9 L 7 16 L 0 16 Z" },
        { "Delete", "M 2 4 L 14 4 L 13 14 L 3 14 Z M 5 4 L 5 2 L 11 2 L 11 4" },
        { "Document", "M 0 0 L 6 0 L 8 2 L 8 8 L 0 8 Z" },
        { "Download", "M 8 0 L 8 10 M 4 6 L 8 10 L 12 6 M 0 14 L 16 14" },
        { "Edit", "M 0 10 L 2 12 L 10 4 L 8 2 Z M 8 2 L 10 0 L 12 2 L 10 4 Z" },
        { "Eye", "M 0 8 C 2 4 6 2 8 2 C 10 2 14 4 16 8 C 14 12 10 14 8 14 C 6 14 2 12 0 8 Z M 8 5 A 3 3 0 1 1 7.999 5.001 Z" },
        { "EyeOff", "M 0 8 C 2 4 6 2 8 2 C 10 2 14 4 16 8 C 14 12 10 14 8 14 C 6 14 2 12 0 8 Z M 8 5 A 3 3 0 1 1 7.999 5.001 Z M 0 0 L 16 16" },
        { "Filter", "M 0 0 L 16 0 L 10 8 L 10 14 L 6 14 L 6 8 Z" },
        { "Flag", "M 0 0 L 0 16 M 0 0 L 10 0 L 8 4 L 0 4 Z M 0 8 L 10 8 L 8 12 L 0 12 Z" },
        { "Folder", "M 0 8 L 0 2 L 3 2 L 4 0 L 8 0 L 8 8 Z" },
        { "Forward", "M 0 0 L 6 4 L 0 8 Z M 6 0 L 12 4 L 6 8 Z" },
        { "Globe", "M 8 0 A 8 8 0 1 1 7.999 0.001 Z M 0 8 L 16 8 M 8 0 C 10 4 10 12 8 16 C 6 12 6 4 8 0 Z" },
        { "Grid", "M 0 0 L 6 0 L 6 6 L 0 6 Z M 10 0 L 16 0 L 16 6 L 10 6 Z M 0 10 L 6 10 L 6 16 L 0 16 Z M 10 10 L 16 10 L 16 16 L 10 16 Z" },
        { "Heart", "M 8 14 L 2 8 A 4 4 0 0 1 8 2 A 4 4 0 0 1 14 8 Z" },
        { "HeartOutline", "M 8 14 L 2 8 A 4 4 0 0 1 8 2 A 4 4 0 0 1 14 8 Z M 8 12 L 3.5 7.5 A 2.5 2.5 0 0 1 8 4 A 2.5 2.5 0 0 1 12.5 7.5 Z" },
        { "Home", "M 0 6 L 8 0 L 16 6 L 16 16 L 10 16 L 10 10 L 6 10 L 6 16 L 0 16 Z" },
        { "HomeOutline", "M 0 6 L 8 0 L 16 6 L 16 16 L 0 16 Z M 2 8 L 2 14 L 14 14 L 14 8 L 8 3 Z" },
        { "Link", "M 4 8 A 4 4 0 0 1 8 4 L 10 4 M 8 12 A 4 4 0 0 1 4 8 L 2 8 M 10 4 L 14 0 M 6 12 L 2 16" },
        { "List", "M 0 2 L 12 2 M 0 6 L 12 6 M 0 10 L 12 10 M 0 14 L 12 14" },
        { "Location", "M 8 0 A 6 6 0 0 1 14 6 C 14 10 8 16 8 16 C 8 16 2 10 2 6 A 6 6 0 0 1 8 0 Z" },
        { "Lock", "M 4 6 L 12 6 L 12 14 L 4 14 Z M 6 6 L 6 4 A 2 2 0 0 1 10 4 L 10 6" },
        { "Mail", "M 0 2 L 16 2 L 16 14 L 0 14 Z M 0 2 L 8 8 L 16 2" },
        { "MapPin", "M 8 0 A 5 5 0 0 1 13 5 C 13 9 8 16 8 16 C 8 16 3 9 3 5 A 5 5 0 0 1 8 0 Z M 8 3 A 2 2 0 1 0 8 7 A 2 2 0 0 0 8 3 Z" },
        { "Medal", "M 8 0 A 4 4 0 1 1 7.999 0.001 Z M 6 4 L 4 8 L 12 8 L 10 4 Z" },
        { "OpenFolder", "M 0 8 L 0 2 L 3 2 L 4 0 L 12 0 L 12 8 Z" },
        { "Paste", "M 4 0 L 8 0 L 8 2 L 12 2 L 12 14 L 0 14 L 0 2 L 4 2 Z M 4 2 L 4 0" },
        { "Pause", "M 0 0 L 5 0 L 5 16 L 0 16 Z M 7 0 L 12 0 L 12 16 L 7 16 Z" },
        { "Phone", "M 3 0 L 6 0 L 7 4 L 5 5 C 6 8 8 10 11 11 L 12 9 L 16 10 L 16 13 C 16 14 14 16 13 16 C 6 16 0 10 0 3 C 0 2 2 0 3 0 Z" },
        { "Play", "M 0 0 L 12 8 L 0 16 Z" },
        { "Print", "M 4 0 L 12 0 L 12 4 L 4 4 Z M 2 4 L 14 4 L 14 10 L 2 10 Z M 4 10 L 12 10 L 12 16 L 4 16 Z" },
        { "Redo", "M 8 4 L 12 8 L 8 12 L 8 9 A 5 5 0 1 1 8 3 Z" },
        { "Refresh", "M 8 0 A 8 8 0 1 1 0 8 L 2 8 M 8 16 A 8 8 0 1 1 16 8 L 14 8" },
        { "Save", "M 0 0 L 12 0 L 12 12 L 0 12 Z M 2 0 L 2 4 L 10 4 L 10 0 Z M 3 6 L 9 6 L 9 10 L 3 10 Z" },
        { "SearchOutline", "M 6 0 A 6 6 0 1 1 5.999 0.001 Z M 10 10 L 14 14" },
        { "Settings", "M 8 4 L 9 4 L 10 2 L 12 2 L 13 4 L 14 4 L 14 6 L 16 7 L 15 9 L 16 11 L 14 12 L 14 14 L 13 14 L 12 16 L 10 16 L 9 14 L 8 14 L 8 12 L 6 11 L 7 9 L 6 7 L 8 6 Z" },
        { "Star", "M 8 0 L 10 6 L 16 6 L 11 10 L 13 16 L 8 12 L 3 16 L 5 10 L 0 6 L 6 6 Z" },
        { "StarOutline", "M 8 0 L 10 6 L 16 6 L 11 10 L 13 16 L 8 12 L 3 16 L 5 10 L 0 6 L 6 6 Z M 8 2 L 9 6 L 13 6 L 10 9 L 11 13 L 8 11 L 5 13 L 6 9 L 3 6 L 7 6 Z" },
        { "Stop", "M 0 0 L 16 0 L 16 16 L 0 16 Z" },
        { "Tag", "M 0 6 L 6 0 L 16 0 L 16 10 L 10 16 L 0 16 Z M 12 4 A 1 1 0 1 1 11.999 4.001 Z" },
        { "Trophy", "M 4 0 L 12 0 L 12 4 L 14 4 L 14 6 C 14 9 12 12 8 12 C 4 12 2 9 2 6 L 2 4 L 4 4 Z M 6 12 L 10 12 L 10 14 L 6 14 Z" },
        { "Undo", "M 4 4 L 0 8 L 4 12 L 4 9 A 5 5 0 1 0 4 3 Z" },
        { "Unlink", "M 4 8 A 4 4 0 0 1 8 4 L 10 4 M 8 12 A 4 4 0 0 1 4 8 L 2 8 M 10 4 L 14 0 M 6 12 L 2 16 M 0 0 L 16 16" },
        { "Unlock", "M 4 6 L 12 6 L 12 14 L 4 14 Z M 10 6 L 10 4 A 2 2 0 0 0 6 4 L 6 6" },
        { "Upload", "M 8 16 L 8 6 M 4 10 L 8 6 L 12 10 M 0 2 L 16 2" },
        { "User", "M 8 2 A 3 3 0 1 1 7.999 2.001 Z M 2 14 C 2 11 5 10 8 10 C 11 10 14 11 14 14 L 14 16 L 2 16 Z" },
        { "Users", "M 5 2 A 2 2 0 1 1 4.999 2.001 Z M 11 2 A 2 2 0 1 1 10.999 2.001 Z M 0 14 C 0 12 2 11 5 11 C 8 11 10 12 10 14 L 10 16 L 0 16 Z M 6 14 C 6 12.5 7.5 12 9 12 C 10.5 12 12 12.5 12 14 L 12 16 L 6 16 Z" },
        { "VolumeMute", "M 0 6 L 4 6 L 8 2 L 8 14 L 4 10 L 0 10 Z M 10 6 L 14 10 M 14 6 L 10 10" },
        { "VolumeUp", "M 0 6 L 4 6 L 8 2 L 8 14 L 4 10 L 0 10 Z M 10 4 A 4 4 0 0 1 10 12 M 12 2 A 6 6 0 0 1 12 14" },
        { "Wifi", "M 0 6 A 8 8 0 0 1 16 6 M 3 9 A 5 5 0 0 1 13 9 M 6 12 A 2 2 0 0 1 10 12 M 8 14 A 0.5 0.5 0 1 1 7.999 14.001 Z" },
        { "ZoomIn", "M 6 0 A 6 6 0 1 1 5.999 0.001 Z M 10 10 L 14 14 M 6 3 L 6 9 M 3 6 L 9 6" },
        { "ZoomOut", "M 6 0 A 6 6 0 1 1 5.999 0.001 Z M 10 10 L 14 14 M 3 6 L 9 6" },
        // Power (stroke)
        { "Power", "M8,2 L8,8 M4,4 A5,5 0 1 0 12,4" },
        // Close
        { "Close", "M2,2 L14,14 M14,2 L2,14" },
        { "CloseFilled", "M3,1 L6.6,4.6 L10.2,1 L15,1 L10.4,5.6 L15,10.2 L10.2,10.2 L6.6,6.6 L3,10.2 L1,10.2 L5.6,5.6 L1,1 Z" },
        // Plus
        { "Plus", "M8,2 L8,14 M2,8 L14,8" },
        { "PlusFilled", "M7,1 L9,1 L9,7 L15,7 L15,9 L9,9 L9,15 L7,15 L7,9 L1,9 L1,7 L7,7 Z" },
        // Minus
        { "Minus", "M2,8 L14,8" },
        { "MinusFilled", "M1,7 L15,7 L15,9 L1,9 Z" },
        // Check
        { "Check", "M2,9 L6.5,12.5 L14,4" },
        { "CheckFilled", "M2,9 L5.5,12.5 L14,4 L12.6,2.6 L5.5,9.7 L3.4,7.6 Z" },
        // Info
        { "Info", "M8,2 A6,6 0 1 1 8,14 A6,6 0 1 1 8,2 Z M8,6 L8,12 M8,4 L8,5" },
        { "InfoFilled", "M8,1 A7,7 0 1 1 8,15 A7,7 0 1 1 8,1 Z M7,6 L9,6 L9,12 L7,12 Z M7,4 L9,4 L9,5 L7,5 Z" },
        // Warning
        { "Warning", "M8,2 L15,14 L1,14 Z M8,6 L8,10 M8,11.5 L8,13" },
        { "WarningFilled", "M8,2 L15,14 L1,14 Z M7,6 L9,6 L9,10 L7,10 Z M7,11.5 L9,11.5 L9,13 L7,13 Z" },
        // Banned
        { "Banned", "M8,2 A6,6 0 1 1 8,14 A6,6 0 1 1 8,2 Z M3.5,4.5 L12.5,13.5" },
        { "BannedFilled", _bannedSymbol },
        // Search
        { "Search", "M7,2 A5,5 0 1 1 7,12 A5,5 0 1 1 7,2 Z M10.5,10.5 L13.5,13.5" },
        { "SearchFilled", "M7,2 A5,5 0 1 1 7,12 A5,5 0 1 1 7,2 Z M10.5,10.5 L13.5,13.5 L12.1,14.9 L9.1,11.9 Z" },
        // Arrows
        { "ArrowRight", "M4,2 L12,8 L4,14 Z" },
        { "ArrowLeft", "M12,2 L4,8 L12,14 Z" },
        { "ArrowUp", "M2,12 L8,4 L14,12 Z" },
        { "ArrowDown", "M2,4 L8,12 L14,4 Z" },
        // Play / Pause / Stop
        { "PlayFilled", "M3,2 L13,8 L3,14 Z" },
        { "PauseFilled", "M3,2 L7,2 L7,14 L3,14 Z M9,2 L13,2 L13,14 L9,14 Z" },
        { "StopFilled", "M3,3 L13,3 L13,13 L3,13 Z" },
        // Star
        { "StarFilled", "M8,1 L10.1,6.1 L15.7,6.2 L11.2,9.7 L12.8,15 L8,12.1 L3.2,15 L4.8,9.7 L0.3,6.2 L5.9,6.1 Z" },
        // Heart
        { "HeartFilled", "M8,14 L2.2,8.2 C0.7,6.7 0.8,4.3 2.4,2.9 C3.8,1.7 5.9,1.9 7.2,3.2 L8,4 L8.8,3.2 C10.1,1.9 12.2,1.7 13.6,2.9 C15.2,4.3 15.3,6.7 13.8,8.2 Z" },
        // Folder / Document
        { "FolderFilled", "M2,4 L6,4 L8,2 L14,2 L14,14 L2,14 Z" },
        { "DocumentFilled", "M4,2 L10,2 L14,6 L14,14 L4,14 Z M10,2 L10,6 L14,6" },
        // Delete
        { "DeleteFilled", "M4,4 L12,4 L11,14 L5,14 Z M6,2 L10,2 L10,4 L6,4 Z" }
    };

    /// <summary>
    /// Creates a Path icon by name.
    /// <code>
    ///   /* Fill-based icon */
    ///   menuItem.Icon = MenuIconFactory.Create("CloseFilled");
    ///   /* Stroke-based icon */
    ///   menuItem.Icon = MenuIconFactory.Create("Close", stroke: Brushes.White, fill: null);
    /// </code>
    /// </summary>
    public static Path Create(string name, double? size = null, Brush? fill = null)
    {
        if (!_icons.TryGetValue(name, out var data))
        {
            data = _bannedSymbol; // not found, use fallback
            fill = new SolidColorBrush(Color.FromRgb(230, 20, 40)); // red
        }
        return new Path
        {
            Data = Geometry.Parse(data),
            Fill = fill ?? DefaultFill,
            Width = size ?? DefaultSize,
            Height = size ?? DefaultSize,
            Stroke = fill ?? DefaultFill,
            StrokeThickness = 1.5,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Stretch = Stretch.Uniform
        };
    }

    /// <summary>
    /// Creates a Path icon by name.
    /// <code>
    ///   /* Fill-based icon */
    ///   menuItem.Icon = MenuIconFactory.Create("CloseFilled");
    ///   /* Stroke-based icon */
    ///   menuItem.Icon = MenuIconFactory.Create("Close", stroke: Brushes.White, fill: null);
    /// </code>
    /// </summary>
    public static Path Create(string name, double? size = null, Brush? fill = null, Brush? stroke = null, double strokeThickness = 1.5, PenLineCap lineCap = PenLineCap.Round, PenLineJoin lineJoin = PenLineJoin.Round)
    {
        if (!_icons.TryGetValue(name, out var data))
        {
            data = _bannedSymbol; // your EvenOdd-filled fallback (red)
            fill ??= new SolidColorBrush(Color.FromRgb(220, 20, 60));
        }

        var path = new Path
        {
            Data = Geometry.Parse(data),
            Width = size ?? DefaultSize,
            Height = size ?? DefaultSize,
            Stretch = Stretch.Uniform
        };

        if (stroke != null || (fill == null && IsStrokeIconName(name)))
        {
            path.Stroke = stroke ?? DefaultFill;
            path.StrokeThickness = strokeThickness;
            path.StrokeStartLineCap = lineCap;
            path.StrokeEndLineCap = lineCap;
            path.StrokeLineJoin = lineJoin;
        }
        else
        {
            path.Fill = fill ?? DefaultFill;
        }

        return path;

        static bool IsStrokeIconName(string n) => n.EndsWith("Outline", StringComparison.OrdinalIgnoreCase) || n is "Close" or "Plus" or "Minus" or "Check" or "Info" or "Search" or "Banned" or "Power";
    }

    /// <summary>
    /// Returns a random icon from the dictionary.
    /// </summary>
    public static Path CreateRandom(double? size = null, Brush? fill = null)
    {
        if (_icons.Count == 0)
        {
            // No icons defined — return banned symbol
            return new Path
            {
                Data = Geometry.Parse(_bannedSymbol),
                Fill = fill ?? new SolidColorBrush(Color.FromRgb(220, 20, 60)),
                Width = size ?? DefaultSize,
                Height = size ?? DefaultSize,
                Stroke = fill ?? DefaultFill,
                StrokeThickness = 1,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Stretch = Stretch.Uniform
            };
        }
        var randomKey = _icons.Keys.ElementAt(Random.Shared.Next(_icons.Count));
        Debug.WriteLine($"[INFO] Random factory icon '{randomKey}'");

        if (fill != null)
        {
            return Create(randomKey, size, fill);
        }
        else
        {
            //return Create(randomKey, size, Extensions.CreateRandomLightBrush());

            // 70% Red — 30% Orange, dark
            var warmDark = Extensions.CreateRandomDarkBrush(new Dictionary<ColorTilt, double>
            {
                { ColorTilt.Red, 0.7 },
                { ColorTilt.Orange, 0.3 }
            });
            // 70% Red — 30% Orange, bright
            var warmLight = Extensions.CreateRandomLightBrush(new Dictionary<ColorTilt, double>
            {
                { ColorTilt.Red, 0.7 },
                { ColorTilt.Orange, 0.3 }
            });
            // 50% Blue — 40% Purple, dark
            var coolDark = Extensions.CreateRandomDarkBrush(new Dictionary<ColorTilt, double>
            {
                { ColorTilt.Blue, 0.5 },
                { ColorTilt.Purple, 0.4 }
            });
            // 50% Blue — 40% Purple, light
            var coolLight = Extensions.CreateRandomLightBrush(new Dictionary<ColorTilt, double>
            {
                { ColorTilt.Blue, 0.5 },
                { ColorTilt.Purple, 0.4 }
            });

            var verdeLight = Extensions.CreateRandomLightBrush(ColorTilt.Green);

            // Make it more vivid by +0.2 saturation
            //var vivid = Extensions.ShiftSaturation(warmLight, 0.2);

            // Make it more muted by -0.2 saturation
            //var muted = Extensions.ShiftSaturation(coolLight, -0.2);

            return Create(randomKey, size, verdeLight);
        }
    }

}

