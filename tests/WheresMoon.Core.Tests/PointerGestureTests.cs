using WheresMoon.Core.Interaction;
using WheresMoon.Core.Scene;

namespace WheresMoon.Core.Tests;

public class PointerGestureTests
{
    private static TimeSpan Ms(int value) => TimeSpan.FromMilliseconds(value);

    [Fact]
    public void A_horizontal_swipe_is_decided_once_and_loses_no_distance()
    {
        var gesture = new PointerGesture();
        gesture.Begin(new PointD(100, 100), Ms(0));

        Assert.Equal(0.0, gesture.Move(new PointD(104, 101), Ms(16)));
        Assert.Equal(DragAxis.Undecided, gesture.Axis);

        Assert.Equal(20.0, gesture.Move(new PointD(120, 102), Ms(32)));
        Assert.Equal(DragAxis.Horizontal, gesture.Axis);

        Assert.Equal(5.0, gesture.Move(new PointD(125, 150), Ms(48)));
        Assert.Equal(DragAxis.Horizontal, gesture.Axis);
    }

    [Fact]
    public void A_vertical_swipe_stays_vertical_even_if_it_drifts_sideways_later()
    {
        var gesture = new PointerGesture();
        gesture.Begin(new PointD(100, 100), Ms(0));

        Assert.Equal(0.0, gesture.Move(new PointD(103, 130), Ms(16)));
        Assert.Equal(DragAxis.Vertical, gesture.Axis);

        Assert.Equal(0.0, gesture.Move(new PointD(300, 135), Ms(32)));
        Assert.Equal(DragAxis.Vertical, gesture.Axis);
    }

    [Fact]
    public void Velocity_follows_the_pointer()
    {
        var gesture = new PointerGesture();
        gesture.Begin(new PointD(0, 0), Ms(0));

        for (int i = 1; i <= 30; i++)
        {
            gesture.Move(new PointD(i * 10.0, 0), Ms(i * 10));
        }

        Assert.InRange(gesture.VelocityX, 950.0, 1050.0);
    }

    private static PointerGesture SwipeAtThousandUnitsPerSecond()
    {
        var gesture = new PointerGesture();
        gesture.Begin(new PointD(0, 0), Ms(0));

        for (int i = 1; i <= 30; i++)
        {
            gesture.Move(new PointD(i * 10.0, 0), Ms(i * 10));
        }

        return gesture;
    }

    [Fact]
    public void Letting_go_in_motion_keeps_the_speed()
    {
        Assert.InRange(SwipeAtThousandUnitsPerSecond().VelocityAt(Ms(308)), 700.0, 1050.0);
    }

    [Fact]
    public void A_pointer_that_rests_before_letting_go_has_stopped()
    {
        Assert.InRange(SwipeAtThousandUnitsPerSecond().VelocityAt(Ms(500)), 0.0, 40.0);
    }

    [Fact]
    public void Two_quick_taps_make_a_double_tap()
    {
        var gesture = new PointerGesture();

        gesture.Begin(new PointD(50, 50), Ms(0));
        Assert.Equal(GestureEnd.Tap, gesture.End(Ms(60)));

        gesture.Begin(new PointD(55, 48), Ms(200));
        Assert.Equal(GestureEnd.DoubleTap, gesture.End(Ms(260)));

        gesture.Begin(new PointD(55, 48), Ms(400));
        Assert.Equal(GestureEnd.Tap, gesture.End(Ms(460)));
    }

    [Fact]
    public void Taps_far_apart_in_time_or_space_stay_single()
    {
        var gesture = new PointerGesture();

        gesture.Begin(new PointD(50, 50), Ms(0));
        gesture.End(Ms(60));
        gesture.Begin(new PointD(50, 50), Ms(900));
        Assert.Equal(GestureEnd.Tap, gesture.End(Ms(960)));

        gesture.Begin(new PointD(300, 300), Ms(1000));
        Assert.Equal(GestureEnd.Tap, gesture.End(Ms(1060)));
    }

    [Fact]
    public void A_drag_or_a_long_press_is_no_tap()
    {
        var gesture = new PointerGesture();

        gesture.Begin(new PointD(0, 0), Ms(0));
        gesture.Move(new PointD(40, 0), Ms(50));
        Assert.Equal(GestureEnd.None, gesture.End(Ms(100)));

        gesture.Begin(new PointD(0, 0), Ms(200));
        Assert.Equal(GestureEnd.None, gesture.End(Ms(900)));
    }
}
