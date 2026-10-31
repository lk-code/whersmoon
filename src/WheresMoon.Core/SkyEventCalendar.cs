using CosineKitty;

namespace WheresMoon.Core;

/// <summary>
/// Finds full and new moons, supermoons and eclipses after any moment for one place. Eclipses can
/// take many search steps and scrubbing asks again for every frame, so found events are kept.
/// Safe to use from several threads.
/// </summary>
public sealed class SkyEventCalendar
{
    /// <summary>
    /// A full moon closer than this counts as a supermoon. There is no official limit. This wide one
    /// gives the supermoons usually listed, while 360,000 km would miss 24 November 2026 (360,754 km).
    /// </summary>
    public const double SupermoonDistanceKm = 367_607.0;

    private const double FullMoonLongitude = 180.0;
    private const double NewMoonLongitude = 0.0;
    private const double LunationLimitDays = 40.0;

    /// <summary>Keeps a search from finding the event it started at.</summary>
    private static readonly TimeSpan StepPastEvent = TimeSpan.FromMinutes(1);

    private readonly object _gate = new();
    private readonly Series[] _series;

    public SkyEventCalendar(GeoLocation location)
    {
        Observer observer = SkyCalculator.ToObserver(location);

        // Same order as SkyEventKind, which is also the order on the page.
        _series =
        [
            new Series(time => MoonPhase(SkyEventKind.FullMoon, FullMoonLongitude, time)),
            new Series(time => MoonPhase(SkyEventKind.NewMoon, NewMoonLongitude, time)),
            new Series(NextSupermoon),
            new Series(NextLunarEclipse),
            new Series(time => NextSolarEclipse(time, observer)),
        ];
    }

    /// <summary>The next event of every kind after <paramref name="time"/>.</summary>
    public IReadOnlyList<SkyEvent> Upcoming(DateTimeOffset time)
    {
        lock (_gate)
        {
            return _series.Select(series => series.NextAfter(time)).ToList();
        }
    }

    /// <summary>All events between two moments, in order.</summary>
    public IReadOnlyList<SkyEvent> Between(DateTimeOffset from, DateTimeOffset to)
    {
        var found = new List<SkyEvent>();

        lock (_gate)
        {
            foreach (Series series in _series)
            {
                SkyEvent next = series.NextAfter(from);
                while (next.Time < to)
                {
                    found.Add(next);
                    next = series.NextAfter(next.Time);
                }
            }
        }

        found.Sort((a, b) => a.Time.CompareTo(b.Time));
        return found;
    }

    private static SkyEvent MoonPhase(SkyEventKind kind, double longitude, DateTimeOffset after)
    {
        AstroTime start = RiseSetResolver.ToAstroTime(after + StepPastEvent);
        AstroTime found = Astronomy.SearchMoonPhase(longitude, start, LunationLimitDays)
            ?? throw new InvalidOperationException("A lunation is shorter than the search limit, so a phase must be found.");

        return new SkyEvent(kind, RiseSetResolver.ToOffset(found)!.Value);
    }

    private static SkyEvent NextSupermoon(DateTimeOffset after)
    {
        DateTimeOffset cursor = after;

        while (true)
        {
            SkyEvent fullMoon = MoonPhase(SkyEventKind.Supermoon, FullMoonLongitude, cursor);
            double distance = Astronomy.GeoMoon(RiseSetResolver.ToAstroTime(fullMoon.Time)).Length() * Astronomy.KM_PER_AU;

            if (distance < SupermoonDistanceKm)
            {
                return fullMoon;
            }

            cursor = fullMoon.Time;
        }
    }

    private static SkyEvent NextLunarEclipse(DateTimeOffset after)
    {
        LunarEclipseInfo eclipse = Astronomy.SearchLunarEclipse(RiseSetResolver.ToAstroTime(after + StepPastEvent));
        return new SkyEvent(SkyEventKind.LunarEclipse, RiseSetResolver.ToOffset(eclipse.peak)!.Value, ToType(eclipse.kind));
    }

    private static SkyEvent NextSolarEclipse(DateTimeOffset after, Observer observer)
    {
        LocalSolarEclipseInfo eclipse = Astronomy.SearchLocalSolarEclipse(RiseSetResolver.ToAstroTime(after + StepPastEvent), observer);
        return new SkyEvent(SkyEventKind.SolarEclipse, RiseSetResolver.ToOffset(eclipse.peak.time)!.Value, ToType(eclipse.kind));
    }

    private static EclipseType ToType(EclipseKind kind) => kind switch
    {
        EclipseKind.Penumbral => EclipseType.Penumbral,
        EclipseKind.Partial => EclipseType.Partial,
        EclipseKind.Annular => EclipseType.Annular,
        EclipseKind.Total => EclipseType.Total,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "A search only returns eclipses that happen."),
    };

    /// <summary>A gapless run of events of one kind. Going back before its first event starts a new run.</summary>
    private sealed class Series(Func<DateTimeOffset, SkyEvent> findNext)
    {
        private readonly List<SkyEvent> _events = [];
        private DateTimeOffset _coveredFrom;

        public SkyEvent NextAfter(DateTimeOffset time)
        {
            if (_events.Count == 0 || time < _coveredFrom)
            {
                _events.Clear();
                _coveredFrom = time;
                _events.Add(findNext(time));
            }

            while (_events[^1].Time <= time)
            {
                _events.Add(findNext(_events[^1].Time));
            }

            int low = 0;
            int high = _events.Count - 1;

            while (low < high)
            {
                int middle = (low + high) / 2;
                if (_events[middle].Time > time)
                {
                    high = middle;
                }
                else
                {
                    low = middle + 1;
                }
            }

            return _events[low];
        }
    }
}
