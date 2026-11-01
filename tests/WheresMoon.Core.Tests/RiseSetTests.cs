using System.Globalization;
using CosineKitty;

namespace WheresMoon.Core.Tests;

public class RiseSetTests
{
    private static readonly TimeSpan SunTolerance = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan MoonTolerance = TimeSpan.FromMinutes(3);

    public static IEnumerable<UsnoFixtures.Day> Days() => UsnoFixtures.Days;

    [TestCaseSource(nameof(Days))]
    public void Sun_matches_usno(UsnoFixtures.Day day)
    {
        SkySnapshot sky = SkyCalculator.Compute(Noon(day), Location(day), TimeZoneInfo.Utc);

        AssertEvent(day, day.SunRise, Today(sky.Sun.Body.Rise), SunTolerance);
        AssertEvent(day, day.SunSet, Today(sky.Sun.Body.Set), SunTolerance);
    }

    [TestCaseSource(nameof(Days))]
    public void Moon_matches_usno(UsnoFixtures.Day day)
    {
        var observer = new Observer(day.Latitude, day.Longitude, 0);
        AstroTime start = RiseSetResolver.ToAstroTime(Midnight(day));

        AssertEvent(day, day.MoonRise, Search(Direction.Rise), MoonTolerance);
        AssertEvent(day, day.MoonSet, Search(Direction.Set), MoonTolerance);

        DateTimeOffset? Search(Direction direction)
        {
            AstroTime? found = RiseSetResolver.Find(Body.Moon, observer, direction, start, 1.0);
            return found is null ? null : new DateTimeOffset(DateTime.SpecifyKind(found.ToUtcDateTime(), DateTimeKind.Utc));
        }
    }

    [TestCaseSource(nameof(Days))]
    public void Illumination_matches_usno(UsnoFixtures.Day day)
    {
        SkySnapshot sky = SkyCalculator.Compute(Noon(day), Location(day), TimeZoneInfo.Utc);

        (sky.Moon.IlluminatedFraction * 100.0).Should().BeInRange(day.IlluminationPercent - 3.0, day.IlluminationPercent + 3.0);
    }

    [TestCase("2026-03-29T10:00:00Z", "06:48", "19:35")]
    [TestCase("2026-10-25T10:00:00Z", "06:50", "16:50")]
    public void Clock_change_days_use_local_midnights_and_the_new_offset(string utc, string rise, string set)
    {
        TimeZoneInfo berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

        SkySnapshot sky = SkyCalculator.Compute(DateTimeOffset.Parse(utc, CultureInfo.InvariantCulture), new GeoLocation(52.52, 13.405), berlin);

        sky.Sun.Body.Rise!.Value.DayOffset.Should().Be(0);
        sky.Sun.Body.Set!.Value.DayOffset.Should().Be(0);
        AssertClock(rise, sky.Sun.Body.Rise.Value.Time);
        AssertClock(set, sky.Sun.Body.Set.Value.Time);
    }

    [Test]
    public void Evening_after_sunset_still_shows_todays_sun()
    {
        var evening = new DateTimeOffset(2026, 10, 25, 20, 0, 0, TimeSpan.Zero);

        SkySnapshot sky = SkyCalculator.Compute(evening, new GeoLocation(52.52, 13.405), TimeZoneInfo.Utc);

        sky.Sun.Body.IsUp.Should().BeFalse();
        sky.Sun.Body.ArcProgress.Should().BeNull();
        sky.Sun.Body.Set!.Value.DayOffset.Should().Be(0);
        sky.Sun.Body.Set.Value.Time.Should().BeBefore(evening);
    }

    [Test]
    public void Midnight_sun_has_no_events_but_keeps_moving_above_the_hills()
    {
        var location = new GeoLocation(69.6492, 18.9553);
        DateTimeOffset start = new(2026, 6, 21, 0, 0, 0, TimeSpan.Zero);
        var progress = new List<double>();

        for (int hour = 0; hour < 24; hour++)
        {
            SkySnapshot sky = SkyCalculator.Compute(start.AddHours(hour), location, TimeZoneInfo.Utc);

            sky.Sun.Body.IsUp.Should().BeTrue();
            sky.Sun.Body.Rise.Should().BeNull();
            sky.Sun.Body.Set.Should().BeNull();
            progress.Add(sky.Sun.Body.ArcProgress!.Value);
        }

        progress.Should().AllSatisfy(t => t.Should().BeInRange(0.1, 0.9));
        (progress.Max() - progress.Min()).Should().BeGreaterThan(0.7);
    }

    [Test]
    public void Polar_night_hides_the_sun()
    {
        SkySnapshot sky = SkyCalculator.Compute(
            new DateTimeOffset(2026, 12, 21, 11, 0, 0, TimeSpan.Zero),
            new GeoLocation(69.6492, 18.9553),
            TimeZoneInfo.Utc);

        sky.Sun.Body.IsUp.Should().BeFalse();
        sky.Sun.Body.ArcProgress.Should().BeNull();
        sky.Sun.Body.Rise.Should().BeNull();
        sky.Sun.Body.Set.Should().BeNull();
    }

