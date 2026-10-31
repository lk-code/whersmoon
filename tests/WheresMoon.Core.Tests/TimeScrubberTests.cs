using System.Globalization;
using WheresMoon.Core.Interaction;

namespace WheresMoon.Core.Tests;

public class TimeScrubberTests
{
    private const double Width = 400.0;
    private const double SlowSpeed = 40.0;
    private const double FastSpeed = 800.0;

    private static readonly TimeSpan Day = TimeSpan.FromHours(24);
    private static readonly DateTimeOffset Start = DateTimeOffset.Parse("2026-11-01T12:00:00Z", CultureInfo.InvariantCulture);

    [Theory]
    [InlineData(InfoPage.SunAndMoon, 24.0)]
    [InlineData(InfoPage.LightHours, 24.0)]
    [InlineData(InfoPage.Events, 720.0)]
    public void A_screen_width_stands_for_a_day_or_a_month(InfoPage page, double hours)
    {
        var scrubber = new TimeScrubber();
        scrubber.Begin(Start);

        scrubber.Move(Width, FastSpeed, Width, TimeScrubber.ScaleOf(page), []);

        Assert.Equal(Start.AddHours(hours), scrubber.Time);
    }

    [Fact]
    public void Moving_left_goes_back_in_time()
    {
        var scrubber = new TimeScrubber();
        scrubber.Begin(Start);

        scrubber.Move(-Width / 4.0, SlowSpeed, Width, Day, []);

        Assert.Equal(Start.AddHours(-6), scrubber.Time);
    }

    [Fact]
    public void Slow_movement_catches_on_a_nearby_event()
    {
        var scrubber = new TimeScrubber();
        DateTimeOffset target = Start.AddHours(2);
        scrubber.Begin(Start);

        bool caught = scrubber.Move(Width * 2.0 / 24.0 - 2.0, SlowSpeed, Width, Day, [target]);

        Assert.True(caught);
        Assert.Equal(target, scrubber.Time);
        Assert.Equal(target, scrubber.Caught);
    }

    [Fact]
    public void Fast_movement_passes_events_by()
    {
        var scrubber = new TimeScrubber();
        DateTimeOffset target = Start.AddHours(2);
        scrubber.Begin(Start);

        bool caught = scrubber.Move(Width * 2.0 / 24.0, FastSpeed, Width, Day, [target]);

        Assert.False(caught);
        Assert.Null(scrubber.Caught);
        Assert.Equal(target, scrubber.Time);
    }

    [Fact]
    public void A_caught_time_holds_until_the_pointer_pulls_harder()
    {
        var scrubber = new TimeScrubber();
        DateTimeOffset target = Start.AddHours(1);
        scrubber.Begin(Start);
        scrubber.Move(Width / 24.0, SlowSpeed, Width, Day, [target]);

        double release = TimeScrubber.ReleaseShare * Width;

        Assert.False(scrubber.Move(release * 0.5, SlowSpeed, Width, Day, [target]));
        Assert.Equal(target, scrubber.Time);

        scrubber.Move(release * 0.6, SlowSpeed, Width, Day, [target]);
        Assert.Null(scrubber.Caught);
    }

    [Fact]
    public void Getting_loose_does_not_jump()
    {
        var scrubber = new TimeScrubber();
        DateTimeOffset target = Start.AddHours(1);
        scrubber.Begin(Start);
        scrubber.Move(Width / 24.0, SlowSpeed, Width, Day, [target]);

        const double Step = 3.0;
        for (int i = 0; i < 20 && scrubber.Caught is not null; i++)
        {
            scrubber.Move(Step, SlowSpeed, Width, Day, [target]);
        }

        Assert.Equal(target + Day * (Step / Width), scrubber.Time);
    }

    [Fact]
    public void The_event_just_left_does_not_catch_again_at_once()
    {
        var scrubber = new TimeScrubber();
        DateTimeOffset target = Start.AddHours(1);
        scrubber.Begin(Start);
        scrubber.Move(Width / 24.0, SlowSpeed, Width, Day, [target]);

        scrubber.Move(TimeScrubber.ReleaseShare * Width + 1.0, SlowSpeed, Width, Day, [target]);

        Assert.False(scrubber.Move(1.0, SlowSpeed, Width, Day, [target]));
        Assert.Null(scrubber.Caught);
    }

