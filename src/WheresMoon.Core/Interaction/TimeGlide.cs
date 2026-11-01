namespace WheresMoon.Core.Interaction;

/// <summary>
/// Carries the time on after the pointer lets go, as if the swipe had given it a push. Friction first takes
/// off as much speed as a slow swipe has, so that one stops with the finger, and then slows the rest down.
/// </summary>
public sealed class TimeGlide
{
    /// <summary>Seconds in which the speed falls to about a third, frame rate aside.</summary>
    public const double DecaySeconds = 0.6;

    /// <summary>Below this speed, in screen widths per second, the glide is over.</summary>
    public const double EndSpeed = 0.02;

    private double _endSpeed;

    /// <summary>Current speed in device independent units per second. Positive means later.</summary>
    public double Speed { get; private set; }

    public bool IsRunning => Speed != 0.0;

    /// <param name="speed">Speed as the pointer let go, in device independent units per second. Positive means later.</param>
    /// <param name="width">Width of the screen in the same units.</param>
    public void Push(double speed, double width)
    {
        double left = Math.Abs(speed) - TimeScrubber.SlowSpeed * width;

        _endSpeed = EndSpeed * width;
        Speed = left > _endSpeed ? Math.CopySign(left, speed) : 0.0;
    }

    /// <summary>Lets time pass and returns the distance covered in it, the same however it is cut into frames.</summary>
    public double Advance(double seconds)
    {
        double remaining = Math.Exp(-seconds / DecaySeconds);
        double distance = Speed * DecaySeconds * (1.0 - remaining);

        Speed *= remaining;
        if (Math.Abs(Speed) < _endSpeed)
        {
            Speed = 0.0;
        }

        return distance;
    }

    public void Stop() => Speed = 0.0;
}
