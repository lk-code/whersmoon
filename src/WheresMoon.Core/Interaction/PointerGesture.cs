using WheresMoon.Core.Scene;

namespace WheresMoon.Core.Interaction;

public enum DragAxis
{
    Undecided,
    Horizontal,
    Vertical,
}

public enum GestureEnd
{
    None,
    Tap,
    DoubleTap,
}

/// <summary>
/// Follows one pointer from press to release. The first clear movement decides the axis for the
/// rest of the gesture, so a slightly diagonal swipe never does both things.
/// </summary>
public sealed class PointerGesture
{
    /// <summary>Movement in device independent units before the axis is decided.</summary>
    public const double DecisionDistance = 10.0;

    private const double DoubleTapDistance = 40.0;
    private const double VelocitySmoothingSeconds = 0.05;

    private static readonly TimeSpan LongestTap = TimeSpan.FromMilliseconds(350);
    private static readonly TimeSpan DoubleTapWindow = TimeSpan.FromMilliseconds(350);

    private PointD _start;
    private PointD _last;
    private TimeSpan _startTime;
    private TimeSpan _lastTime;
    private PointD? _lastTapPoint;
    private TimeSpan _lastTapTime;

    public DragAxis Axis { get; private set; }

    /// <summary>Smoothed horizontal speed in device independent units per second.</summary>
    public double VelocityX { get; private set; }

    public PointD Start => _start;

    /// <summary>Speed at a later moment. A pointer resting on the screen reports nothing, but it has stopped all the same.</summary>
    public double VelocityAt(TimeSpan time) =>
        VelocityX * Math.Exp(-(time - _lastTime).TotalSeconds / VelocitySmoothingSeconds);

    public void Begin(PointD point, TimeSpan time)
    {
        _start = point;
        _last = point;
        _startTime = time;
        _lastTime = time;
        Axis = DragAxis.Undecided;
        VelocityX = 0.0;
    }

    /// <summary>Returns the horizontal distance to apply. The first result covers everything since the press.</summary>
    public double Move(PointD point, TimeSpan time)
    {
        double dt = (time - _lastTime).TotalSeconds;
        double previousX = _last.X;

        if (dt > 0.0)
        {
            double share = 1.0 - Math.Exp(-dt / VelocitySmoothingSeconds);
            VelocityX += ((point.X - previousX) / dt - VelocityX) * share;
        }

        _last = point;
        _lastTime = time;

        if (Axis == DragAxis.Undecided)
        {
            double dx = point.X - _start.X;
            double dy = point.Y - _start.Y;

            if (Math.Max(Math.Abs(dx), Math.Abs(dy)) < DecisionDistance)
            {
                return 0.0;
            }

            Axis = Math.Abs(dx) > Math.Abs(dy) ? DragAxis.Horizontal : DragAxis.Vertical;
            return Axis == DragAxis.Horizontal ? dx : 0.0;
        }

        return Axis == DragAxis.Horizontal ? point.X - previousX : 0.0;
    }

    public GestureEnd End(TimeSpan time)
    {
        if (Axis != DragAxis.Undecided || time - _startTime > LongestTap)
        {
            _lastTapPoint = null;
            return GestureEnd.None;
        }

        bool isDouble = _lastTapPoint is PointD previous
            && time - _lastTapTime <= DoubleTapWindow
            && Math.Max(Math.Abs(previous.X - _start.X), Math.Abs(previous.Y - _start.Y)) <= DoubleTapDistance;

        _lastTapPoint = isDouble ? null : _start;
        _lastTapTime = time;

        return isDouble ? GestureEnd.DoubleTap : GestureEnd.Tap;
    }
}
