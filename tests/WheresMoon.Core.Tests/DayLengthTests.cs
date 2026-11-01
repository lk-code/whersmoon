using System.Globalization;

namespace WheresMoon.Core.Tests;

public class DayLengthTests
{
    private static readonly GeoLocation Berlin = new(52.52, 13.405);
    private static readonly GeoLocation Tromso = new(69.6492, 18.9553);

    private static readonly TimeSpan Tolerance = TimeSpan.FromMinutes(2);

    // New York's June sunset and Sydney's whole day cross midnight UT, so their UT days have no matching pair.
    public static IEnumerable<UsnoFixtures.Day> NormalDays() =>
        UsnoFixtures.Days.Where(d => d.Place is "Berlin" or "Quito" or "Longyearbyen" && d.SunRise is not null && d.SunSet is not null);

    [TestCaseSource(nameof(NormalDays))]
    public void Length_matches_usno(UsnoFixtures.Day day)
    {
        TimeSpan expected = TimeSpan.Parse(day.SunSet!, CultureInfo.InvariantCulture) - TimeSpan.Parse(day.SunRise!, CultureInfo.InvariantCulture);

        DayLengthInfo info = DayLengthCalculator.Compute(Noon(day.Date), new GeoLocation(day.Latitude, day.Longitude), TimeZoneInfo.Utc);

        info.Kind.Should().Be(DaylightKind.Normal);
        info.Length.Should().BeCloseTo(expected, Tolerance);
    }

    [Test]
    public void Days_get_shorter_in_autumn_and_longer_in_spring()
    {
        DayLengthInfo autumn = DayLengthCalculator.Compute(Noon("2026-10-15"), Berlin, TimeZoneInfo.Utc);
        DayLengthInfo spring = DayLengthCalculator.Compute(Noon("2026-03-15"), Berlin, TimeZoneInfo.Utc);

        autumn.ChangeFromYesterday!.Value.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMinutes(-5)).And.BeLessThanOrEqualTo(TimeSpan.FromMinutes(-2));
        spring.ChangeFromYesterday!.Value.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMinutes(2)).And.BeLessThanOrEqualTo(TimeSpan.FromMinutes(5));
    }

    [Test]
    public void Summer_solstice_barely_changes()
    {
        DayLengthInfo info = DayLengthCalculator.Compute(Noon("2026-06-21"), Berlin, TimeZoneInfo.Utc);

        info.ChangeFromYesterday!.Value.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMinutes(-1)).And.BeLessThanOrEqualTo(TimeSpan.FromMinutes(1));
    }

    [Test]
    public void Midnight_sun_lasts_all_day()
    {
        DayLengthInfo info = DayLengthCalculator.Compute(Noon("2026-06-21"), Tromso, TimeZoneInfo.Utc);

        info.Kind.Should().Be(DaylightKind.MidnightSun);
        info.Length.Should().Be(TimeSpan.FromDays(1));
        info.ChangeFromYesterday.Should().BeNull();
    }

    [Test]
    public void Polar_night_has_no_daylight()
    {
        DayLengthInfo info = DayLengthCalculator.Compute(Noon("2026-12-21"), Tromso, TimeZoneInfo.Utc);

        info.Kind.Should().Be(DaylightKind.PolarNight);
        info.Length.Should().Be(TimeSpan.Zero);
        info.ChangeFromYesterday.Should().BeNull();
    }

    [Test]
    public void First_sunrise_after_the_polar_night_has_nothing_to_compare_with()
    {
        // Tromsø's polar night ends in the middle of January.
        DateTimeOffset time = Noon("2026-01-01");
        DayLengthInfo? firstDay = null;

        for (int i = 0; i < 40 && firstDay is null; i++)
        {
            DayLengthInfo info = DayLengthCalculator.Compute(time.AddDays(i), Tromso, TimeZoneInfo.Utc);
            if (info.Kind == DaylightKind.Normal)
            {
                firstDay = info;
            }
        }

        firstDay.Should().NotBeNull();
        firstDay.ChangeFromYesterday.Should().BeNull();
        firstDay.Length.Should().BeLessThan(TimeSpan.FromHours(2));
    }

    [Test]
    public void The_local_calendar_day_decides_which_day_is_measured()
    {
        TimeZoneInfo auckland = TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland");
        var city = new GeoLocation(-36.85, 174.76);
        DateTimeOffset time = DateTimeOffset.Parse("2026-06-21T22:00:00Z", CultureInfo.InvariantCulture);

        DayLengthInfo local = DayLengthCalculator.Compute(time, city, auckland);
        DayLengthInfo nextUtcDay = DayLengthCalculator.Compute(time.AddDays(1), city, auckland);

        nextUtcDay.Length.Should().NotBe(local.Length);
    }

    private static DateTimeOffset Noon(string date) =>
        DateTimeOffset.Parse($"{date}T12:00:00Z", CultureInfo.InvariantCulture);
}
