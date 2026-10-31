using System.Globalization;

namespace WheresMoon.Core.Tests;

public class DayLengthTests
{
    private static readonly GeoLocation Berlin = new(52.52, 13.405);
    private static readonly GeoLocation Tromso = new(69.6492, 18.9553);

    private static readonly TimeSpan Tolerance = TimeSpan.FromMinutes(2);

    public static TheoryData<UsnoFixtures.Day> NormalDays()
    {
        var data = new TheoryData<UsnoFixtures.Day>();

        // New York's June sunset and Sydney's whole day cross midnight UT, so their UT days have no matching pair.
        foreach (UsnoFixtures.Day day in UsnoFixtures.Days.Where(d => d.Place is "Berlin" or "Quito" or "Longyearbyen"))
        {
            if (day.SunRise is not null && day.SunSet is not null)
            {
                data.Add(day);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(NormalDays))]
    public void Length_matches_usno(UsnoFixtures.Day day)
    {
        TimeSpan expected = TimeSpan.Parse(day.SunSet!, CultureInfo.InvariantCulture) - TimeSpan.Parse(day.SunRise!, CultureInfo.InvariantCulture);

        DayLengthInfo info = DayLengthCalculator.Compute(Noon(day.Date), new GeoLocation(day.Latitude, day.Longitude), TimeZoneInfo.Utc);

        Assert.Equal(DaylightKind.Normal, info.Kind);
        Assert.InRange(info.Length, expected - Tolerance, expected + Tolerance);
    }

    [Fact]
    public void Days_get_shorter_in_autumn_and_longer_in_spring()
    {
        DayLengthInfo autumn = DayLengthCalculator.Compute(Noon("2026-10-15"), Berlin, TimeZoneInfo.Utc);
        DayLengthInfo spring = DayLengthCalculator.Compute(Noon("2026-03-15"), Berlin, TimeZoneInfo.Utc);

        Assert.InRange(autumn.ChangeFromYesterday!.Value, TimeSpan.FromMinutes(-5), TimeSpan.FromMinutes(-2));
        Assert.InRange(spring.ChangeFromYesterday!.Value, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void Summer_solstice_barely_changes()
    {
        DayLengthInfo info = DayLengthCalculator.Compute(Noon("2026-06-21"), Berlin, TimeZoneInfo.Utc);

        Assert.InRange(info.ChangeFromYesterday!.Value, TimeSpan.FromMinutes(-1), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void Midnight_sun_lasts_all_day()
    {
        DayLengthInfo info = DayLengthCalculator.Compute(Noon("2026-06-21"), Tromso, TimeZoneInfo.Utc);

        Assert.Equal(DaylightKind.MidnightSun, info.Kind);
        Assert.Equal(TimeSpan.FromDays(1), info.Length);
        Assert.Null(info.ChangeFromYesterday);
    }

    [Fact]
    public void Polar_night_has_no_daylight()
    {
        DayLengthInfo info = DayLengthCalculator.Compute(Noon("2026-12-21"), Tromso, TimeZoneInfo.Utc);

        Assert.Equal(DaylightKind.PolarNight, info.Kind);
        Assert.Equal(TimeSpan.Zero, info.Length);
        Assert.Null(info.ChangeFromYesterday);
    }

    [Fact]
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

        Assert.NotNull(firstDay);
        Assert.Null(firstDay.ChangeFromYesterday);
        Assert.True(firstDay.Length < TimeSpan.FromHours(2));
    }

    [Fact]
    public void The_local_calendar_day_decides_which_day_is_measured()
    {
        TimeZoneInfo auckland = TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland");
        var city = new GeoLocation(-36.85, 174.76);
        DateTimeOffset time = DateTimeOffset.Parse("2026-06-21T22:00:00Z", CultureInfo.InvariantCulture);

        DayLengthInfo local = DayLengthCalculator.Compute(time, city, auckland);
        DayLengthInfo nextUtcDay = DayLengthCalculator.Compute(time.AddDays(1), city, auckland);

        Assert.NotEqual(local.Length, nextUtcDay.Length);
    }

    private static DateTimeOffset Noon(string date) =>
        DateTimeOffset.Parse($"{date}T12:00:00Z", CultureInfo.InvariantCulture);
}
