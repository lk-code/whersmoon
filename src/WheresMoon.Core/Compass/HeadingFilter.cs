namespace WheresMoon.Core.Compass;

/// <summary>
/// Smooths a heading on its way to the screen. <see cref="Value"/> keeps counting past 360 degrees,
/// so that the step from 359 to 0 is a step of one degree. Very small changes are ignored, because
/// the noise of a resting sensor would otherwise redraw the scene all the time.
/// </summary>
public sealed class HeadingFilter
{
    private const double TimeConstantSeconds = 0.12;
    public const double Deadband = 0.6;

    private const double SettledDistance = 0.02;

    private double _target;
    private bool _hasValue;

    public double Value { get; private set; }

    /// <summary>The latest reading, in degrees from 0 to 360. Returns true for the first one, which applies at once.</summary>
    public bool SetTarget(double heading)
    {
        if (!_hasValue)
        {
            _hasValue = true;
            _target = heading;
            Value = heading;
            return true;
        }

        double difference = Difference(heading, _target);

        if (Math.Abs(difference) >= Deadband)
        {
            _target += difference;
        }

        return false;
    }

    /// <summary>The shortest turn from one heading to another, from -180 to 180 degrees.</summary>
    public static double Difference(double to, double from)
    {
        double difference = (to - from) % 360.0;
        return difference + (difference > 180.0 ? -360.0 : difference < -180.0 ? 360.0 : 0.0);
    }

    /// <summary>Moves on by some time and returns whether the value changed.</summary>
    public bool Advance(double seconds)
    {
        double distance = _target - Value;
        if (distance == 0.0)
        {
            return false;
        }

        Value = Math.Abs(distance) < SettledDistance
            ? _target
            : Value + distance * (1.0 - Math.Exp(-seconds / TimeConstantSeconds));

        return true;
    }
}
