using WheresMoon.Core.Compass;

namespace WheresMoon.Core.Tests;

public class RingInertiaTests
{
    private const double Frame = 1.0 / 60.0;

    private static RingInertia Resting()
    {
        var inertia = new RingInertia();
        Run(inertia, 30);
        return inertia;
    }

    private static void Run(RingInertia inertia, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            inertia.Advance(Frame);
        }
    }

    /// <summary>A push of the device, as a burst of acceleration and the braking after it.</summary>
    private static double LargestOffsetOfPush(double acceleration)
    {
        RingInertia inertia = Resting();
        double largest = 0.0;

        inertia.AccelerationX = -acceleration;
        for (int i = 0; i < 12; i++)
        {
            inertia.Advance(Frame);
            largest = Math.Max(largest, Math.Abs(inertia.OffsetX));
        }

        inertia.AccelerationX = acceleration;
        for (int i = 0; i < 12; i++)
        {
            inertia.Advance(Frame);
            largest = Math.Max(largest, Math.Abs(inertia.OffsetX));
        }

        return largest;
    }

    [Test]
    public void A_resting_device_leaves_the_rings_alone()
    {
        RingInertia inertia = Resting();

        inertia.OffsetX.Should().Be(0.0);
        inertia.OffsetY.Should().Be(0.0);
    }

    [Test]
    public void Gravity_alone_moves_nothing_at_any_angle()
    {
        var inertia = new RingInertia { AccelerationX = 0.3, AccelerationY = -0.9 };

        Run(inertia, 120);

        inertia.OffsetX.Should().Be(0.0);
        inertia.OffsetY.Should().Be(0.0);
    }

    [Test]
    public void Moving_left_shifts_the_rings_right()
    {
        RingInertia inertia = Resting();

        inertia.AccelerationX = -0.5;
        Run(inertia, 12);

        inertia.OffsetX.Should().BeGreaterThan(0.0);
    }

    [Test]
    public void Moving_up_shifts_the_rings_down()
    {
        RingInertia inertia = Resting();

        inertia.AccelerationY = 0.5;
        Run(inertia, 12);

        inertia.OffsetY.Should().BeGreaterThan(0.0);
    }

    [Test]
    public void Turning_left_shifts_the_rings_right()
    {
        RingInertia inertia = Resting();

        inertia.TurnY = 2.0;
        Run(inertia, 20);

        inertia.OffsetX.Should().BeGreaterThan(0.0);
    }

    [Test]
    public void Faster_movement_gives_a_larger_shift()
    {
        double slow = LargestOffsetOfPush(0.1);
        double medium = LargestOffsetOfPush(0.5);
        double fast = LargestOffsetOfPush(2.0);

        slow.Should().BeLessThan(0.25, $"slow movement shifted by {slow:F2}");
        (medium > slow && fast > medium).Should().BeTrue($"{slow:F2}, {medium:F2}, {fast:F2}");
    }

    [Test]
    public void The_shift_is_limited_however_hard_the_device_moves()
    {
        RingInertia inertia = Resting();

        inertia.AccelerationX = -30.0;
        inertia.TurnY = 30.0;
        for (int i = 0; i < 300; i++)
        {
            inertia.Advance(Frame);
            inertia.OffsetX.Should().BeInRange(-1.0, 1.0);
        }
    }

    [Test]
    public void The_rings_return_without_overshoot()
    {
        RingInertia inertia = Resting();
        inertia.TurnY = 2.0;
        Run(inertia, 30);
        double before = inertia.OffsetX;
        inertia.TurnY = 0.0;

        for (int i = 0; i < 300; i++)
        {
            double previous = inertia.OffsetX;
            inertia.Advance(Frame);

            inertia.OffsetX.Should().BeGreaterThanOrEqualTo(0.0, "crossed zero");
            inertia.OffsetX.Should().BeLessThanOrEqualTo(before + 0.001, "swung further out");
            inertia.OffsetX.Should().BeLessThanOrEqualTo(previous + 0.08, "jumped");
        }

        inertia.OffsetX.Should().Be(0.0);
    }

    [Test]
    public void Coming_to_rest_ends_the_need_to_redraw()
    {
        RingInertia inertia = Resting();
        inertia.TurnY = 2.0;
        Run(inertia, 30);
        inertia.TurnY = 0.0;
        Run(inertia, 300);

        inertia.Advance(Frame).Should().BeFalse();
    }

    [TestCase(0, 1.0, 2.0, 1.0, 2.0)]
    [TestCase(90, 1.0, 2.0, -2.0, 1.0)]
    [TestCase(180, 1.0, 2.0, -1.0, -2.0)]
    [TestCase(270, 1.0, 2.0, 2.0, -1.0)]
    public void Device_axes_follow_the_screen_rotation(int rotation, double x, double y, double expectedX, double expectedY)
    {
        RingInertia.ToScreen(x, y, rotation).Should().Be((expectedX, expectedY));
    }
}
