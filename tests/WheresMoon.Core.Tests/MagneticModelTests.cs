using System.Globalization;
using WheresMoon.Core.Compass;

namespace WheresMoon.Core.Tests;

public class MagneticModelTests
{
    /// <summary>The test file gives two decimals, so this is its rounding and nothing more.</summary>
    private const double Tolerance = 0.0051;

    public static IEnumerable<TestCaseData> Points() =>
        WmmFixtures.Points.Select(point => new TestCaseData(point.Year, point.HeightKm, point.Latitude, point.Longitude, point.Declination));

    [TestCaseSource(nameof(Points))]
    public void Declination_matches_the_official_test_values(double year, double heightKm, double latitude, double longitude, double expected)
    {
        double actual = WorldMagneticModel.Declination(latitude, longitude, heightKm, year);

        actual.Should().BeInRange(expected - Tolerance, expected + Tolerance);
    }

    [Test]
    public void All_official_test_points_are_covered()
    {
        WmmFixtures.Points.Count.Should().Be(100);
    }

    [Test]
    public void Berlin_lies_a_few_degrees_east()
    {
        double declination = WorldMagneticModel.Declination(new GeoLocation(52.52, 13.405), Parse("2026-11-01T00:00:00Z"));

        declination.Should().BeInRange(4.0, 7.0);
    }

    [Test]
    public void Seattle_lies_far_east_and_Rio_far_west()
    {
        DateTimeOffset time = Parse("2026-11-01T00:00:00Z");

        WorldMagneticModel.Declination(new GeoLocation(47.6, -122.3), time).Should().BeInRange(13.0, 17.0);
        WorldMagneticModel.Declination(new GeoLocation(-22.9, -43.2), time).Should().BeInRange(-26.0, -21.0);
    }

    [TestCase("2025-01-01T00:00:00Z", 2025.0)]
    [TestCase("2027-07-02T12:00:00Z", 2027.5)]
    [TestCase("2028-01-01T00:00:00+01:00", 2027.99989)]
    public void Decimal_year_counts_from_the_start_of_the_year_in_utc(string text, double expected)
    {
        WorldMagneticModel.DecimalYear(Parse(text)).Should().BeApproximately(expected, 0.5e-4);
    }

    [TestCase(90.0, 0.0)]
    [TestCase(-90.0, 0.0)]
    public void The_poles_give_a_number(double latitude, double longitude)
    {
        double.IsFinite(WorldMagneticModel.Declination(new GeoLocation(latitude, longitude), Parse("2026-01-01T00:00:00Z"))).Should().BeTrue();
    }

    private static DateTimeOffset Parse(string text) => DateTimeOffset.Parse(text, CultureInfo.InvariantCulture);
}
