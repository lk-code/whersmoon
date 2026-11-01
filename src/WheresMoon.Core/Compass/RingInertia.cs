namespace WheresMoon.Core.Compass;

/// <summary>
/// Lets the compass rings lag behind the device as if on a soft spring. Movement sets a goal, and the
/// offset follows it through two smoothing stages in a row. That is what a critically damped spring does,
/// and as each stage stays between its old value and its goal, the offset can never swing past zero.
/// </summary>
public sealed class RingInertia
{
    private const double MetresPerSecondSquaredPerG = 9.81;

    /// <summary>How quickly the gravity estimate follows a change, in seconds.</summary>
    private const double GravitySeconds = 0.5;

    /// <summary>Speed estimates fade by this time constant, as sensor drift would otherwise add up.</summary>
    private const double SpeedSeconds = 0.25;

    /// <summary>Time constant of each of the two spring stages, in seconds.</summary>
    private const double SpringSeconds = 0.09;

    /// <summary>Hand speed in metres per second that nearly reaches the largest offset.</summary>
    private const double FullSpeed = 0.7;

    /// <summary>Turning speed in radians per second that nearly reaches the largest offset.</summary>
    private const double FullTurn = 3.0;

    /// <summary>Acceleration in g below which a sensor counts as resting.</summary>
    private const double AccelerationNoise = 0.015;

    /// <summary>Change of the offset that is too small to see.</summary>
    private const double SmallestChange = 0.002;

    private double _gravityX;
    private double _gravityY;
    private double _speedX;
    private double _speedY;
    private double _middleX;
    private double _middleY;
    private bool _hasGravity;

    /// <summary>Offset to the right, from -1 to 1 of the largest shift.</summary>
    public double OffsetX { get; private set; }

    /// <summary>Offset downwards, from -1 to 1 of the largest shift.</summary>
    public double OffsetY { get; private set; }

    /// <summary>Latest acceleration in g along the screen, x to the right and y upwards.</summary>
    public double AccelerationX { get; set; }

    public double AccelerationY { get; set; }

    /// <summary>Latest rotation in radians per second around the screen's axes, x to the right and y upwards.</summary>
    public double TurnX { get; set; }

    public double TurnY { get; set; }

    /// <summary>Turns device axes into screen axes for a screen rotated by 0, 90, 180 or 270 degrees.</summary>
    public static (double X, double Y) ToScreen(double x, double y, int screenRotation) => screenRotation switch
    {
        90 => (-y, x),
        180 => (-x, -y),
        270 => (y, -x),
        _ => (x, y),
    };

    /// <summary>Moves on by some time and returns whether the offset changed visibly.</summary>
    public bool Advance(double seconds)
    {
        if (!_hasGravity)
        {
            _hasGravity = true;
            _gravityX = AccelerationX;
            _gravityY = AccelerationY;
        }

        double gravityShare = Share(seconds, GravitySeconds);
        _gravityX += (AccelerationX - _gravityX) * gravityShare;
        _gravityY += (AccelerationY - _gravityY) * gravityShare;

        double fade = Math.Exp(-seconds / SpeedSeconds);
        _speedX = (_speedX + Quieted(AccelerationX - _gravityX) * MetresPerSecondSquaredPerG * seconds) * fade;
        _speedY = (_speedY + Quieted(AccelerationY - _gravityY) * MetresPerSecondSquaredPerG * seconds) * fade;

        // Moving left pushes the rings right. Turning left around the upright axis shifts the view right as well.
        double goalX = Limited(-_speedX / FullSpeed + TurnY / FullTurn);
        double goalY = Limited(_speedY / FullSpeed - TurnX / FullTurn);

        double stage = Share(seconds, SpringSeconds);
        _middleX += (goalX - _middleX) * stage;
        _middleY += (goalY - _middleY) * stage;

        double previousX = OffsetX;
        double previousY = OffsetY;

        OffsetX += (_middleX - OffsetX) * stage;
        OffsetY += (_middleY - OffsetY) * stage;

        // Close to rest the steps turn invisible, and the last bit would otherwise stay forever.
        if (goalX == 0.0 && goalY == 0.0 && Math.Abs(OffsetX) < SmallestChange && Math.Abs(OffsetY) < SmallestChange)
        {
            OffsetX = 0.0;
            OffsetY = 0.0;
            _middleX = 0.0;
            _middleY = 0.0;
        }

        return Math.Abs(OffsetX - previousX) >= SmallestChange
            || Math.Abs(OffsetY - previousY) >= SmallestChange
            || (OffsetX == 0.0 && OffsetY == 0.0 && (previousX != 0.0 || previousY != 0.0));
    }

    private static double Share(double seconds, double timeConstant) => 1.0 - Math.Exp(-seconds / timeConstant);

    private static double Quieted(double acceleration) => Math.Abs(acceleration) < AccelerationNoise ? 0.0 : acceleration;

    /// <summary>Follows small movements in proportion and approaches the limit for large ones.</summary>
    private static double Limited(double drive) => Math.Tanh(drive);
}
