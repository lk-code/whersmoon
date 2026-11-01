using CosineKitty;

namespace WheresMoon.Core.Interaction;

/// <summary>The events scrubbing catches on for each page. Days stay cached, as scrubbing asks on every frame.</summary>
public sealed class SnapPoints(GeoLocation location, TimeZoneInfo zone, SkyEventCalendar events)
{
    private readonly Observer _observer = SkyCalculator.ToObserver(location);
    private readonly Dictionary<DateTime, IReadOnlyList<DateTimeOffset>> _riseAndSet = [];
    private readonly Dictionary<DateTime, IReadOnlyList<DateTimeOffset>> _lightHours = [];

    public IReadOnlyList<DateTimeOffset> Around(InfoPage page, DateTimeOffset time, TimeSpan reach)
    {
        DateTimeOffset from = time - reach;
        DateTimeOffset to = time + reach;

        return page == InfoPage.Events
            ? events.Between(from, to).Select(item => item.Time).Distinct().ToList()
            : InDays(from, to, page == InfoPage.LightHours ? LightHoursOf : RiseAndSetOf);
    }

    private List<DateTimeOffset> InDays(DateTimeOffset from, DateTimeOffset to, Func<DateTime, IReadOnlyList<DateTimeOffset>> ofDay)
    {
        var found = new List<DateTimeOffset>();
        DateTime lastDay = TimeZoneInfo.ConvertTime(to, zone).Date;

        for (DateTime day = TimeZoneInfo.ConvertTime(from, zone).Date; day <= lastDay; day = day.AddDays(1))
        {
            found.AddRange(ofDay(day).Where(time => time >= from && time <= to));
        }

        return found;
    }

    private IReadOnlyList<DateTimeOffset> RiseAndSetOf(DateTime day)
    {
        if (!_riseAndSet.TryGetValue(day, out IReadOnlyList<DateTimeOffset>? times))
        {
            DateTimeOffset start = RiseSetResolver.LocalMidnight(day, zone);
            double length = (RiseSetResolver.LocalMidnight(day.AddDays(1), zone) - start).TotalDays;
            AstroTime begin = RiseSetResolver.ToAstroTime(start);

            times = new[] { Body.Sun, Body.Moon }
                .SelectMany(_ => new[] { Direction.Rise, Direction.Set }, (body, direction) => (body, direction))
                .Select(item => RiseSetResolver.ToOffset(RiseSetResolver.Find(item.body, _observer, item.direction, begin, length)))
                .OfType<DateTimeOffset>()
                .ToList();

            _riseAndSet[day] = times;
        }

        return times;
    }

    private IReadOnlyList<DateTimeOffset> LightHoursOf(DateTime day)
    {
        if (!_lightHours.TryGetValue(day, out IReadOnlyList<DateTimeOffset>? times))
        {
            times = LightHoursCalculator.Compute(RiseSetResolver.LocalMidnight(day, zone), location, zone).Boundaries;
            _lightHours[day] = times;
        }

        return times;
    }
}