    [Fact]
    public void An_event_catches_again_after_moving_away_and_coming_back()
    {
        var scrubber = new TimeScrubber();
        DateTimeOffset target = Start.AddHours(1);
        scrubber.Begin(Start);
        scrubber.Move(Width / 24.0, SlowSpeed, Width, Day, [target]);
        double loose = TimeScrubber.ReleaseShare * Width + 1.0;
        scrubber.Move(loose, SlowSpeed, Width, Day, [target]);

        scrubber.Move(Width / 8.0, SlowSpeed, Width, Day, [target]);
        Assert.Null(scrubber.Caught);

        Assert.True(scrubber.Move(-Width / 8.0 - loose - 1.0, SlowSpeed, Width, Day, [target]));
        Assert.Equal(target, scrubber.Caught);
    }

    [Fact]
    public void A_fast_pull_gets_loose_at_once()
    {
        var scrubber = new TimeScrubber();
        DateTimeOffset target = Start.AddHours(1);
        scrubber.Begin(Start);
        scrubber.Move(Width / 24.0, SlowSpeed, Width, Day, [target]);

        scrubber.Move(1.0, FastSpeed, Width, Day, [target]);

        Assert.Null(scrubber.Caught);
    }

    [Fact]
    public void The_nearest_of_several_events_wins()
    {
        var scrubber = new TimeScrubber();
        scrubber.Begin(Start);

        scrubber.Move(1.0, SlowSpeed, Width, Day, [Start.AddMinutes(25), Start.AddMinutes(10), Start.AddMinutes(-20)]);

        Assert.Equal(Start.AddMinutes(10), scrubber.Caught);
    }

    [Fact]
    public void Key_steps_ignore_events()
    {
        var scrubber = new TimeScrubber();
        scrubber.Begin(Start);

        scrubber.Step(TimeSpan.FromMinutes(15));
        scrubber.Step(TimeSpan.FromMinutes(15));

        Assert.Equal(Start.AddMinutes(30), scrubber.Time);
        Assert.Null(scrubber.Caught);
    }

    [Theory]
    [InlineData(InfoPage.SunAndMoon)]
    [InlineData(InfoPage.LightHours)]
    public void Day_pages_catch_on_their_own_events(InfoPage page)
    {
        var berlin = new GeoLocation(52.52, 13.405);
        var points = new SnapPoints(berlin, TimeZoneInfo.Utc, new SkyEventCalendar(berlin));

        IReadOnlyList<DateTimeOffset> around = points.Around(page, Start, TimeSpan.FromHours(14));

        Assert.NotEmpty(around);
        Assert.All(around, time => Assert.InRange(time, Start.AddHours(-14), Start.AddHours(14)));
    }

    [Fact]
    public void Sun_and_moon_page_catches_on_sunrise()
    {
        var berlin = new GeoLocation(52.52, 13.405);
        var points = new SnapPoints(berlin, TimeZoneInfo.Utc, new SkyEventCalendar(berlin));
        SkySnapshot sky = SkyCalculator.Compute(Start, berlin, TimeZoneInfo.Utc);
        DateTimeOffset sunrise = sky.Sun.Body.Rise!.Value.Time;

        IReadOnlyList<DateTimeOffset> around = points.Around(InfoPage.SunAndMoon, sunrise, TimeSpan.FromMinutes(10));

        Assert.Contains(around, time => (time - sunrise).Duration() < TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Light_page_catches_on_the_edges_of_the_golden_hour()
    {
        var berlin = new GeoLocation(52.52, 13.405);
        var points = new SnapPoints(berlin, TimeZoneInfo.Utc, new SkyEventCalendar(berlin));
        DateTimeOffset edge = LightHoursCalculator.Compute(Start, berlin, TimeZoneInfo.Utc).Boundaries[1];

        IReadOnlyList<DateTimeOffset> around = points.Around(InfoPage.LightHours, edge, TimeSpan.FromMinutes(5));

        Assert.Contains(edge, around);
    }

    [Fact]
    public void Events_page_catches_on_a_supermoon_once()
    {
        var berlin = new GeoLocation(52.52, 13.405);
        var calendar = new SkyEventCalendar(berlin);
        var points = new SnapPoints(berlin, TimeZoneInfo.Utc, calendar);
        DateTimeOffset moon = DateTimeOffset.Parse("2026-11-24T14:54:00Z", CultureInfo.InvariantCulture);

        IReadOnlyList<DateTimeOffset> around = points.Around(InfoPage.Events, moon, TimeSpan.FromDays(1));

        Assert.Single(around);
    }
}
