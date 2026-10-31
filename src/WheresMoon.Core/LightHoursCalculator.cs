using CosineKitty;

namespace WheresMoon.Core;

/// <summary>Finds the golden and blue hours of a local calendar day from the sun's altitude.</summary>
public static class LightHoursCalculator
{
    /// <summary>Altitudes that separate the bands: night, blue hour, golden hour, day.</summary>
    private static readonly double[] Limits = [-6.0, -4.0, 6.0];

    private const int BlueBand = 1;
    private const int GoldenBand = 2;

    private const double StepPastEvent = 1.0 / 1440.0;

    private readonly record struct Crossing(AstroTime Time, int LimitIndex, bool IsRising);

    public static LightHours Compute(DateTimeOffset time, GeoLocation location, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(zone);

        Observer observer = SkyCalculator.ToObserver(location);
        DateTime today = TimeZoneInfo.ConvertTime(time, zone).Date;
        AstroTime start = RiseSetResolver.ToAstroTime(RiseSetResolver.LocalMidnight(today, zone));
        AstroTime end = RiseSetResolver.ToAstroTime(RiseSetResolver.LocalMidnight(today.AddDays(1), zone));

        int band = BandOf(SkyCalculator.GeometricSunAltitude(observer, start));
        List<Crossing> crossings = FindCrossings(observer, start, end);

        if (crossings.Count == 0)
        {
            return new LightHours([], Range(band), []);
        }

        AstroTime noon = Astronomy.SearchHourAngle(Body.Sun, observer, 0.0, start, +1).time;
        var windows = new List<LightWindow>();
        AstroTime? windowStart = null;

        foreach (Crossing crossing in crossings)
        {
            AddWindow(windowStart, crossing.Time);
            band = crossing.IsRising ? crossing.LimitIndex + 1 : crossing.LimitIndex;
            windowStart = crossing.Time;
        }

        AddWindow(windowStart, null);

        return new LightHours(windows, SunRange.Mixed, crossings.Select(c => ToLocal(c.Time, zone)).ToList());

        void AddWindow(AstroTime? from, AstroTime? to)
        {
            if (band is BlueBand or GoldenBand)
            {
                windows.Add(new LightWindow(
                    band == GoldenBand ? LightKind.Golden : LightKind.Blue,
                    PartOf(from, to, noon),
                    from is null ? null : ToLocal(from, zone),
                    to is null ? null : ToLocal(to, zone)));
            }
        }
    }

    private static List<Crossing> FindCrossings(Observer observer, AstroTime start, AstroTime end)
    {
        var crossings = new List<Crossing>();

        for (int limit = 0; limit < Limits.Length; limit++)
        {
            foreach (Direction direction in new[] { Direction.Rise, Direction.Set })
            {
                double cursor = start.ut;

                while (cursor < end.ut)
                {
                    AstroTime? found = Astronomy.SearchAltitude(
                        Body.Sun, observer, direction, new AstroTime(cursor), end.ut - cursor, Limits[limit]);

                    if (found is null)
                    {
                        break;
                    }

                    crossings.Add(new Crossing(found, limit, direction == Direction.Rise));
                    cursor = found.ut + StepPastEvent;
                }
            }
        }

        crossings.Sort((a, b) => a.Time.ut.CompareTo(b.Time.ut));
        return crossings;
    }

    private static int BandOf(double altitude) => Limits.Count(limit => altitude >= limit);

    private static SunRange Range(int band) => band >= Limits.Length ? SunRange.AlwaysHigh : SunRange.AlwaysLow;

    /// <summary>A missing end lies beyond the day, so it counts as after noon, and a missing start as before it.</summary>
    private static DayPart PartOf(AstroTime? from, AstroTime? to, AstroTime noon)
    {
        bool startsBeforeNoon = from is null || from.ut < noon.ut;
        bool endsAfterNoon = to is null || to.ut > noon.ut;

        if (startsBeforeNoon && endsAfterNoon)
        {
            return DayPart.Midday;
        }

        return endsAfterNoon ? DayPart.Evening : DayPart.Morning;
    }

    private static DateTimeOffset ToLocal(AstroTime time, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTime(RiseSetResolver.ToOffset(time)!.Value, zone);
}
