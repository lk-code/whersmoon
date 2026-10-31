using System.Globalization;

namespace WheresMoon.Core.Places;

/// <summary>A named place with what the sky calculation needs. Zone is an IANA time zone id.</summary>
public sealed record Place(
    string Name,
    string Region,
    string Country,
    double Latitude,
    double Longitude,
    double Elevation,
    string Zone)
{
    private const char Separator = '\t';
    private const int FieldCount = 7;

    public GeoLocation Location => new(Latitude, Longitude, Elevation);

    /// <summary>One line of text for storing the place in the settings.</summary>
    public string ToStoredText() => string.Join(
        Separator,
        Name,
        Region,
        Country,
        Latitude.ToString(CultureInfo.InvariantCulture),
        Longitude.ToString(CultureInfo.InvariantCulture),
        Elevation.ToString(CultureInfo.InvariantCulture),
        Zone);

    /// <summary>Reads what <see cref="ToStoredText"/> wrote. Null for anything else, as settings can outlive their format.</summary>
    public static Place? FromStoredText(string? text)
    {
        string[]? fields = text?.Split(Separator);

        if (fields?.Length == FieldCount
            && double.TryParse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double latitude)
            && double.TryParse(fields[4], NumberStyles.Float, CultureInfo.InvariantCulture, out double longitude)
            && double.TryParse(fields[5], NumberStyles.Float, CultureInfo.InvariantCulture, out double elevation))
        {
            return new Place(fields[0], fields[1], fields[2], latitude, longitude, elevation, fields[6]);
        }

        return null;
    }
}
