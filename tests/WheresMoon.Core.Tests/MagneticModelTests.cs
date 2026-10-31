using System.Globalization;
using WheresMoon.Core.Compass;

namespace WheresMoon.Core.Tests;

public class MagneticModelTests
{
    /// <summary>The test file gives two decimals, so this is its rounding and nothing more.</summary>
    private const double Tolerance = 0.0051;

    public static TheoryData<double, double, double, double, double> Points()
    {
        var data = new TheoryData<double, double, double, double, double>();

        foreach ((double year, double height, double latitude, double longitude, double declination) in WmmFixtures.Points)
        {
            data.Add(year, height, latitude, longitude, declination);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Points))]
    public void Declination_matches_the_official_test_values(double year, double heightKm, double latitude, double longitude, double expected)
    {
        double actual = WorldMagneticModel.Declination(latitude, longitude, heightKm, year);

        Assert.InRange(actual, expected - Tolerance, expected + Tolerance);
    }

    [Fact]
    public void All_official_test_points_are_covered()
    {
        Assert.Equal(100, WmmFixtures.Points.Count);
    }

    [Fact]
    public void Berlin_lies_a_few_degrees_east()
    {
        double declination = WorldMagneticModel.Declination(new GeoLocation(52.52, 13.405), Parse("2026-11-01T00:00:00Z"));

        Assert.InRange(declination, 4.0, 7.0);
    }

    [Fact]
    public void Seattle_lies_far_east_and_Rio_far_west()
    {
        DateTimeOffset time = Parse("2026-11-01T00:00:00Z");

        Assert.InRange(WorldMagneticModel.Declination(new GeoLocation(47.6, -122.3), time), 13.0, 17.0);
        Assert.InRange(WorldMagneticModel.Declination(new GeoLocation(-22.9, -43.2), time), -26.0, -21.0);
    }

    [Theory]
    [InlineData("2025-01-01T00:00:00Z", 2025.0)]
    [InlineData("2027-07-02T12:00:00Z", 2027.5)]
    [InlineData("2028-01-01T00:00:00+01:00", 2027.99989)]
    public void Decimal_year_counts_from_the_start_of_the_year_in_utc(string text, double expected)
    {
        Assert.Equal(expected, WorldMagneticModel.DecimalYear(Parse(text)), 4);
    }

    [Theory]
    [InlineData(90.0, 0.0)]
    [InlineData(-90.0, 0.0)]
    public void The_poles_give_a_number(double latitude, double longitude)
    {
        Assert.True(double.IsFinite(WorldMagneticModel.Declination(new GeoLocation(latitude, longitude), Parse("2026-01-01T00:00:00Z"))));
    }

    private static DateTimeOffset Parse(string text) => DateTimeOffset.Parse(text, CultureInfo.InvariantCulture);
}
