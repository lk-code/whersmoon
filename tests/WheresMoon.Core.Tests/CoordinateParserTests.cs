namespace WheresMoon.Core.Tests;

public class CoordinateParserTests
{
    [Theory]
    [InlineData("52.52", "13.405", "", 52.52, 13.405, 0.0)]
    [InlineData("52,52", "13,405", "34", 52.52, 13.405, 34.0)]
    [InlineData(" -33.87 ", "151.21", null, -33.87, 151.21, 0.0)]
    [InlineData("−33,87", "−58,38", "-5", -33.87, -58.38, -5.0)]
    [InlineData("90", "-180", "", 90.0, -180.0, 0.0)]
    public void Valid_input_is_understood(string latitude, string longitude, string? elevation, double lat, double lon, double height)
    {
        Assert.True(CoordinateParser.TryParse(latitude, longitude, elevation, out GeoLocation location));
        Assert.Equal(new GeoLocation(lat, lon, height), location);
    }

    [Theory]
    [InlineData("", "13.4", "")]
    [InlineData("52.5", "", "")]
    [InlineData("abc", "13.4", "")]
    [InlineData("90.1", "13.4", "")]
    [InlineData("52.5", "180.5", "")]
    [InlineData("52.5", "13.4", "hoch")]
    [InlineData("NaN", "13.4", "")]
    [InlineData("52.5", "Infinity", "")]
    public void Invalid_input_is_rejected(string latitude, string longitude, string elevation)
    {
        Assert.False(CoordinateParser.TryParse(latitude, longitude, elevation, out _));
    }
}
