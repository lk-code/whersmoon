using CosineKitty;

namespace WheresMoon.Core;

/// <summary>Measures how long the sun is up on a local calendar day.</summary>
public static class DayLengthCalculator
{
    /// <summary>Keeps a search from finding the event it just started at.</summary>
    private const double StepPastEvent = 1.0 / 86400.0;

    public static DayLengthInfo Compute(DateTimeOffset time, GeoLocation location, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(zone);

        Observer observer = SkyCalculator.ToObserver(location);
        DateTime today = TimeZoneInfo.ConvertTime(time, zone).Date;

        (DaylightKind kind, TimeSpan length) = Measure(observer, today, zone);
        (DaylightKind previousKind, TimeSpan previous) = Measure(observer, today.AddDays(-1), zone);

        bool isComparable = kind == DaylightKind.Normal && previousKind == DaylightKind.Normal;
        return new DayLengthInfo(kind, length, isComparable ? length - previous : null);
    }

    /// <summary>Adds up the time between rises and sets, because near the polar circles a day can hold any mix of them.</summary>
    private static (DaylightKind Kind, TimeSpan Length) Measure(Observer observer, DateTime date, TimeZoneInfo zone)
    {
        AstroTime start = RiseSetResolver.ToAstroTime(RiseSetResolver.LocalMidnight(date, zone));
        AstroTime end = RiseSetResolver.ToAstroTime(RiseSetResolver.LocalMidnight(date.AddDays(1), zone));

        bool isUp = RiseSetResolver.IsUp(Body.Sun, SkyCalculator.GeometricSunAltitude(observer, start));
        bool startedUp = isUp;
        bool hasEvents = false;
        double upDays = 0.0;
        double cursor = start.ut;

        while (true)
        {
            AstroTime? next = RiseSetResolver.Find(
                Body.Sun, observer, isUp ? Direction.Set : Direction.Rise, new AstroTime(cursor), end.ut - cursor);

            if (next is null)
            {
                upDays += isUp ? end.ut - cursor : 0.0;
                break;
            }

            upDays += isUp ? next.ut - cursor : 0.0;
            isUp = !isUp;
            hasEvents = true;
            cursor = next.ut + StepPastEvent;
        }

        DaylightKind kind = hasEvents ? DaylightKind.Normal : startedUp ? DaylightKind.MidnightSun : DaylightKind.PolarNight;
        return (kind, TimeSpan.FromDays(upDays));
    }
}
