using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace AppRestorer
{
    public static class Extensions
    {
        /// <summary>
        /// Display a readable sentence as to when the time will happen.
        /// e.g. "in one second" or "in 2 days"
        /// </summary>
        /// <param name="value"><see cref="TimeSpan"/>the future time to compare from now</param>
        /// <returns>human friendly format</returns>
        public static string ToReadableTime(this TimeSpan value, bool reportMilliseconds = false)
        {
            double delta = value.TotalSeconds;
            if (delta < 1 && !reportMilliseconds) { return "less than one second"; }
            if (delta < 1 && reportMilliseconds) { return $"{value.TotalMilliseconds:N1} milliseconds"; }
            if (delta < 60) { return value.Seconds == 1 ? "one second" : value.Seconds + " seconds"; }
            if (delta < 120) { return "a minute"; }                  // 2 * 60
            if (delta < 3000) { return value.Minutes + " minutes"; } // 50 * 60
            if (delta < 5400) { return "an hour"; }                  // 90 * 60
            if (delta < 86400) { return value.Hours + " hours"; }    // 24 * 60 * 60
            if (delta < 172800) { return "one day"; }                // 48 * 60 * 60
            if (delta < 2592000) { return value.Days + " days"; }    // 30 * 24 * 60 * 60
            if (delta < 31104000)                                    // 12 * 30 * 24 * 60 * 60
            {
                int months = Convert.ToInt32(Math.Floor((double)value.Days / 30));
                return months <= 1 ? "one month" : months + " months";
            }
            int years = Convert.ToInt32(Math.Floor((double)value.Days / 365));
            return years <= 1 ? "one year" : years + " years";
        }

        /// <summary>
        /// Returns a description like: "today at 3:45 PM (in 1h 20m)" or "on Fri at 9:00 AM (in 2d 4h)"
        /// </summary>
        public static string DescribeFutureTime(this TimeSpan delta, DateTimeOffset? reference = null, CultureInfo? culture = null)
        {
            if (delta < TimeSpan.Zero)
                delta = TimeSpan.Zero; // clamp

            var now = reference ?? DateTimeOffset.Now;
            var target = now + delta;

            culture ??= CultureInfo.CurrentCulture;

            string when = DescribeDayAndTime(now, target, culture);
            string rel = DescribeRelative(delta);

            return $"{when} ({rel})";
        }

        /// <summary>
        /// Optional version: Returns both the target and the description.
        /// </summary>
        public static (DateTimeOffset Target, string Description) DescribeFutureTimeWithTarget(TimeSpan delta, DateTimeOffset? reference = null, CultureInfo? culture = null)
        {
            if (delta < TimeSpan.Zero)
                delta = TimeSpan.Zero;

            var now = reference ?? DateTimeOffset.Now;
            var target = now + delta;
            return (target, DescribeFutureTime(delta, now, culture));
        }

        static string DescribeDayAndTime(DateTimeOffset now, DateTimeOffset target, CultureInfo culture)
        {
            var today = now.Date;
            var tDate = target.Date;

            string dayPart;

            if (tDate == today)
                dayPart = "today";
            else if (tDate == today.AddDays(1))
                dayPart = "tomorrow";
            else if (tDate <= today.AddDays(7)) // e.g., "on Tue"
                dayPart = "on " + culture.DateTimeFormat.AbbreviatedDayNames[(int)tDate.DayOfWeek];
            else // e.g., "on Aug 25, 2025"
                dayPart = "on " + target.ToString(culture.DateTimeFormat.ShortDatePattern, culture);

            string timePart = target.ToLocalTime().ToString("t", culture); // short time

            return $"{dayPart} at {timePart}";
        }

        static string DescribeRelative(TimeSpan delta)
        {
            if (delta < TimeSpan.FromSeconds(1))
                return "now";

            int components = 0;
            var sb = new StringBuilder();

            void add(string label, long value)
            {
                if (value <= 0 || components >= 2) 
                    return;
                if (sb.Length > 0) 
                    sb.Append(' ');
                sb.Append(value).Append(label);
                components++;
            }

            add("day", (long)delta.TotalDays);
            delta -= TimeSpan.FromDays((long)delta.TotalDays);

            add("hr", (long)delta.TotalHours);
            delta -= TimeSpan.FromHours((long)delta.TotalHours);

            add("min", (long)delta.TotalMinutes);
            delta -= TimeSpan.FromMinutes((long)delta.TotalMinutes);

            if (components < 2)
            {
                add("sec", (long)Math.Round(delta.TotalSeconds));
            }

            return $"in {sb}";
        }

        /// <summary>
        /// Generate a random brush
        /// </summary>
        /// <returns><see cref="SolidColorBrush"/></returns>
        public static SolidColorBrush GenerateRandomBrush()
        {
            byte r = (byte)Random.Shared.Next(0, 256);
            byte g = (byte)Random.Shared.Next(0, 256);
            byte b = (byte)Random.Shared.Next(0, 256);
            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
        }

        /// <summary>
        /// Generate a random color
        /// </summary>
        /// <returns><see cref="System.Windows.Media.Color"/></returns>
        public static System.Windows.Media.Color GenerateRandomColor()
        {
            byte r = (byte)Random.Shared.Next(0, 256);
            byte g = (byte)Random.Shared.Next(0, 256);
            byte b = (byte)Random.Shared.Next(0, 256);
            return System.Windows.Media.Color.FromRgb(r, g, b);
        }

        /// <summary>
        /// Fetch all <see cref="System.Windows.Media.Brushes"/>.
        /// </summary>
        /// <returns><see cref="List{T}"/></returns>
        public static List<Brush> GetAllMediaBrushes()
        {
            List<Brush> brushes = new List<Brush>();
            Type brushesType = typeof(Brushes);

            //TypeAttributes ta = typeof(Brushes).Attributes;
            //Debug.WriteLine($"[INFO] TypeAttributes: {ta}");

            // Iterate through the static properties of the Brushes class type.
            foreach (PropertyInfo pi in brushesType.GetProperties(BindingFlags.Static | BindingFlags.Public))
            {
                // Check if the property type is Brush/SolidColorBrush
                if (pi != null && (pi.PropertyType == typeof(Brush) || pi.PropertyType == typeof(SolidColorBrush)))
                {
                    if (pi.Name.Contains("Transparent"))
                        continue;

                    Debug.WriteLine($"[INFO] Adding brush '{pi.Name}'");

                    // Get the brush value from the static property
                    var br = (Brush?)pi?.GetValue(null, null);
                    if (br != null)
                        brushes.Add(br);
                }
            }
            return brushes;
        }

        /// <summary>
        /// 'BitmapCacheBrush','DrawingBrush','GradientBrush','ImageBrush',
        /// 'LinearGradientBrush','RadialGradientBrush','SolidColorBrush',
        /// 'TileBrush','VisualBrush','ImplicitInputBrush'
        /// </summary>
        /// <returns><see cref="List{T}"/></returns>
        public static List<Type> GetAllDerivedBrushClasses()
        {
            List<Type> derivedBrushes = new List<Type>();
            // Get the assembly containing the Brush class
            Assembly assembly = typeof(Brush).Assembly;
            try
            {   // Iterate through all types in the assembly
                foreach (Type type in assembly.GetTypes())
                {
                    // Check if the type is a subclass of Brush
                    if (type.IsSubclassOf(typeof(Brush)))
                    {
                        //Debug.WriteLine($"[INFO] Adding type '{type.Name}'");
                        derivedBrushes.Add(type);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] GetAllDerivedBrushClasses: {ex.Message}");
            }
            return derivedBrushes;
        }

        /// <summary>
        /// Fetch all derived types from a super class.
        /// </summary>
        /// <returns><see cref="List{T}"/></returns>
        public static List<Type> GetDerivedSubClasses<T>(T objectClass) where T : class
        {
            List<Type> derivedClasses = new List<Type>();
            // Get the assembly containing the base class
            Assembly assembly = typeof(T).Assembly;
            try
            {   // Iterate through all types in the assembly
                foreach (Type type in assembly.GetTypes())
                {
                    // Check if the type is a subclass of T
                    if (type.IsSubclassOf(typeof(T)))
                    {
                        //Debug.WriteLine($"[INFO] Adding subclass type '{type.Name}'");
                        derivedClasses.Add(type);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] GetDerivedClasses: {ex.Message}");
            }
            return derivedClasses;
        }

        /// <summary>
        /// Returns the Euclidean distance between two <see cref="System.Windows.Media.Color"/>s.
        /// </summary>
        /// <param name="color1">1st <see cref="System.Windows.Media.Color"/></param>
        /// <param name="color2">2nd <see cref="System.Windows.Media.Color"/></param>
        public static double ColorDistance(System.Windows.Media.Color color1, System.Windows.Media.Color color2)
        {
            return Math.Sqrt(Math.Pow(color1.R - color2.R, 2) + Math.Pow(color1.G - color2.G, 2) + Math.Pow(color1.B - color2.B, 2));
        }

        public const double Epsilon = 0.000000000001;
        public static bool IsZeroOrLess(this double value) => value < Epsilon;
        public static bool IsZeroOrLess(this float value) => value < (float)Epsilon;
        public static bool IsZero(this double value) => Math.Abs(value) < Epsilon;
        public static bool IsZero(this float value) => Math.Abs(value) < (float)Epsilon;
        public static bool IsInvalid(this double value)
        {
            if (value == double.NaN || value == double.NegativeInfinity || value == double.PositiveInfinity)
                return true;

            return false;
        }
        public static bool IsInvalidOrZero(this double value)
        {
            if (value == double.NaN || value == double.NegativeInfinity || value == double.PositiveInfinity || value <= 0)
                return true;

            return false;
        }
        public static bool IsOne(this double value)
        {
            return Math.Abs(value) >= 1d - Epsilon && Math.Abs(value) <= 1d + Epsilon;
        }
        public static bool AreClose(this double left, double right)
        {
            if (left == right)
                return true;

            double a = (Math.Abs(left) + Math.Abs(right) + 10.0d) * Epsilon;
            double b = left - right;
            return (-a < b) && (a > b);
        }
        public static bool AreClose(this float left, float right)
        {
            if (left == right)
                return true;

            float a = (Math.Abs(left) + Math.Abs(right) + 10.0f) * (float)Epsilon;
            float b = left - right;
            return (-a < b) && (a > b);
        }
    }
}
