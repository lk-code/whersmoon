namespace WheresMoon.Core.Places;

/// <summary>Turns a stored IANA zone id into the zone of the running system.</summary>
public static class PlaceZone
{
    private const double DegreesPerHour = 15.0;

    /// <summary>
    /// Finds the zone. A system may not know every id, for example a recent one on an old phone.
    /// Mean solar time from the longitude is then close enough and never fails.
    /// </summary>
    public static TimeZoneInfo Resolve(string id, double longitude)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException or System.Security.SecurityException)
        {
            var offset = TimeSpan.FromHours(Math.Round(longitude / DegreesPerHour));
            return TimeZoneInfo.CreateCustomTimeZone(id, offset, id, id);
        }
    }
}
