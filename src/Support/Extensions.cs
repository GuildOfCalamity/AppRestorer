using System;
using System.Collections.Concurrent;
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
        #region [Logger with automatic duplicate checking]
        static HashSet<string> _logCache = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        static DateTime _logCacheUpdated = DateTime.Now;
        static int _repeatAllowedSeconds = 5;
        public static void WriteToLog(this string message, string fileName = "AppLog.txt")
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            if (_logCache.Add(message))
            {
                _logCacheUpdated = DateTime.Now;
                try { System.IO.File.AppendAllText(fileName, $"{message}{Environment.NewLine}"); }
                catch (Exception) { }
                
            }
            else
            {
                var diff = DateTime.Now - _logCacheUpdated;
                if (diff.Seconds > _repeatAllowedSeconds)
                    _logCache.Clear();
                else
                    Debug.WriteLine($"[LOGGING] Duplicate not allowed: {diff.Seconds}secs < {_repeatAllowedSeconds}secs");
            }
        }
        #endregion

        /// <summary>
        /// An updated string truncation helper.
        /// </summary>
        /// <remarks>
        /// This can be helpful when the CharacterEllipsis TextTrimming Property is not available.
        /// </remarks>
        public static string Truncate(this string text, int maxLength, string mesial = "…")
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            if (maxLength > 0 && text.Length > maxLength)
            {
                var limit = maxLength / 2;
                if (limit > 1)
                {
                    return String.Format("{0}{1}{2}", text.Substring(0, limit).Trim(), mesial, text.Substring(text.Length - limit).Trim());
                }
                else
                {
                    var tmp = text.Length <= maxLength ? text : text.Substring(0, maxLength).Trim();
                    return String.Format("{0}{1}", tmp, mesial);
                }
            }
            return text;
        }

        /// <summary>
        /// De-dupe file reader using a <see cref="HashSet{string}"/>.
        /// </summary>
        public static HashSet<string> ReadLines(string path)
        {
            if (!System.IO.File.Exists(path))
                return new HashSet<string>();
            return new HashSet<string>(System.IO.File.ReadAllLines(path), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// De-dupe file writer using a <see cref="HashSet{string}"/>.
        /// </summary>
        public static bool WriteLines(string path, IEnumerable<string> lines)
        {
            var output = new HashSet<string>(lines, StringComparer.OrdinalIgnoreCase);
            using (System.IO.TextWriter writer = System.IO.File.CreateText(path))
            {
                foreach (var line in output)
                    writer.WriteLine(line);
            }
            return true;
        }

        /// <summary>
        /// Converts long file size into typical browser file size.
        /// </summary>
        public static string ToFileSize(this long size)
        {
            if (size < 1024) { return (size).ToString("F0") + " Bytes"; }
            if (size < Math.Pow(1024, 2)) { return (size / 1024).ToString("F0") + " KB"; }
            if (size < Math.Pow(1024, 3)) { return (size / Math.Pow(1024, 2)).ToString("F1") + " MB"; }
            if (size < Math.Pow(1024, 4)) { return (size / Math.Pow(1024, 3)).ToString("F1") + " GB"; }
            if (size < Math.Pow(1024, 5)) { return (size / Math.Pow(1024, 4)).ToString("F1") + " TB"; }
            if (size < Math.Pow(1024, 6)) { return (size / Math.Pow(1024, 5)).ToString("F1") + " PB"; }
            return (size / Math.Pow(1024, 6)).ToString("F1") + " EB";
        }

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

        /// <summary>
        /// Convert a <see cref="DateTime"/> object into an ISO 8601 formatted string.
        /// </summary>
        /// <param name="dateTime"><see cref="DateTime"/></param>
        /// <returns>ISO 8601 formatted string</returns>
        /// <remarks>You can also use <c>DateTime.UtcNow.ToString("o")</c></remarks>
        public static string ToJsonFriendlyFormat(this DateTime dateTime) => dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");

        /// <summary>
        /// Converts a JSON date time format string "yyyy-MM-ddTHH:mm:ssZ" into a DateTime object.
        /// </summary>
        /// <param name="jsonDateTimeString">The JSON date time string to convert (e.g., "2023-10-27T10:30:00Z").</param>
        /// <returns>A DateTime object representing the parsed date and time, or null if the string is invalid.</returns>
        public static DateTime ParseJsonDateTime(this string jsonDateTimeString)
        {
            if (string.IsNullOrEmpty(jsonDateTimeString))
                return DateTime.MinValue;

            try
            {
                return DateTime.ParseExact(jsonDateTimeString, "yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal);
            }
            catch (FormatException)
            {
                return DateTime.MinValue;
            }
        }

        public static IEnumerable<T> JoinLists<T>(this IEnumerable<T> list1, IEnumerable<T> list2)
        {
            var joined = new[] { list1, list2 }.Where(x => x != null).SelectMany(x => x);
            return joined ?? Enumerable.Empty<T>();
        }
        public static IEnumerable<T> JoinMany<T>(params IEnumerable<T>[] array)
        {
            var final = array.Where(x => x != null).SelectMany(x => x);
            return final ?? Enumerable.Empty<T>();
        }

        public static void ForEach<T>(this IEnumerable<T> ie, Action<T> action, Action<Exception>? onError = null)
        {
            foreach (var i in ie)
            {
                try { action(i); }
                catch (Exception ex) { onError?.Invoke(ex); }
            }
        }

        public static string NameOf(this object o)
        {
            if (o == null)
                return "null";

            // Similar: System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name
            return $"{o?.GetType()?.Name} ⇒ {o?.GetType()?.BaseType?.Name}";
        }

        public static bool IsDisposable(this Type type)
        {
            if (!typeof(IDisposable).IsAssignableFrom(type))
                return false;

            return true;
        }

        public static bool IsClonable(this Type type)
        {
            if (!typeof(ICloneable).IsAssignableFrom(type))
                return false;
            return true;
        }

        public static bool IsComparable(this Type type)
        {
            if (!typeof(IComparable).IsAssignableFrom(type))
                return false;
            return true;
        }

        public static bool IsConvertible(this Type type)
        {
            if (!typeof(IConvertible).IsAssignableFrom(type))
                return false;
            return true;
        }

        public static bool IsFormattable(this Type type)
        {
            if (!typeof(IFormattable).IsAssignableFrom(type))
                return false;
            return true;
        }

        public static bool IsEnumerable<T>(this Type type)
        {
            if (!typeof(IEnumerable<T>).IsAssignableFrom(type))
                return false;
            return true;
        }

        /// <summary>
        ///   Generic retry mechanism with 2-second retry until <paramref name="attempts"/>.
        /// </summary>
        public static T Retry<T>(this Func<T> operation, int attempts)
        {
            while (true)
            {
                try
                {
                    attempts--;
                    return operation();
                }
                catch (Exception ex) when (attempts > 0)
                {
                    Debug.WriteLine($"[ERROR] Failed: {ex.Message}");
                    Debug.WriteLine($"[INFO] Attempts left: {attempts}");
                    Thread.Sleep(2000);
                }
            }
        }

        /// <summary>
        ///   Generic retry mechanism with exponential back-off
        /// <example><code>
        ///   Retry(() => MethodThatHasNoReturnValue());
        /// </code></example>
        /// </summary>
        public static void Retry(this Action action, int maxRetry = 3, int retryDelay = 1000)
        {
            int retries = 0;
            while (true)
            {
                try
                {
                    action();
                    break;
                }
                catch (Exception ex)
                {
                    retries++;
                    if (retries > maxRetry)
                    {
                        throw new TimeoutException($"Operation failed after {maxRetry} retries: {ex.Message}", ex);
                    }
                    Debug.WriteLine($"[ERROR] Retry {retries}/{maxRetry} after failure: {ex.Message}. Retrying in {retryDelay} ms...");
                    Thread.Sleep(retryDelay);
                    retryDelay *= 2; // Double the delay after each attempt.
                }
            }
        }

        /// <summary>
        ///   Modified retry mechanism for return value with exponential back-off.
        /// <example><code>
        ///   int result = Retry(() => MethodThatReturnsAnInteger());
        /// </code></example>
        /// </summary>
        public static T Retry<T>(this Func<T> func, int maxRetry = 3, int retryDelay = 1000)
        {
            int retries = 0;
            while (true)
            {
                try
                {
                    return func();
                }
                catch (Exception ex)
                {
                    retries++;
                    if (retries > maxRetry)
                    {
                        throw new TimeoutException($"Operation failed after {maxRetry} retries: {ex.Message}", ex);
                    }
                    Debug.WriteLine($"[ERROR] Retry {retries}/{maxRetry} after failure: {ex.Message}. Retrying in {retryDelay} ms...");
                    Thread.Sleep(retryDelay);
                    retryDelay *= 2; // Double the delay after each attempt.
                }
            }
        }

        /// <summary>
        ///   Generic retry mechanism with exponential back-off
        /// <example><code>
        ///   await RetryAsync(() => AsyncMethodThatHasNoReturnValue());
        /// </code></example>
        /// </summary>
        public static async Task RetryAsync(this Func<Task> action, int maxRetry = 3, int retryDelay = 1000)
        {
            int retries = 0;
            while (true)
            {
                try
                {
                    await action();
                    break;
                }
                catch (Exception ex)
                {
                    retries++;
                    if (retries > maxRetry)
                    {
                        throw new InvalidOperationException($"Operation failed after {maxRetry} retries: {ex.Message}", ex);
                    }
                    Debug.WriteLine($"[ERROR] Retry {retries}/{maxRetry} after failure: {ex.Message}. Retrying in {retryDelay} ms...");
                    await Task.Delay(retryDelay);
                    retryDelay *= 2; // Double the delay after each attempt.
                }
            }
        }

        /// <summary>
        ///   Modified retry mechanism for return value with exponential back-off.
        /// <example><code>
        ///   int result = await RetryAsync(() => AsyncMethodThatReturnsAnInteger());
        /// </code></example>
        /// </summary>
        public static async Task<T> RetryAsync<T>(this Func<Task<T>> func, int maxRetry = 3, int retryDelay = 1000)
        {
            int retries = 0;
            while (true)
            {
                try
                {
                    return await func();
                }
                catch (Exception ex)
                {
                    retries++;
                    if (retries > maxRetry)
                    {
                        throw new InvalidOperationException($"Operation failed after {maxRetry} retries: {ex.Message}", ex);
                    }
                    Debug.WriteLine($"[ERROR] Retry {retries}/{maxRetry} after failure: {ex.Message}. Retrying in {retryDelay} ms...");
                    await Task.Delay(retryDelay);
                    retryDelay *= 2; // Double the delay after each attempt.
                }
            }
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

        /// <summary>
        /// Home-brew parallel invoke that will not block while actions run.
        /// </summary>
        /// <param name="actions">array of <see cref="Action"/>s</param>
        public static void ParallelInvokeAndForget(params Action[] action)
        {
            action.ForEach(a =>
            {
                try
                {
                    ThreadPool.QueueUserWorkItem((obj) => { a.Invoke(); });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] ParallelInvokeAndForget: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// An un-optimized, home-brew parallel for each implementation.
        /// </summary>
        public static void ParallelForEach<T>(IEnumerable<T> source, Action<T> action)
        {
            var tasks = from item in source select Task.Run(() => action(item));
            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// An optimized, home-brew parallel ForEach implementation.
        /// Creates branched execution based on available processors.
        /// </summary>
        public static void ParallelForEachUsingEnumerator<T>(IEnumerable<T> source, Action<T> action, Action<Exception> onError)
        {
            IEnumerator<T> e = source.GetEnumerator();
            IEnumerable<Task> tasks = from i
                 in Enumerable.Range(0, Environment.ProcessorCount)
                                      select Task.Run(() =>
                                      {
                                          while (true)
                                          {
                                              T item;
                                              lock (e)
                                              {
                                                  if (!e.MoveNext()) { return; }
                                                  item = e.Current;
                                              }
                                              #region [Must stay outside locking scope, or defeats the purpose of parallelism]
                                              try
                                              {
                                                  action(item);
                                              }
                                              catch (Exception ex)
                                              {
                                                  onError?.Invoke(ex);
                                              }
                                              #endregion
                                          }
                                      });
            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// An optimized, home-brew parallel ForEach implementation.
        /// Creates branched execution based on available processors.
        /// </summary>
        public static void ParallelForEachUsingPartitioner<T>(IEnumerable<T> source, Action<T> action, Action<Exception> onError, EnumerablePartitionerOptions options = EnumerablePartitionerOptions.NoBuffering)
        {
            //IList<IEnumerator<T>> partitions = Partitioner.Create(source, options).GetPartitions(Environment.ProcessorCount);
            IEnumerable<Task> tasks = from partition
                in Partitioner.Create(source, options).GetPartitions(Environment.ProcessorCount)
                                      select Task.Run(() =>
                                      {
                                          using (partition) // partitions are disposable
                                          {
                                              while (partition.MoveNext())
                                              {
                                                  try
                                                  {
                                                      action(partition.Current);
                                                  }
                                                  catch (Exception ex)
                                                  {
                                                      onError?.Invoke(ex);
                                                  }
                                              }
                                          }
                                      });
            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// An optimized, home-brew parallel ForEach implementation.
        /// </summary>
        public static void ParallelForEachUsingPartitioner<T>(IList<T> list, Action<T> action, Action<Exception> onError, EnumerablePartitionerOptions options = EnumerablePartitionerOptions.NoBuffering)
        {
            //IList<IEnumerator<T>> partitions = Partitioner.Create(list, options).GetPartitions(Environment.ProcessorCount);
            IEnumerable<Task> tasks = from partition
                in Partitioner.Create(list, options).GetPartitions(Environment.ProcessorCount)
                                      select Task.Run(() =>
                                      {
                                          using (partition) // partitions are disposable
                                          {
                                              while (partition.MoveNext())
                                              {
                                                  try
                                                  {
                                                      action(partition.Current);
                                                  }
                                                  catch (Exception ex)
                                                  {
                                                      onError?.Invoke(ex);
                                                  }
                                              }
                                          }
                                      });
            Task.WaitAll(tasks.ToArray());
        }

        #region [Task Helpers]
        /// <summary>
        /// Task.Factory.StartNew (() => { throw null; }).IgnoreExceptions();
        /// </summary>
        public static void IgnoreExceptions(this Task task, Action<Exception>? errorHandler = null)
        {
            task.ContinueWith(t =>
            {
                AggregateException? ignore = t.Exception;
                ignore?.Flatten().Handle(ex =>
                {
                    if (errorHandler != null)
                        errorHandler(ex);

                    return true; // don't re-throw
                });

            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Chainable task helper.
        /// var result = await SomeLongAsyncFunction().WithTimeout(TimeSpan.FromSeconds(2));
        /// </summary>
        /// <typeparam name="TResult">the type of task result</typeparam>
        /// <returns><see cref="Task"/>TResult</returns>
        public async static Task<TResult> WithTimeout<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            Task winner = await (Task.WhenAny(task, Task.Delay(timeout)));

            if (winner != task)
                throw new TimeoutException();

            return await task; // Unwrap result/re-throw
        }

        /// <summary>
        /// Task extension to add a timeout.
        /// </summary>
        /// <returns>The task with timeout.</returns>
        /// <param name="task">Task.</param>
        /// <param name="timeoutInMilliseconds">Timeout duration in Milliseconds.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public async static Task<T> WithTimeout<T>(this Task<T> task, int timeoutInMilliseconds)
        {
            var retTask = await Task.WhenAny(task, Task.Delay(timeoutInMilliseconds)).ConfigureAwait(false);

            #pragma warning disable CS8603 // Possible null reference return.
            return retTask is Task<T> ? task.Result : default;
            #pragma warning restore CS8603 // Possible null reference return.
        }

        /// <summary>
        /// Chainable task helper.
        /// var result = await SomeLongAsyncFunction().WithCancellation(cts.Token);
        /// </summary>
        /// <typeparam name="TResult">the type of task result</typeparam>
        /// <returns><see cref="Task"/>TResult</returns>
        public static Task<TResult> WithCancellation<TResult>(this Task<TResult> task, CancellationToken cancelToken)
        {
            TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
            CancellationTokenRegistration reg = cancelToken.Register(() => tcs.TrySetCanceled());
            task.ContinueWith(ant =>
            {
                reg.Dispose(); // NOTE: it's important to dispose of CancellationTokenRegistrations or they will hand around in memory until the application closes
                if (ant.IsCanceled)
                    tcs.TrySetCanceled();
                else if (ant.IsFaulted)
                    tcs.TrySetException(ant.Exception?.InnerException ?? ant.Exception ?? new Exception("No exception information available."));
                else
                    tcs.TrySetResult(ant.Result);
            });
            return tcs.Task; // Return the TaskCompletionSource result
        }

        public static Task<T> WithAllExceptions<T>(this Task<T> task)
        {
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

            task.ContinueWith(ignored =>
            {
                switch (task.Status)
                {
                    case TaskStatus.Canceled:
                        Debug.WriteLine($"[WARNING] TaskStatus.Canceled");
                        tcs.SetCanceled();
                        break;
                    case TaskStatus.RanToCompletion:
                        tcs.SetResult(task.Result);
                        //Debug.WriteLine($"[INFO] TaskStatus.RanToCompletion");
                        break;
                    case TaskStatus.Faulted:
                        // SetException will automatically wrap the original AggregateException
                        // in another one. The new wrapper will be removed in TaskAwaiter, leaving
                        // the original intact.
                        Debug.WriteLine($"[ERROR] TaskStatus.Faulted: {task.Exception?.Message}");
                        tcs.SetException(task.Exception ?? new Exception("No exception information available."));
                        break;
                    default:
                        Debug.WriteLine($"[ERROR] TaskStatus: Continuation called illegally.");
                        tcs.SetException(new InvalidOperationException("Continuation called illegally."));
                        break;
                }
            });

            return tcs.Task;
        }

        #pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
        /// <summary>
        /// Attempts to await on the task and catches exception
        /// </summary>
        /// <param name="task">Task to execute</param>
        /// <param name="onException">What to do when method has an exception</param>
        /// <param name="continueOnCapturedContext">If the context should be captured.</param>
        public static async void SafeFireAndForget(this Task task, Action<Exception>? onException = null, bool continueOnCapturedContext = false)
        #pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
        {
            try
            {
                await task.ConfigureAwait(continueOnCapturedContext);
            }
            catch (Exception ex) when (onException != null)
            {
                onException.Invoke(ex);
            }
            catch (Exception ex) when (onException == null)
            {
                Debug.WriteLine($"[WARNING] SafeFireAndForget: {ex.Message}");
            }
        }
        #endregion

        /// <summary>
        /// Fetch all referenced <see cref="System.Reflection.AssemblyName"/> used by the current process.
        /// </summary>
        /// <returns><see cref="List{T}"/></returns>
        public static List<string> ListAllAssemblies()
        {
            List<string> results = new List<string>();
            try
            {
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                System.Reflection.AssemblyName main = assembly.GetName();
                results.Add($"Main Assembly: {main.Name}, Version: {main.Version}");
                IOrderedEnumerable<System.Reflection.AssemblyName> names = assembly.GetReferencedAssemblies().OrderBy(o => o.Name);
                foreach (var sas in names)
                    results.Add($"Sub Assembly: {sas.Name}, Version: {sas.Version}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] ListAllAssemblies: {ex.Message}");
            }
            return results;
        }
    }
}
