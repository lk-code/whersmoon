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

    [TestCase(0.0)]
    [TestCase(40.0)]
    [TestCase(-40.0)]
    [TestCase(TimeScrubber.SlowSpeed * Width)]
    public void A_slow_swipe_stops_with_the_finger(double speed)
    {
        var glide = new TimeGlide();

        glide.Push(speed, Width);

        glide.IsRunning.Should().BeFalse();
    }

    [TestCase(800.0)]
    [TestCase(-800.0)]
    public void A_fast_swipe_carries_on_in_its_own_direction(double speed)
    {
        Math.Sign(Flight(speed)).Should().Be(Math.Sign(speed));
    }

    [Test]
    public void The_faster_the_push_the_further_it_flies()
    {
        double gentle = Flight(300.0);
        double firm = Flight(1000.0);
        double hard = Flight(3000.0);

        (gentle < firm && firm < hard).Should().BeTrue();
        (hard / gentle).Should().BeGreaterThan(3000.0 / 300.0, "a hard push should fly more than its speed alone says");
    }

    [Test]
    public void Flying_does_not_start_with_a_jump_just_above_a_slow_swipe()
    {
        Flight(TimeScrubber.SlowSpeed * Width + 20.0).Should().BeInRange(0.0, 0.05 * Width);
    }

    [Test]
    public void A_flight_is_the_same_at_any_frame_rate()
    {
        Math.Abs(Flight(1500.0, 1.0 / 120.0) - Flight(1500.0, 1.0 / 30.0)).Should().BeInRange(0.0, 3.0);
    }

    [Test]
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

        glide.IsRunning.Should().BeFalse();
        seconds.Should().BeInRange(1.0, 6.0);
    }

    [Test]
    public void The_speed_only_ever_falls()
    {
        var glide = new TimeGlide();
        glide.Push(1500.0, Width);
        double previous = glide.Speed;

        while (glide.IsRunning)
        {
            glide.Advance(Frame);
            glide.Speed.Should().BeLessThan(previous);
            previous = glide.Speed;
        }
    }

    [Test]
    public void Grabbing_stops_a_flight_at_once()
    {
        var glide = new TimeGlide();
        glide.Push(1500.0, Width);

        glide.Stop();

        glide.IsRunning.Should().BeFalse();
        glide.Advance(Frame).Should().Be(0.0);
    }

    [Test]
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

        scrubber.Caught.Should().Be(landing);
        scrubber.Time.Should().Be(landing);
    }

    [Test]
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

        scrubber.Time.Should().BeBefore(Start);
        (scrubber.Time - (Start + Day * (flown / Width))).Duration().Should().BeGreaterThanOrEqualTo(TimeSpan.Zero).And.BeLessThanOrEqualTo(TimeSpan.FromSeconds(1));
    }
}
