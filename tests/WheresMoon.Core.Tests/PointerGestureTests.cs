using WheresMoon.Core.Interaction;
using WheresMoon.Core.Scene;

namespace WheresMoon.Core.Tests;

public class PointerGestureTests
{
    private static TimeSpan Ms(int value) => TimeSpan.FromMilliseconds(value);

    [Test]
    public void A_horizontal_swipe_is_decided_once_and_loses_no_distance()
    {
        var gesture = new PointerGesture();
        gesture.Begin(new PointD(100, 100), Ms(0));

        gesture.Move(new PointD(104, 101), Ms(16)).Should().Be(0.0);
        gesture.Axis.Should().Be(DragAxis.Undecided);

        gesture.Move(new PointD(120, 102), Ms(32)).Should().Be(20.0);
        gesture.Axis.Should().Be(DragAxis.Horizontal);

        gesture.Move(new PointD(125, 150), Ms(48)).Should().Be(5.0);
        gesture.Axis.Should().Be(DragAxis.Horizontal);
    }

    [Test]
    public void A_vertical_swipe_stays_vertical_even_if_it_drifts_sideways_later()
    {
        var gesture = new PointerGesture();
        gesture.Begin(new PointD(100, 100), Ms(0));

        gesture.Move(new PointD(103, 130), Ms(16)).Should().Be(0.0);
        gesture.Axis.Should().Be(DragAxis.Vertical);

        gesture.Move(new PointD(300, 135), Ms(32)).Should().Be(0.0);
        gesture.Axis.Should().Be(DragAxis.Vertical);
    }

    [Test]
    public void Velocity_follows_the_pointer()
    {
        var gesture = new PointerGesture();
        gesture.Begin(new PointD(0, 0), Ms(0));

        for (int i = 1; i <= 30; i++)
        {
            gesture.Move(new PointD(i * 10.0, 0), Ms(i * 10));
        }

        gesture.VelocityX.Should().BeInRange(950.0, 1050.0);
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

    [Test]
    public void Letting_go_in_motion_keeps_the_speed()
    {
        SwipeAtThousandUnitsPerSecond().VelocityAt(Ms(308)).Should().BeInRange(700.0, 1050.0);
    }

    [Test]
    public void A_pointer_that_rests_before_letting_go_has_stopped()
    {
        SwipeAtThousandUnitsPerSecond().VelocityAt(Ms(500)).Should().BeInRange(0.0, 40.0);
    }

    [Test]
    public void Two_quick_taps_make_a_double_tap()
    {
        var gesture = new PointerGesture();

        gesture.Begin(new PointD(50, 50), Ms(0));
        gesture.End(Ms(60)).Should().Be(GestureEnd.Tap);

        gesture.Begin(new PointD(55, 48), Ms(200));
        gesture.End(Ms(260)).Should().Be(GestureEnd.DoubleTap);

        gesture.Begin(new PointD(55, 48), Ms(400));
        gesture.End(Ms(460)).Should().Be(GestureEnd.Tap);
    }

    [Test]
    public void Taps_far_apart_in_time_or_space_stay_single()
    {
        var gesture = new PointerGesture();

        gesture.Begin(new PointD(50, 50), Ms(0));
        gesture.End(Ms(60));
        gesture.Begin(new PointD(50, 50), Ms(900));
        gesture.End(Ms(960)).Should().Be(GestureEnd.Tap);

        gesture.Begin(new PointD(300, 300), Ms(1000));
        gesture.End(Ms(1060)).Should().Be(GestureEnd.Tap);
    }

    [Test]
    public void A_drag_or_a_long_press_is_no_tap()
    {
        var gesture = new PointerGesture();

        gesture.Begin(new PointD(0, 0), Ms(0));
        gesture.Move(new PointD(40, 0), Ms(50));
        gesture.End(Ms(100)).Should().Be(GestureEnd.None);

        gesture.Begin(new PointD(0, 0), Ms(200));
        gesture.End(Ms(900)).Should().Be(GestureEnd.None);
    }
}
