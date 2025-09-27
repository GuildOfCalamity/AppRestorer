using System;
using System.Diagnostics;

namespace AppRestorer;

public static class HighResolutionDelay
{
    static readonly double TicksPerMicrosecond = (double)Stopwatch.Frequency / 1_000_000.0;
    static readonly double TicksPerNanosecond = (double)Stopwatch.Frequency / 1_000_000_000.0;

    /// <summary>
    /// Busy-wait delay for a specified number of microseconds.<br/>
    /// </summary>
    /// <param name="microseconds">The number of microseconds to delay.</param>
    public static void DelayMicroseconds(int microseconds)
    {
        if (microseconds <= 0)
            return;

        long start = Stopwatch.GetTimestamp();
        long targetTicks = (long)(microseconds * TicksPerMicrosecond);
        long end = start + targetTicks;
        while (Stopwatch.GetTimestamp() < end)
        {
            // Spin the core
        }
    }

    /// <summary>
    /// Busy-wait delay for a specified number of nanoseconds.<br/>
    /// Actual precision is limited by Stopwatch resolution.<br/>
    /// </summary>
    /// <remarks>
    /// This is best effort, resolution is ~30–100 nanoseconds at best.
    /// </remarks>
    public static void DelayNanoseconds(long nanoseconds)
    {
        if (nanoseconds <= 0)
            return;

        long start = Stopwatch.GetTimestamp();
        long targetTicks = (long)(nanoseconds * TicksPerNanosecond);
        long end = start + targetTicks;
        while (Stopwatch.GetTimestamp() < end)
        {
            // Spin the core
        }
    }

    /// <summary>
    /// Measures the smallest resolvable delay (tick size) in nanoseconds and microseconds.<br/>
    /// </summary>
    public static (double Nanoseconds, double Microseconds) CalibrateResolution()
    {
        // One Stopwatch tick duration in seconds
        double tickSeconds = 1.0 / Stopwatch.Frequency;
        double tickNanoseconds = tickSeconds * 1_000_000_000.0;
        double tickMicroseconds = tickSeconds * 1_000_000.0;
        return (tickNanoseconds, tickMicroseconds);
    }

    /// <summary>
    /// Tests the accuracy of a requested delay.<br/>
    /// Returns requested vs actual elapsed time in nanoseconds.<br/>
    /// </summary>
    public static (long RequestedNanoseconds, double ActualNanoseconds) TestDelayAccuracy(long requestedNanoseconds)
    {
        if (requestedNanoseconds <= 0)
            return (0, 0);

        long start = Stopwatch.GetTimestamp();
        DelayNanoseconds(requestedNanoseconds);
        long end = Stopwatch.GetTimestamp();
        double elapsedSeconds = (double)(end - start) / Stopwatch.Frequency;
        double elapsedNanoseconds = elapsedSeconds * 1_000_000_000.0;
        return (requestedNanoseconds, elapsedNanoseconds);
    }
}