using WheresMoon.Core.Compass;

namespace WheresMoon.Core.Tests;

public class HeadingTests
{
    // Rows are east, north and up. Columns are the device's right edge, top edge and screen normal.
    private static readonly float[] FlatFacingNorth = [1, 0, 0, 0, 1, 0, 0, 0, 1];
    private static readonly float[] FlatFacingEast = [0, 1, 0, -1, 0, 0, 0, 0, 1];
    private static readonly float[] UprightFacingNorth = [1, 0, 0, 0, 0, -1, 0, 1, 0];
    private static readonly float[] UprightFacingEast = [0, 0, -1, -1, 0, 0, 0, 1, 0];

    [Theory]
    [InlineData(0, 0.0)]
    [InlineData(90, 90.0)]
    [InlineData(180, 180.0)]
    [InlineData(270, 270.0)]
    public void Lying_flat_the_top_of_the_screen_decides(int screenRotation, double expected)
    {
        Assert.Equal(expected, DeviceHeading.FromRotationMatrix(FlatFacingNorth, screenRotation), 6);
    }

    [Fact]
    public void Turning_a_flat_device_turns_the_heading()
    {
        Assert.Equal(90.0, DeviceHeading.FromRotationMatrix(FlatFacingEast, 0), 6);
    }

    [Fact]
    public void Held_upright_the_back_of_the_device_decides()
    {
        Assert.Equal(0.0, DeviceHeading.FromRotationMatrix(UprightFacingNorth, 0), 6);
        Assert.Equal(90.0, DeviceHeading.FromRotationMatrix(UprightFacingEast, 0), 6);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(20.0)]
    [InlineData(45.0)]
    [InlineData(70.0)]
    [InlineData(90.0)]
    public void Tilting_from_flat_to_upright_keeps_the_heading(double tilt)
    {
        float cos = (float)Math.Cos(tilt * Math.PI / 180.0);
        float sin = (float)Math.Sin(tilt * Math.PI / 180.0);

        // Facing east, the top edge rises from the ground while the screen turns towards the user in the west.
        float[] matrix = [0, cos, -sin, -1, 0, 0, 0, sin, cos];

        Assert.Equal(90.0, DeviceHeading.FromRotationMatrix(matrix, 0), 4);
    }

    [Fact]
    public void A_landscape_device_upright_still_looks_out_of_its_back()
    {
        // Top edge pointing west, right edge up, screen facing south: the user looks north.
        float[] matrix = [0, -1, 0, 0, 0, -1, 1, 0, 0];

        Assert.Equal(0.0, DeviceHeading.FromRotationMatrix(matrix, 90), 6);
    }

    [Theory]
    [InlineData(10.0, 5.0, 15.0)]
    [InlineData(358.0, 5.0, 3.0)]
    [InlineData(2.0, -5.0, 357.0)]
    public void Eastern_declination_moves_the_true_heading_clockwise(double magnetic, double declination, double expected)
    {
        Assert.Equal(expected, DeviceHeading.ToTrueNorth(magnetic, declination), 9);
    }

    [Fact]
    public void Crossing_north_takes_the_short_way()
    {
        var filter = new HeadingFilter();
        filter.SetTarget(359.0);

        filter.SetTarget(1.0);
        for (int i = 0; i < 120; i++)
        {
            filter.Advance(1.0 / 60.0);
        }

        Assert.Equal(361.0, filter.Value, 6);
    }

    [Fact]
    public void Each_step_across_north_stays_small()
    {
        var filter = new HeadingFilter();
        filter.SetTarget(350.0);
        double previous = filter.Value;

        foreach (double heading in new[] { 353.0, 356.0, 359.0, 2.0, 5.0, 8.0 })
        {
            filter.SetTarget(heading);
            filter.Advance(1.0 / 20.0);

            Assert.InRange(filter.Value - previous, 0.0, 3.5);
            previous = filter.Value;
        }
    }

    [Fact]
    public void Turning_a_full_circle_keeps_counting()
    {
        var filter = new HeadingFilter();
        filter.SetTarget(0.0);

        for (int degrees = 10; degrees <= 720; degrees += 10)
        {
            filter.SetTarget(degrees % 360);
            for (int i = 0; i < 30; i++)
            {
                filter.Advance(1.0 / 60.0);
            }
        }

        Assert.Equal(720.0, filter.Value, 0);
    }

    [Fact]
    public void The_first_reading_needs_no_smoothing()
    {
        var filter = new HeadingFilter();

        filter.SetTarget(123.0);

        Assert.Equal(123.0, filter.Value);
        Assert.False(filter.Advance(0.016));
    }

    [Fact]
    public void Smoothing_takes_its_time()
    {
        var filter = new HeadingFilter();
        filter.SetTarget(0.0);
        filter.SetTarget(90.0);

        filter.Advance(1.0 / 60.0);

        Assert.InRange(filter.Value, 1.0, 20.0);
    }

    [Fact]
    public void Sensor_noise_at_rest_causes_no_redraws()
    {
        var filter = new HeadingFilter();
        filter.SetTarget(100.0);

        foreach (double noise in new[] { 0.3, -0.2, 0.4, -0.5, 0.1, 0.2, -0.3 })
        {
            filter.SetTarget(100.0 + noise);
            Assert.False(filter.Advance(1.0 / 60.0));
        }
    }

    [Fact]
    public void Movement_comes_to_a_complete_stop()
    {
        var filter = new HeadingFilter();
        filter.SetTarget(0.0);
        filter.SetTarget(45.0);

        int ticks = 0;
        while (filter.Advance(1.0 / 60.0) && ticks < 600)
        {
            ticks++;
        }

        Assert.True(ticks < 200, $"still moving after {ticks} ticks");
        Assert.Equal(45.0, filter.Value);
    }
}
