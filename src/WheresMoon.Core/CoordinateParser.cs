using System.Globalization;

namespace WheresMoon.Core;

/// <summary>Reads coordinates as typed by a person, with a dot or a comma as decimal separator.</summary>
public static class CoordinateParser
{
    private const double MaxLatitude = 90.0;
    private const double MaxLongitude = 180.0;

    /// <returns>False when a value is missing, not a number or out of range. An empty elevation counts as 0.</returns>
    public static bool TryParse(string? latitude, string? longitude, string? elevation, out GeoLocation location)
    {
        location = default;

        if (!TryReadNumber(latitude, out double lat) || Math.Abs(lat) > MaxLatitude ||
            !TryReadNumber(longitude, out double lon) || Math.Abs(lon) > MaxLongitude)
        {
            return false;
        }

        double height = 0.0;
        if (!string.IsNullOrWhiteSpace(elevation) && !TryReadNumber(elevation, out height))
        {
            return false;
        }

        location = new GeoLocation(lat, lon, height);
        return true;
    }

    private static bool TryReadNumber(string? text, out double value)
    {
        string normalized = (text ?? string.Empty).Trim().Replace(',', '.').Replace('−', '-');
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
            && double.IsFinite(value);
    }
}
