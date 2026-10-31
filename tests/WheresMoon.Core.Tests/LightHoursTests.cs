using System.Globalization;
using CosineKitty;

namespace WheresMoon.Core.Tests;

public class LightHoursTests
{
    private static readonly GeoLocation Berlin = new(52.52, 13.405);
    private static readonly GeoLocation Tromso = new(69.6492, 18.9553);
    private static readonly GeoLocation Quito = new(-0.1807, -78.4678);
    private static readonly GeoLocation Longyearbyen = new(78.2232, 15.6267);

    private static readonly TimeSpan Second = TimeSpan.FromSeconds(1);

    [Fact]
    public void A_normal_day_runs_blue_golden_golden_blue()
    {
        LightHours hours = LightHoursCalculator.Compute(Noon("2026-10-15"), Berlin, TimeZoneInfo.Utc);

        Assert.Equal(SunRange.Mixed, hours.Range);
        Assert.Equal(
            [
                (LightKind.Blue, DayPart.Morning),
                (LightKind.Golden, DayPart.Morning),
                (LightKind.Golden, DayPart.Evening),
                (LightKind.Blue, DayPart.Evening),
            ],
            hours.Windows.Select(w => (w.Kind, w.Part)));

        Assert.Equal(hours.Windows[0].End, hours.Windows[1].Start);
        Assert.Equal(hours.Windows[2].End, hours.Windows[3].Start);
        Assert.Equal(6, hours.Boundaries.Count);
    }

    [Theory]
    [InlineData("2026-10-15")]
    [InlineData("2026-06-21")]
    [InlineData("2026-12-21")]
    public void Window_ends_lie_on_the_specified_sun_altitudes(string date)
    {
        var observer = new Observer(Berlin.Latitude, Berlin.Longitude, 0);
        LightHours hours = LightHoursCalculator.Compute(Noon(date), Berlin, TimeZoneInfo.Utc);

        foreach (LightWindow window in hours.Windows)
        {
            double[] expected = window.Kind == LightKind.Golden ? [-4.0, 6.0] : [-6.0, -4.0];

            Assert.Contains(expected, altitude => Math.Abs(altitude - Altitude(observer, window.Start!.Value)) < 0.02);
            Assert.Contains(expected, altitude => Math.Abs(altitude - Altitude(observer, window.End!.Value)) < 0.02);
        }
    }

    [Fact]
    public void Blue_hour_takes_minutes_and_golden_hour_longer()
    {
        LightHours hours = LightHoursCalculator.Compute(Noon("2026-10-15"), Berlin, TimeZoneInfo.Utc);
        LightWindow blue = hours.Windows[0];
        LightWindow golden = hours.Windows[1];

        Assert.InRange(blue.End!.Value - blue.Start!.Value, TimeSpan.FromMinutes(8), TimeSpan.FromMinutes(20));
        Assert.InRange(golden.End!.Value - golden.Start!.Value, TimeSpan.FromMinutes(50), TimeSpan.FromMinutes(110));
    }

    [Fact]
    public void Near_the_equator_the_sun_passes_quickly()
    {
        LightHours hours = LightHoursCalculator.Compute(Noon("2026-03-20"), Quito, TimeZoneInfo.Utc);

        Assert.All(hours.Windows, w => Assert.True(w.End!.Value - w.Start!.Value < TimeSpan.FromMinutes(45)));
    }

    [Fact]
    public void The_midnight_sun_leaves_no_window_when_it_stays_high()
    {
        LightHours hours = LightHoursCalculator.Compute(Noon("2026-06-21"), Longyearbyen, TimeZoneInfo.Utc);

        Assert.Empty(hours.Windows);
        Assert.Equal(SunRange.AlwaysHigh, hours.Range);
    }

    [Fact]
    public void The_polar_night_leaves_no_window_when_the_sun_stays_low()
    {
        LightHours hours = LightHoursCalculator.Compute(Noon("2026-12-21"), Longyearbyen, TimeZoneInfo.Utc);

        Assert.Empty(hours.Windows);
        Assert.Equal(SunRange.AlwaysLow, hours.Range);
    }

    [Fact]
    public void Light_nights_have_no_blue_hour_and_a_golden_hour_across_midnight()
    {
        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");

        // In mid May the sun no longer sinks four degrees below Tromsø's horizon, but it still sets.
        LightHours hours = LightHoursCalculator.Compute(Noon("2026-05-12"), Tromso, zone);

        Assert.DoesNotContain(hours.Windows, w => w.Kind == LightKind.Blue);
        Assert.Contains(hours.Windows, w => w.Part == DayPart.Morning && w.Start is null && w.End is not null);
        Assert.Contains(hours.Windows, w => w.Part == DayPart.Evening && w.Start is not null && w.End is null);
    }

    [Fact]
    public void In_the_polar_twilight_one_golden_hour_spans_noon()
    {
        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");

        // Around the winter solstice the sun peaks about three degrees below Tromsø's horizon.
        LightHours hours = LightHoursCalculator.Compute(Noon("2026-12-21"), Tromso, zone);
        LightWindow midday = hours.Windows.Single(w => w.Part == DayPart.Midday);

        Assert.Equal(LightKind.Golden, midday.Kind);

        // Tromsø lies east of the zone's central meridian, so its solar noon comes before 12:00.
        DateTimeOffset middle = midday.Start!.Value + (midday.End!.Value - midday.Start.Value) / 2;
        Assert.InRange(middle.TimeOfDay, TimeSpan.FromHours(11.25), TimeSpan.FromHours(12.25));
        Assert.Contains(hours.Windows, w => w.Kind == LightKind.Blue && w.Part == DayPart.Morning);
        Assert.Contains(hours.Windows, w => w.Kind == LightKind.Blue && w.Part == DayPart.Evening);
    }

    [Fact]
    public void Times_are_in_the_requested_zone()
    {
        TimeZoneInfo berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

        LightHours hours = LightHoursCalculator.Compute(Noon("2026-10-15"), Berlin, berlin);

        Assert.All(hours.Boundaries, time => Assert.Equal(TimeSpan.FromHours(2), time.Offset));
        Assert.All(hours.Boundaries, time => Assert.Equal(15, time.Day));
    }

    [Fact]
    public void Boundaries_are_in_order()
    {
        LightHours hours = LightHoursCalculator.Compute(Noon("2026-10-15"), Berlin, TimeZoneInfo.Utc);

        Assert.Equal(hours.Boundaries.OrderBy(time => time), hours.Boundaries);
        Assert.Equal(hours.Boundaries.Distinct(), hours.Boundaries);
    }

    private static double Altitude(Observer observer, DateTimeOffset time) =>
        SkyCalculator.GeometricSunAltitude(observer, RiseSetResolver.ToAstroTime(time + Second));

    private static DateTimeOffset Noon(string date) =>
        DateTimeOffset.Parse($"{date}T12:00:00Z", CultureInfo.InvariantCulture);
}