    [Test]
    public void Sunset_after_local_midnight_is_shown_as_next_day()
    {
        TimeZoneInfo oslo = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");
        var location = new GeoLocation(69.6492, 18.9553);
        DateTimeOffset start = new(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);

        IEnumerable<TimeEvent?> sets = Enumerable.Range(0, 25)
            .Select(day => SkyCalculator.Compute(start.AddDays(day), location, oslo).Sun.Body.Set);

        sets.Should().Contain(set => set.HasValue && set.Value.DayOffset == 1);
    }

    [TestCase(52.52, 13.405, "Europe/Berlin")]
    [TestCase(-33.8688, 151.2093, "Australia/Sydney")]
    [TestCase(-0.1807, -78.4678, "America/Guayaquil")]
    public void Moon_uses_the_current_pair_while_up_and_the_next_pair_while_down(double latitude, double longitude, string zoneId)
    {
        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        var location = new GeoLocation(latitude, longitude);
        DateTimeOffset start = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var seenStates = new HashSet<bool>();

        for (int hour = 0; hour < 24 * 30; hour++)
        {
            DateTimeOffset now = start.AddHours(hour);
            BodyInfo moon = SkyCalculator.Compute(now, location, zone).Moon.Body;

            DateTimeOffset rise = moon.Rise!.Value.Time;
            DateTimeOffset set = moon.Set!.Value.Time;
            seenStates.Add(moon.IsUp);

            if (moon.IsUp)
            {
                (rise <= now && now <= set).Should().BeTrue($"{now}: up, but window is {rise} to {set}");
                moon.ArcProgress!.Value.Should().BeInRange(0.0, 1.0);
            }
            else
            {
                (now < rise && rise < set).Should().BeTrue($"{now}: down, but window is {rise} to {set}");
                moon.ArcProgress.Should().BeNull();
            }

            DateTime today = TimeZoneInfo.ConvertTime(now, zone).Date;
            moon.Rise.Value.DayOffset.Should().Be((rise.Date - today).Days);
            moon.Set.Value.DayOffset.Should().Be((set.Date - today).Days);
        }

        seenStates.Count.Should().Be(2);
    }

    [Test]
    public void Moon_near_the_pole_never_breaks_the_state_rules()
    {
        var location = new GeoLocation(78.2232, 15.6267);
        DateTimeOffset start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        for (int step = 0; step < 4 * 60; step++)
        {
            BodyInfo moon = SkyCalculator.Compute(start.AddHours(6 * step), location, TimeZoneInfo.Utc).Moon.Body;

            (moon.ArcProgress is not null).Should().Be(moon.IsUp);
            if (moon.Rise is not null)
            {
                moon.Rise.Value.DayOffset.Should().BeInRange(-2, 2);
            }

            if (moon.Set is not null)
            {
                moon.Set.Value.DayOffset.Should().BeInRange(-2, 2);
            }
        }
    }

    [TestCase(-30.0, TwilightPhase.Night)]
    [TestCase(-15.0, TwilightPhase.AstronomicalTwilight)]
    [TestCase(-9.0, TwilightPhase.NauticalTwilight)]
    [TestCase(-3.0, TwilightPhase.CivilTwilight)]
    [TestCase(3.0, TwilightPhase.GoldenHour)]
    [TestCase(30.0, TwilightPhase.Day)]
    public void Twilight_follows_the_sun_altitude(double altitude, TwilightPhase expected)
    {
        SkyCalculator.ClassifyTwilight(altitude).Should().Be(expected);
    }

    [Test]
    public void Absurd_gps_elevation_is_tolerated()
    {
        SkySnapshot sky = SkyCalculator.Compute(DateTimeOffset.UnixEpoch.AddYears(56), new GeoLocation(52.52, 13.405, 1e7), TimeZoneInfo.Utc);

        sky.Should().NotBeNull();
    }

    private static DateTimeOffset? Today(TimeEvent? timeEvent) =>
        timeEvent is { DayOffset: 0 } value ? value.Time : null;

    private static void AssertEvent(UsnoFixtures.Day day, string? expected, DateTimeOffset? actual, TimeSpan tolerance)
    {
        if (expected is null)
        {
            actual.Should().BeNull($"{day.Place} {day.Date}: expected no event, got {actual}");
            return;
        }

        actual.Should().NotBeNull($"{day.Place} {day.Date}: expected {expected}, got none");

        DateTimeOffset reference = Midnight(day) + TimeSpan.ParseExact(expected, @"hh\:mm", CultureInfo.InvariantCulture);
        TimeSpan difference = (actual!.Value - reference).Duration();

        difference.Should().BeLessThanOrEqualTo(tolerance, $"{day.Place} {day.Date}: expected {reference:HH:mm}, got {actual:HH:mm:ss}");
    }

    private static void AssertClock(string expected, DateTimeOffset actual)
    {
        TimeSpan reference = TimeSpan.ParseExact(expected, @"hh\:mm", CultureInfo.InvariantCulture);
        (actual.TimeOfDay - reference).Duration().Should().BeLessThanOrEqualTo(SunTolerance, $"expected {expected}, got {actual:HH:mm:ss zzz}");
    }

    private static GeoLocation Location(UsnoFixtures.Day day) => new(day.Latitude, day.Longitude);

    private static DateTimeOffset Midnight(UsnoFixtures.Day day) =>
        DateTimeOffset.ParseExact(day.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

    private static DateTimeOffset Noon(UsnoFixtures.Day day) => Midnight(day).AddHours(12);
}
