using System.Globalization;

namespace WheresMoon.Core.Tests;

/// <summary>
/// Moon phases from Fred Espenak's table (astropixels.com/ephemeris/phasescat/phases2001.html), lunar
/// eclipses from NASA's catalogue (eclipse.gsfc.nasa.gov/LEdecade/LEdecade2021.html) converted from
/// dynamical time to UT. Supermoon dates and distances from EarthSky's table (earthsky.org/space/what-is-a-supermoon).
/// All retrieved 2026-10-31.
/// </summary>
public class SkyEventTests
{
    private static readonly GeoLocation Berlin = new(52.52, 13.405);

    private static readonly TimeSpan PhaseTolerance = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan EclipseTolerance = TimeSpan.FromMinutes(5);

    [TestCase("2026-10-31T00:00:00Z", "2026-11-24T14:53:00Z")]
    [TestCase("2026-11-25T00:00:00Z", "2026-12-24T01:28:00Z")]
    [TestCase("2026-01-01T00:00:00Z", "2026-01-03T10:03:00Z")]
    public void Full_moon_matches_the_tables(string after, string expected)
    {
        AssertClose(expected, Next(SkyEventKind.FullMoon, after).Time, PhaseTolerance);
    }

    [TestCase("2026-10-31T00:00:00Z", "2026-11-09T07:02:00Z")]
    [TestCase("2026-11-10T00:00:00Z", "2026-12-09T00:52:00Z")]
    public void New_moon_matches_the_tables(string after, string expected)
    {
        AssertClose(expected, Next(SkyEventKind.NewMoon, after).Time, PhaseTolerance);
    }

    [TestCase("2026-10-31T00:00:00Z", "2026-11-24T14:53:00Z")]
    [TestCase("2026-11-25T00:00:00Z", "2026-12-24T01:28:00Z")]
    public void Supermoon_is_a_full_moon_near_perigee(string after, string expected)
    {
        AssertClose(expected, Next(SkyEventKind.Supermoon, after).Time, PhaseTolerance);
    }

    [Test]
    public void Full_moons_far_from_perigee_are_skipped()
    {
        // The full moons of spring and summer 2026 are all too far away.
        SkyEvent next = Next(SkyEventKind.Supermoon, "2026-02-01T00:00:00Z");

        next.Time.Year.Should().Be(2026);
        next.Time.Month.Should().Be(11);
    }

    [TestCase("2026-02-01T00:00:00Z", "2026-03-03T11:33:00Z", EclipseType.Total)]
    [TestCase("2026-03-10T00:00:00Z", "2026-08-28T04:13:00Z", EclipseType.Partial)]
    [TestCase("2026-09-01T00:00:00Z", "2027-02-20T23:13:00Z", EclipseType.Penumbral)]
    public void Lunar_eclipse_matches_the_catalogue(string after, string expected, EclipseType type)
    {
        SkyEvent eclipse = Next(SkyEventKind.LunarEclipse, after);

        AssertClose(expected, eclipse.Time, EclipseTolerance);
        eclipse.Eclipse.Should().Be(type);
    }

    // No catalogue behind the local peaks: these fix the type and the rough time, hence the wider tolerance.
    [TestCase(32.78, -96.80, "2024-04-01T00:00:00Z", "2024-04-08T18:42:00Z", EclipseType.Total)] // Dallas
    [TestCase(35.08, -106.65, "2023-10-01T00:00:00Z", "2023-10-14T16:36:00Z", EclipseType.Annular)] // Albuquerque
    [TestCase(52.52, 13.405, "2025-03-01T00:00:00Z", "2025-03-29T11:17:00Z", EclipseType.Partial)] // Berlin
    public void Solar_eclipse_matches_the_local_circumstances(double latitude, double longitude, string after, string expected, EclipseType type)
    {
        var calendar = new SkyEventCalendar(new GeoLocation(latitude, longitude));

        SkyEvent eclipse = calendar.Upcoming(Parse(after)).Single(e => e.Kind == SkyEventKind.SolarEclipse);

        AssertClose(expected, eclipse.Time, EclipseTolerance);
        eclipse.Eclipse.Should().Be(type);
    }

    [Test]
    public void Solar_eclipses_out_of_sight_are_skipped()
    {
        // The eclipse of 2026-08-12 is over Europe and the Arctic, far from Dallas.
        var calendar = new SkyEventCalendar(new GeoLocation(32.78, -96.80));

        SkyEvent eclipse = calendar.Upcoming(Parse("2026-08-01T00:00:00Z")).Single(e => e.Kind == SkyEventKind.SolarEclipse);

        eclipse.Time.Should().BeAfter(Parse("2026-09-01T00:00:00Z"));
    }

