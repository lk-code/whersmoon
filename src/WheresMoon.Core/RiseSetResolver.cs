using CosineKitty;

namespace WheresMoon.Core;

/// <summary>
/// Turns raw rise/set searches into what the display needs: which pair of events applies,
/// whether the body is up, and where it sits on its arc.
/// </summary>
internal static class RiseSetResolver
{
    /// <summary>How far to look for an event. Beyond this a body counts as up or down for the whole period.</summary>
    private const double SearchDays = 3.0;

    /// <summary>Windows longer than this do not fit the rise-to-set arc, which happens near the poles.</summary>
    private static readonly TimeSpan LongestArcWindow = TimeSpan.FromHours(26);

    /// <summary>A moon event further away than this is shown as "none today".</summary>
    private const int LargestMoonDayOffset = 2;

    /// <summary>Keeps a circling body clear of the hills, where the arc ends would suggest it is setting.</summary>
    private const double CirclingArcMargin = 0.1;

    private const double SolarDayHours = 24.0;
    private const double LunarDayHours = 24.84;

    /// <summary>Atmospheric refraction at the horizon in degrees, as used by SearchRiseSet.</summary>
    private const double HorizonRefraction = 34.0 / 60.0;

    private const double SunRadiusDegrees = 0.2667;
    private const double MoonRadiusDegrees = 0.2589;

    internal readonly record struct ArcState(bool IsUp, DateTimeOffset? Rise, DateTimeOffset? Set, double? Progress);

    /// <summary>Finds the applicable rise/set pair at <paramref name="now"/> and the arc position.</summary>
    internal static ArcState ResolveArc(Body body, Observer observer, AstroTime now, double geometricAltitude)
    {
        AstroTime? nextRise = Find(body, observer, Direction.Rise, now, SearchDays);
        AstroTime? nextSet = Find(body, observer, Direction.Set, now, SearchDays);

        bool isUp = (nextRise, nextSet) switch
        {
            (null, null) => geometricAltitude > -HorizonRefraction - RadiusOf(body),
            (null, _) => true,
            (_, null) => false,
            _ => nextSet.ut < nextRise.ut,
        };

        AstroTime? rise;
        AstroTime? set;
        if (isUp)
        {
            rise = Find(body, observer, Direction.Rise, now, -SearchDays);
            set = nextSet;
        }
        else
        {
            rise = nextRise;
            set = nextRise is null ? null : Find(body, observer, Direction.Set, nextRise, SearchDays);
        }

        return new ArcState(isUp, ToOffset(rise), ToOffset(set), isUp ? Progress(body, observer, now, rise, set) : null);
    }

    /// <summary>
    /// Events of the local calendar day containing <paramref name="now"/>. A missing event may still
    /// fall just after midnight, for example in spring at high latitudes, so the next day is checked too.
    /// </summary>
    internal static (TimeEvent? Rise, TimeEvent? Set) ResolveCalendarDay(
        Body body, Observer observer, DateTimeOffset now, TimeZoneInfo zone)
    {
        DateTime today = TimeZoneInfo.ConvertTime(now, zone).Date;

        return (FindOnDay(Direction.Rise), FindOnDay(Direction.Set));

        TimeEvent? FindOnDay(Direction direction)
        {
            for (int offset = 0; offset <= 1; offset++)
            {
                DateTimeOffset start = LocalMidnight(today.AddDays(offset), zone);
                DateTimeOffset end = LocalMidnight(today.AddDays(offset + 1), zone);

                AstroTime? found = Find(body, observer, direction, ToAstroTime(start), (end - start).TotalDays);
                if (found is not null)
                {
                    return ToTimeEvent(ToOffset(found)!.Value, today, zone);
                }
            }

            return null;
        }
    }

    /// <summary>Converts an arc event for display, dropping it when it is too far from today.</summary>
    internal static TimeEvent? ToMoonEvent(DateTimeOffset? time, DateTimeOffset now, TimeZoneInfo zone)
    {
        if (time is null)
        {
            return null;
        }

        TimeEvent result = ToTimeEvent(time.Value, TimeZoneInfo.ConvertTime(now, zone).Date, zone);
        return Math.Abs(result.DayOffset) <= LargestMoonDayOffset ? result : null;
    }

    /// <summary>Calls SearchRiseSet. Null is its normal way of saying there is no event in the window.</summary>
    internal static AstroTime? Find(Body body, Observer observer, Direction direction, AstroTime start, double limitDays) =>
        Astronomy.SearchRiseSet(body, observer, direction, start, limitDays);

    internal static AstroTime ToAstroTime(DateTimeOffset time) => new(time.UtcDateTime);

    private static DateTimeOffset? ToOffset(AstroTime? time) =>
        time is null ? null : new DateTimeOffset(DateTime.SpecifyKind(time.ToUtcDateTime(), DateTimeKind.Utc));

    private static TimeEvent ToTimeEvent(DateTimeOffset time, DateTime today, TimeZoneInfo zone)
    {
        DateTimeOffset local = TimeZoneInfo.ConvertTime(time, zone);
        return new TimeEvent(local, (local.Date - today).Days);
    }

    private static DateTimeOffset LocalMidnight(DateTime date, TimeZoneInfo zone)
    {
        DateTime local = DateTime.SpecifyKind(date, DateTimeKind.Unspecified);

        // A few zones change their clocks at midnight, so 00:00 may not exist on that day.
        while (zone.IsInvalidTime(local))
        {
            local = local.AddMinutes(30);
        }

        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone), TimeSpan.Zero);
    }

    private static double Progress(Body body, Observer observer, AstroTime now, AstroTime? rise, AstroTime? set)
    {
        if (rise is not null && set is not null && TimeSpan.FromDays(set.ut - rise.ut) <= LongestArcWindow)
        {
            return Math.Clamp((now.ut - rise.ut) / (set.ut - rise.ut), 0.0, 1.0);
        }

        // No usable window: the body circles the sky, so follow its culmination instead.
        double lastTransit = Astronomy.SearchHourAngle(body, observer, 0.0, now, -1).time.ut;
        double cycleHours = body == Body.Moon ? LunarDayHours : SolarDayHours;
        double sinceTransit = (now.ut - lastTransit) * 24.0 / cycleHours;
        double cycle = (sinceTransit + 0.5) % 1.0;

        return CirclingArcMargin + (1.0 - 2.0 * CirclingArcMargin) * cycle;
    }

    private static double RadiusOf(Body body) => body == Body.Moon ? MoonRadiusDegrees : SunRadiusDegrees;
}
