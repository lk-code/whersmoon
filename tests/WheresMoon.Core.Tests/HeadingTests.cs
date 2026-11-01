using WheresMoon.Core.Compass;

namespace WheresMoon.Core.Tests;

public class HeadingTests
{
    // Rows are east, north and up. Columns are the device's right edge, top edge and screen normal.
    private static readonly float[] FlatFacingNorth = [1, 0, 0, 0, 1, 0, 0, 0, 1];
    private static readonly float[] FlatFacingEast = [0, 1, 0, -1, 0, 0, 0, 0, 1];
    private static readonly float[] UprightFacingNorth = [1, 0, 0, 0, 0, -1, 0, 1, 0];
    private static readonly float[] UprightFacingEast = [0, 0, -1, -1, 0, 0, 0, 1, 0];

    [TestCase(0, 0.0)]
    [TestCase(90, 90.0)]
    [TestCase(180, 180.0)]
    [TestCase(270, 270.0)]
    public void Lying_flat_the_top_of_the_screen_decides(int screenRotation, double expected)
    {
        DeviceHeading.FromRotationMatrix(FlatFacingNorth, screenRotation).Should().BeApproximately(expected, 0.5e-6);
    }

    [Test]
    public void Turning_a_flat_device_turns_the_heading()
    {
        DeviceHeading.FromRotationMatrix(FlatFacingEast, 0).Should().BeApproximately(90.0, 0.5e-6);
    }

    [Test]
    public void Held_upright_the_back_of_the_device_decides()
    {
        DeviceHeading.FromRotationMatrix(UprightFacingNorth, 0).Should().BeApproximately(0.0, 0.5e-6);
        DeviceHeading.FromRotationMatrix(UprightFacingEast, 0).Should().BeApproximately(90.0, 0.5e-6);
    }

    [TestCase(0.0)]
    [TestCase(20.0)]
    [TestCase(45.0)]
    [TestCase(70.0)]
    [TestCase(90.0)]
    public void Tilting_from_flat_to_upright_keeps_the_heading(double tilt)
    {
        float cos = (float)Math.Cos(tilt * Math.PI / 180.0);
        float sin = (float)Math.Sin(tilt * Math.PI / 180.0);

        // Facing east, the top edge rises from the ground while the screen turns towards the user in the west.
        float[] matrix = [0, cos, -sin, -1, 0, 0, 0, sin, cos];

        DeviceHeading.FromRotationMatrix(matrix, 0).Should().BeApproximately(90.0, 0.5e-4);
    }

    [Test]
    public void A_landscape_device_upright_still_looks_out_of_its_back()
    {
        // Top edge pointing west, right edge up, screen facing south: the user looks north.
        float[] matrix = [0, -1, 0, 0, 0, -1, 1, 0, 0];

        DeviceHeading.FromRotationMatrix(matrix, 90).Should().BeApproximately(0.0, 0.5e-6);
    }

    [TestCase(10.0, 5.0, 15.0)]
    [TestCase(358.0, 5.0, 3.0)]
    [TestCase(2.0, -5.0, 357.0)]
    public void Eastern_declination_moves_the_true_heading_clockwise(double magnetic, double declination, double expected)
    {
        DeviceHeading.ToTrueNorth(magnetic, declination).Should().BeApproximately(expected, 0.5e-9);
    }

    [Test]
    public void Crossing_north_takes_the_short_way()
    {
        var filter = new HeadingFilter();
        filter.SetTarget(359.0);

        filter.SetTarget(1.0);
        for (int i = 0; i < 120; i++)
        {
            filter.Advance(1.0 / 60.0);
        }

        filter.Value.Should().BeApproximately(361.0, 0.5e-6);
    }

    [Test]
    public void Each_step_across_north_stays_small()
    {
        var filter = new HeadingFilter();
        filter.SetTarget(350.0);
        double previous = filter.Value;

        foreach (double heading in new[] { 353.0, 356.0, 359.0, 2.0, 5.0, 8.0 })
        {
            filter.SetTarget(heading);
            filter.Advance(1.0 / 20.0);

            (filter.Value - previous).Should().BeInRange(0.0, 3.5);
            previous = filter.Value;
        }
    }

    [Test]
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

        filter.Value.Should().BeApproximately(720.0, 0.5);
    }

    [Test]
    public void The_first_reading_needs_no_smoothing()
    {
        var filter = new HeadingFilter();

        filter.SetTarget(123.0);

        filter.Value.Should().Be(123.0);
        filter.Advance(0.016).Should().BeFalse();
    }

    [Test]
    public void Smoothing_takes_its_time()
    {
        var filter = new HeadingFilter();
        filter.SetTarget(0.0);
        filter.SetTarget(90.0);

        filter.Advance(1.0 / 60.0);

        filter.Value.Should().BeInRange(1.0, 20.0);
    }

    [Test]
    public void Sensor_noise_at_rest_causes_no_redraws()
    {
        var filter = new HeadingFilter();
        filter.SetTarget(100.0);

        foreach (double noise in new[] { 0.3, -0.2, 0.4, -0.5, 0.1, 0.2, -0.3 })
        {
            filter.SetTarget(100.0 + noise);
            filter.Advance(1.0 / 60.0).Should().BeFalse();
        }
    }

    [Test]
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

        ticks.Should().BeLessThan(200, $"still moving after {ticks} ticks");
        filter.Value.Should().Be(45.0);
    }
}
