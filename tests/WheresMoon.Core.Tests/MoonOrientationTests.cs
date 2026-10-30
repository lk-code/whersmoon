using System.Globalization;

namespace WheresMoon.Core.Tests;

public class MoonOrientationTests
{
    /// <summary>
    /// JPL Horizons, moon seen from a site at sea level (quantities 2, 4, 10, 27, retrieved 2026-10-30):
    /// https://ssd.jpl.nasa.gov/api/horizons.api with COMMAND='301', CENTER='coord@399'.
    /// PsAng points away from the sun, so the bright limb lies 180 degrees from it.
    /// </summary>
    public sealed record HorizonsCase(
        string Name, string Utc, double Latitude, double Longitude,
        double Dec, double Azimuth, double Elevation, double IlluminationPercent, double PsAng);

    public static readonly TheoryData<HorizonsCase> Cases =
    [
        new("Berlin gibbous", "2026-10-30T00:00:00Z", 52.52, 13.40, 27.248416320, 117.730357774, 52.551235579, 80.43759, 266.506),
        new("Berlin crescent", "2026-11-12T15:30:00Z", 52.52, 13.40, -28.512672747, 206.797716314, 4.626756002, 10.29172, 98.121),
        new("Sydney crescent", "2026-11-12T09:00:00Z", -33.8688, 151.2093, -27.347764084, 253.579216327, 26.597073632, 8.50043, 99.137),
        new("Sydney below horizon", "2026-10-30T12:00:00Z", -33.8688, 151.2093, 27.733816958, 65.299577053, -13.237877680, 75.22613, 270.268),
        new("Quito crescent", "2026-11-12T23:30:00Z", -0.1807, -78.4678, -27.720432666, 237.498799257, 30.374312529, 11.95829, 94.883),
        new("Tromso full", "2026-09-23T20:00:00Z", 69.6492, 18.9553, -14.043632581, 173.311164277, 6.168129635, 90.99267, 68.537),
    ];

    [Theory]
    [MemberData(nameof(Cases))]
    public void Position_and_illumination_match_horizons(HorizonsCase c)
    {
        MoonInfo moon = Compute(c).Moon;

        Assert.InRange(moon.Body.Azimuth, c.Azimuth - 0.1, c.Azimuth + 0.1);
        Assert.InRange(moon.IlluminatedFraction * 100.0, c.IlluminationPercent - 0.15, c.IlluminationPercent + 0.15);

        // Horizons is airless here, the snapshot includes refraction. Astronomy Engine keeps
        // its value for -1 degree for anything lower, which is about 0.56 degrees.
        double refraction = c.Elevation > 0 ? 1.02 / Math.Tan(Radians(c.Elevation + 10.3 / (c.Elevation + 5.11))) / 60.0 : 0.56;
        Assert.InRange(moon.Body.Altitude, c.Elevation + refraction - 0.1, c.Elevation + refraction + 0.1);
    }

