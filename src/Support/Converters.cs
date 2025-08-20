using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace AppRestorer
{
    public class BoolToReverseConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var val = (bool)value;
            return !val;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var val = (bool)value;
            if (parameter is string param && (param.ToString().Equals("inverse", StringComparison.OrdinalIgnoreCase) || param.ToString().Equals("reverse", StringComparison.OrdinalIgnoreCase) || param.ToString().Equals("opposite", StringComparison.OrdinalIgnoreCase)))
                val = !val;
            return val ? Visibility.Visible : Visibility.Collapsed;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class BoolToImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool enabled = (bool)value;
                if (enabled)
                    return new BitmapImage(App.FavEnabled);
                else
                    return new BitmapImage(App.FavDisabled);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] BoolToImageConverter: {ex.Message}");
                return null;
            }
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class PathToImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string imagePath = (string)value;
                BitmapImage imageBitmap = new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute));
                return imageBitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] PathToImageConverter: {ex.Message}");
                return null;
            }
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }


    public class ImagePathConverter : IValueConverter
    {
        string imageDirectory = System.IO.Directory.GetCurrentDirectory();
        public string ImageDirectory
        {
            get { return imageDirectory; }
            set { imageDirectory = value; }
        }

        public object? Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                string imagePath = System.IO.Path.Combine(ImageDirectory, (string)value);
                return new BitmapImage(new Uri(imagePath));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] ImagePathConverter: {ex.Message}");
                return null;
            }
        }

        public object? ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
