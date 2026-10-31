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

    [Fact]
    public void A_resting_device_leaves_the_rings_alone()
    {
        RingInertia inertia = Resting();

        Assert.Equal(0.0, inertia.OffsetX);
        Assert.Equal(0.0, inertia.OffsetY);
    }

    [Fact]
    public void Gravity_alone_moves_nothing_at_any_angle()
    {
        var inertia = new RingInertia { AccelerationX = 0.3, AccelerationY = -0.9 };

        Run(inertia, 120);

        Assert.Equal(0.0, inertia.OffsetX);
        Assert.Equal(0.0, inertia.OffsetY);
    }

    [Fact]
    public void Moving_left_shifts_the_rings_right()
    {
        RingInertia inertia = Resting();

        inertia.AccelerationX = -0.5;
        Run(inertia, 12);

        Assert.True(inertia.OffsetX > 0.0);
    }

    [Fact]
    public void Moving_up_shifts_the_rings_down()
    {
        RingInertia inertia = Resting();

        inertia.AccelerationY = 0.5;
        Run(inertia, 12);

        Assert.True(inertia.OffsetY > 0.0);
    }

    [Fact]
    public void Turning_left_shifts_the_rings_right()
    {
        RingInertia inertia = Resting();

        inertia.TurnY = 2.0;
        Run(inertia, 20);

        Assert.True(inertia.OffsetX > 0.0);
    }

    [Fact]
    public void Faster_movement_gives_a_larger_shift()
    {
        double slow = LargestOffsetOfPush(0.1);
        double medium = LargestOffsetOfPush(0.5);
        double fast = LargestOffsetOfPush(2.0);

        Assert.True(slow < 0.25, $"slow movement shifted by {slow:F2}");
        Assert.True(medium > slow && fast > medium, $"{slow:F2}, {medium:F2}, {fast:F2}");
    }

    [Fact]
    public void The_shift_is_limited_however_hard_the_device_moves()
    {
        RingInertia inertia = Resting();

        inertia.AccelerationX = -30.0;
        inertia.TurnY = 30.0;
        for (int i = 0; i < 300; i++)
        {
            inertia.Advance(Frame);
            Assert.InRange(inertia.OffsetX, -1.0, 1.0);
        }
    }

    [Fact]
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

            Assert.True(inertia.OffsetX >= 0.0, "crossed zero");
            Assert.True(inertia.OffsetX <= before + 0.001, "swung further out");
            Assert.True(inertia.OffsetX <= previous + 0.08, "jumped");
        }

        Assert.Equal(0.0, inertia.OffsetX);
    }

    [Fact]
    public void Coming_to_rest_ends_the_need_to_redraw()
    {
        RingInertia inertia = Resting();
        inertia.TurnY = 2.0;
        Run(inertia, 30);
        inertia.TurnY = 0.0;
        Run(inertia, 300);

        Assert.False(inertia.Advance(Frame));
    }

    [Theory]
    [InlineData(0, 1.0, 2.0, 1.0, 2.0)]
    [InlineData(90, 1.0, 2.0, -2.0, 1.0)]
    [InlineData(180, 1.0, 2.0, -1.0, -2.0)]
    [InlineData(270, 1.0, 2.0, 2.0, -1.0)]
    public void Device_axes_follow_the_screen_rotation(int rotation, double x, double y, double expectedX, double expectedY)
    {
        Assert.Equal((expectedX, expectedY), RingInertia.ToScreen(x, y, rotation));
    }
}