    [Test]
    public void Every_kind_appears_once_in_a_fixed_order()
    {
        IReadOnlyList<SkyEvent> events = new SkyEventCalendar(Berlin).Upcoming(Parse("2026-10-31T00:00:00Z"));

        events.Select(e => e.Kind).Should().Equal(Enum.GetValues<SkyEventKind>());
    }

    [Test]
    public void An_event_counts_as_past_at_its_exact_moment()
    {
        var calendar = new SkyEventCalendar(Berlin);
        SkyEvent first = calendar.Upcoming(Parse("2026-10-31T00:00:00Z"))[0];

        SkyEvent next = calendar.Upcoming(first.Time)[0];

        next.Time.Should().BeAfter(first.Time.AddDays(20));
    }

    [Test]
    public void Between_finds_all_events_in_order()
    {
        IReadOnlyList<SkyEvent> events = new SkyEventCalendar(Berlin)
            .Between(Parse("2026-11-01T00:00:00Z"), Parse("2026-12-31T00:00:00Z"));

        // Two new moons, two full moons and both of those again as supermoons.
        events.Count.Should().Be(6);
        events.Should().Equal(events.OrderBy(e => e.Time));
    }

    [Test]
    public void Scrubbing_backwards_finds_earlier_events_too()
    {
        var calendar = new SkyEventCalendar(Berlin);

        calendar.Upcoming(Parse("2026-10-31T00:00:00Z"));
        SkyEvent earlier = calendar.Upcoming(Parse("2026-02-01T00:00:00Z"))[0];

        earlier.Time.Year.Should().Be(2026);
        earlier.Time.Month.Should().Be(2);
    }

    [TestCase(-2880)]
    [TestCase(-30)]
    [TestCase(-5)]
    [TestCase(-2)]
    [TestCase(1)]
    [TestCase(5)]
    public void Time_around_an_eclipse_peak_finds_the_right_one(int minutes)
    {
        var calendar = new SkyEventCalendar(Berlin);
        SkyEvent eclipse = calendar.Upcoming(Parse("2026-08-01T00:00:00Z")).Single(e => e.Kind == SkyEventKind.LunarEclipse);
        DateTimeOffset time = eclipse.Time.AddMinutes(minutes);

        SkyEvent next = calendar.Upcoming(time).Single(e => e.Kind == SkyEventKind.LunarEclipse);

        next.Time.Should().BeAfter(time);
        (minutes < 0 ? next.Time : next.Time.Date).Should().Be(minutes < 0 ? eclipse.Time : Parse("2027-02-20T23:13:00Z").Date);
    }

    [Test]
    public void Passing_eclipses_never_gets_stuck()
    {
        IReadOnlyList<SkyEvent> events = new SkyEventCalendar(Berlin)
            .Between(Parse("2025-01-01T00:00:00Z"), Parse("2029-01-01T00:00:00Z"));

        List<SkyEvent> lunar = events.Where(e => e.Kind == SkyEventKind.LunarEclipse).ToList();
        List<SkyEvent> solar = events.Where(e => e.Kind == SkyEventKind.SolarEclipse).ToList();

        // NASA's catalogue lists these ten from 2025 to 2028.
        lunar.Select(e => e.Time.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Should().Equal(
            ["2025-03-14", "2025-09-07", "2026-03-03", "2026-08-28", "2027-02-20", "2027-07-18", "2027-08-17", "2028-01-12", "2028-07-06", "2028-12-31"]);

        solar.Should().NotBeEmpty();
        solar.Select(e => e.Time.Date).Distinct().Count().Should().Be(solar.Count);
    }

    [Test]
    public void Scrubbing_backwards_step_by_step_stays_correct_and_does_not_search_again()
    {
        var calendar = new SkyEventCalendar(Berlin);
        DateTimeOffset start = Parse("2026-11-20T00:00:00Z");
        calendar.Upcoming(start);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (int hour = 0; hour < 24 * 30; hour++)
        {
            DateTimeOffset time = start.AddHours(-hour);
            SkyEvent fullMoon = calendar.Upcoming(time)[0];

            fullMoon.Time.Should().BeAfter(time);
            fullMoon.Time.Should().BeBefore(time.AddDays(30));
        }

        // A month of hourly frames. Searching anew each time would take a large multiple of this.
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, $"took {stopwatch.ElapsedMilliseconds} ms");
    }

    private static SkyEvent Next(SkyEventKind kind, string after) =>
        new SkyEventCalendar(Berlin).Upcoming(Parse(after)).Single(e => e.Kind == kind);

    private static void AssertClose(string expected, DateTimeOffset actual, TimeSpan tolerance)
    {
        DateTimeOffset time = Parse(expected);
        actual.Should().BeCloseTo(time, tolerance);
    }

    private static DateTimeOffset Parse(string text) => DateTimeOffset.Parse(text, CultureInfo.InvariantCulture);
}
