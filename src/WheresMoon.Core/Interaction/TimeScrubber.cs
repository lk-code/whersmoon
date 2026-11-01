namespace WheresMoon.Core.Interaction;

public enum InfoPage
{
    SunAndMoon,
    Events,
    LightHours,
}

/// <summary>
/// Turns horizontal movement into a chosen time. Slow movement catches on nearby events, and the
/// pointer then has to pull a little further to get loose. Fast movement passes them by.
/// </summary>
public sealed class TimeScrubber
{
    /// <summary>Above this speed, in screen widths per second, events do not catch.</summary>
    public const double SlowSpeed = 0.3;

    /// <summary>How close to an event the time must come, as a share of the screen width.</summary>
    public const double CaptureShare = 0.02;

    /// <summary>How far the pointer must pull to get loose, as a share of the screen width.</summary>
    public const double ReleaseShare = 0.05;

    private DateTimeOffset _free;
    private DateTimeOffset? _justLeft;
    private double _pull;

    /// <summary>The chosen time: the event while caught on one, otherwise the time under the pointer.</summary>
    public DateTimeOffset Time { get; private set; }

    /// <summary>The event the time is caught on, if any.</summary>
    public DateTimeOffset? Caught { get; private set; }

    /// <summary>Time one screen width stands for on a page.</summary>
    public static TimeSpan ScaleOf(InfoPage page) =>
        page == InfoPage.Events ? TimeSpan.FromDays(30) : TimeSpan.FromHours(24);

    /// <summary>How far from the current time events can matter to the next step.</summary>
    public static TimeSpan ReachOf(InfoPage page) => ScaleOf(page) * CaptureShare * 2.0;

    /// <summary>
    /// Turns a pointer's movement or speed into one where positive means later. The time is pulled along
    /// like a strip that holds the future on its right, so moving the pointer to the left brings it in.
    /// </summary>
    public static double TowardsLater(double pointer) => -pointer;

    public void Begin(DateTimeOffset time)
    {
        Time = time;
        _free = time;
        _pull = 0.0;
        _justLeft = null;
        Caught = null;
    }

    /// <param name="distance">Movement in device independent units. Positive means later.</param>
    /// <param name="speed">Current speed of the movement in device independent units per second.</param>
    /// <param name="width">Width of the screen in the same units.</param>
    /// <param name="scale">Time one screen width stands for.</param>
    /// <param name="events">Events near the current time.</param>
    /// <returns>True when the time just caught on an event.</returns>
    public bool Move(double distance, double speed, double width, TimeSpan scale, IReadOnlyList<DateTimeOffset> events)
    {
        TimeSpan step = scale * (distance / width);
        bool isSlow = Math.Abs(speed) / width <= SlowSpeed;

        if (Caught is DateTimeOffset held)
        {
            _pull += distance;

            if (isSlow && Math.Abs(_pull) < ReleaseShare * width)
            {
                return false;
            }

            // Continuing from the event instead of the pointer keeps the sky from jumping on release.
            Caught = null;
            _justLeft = held;
            _free = held + step;
            Time = _free;
            return false;
        }

        _free += step;
        Time = _free;

        TimeSpan capture = scale * CaptureShare;
        if (_justLeft is DateTimeOffset left && (Time - left).Duration() > capture)
        {
            _justLeft = null;
        }

        if (!isSlow)
        {
            return false;
        }

        DateTimeOffset? nearest = null;
        foreach (DateTimeOffset candidate in events)
        {
            bool isCloser = nearest is null || (candidate - Time).Duration() < (nearest.Value - Time).Duration();
            if (isCloser && candidate != _justLeft && (candidate - Time).Duration() <= capture)
            {
                nearest = candidate;
            }
        }

        if (nearest is not DateTimeOffset caught)
        {
            return false;
        }

        Caught = caught;
        Time = caught;
        _pull = 0.0;
        return true;
    }

    /// <summary>A step of fixed size, as from a key press, which does not catch on events.</summary>
    public void Step(TimeSpan step)
    {
        Caught = null;
        _free = Time + step;
        Time = _free;
    }
}