    [Theory]
    [InlineData(52.52, 13.40)]
    [InlineData(-33.8688, 151.2093)]
    [InlineData(-0.1807, -78.4678)]
    [InlineData(69.6492, 18.9553)]
    [InlineData(-54.8, -68.3)]
    public void Bright_limb_points_along_the_sky_towards_the_sun(double latitude, double longitude)
    {
        // Built from sky directions alone, without position or parallactic angles: seen from the
        // observer, the limb must point along the great circle from the moon to the sun.
        DateTimeOffset start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        int checkedMoments = 0;

        for (int hour = 0; hour < 24 * 365; hour += 5)
        {
            SkySnapshot sky = SkyCalculator.Compute(start.AddHours(hour), new GeoLocation(latitude, longitude), TimeZoneInfo.Utc);

            double[] moon = Direction(sky.Moon.Body.Azimuth, sky.Moon.Body.Altitude);
            double[] sun = Direction(sky.Sun.Body.Azimuth, sky.Sun.GeometricAltitude);
            double separation = Math.Acos(Dot(moon, sun)) * 180.0 / Math.PI;

            // Near conjunction or opposition the direction is ill defined, near the zenith "up" is.
            if (sky.Moon.Body.Altitude is < 8.0 or > 80.0 || separation is < 20.0 or > 160.0)
            {
                continue;
            }

            double[] up = Normalize(Subtract([0, 0, 1], Scale(moon, moon[2])));
            double[] right = Cross(moon, up);
            double[] towardsSun = Normalize(Subtract(sun, Scale(moon, Dot(sun, moon))));

            double expected = Math.Atan2(-Dot(towardsSun, right), Dot(towardsSun, up)) * 180.0 / Math.PI;
            AssertAngle(expected, sky.Moon.BrightLimbZenithAngle, 1.0);
            checkedMoments++;
        }

        Assert.True(checkedMoments > 100, $"only {checkedMoments} moments were usable");
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Bright_limb_relative_to_zenith_matches_horizons(HorizonsCase c)
    {
        // Derived from azimuth and altitude only, so it does not share a path with the hour angle in the code.
        double az = Radians(c.Azimuth), alt = Radians(c.Elevation), dec = Radians(c.Dec), lat = Radians(c.Latitude);
        double sinQ = -Math.Sin(az) * Math.Cos(lat) / Math.Cos(dec);
        double cosQ = (Math.Sin(lat) - Math.Sin(dec) * Math.Sin(alt)) / (Math.Cos(dec) * Math.Cos(alt));
        double parallactic = Math.Atan2(sinQ, cosQ) * 180.0 / Math.PI;

        double expected = c.PsAng + 180.0 - parallactic;

        // Horizons refers PsAng to the ICRF pole, the app to the pole of date, which differ by a few tenths.
        AssertAngle(expected, Compute(c).Moon.BrightLimbZenithAngle, 0.6);
    }

    [Fact]
    public void Position_angle_matches_meeus_example_48a()
    {
        double chi = MoonOrientation.BrightLimbPositionAngle(sunRa: 20.6579, sunDec: 8.6964, moonRa: 134.6885, moonDec: 13.7684);

        AssertAngle(285.0, chi, 0.1);
    }

    [Fact]
    public void Illuminated_fraction_matches_meeus_example_48a()
    {
        Assert.Equal(0.6786, MoonOrientation.IlluminatedFraction(69.0756), 3);
    }

    [Theory]
    [InlineData(0.0, 0.0, 45.0, 0.0)]
    [InlineData(1.0, 0.0, 45.0, 90.0)]
    [InlineData(-1.0, 0.0, 45.0, -90.0)]
    public void Parallactic_angle_has_the_expected_sign(double sinHour, double declination, double latitude, double expectedSign)
    {
        double q = MoonOrientation.ParallacticAngle(Math.Asin(sinHour) * 180.0 / Math.PI, declination, latitude);

        Assert.Equal(Math.Sign(expectedSign), Math.Sign(q));
    }

    [Theory]
    [InlineData(90.0)]
    [InlineData(-90.0)]
    public void Parallactic_angle_stays_defined_at_the_poles(double latitude)
    {
        Assert.True(double.IsFinite(MoonOrientation.ParallacticAngle(30.0, 10.0, latitude)));
    }

    [Fact]
    public void Evening_crescent_in_berlin_is_lit_on_the_right()
    {
        (double x, _) = ScreenDirection(Compute("2026-11-12T15:30:00Z", 52.52, 13.40).Moon);

        Assert.True(x > 0.5);
    }

    [Fact]
    public void Autumn_evening_crescent_in_sydney_is_lit_on_the_left()
    {
        // April in Sydney, because the ecliptic lies as flat at sunset as in a northern autumn.
        // In a southern spring it stands steep and the crescent is lit from below instead.
        SkySnapshot sky = Compute("2026-04-20T08:30:00Z", -33.8688, 151.2093);
        (double x, _) = ScreenDirection(sky.Moon);

        Assert.Equal(MoonPhaseName.WaxingCrescent, sky.Moon.PhaseName);
        Assert.True(x < -0.5);
    }

    [Fact]
    public void Evening_crescent_at_the_equator_is_lit_from_below()
    {
        (double x, double y) = ScreenDirection(Compute("2026-11-12T23:30:00Z", -0.1807, -78.4678).Moon);

        Assert.True(y > 0.5 && Math.Abs(x) < y);
    }

    [Fact]
    public void Limb_points_towards_the_sun_even_when_it_is_below_the_horizon()
    {
        // Dusk over Berlin: the sun has set in the south-west, the moon stands to its upper left.
        SkySnapshot sky = Compute("2026-11-12T16:30:00Z", 52.52, 13.40);
        (double x, double y) = ScreenDirection(sky.Moon);

        Assert.True(sky.Sun.GeometricAltitude < 0);
        Assert.True(x > 0 && y > 0);
    }

    [Theory]
    [InlineData(0.0, MoonPhaseName.NewMoon)]
    [InlineData(359.0, MoonPhaseName.NewMoon)]
    [InlineData(45.0, MoonPhaseName.WaxingCrescent)]
    [InlineData(90.0, MoonPhaseName.FirstQuarter)]
    [InlineData(135.0, MoonPhaseName.WaxingGibbous)]
    [InlineData(180.0, MoonPhaseName.FullMoon)]
    [InlineData(225.0, MoonPhaseName.WaningGibbous)]
    [InlineData(270.0, MoonPhaseName.LastQuarter)]
    [InlineData(315.0, MoonPhaseName.WaningCrescent)]
    public void Phase_names_follow_the_elongation(double elongation, MoonPhaseName expected)
    {
        Assert.Equal(expected, MoonOrientation.PhaseName(elongation));
    }

    [Theory]
    [InlineData("2026-10-26T04:12:00Z", MoonPhaseName.FullMoon)]
    [InlineData("2026-11-09T07:02:00Z", MoonPhaseName.NewMoon)]
    public void Known_phases_are_named_correctly(string utc, MoonPhaseName expected)
    {
        Assert.Equal(expected, Compute(utc, 52.52, 13.40).Moon.PhaseName);
    }

    private static SkySnapshot Compute(HorizonsCase c) => Compute(c.Utc, c.Latitude, c.Longitude);

    private static SkySnapshot Compute(string utc, double latitude, double longitude) => SkyCalculator.Compute(
        DateTimeOffset.Parse(utc, CultureInfo.InvariantCulture), new GeoLocation(latitude, longitude), TimeZoneInfo.Utc);

    /// <summary>Bright limb direction on a screen with y pointing down.</summary>
    private static (double X, double Y) ScreenDirection(MoonInfo moon)
    {
        double angle = Radians(moon.BrightLimbZenithAngle);
        return (-Math.Sin(angle), -Math.Cos(angle));
    }

    private static void AssertAngle(double expected, double actual, double tolerance)
    {
        double difference = Math.Abs((actual - expected) % 360.0);
        difference = Math.Min(difference, 360.0 - difference);

        Assert.True(difference <= tolerance, $"expected {expected:F2}, got {actual:F2}");
    }

    private static double Radians(double degrees) => degrees * Math.PI / 180.0;

    /// <summary>Unit vector with x east, y north, z up.</summary>
    private static double[] Direction(double azimuth, double altitude)
    {
        double az = Radians(azimuth), alt = Radians(altitude);
        return [Math.Sin(az) * Math.Cos(alt), Math.Cos(az) * Math.Cos(alt), Math.Sin(alt)];
    }

    private static double Dot(double[] a, double[] b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];

    private static double[] Scale(double[] a, double factor) => [a[0] * factor, a[1] * factor, a[2] * factor];

    private static double[] Subtract(double[] a, double[] b) => [a[0] - b[0], a[1] - b[1], a[2] - b[2]];

    private static double[] Cross(double[] a, double[] b) =>
        [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]];

    private static double[] Normalize(double[] a) => Scale(a, 1.0 / Math.Sqrt(Dot(a, a)));
}
