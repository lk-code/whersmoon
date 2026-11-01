using System.Globalization;
using WheresMoon.Core.Interaction;

namespace WheresMoon.Core.Tests;

public class TimeGlideTests
{
    private const double Width = 400.0;
    private const double Frame = 1.0 / 60.0;

    private static readonly TimeSpan Day = TimeSpan.FromHours(24);
    private static readonly DateTimeOffset Start = DateTimeOffset.Parse("2026-11-01T12:00:00Z", CultureInfo.InvariantCulture);

    private static double Flight(double speed, double frame = Frame)
    {
        var glide = new TimeGlide();
        glide.Push(speed, Width);

        double distance = 0.0;
        while (glide.IsRunning)
        {
            distance += glide.Advance(frame);
        }

        return distance;
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(40.0)]
    [InlineData(-40.0)]
    [InlineData(TimeScrubber.SlowSpeed * Width)]
    public void A_slow_swipe_stops_with_the_finger(double speed)
    {
        var glide = new TimeGlide();

        glide.Push(speed, Width);

        Assert.False(glide.IsRunning);
    }

    [Theory]
    [InlineData(800.0)]
    [InlineData(-800.0)]
    public void A_fast_swipe_carries_on_in_its_own_direction(double speed)
    {
        Assert.Equal(Math.Sign(speed), Math.Sign(Flight(speed)));
    }

    [Fact]
    public void The_faster_the_push_the_further_it_flies()
    {
        double gentle = Flight(300.0);
        double firm = Flight(1000.0);
        double hard = Flight(3000.0);

        Assert.True(gentle < firm && firm < hard);
        Assert.True(hard / gentle > 3000.0 / 300.0, "a hard push should fly more than its speed alone says");
    }

    [Fact]
    public void Flying_does_not_start_with_a_jump_just_above_a_slow_swipe()
    {
        Assert.InRange(Flight(TimeScrubber.SlowSpeed * Width + 20.0), 0.0, 0.05 * Width);
    }

    [Fact]
    public void A_flight_is_the_same_at_any_frame_rate()
    {
        Assert.InRange(Math.Abs(Flight(1500.0, 1.0 / 120.0) - Flight(1500.0, 1.0 / 30.0)), 0.0, 3.0);
    }

    [Fact]
    public void A_flight_ends_by_itself_within_seconds()
    {
        var glide = new TimeGlide();
        glide.Push(8000.0, Width);

        double seconds = 0.0;
        while (glide.IsRunning && seconds < 60.0)
        {
            glide.Advance(Frame);
            seconds += Frame;
        }

        Assert.False(glide.IsRunning);
        Assert.InRange(seconds, 1.0, 6.0);
    }

    [Fact]
    public void The_speed_only_ever_falls()
    {
        var glide = new TimeGlide();
        glide.Push(1500.0, Width);
        double previous = glide.Speed;

        while (glide.IsRunning)
        {
            glide.Advance(Frame);
            Assert.True(glide.Speed < previous);
            previous = glide.Speed;
        }
    }

    [Fact]
    public void Grabbing_stops_a_flight_at_once()
    {
        var glide = new TimeGlide();
        glide.Push(1500.0, Width);

        glide.Stop();

        Assert.False(glide.IsRunning);
        Assert.Equal(0.0, glide.Advance(Frame));
    }

    [Fact]
    public void A_glide_flies_past_events_and_lands_on_one_as_it_slows_down()
    {
        const double Push = 2000.0;

        double flight = Flight(Push) / Width;
        DateTimeOffset passed = Start.AddHours(6);
        DateTimeOffset landing = Start + Day * (flight - 0.01);
        DateTimeOffset[] events = [passed, landing];

        var glide = new TimeGlide();
        var scrubber = new TimeScrubber();
        scrubber.Begin(Start);
        glide.Push(Push, Width);

        while (glide.IsRunning && scrubber.Caught is null)
        {
            double distance = glide.Advance(Frame);
            scrubber.Move(distance, glide.Speed, Width, Day, events);
        }

        Assert.Equal(landing, scrubber.Caught);
        Assert.Equal(landing, scrubber.Time);
    }

    [Fact]
    public void A_glide_covers_as_much_time_as_its_flight_says()
    {
        var glide = new TimeGlide();
        var scrubber = new TimeScrubber();
        scrubber.Begin(Start);
        glide.Push(-1000.0, Width);

        double flown = 0.0;
        while (glide.IsRunning)
        {
            double distance = glide.Advance(Frame);
            flown += distance;
            scrubber.Move(distance, glide.Speed, Width, Day, []);
        }

        Assert.True(scrubber.Time < Start);
        Assert.InRange((scrubber.Time - (Start + Day * (flown / Width))).Duration(), TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }
}
