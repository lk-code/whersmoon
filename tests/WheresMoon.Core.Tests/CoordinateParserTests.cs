namespace WheresMoon.Core.Tests;

public class CoordinateParserTests
{
    [TestCase("52.52", "13.405", "", 52.52, 13.405, 0.0)]
    [TestCase("52,52", "13,405", "34", 52.52, 13.405, 34.0)]
    [TestCase(" -33.87 ", "151.21", null, -33.87, 151.21, 0.0)]
    [TestCase("−33,87", "−58,38", "-5", -33.87, -58.38, -5.0)]
    [TestCase("90", "-180", "", 90.0, -180.0, 0.0)]
    public void Valid_input_is_understood(string latitude, string longitude, string? elevation, double lat, double lon, double height)
    {
        CoordinateParser.TryParse(latitude, longitude, elevation, out GeoLocation location).Should().BeTrue();
        location.Should().Be(new GeoLocation(lat, lon, height));
    }

    [TestCase("", "13.4", "")]
    [TestCase("52.5", "", "")]
    [TestCase("abc", "13.4", "")]
    [TestCase("90.1", "13.4", "")]
    [TestCase("52.5", "180.5", "")]
    [TestCase("52.5", "13.4", "hoch")]
    [TestCase("NaN", "13.4", "")]
    [TestCase("52.5", "Infinity", "")]
    public void Invalid_input_is_rejected(string latitude, string longitude, string elevation)
    {
        CoordinateParser.TryParse(latitude, longitude, elevation, out _).Should().BeFalse();
    }
}
