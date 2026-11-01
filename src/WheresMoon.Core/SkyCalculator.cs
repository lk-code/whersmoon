using CosineKitty;

namespace WheresMoon.Core;

/// <summary>Entry point of the library: computes the sky for one moment and place.</summary>
public static class SkyCalculator
{
    private const double SunriseAltitude = -0.833;
    private const double GoldenHourTop = 6.0;

    // Astronomy Engine's atmosphere model throws outside -500..100000 m. Raw GPS values can be wild.
    private const double LowestElevation = -500.0;
    private const double HighestElevation = 10000.0;

    /// <param name="time">The moment to compute. Any offset is accepted.</param>
    /// <param name="location">Observer position.</param>
    /// <param name="zone">Time zone for displayed times and for what counts as "today".</param>
    public static SkySnapshot Compute(DateTimeOffset time, GeoLocation location, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(zone);

        Observer observer = ToObserver(location);
        AstroTime now = RiseSetResolver.ToAstroTime(time);

        Equatorial sunEquator = Astronomy.Equator(Body.Sun, now, observer, EquatorEpoch.OfDate, Aberration.Corrected);
        Equatorial moonEquator = Astronomy.Equator(Body.Moon, now, observer, EquatorEpoch.OfDate, Aberration.Corrected);

        SunInfo sun = ComputeSun(time, location, zone, observer, now, sunEquator);
        MoonInfo moon = ComputeMoon(time, location, zone, observer, now, sunEquator, moonEquator);

        return new SkySnapshot
        {
            Time = TimeZoneInfo.ConvertTime(time, zone),
            Location = location,
            Sun = sun,
            Moon = moon,
            Twilight = ClassifyTwilight(sun.GeometricAltitude),
        };
    }

    internal static Observer ToObserver(GeoLocation location) => new(
        location.Latitude,
        location.Longitude,
        Math.Clamp(location.Elevation, LowestElevation, HighestElevation));

    /// <summary>Sun altitude without refraction, which is what twilight limits are defined on.</summary>
    internal static double GeometricSunAltitude(Observer observer, AstroTime time)
    {
        Equatorial equator = Astronomy.Equator(Body.Sun, time, observer, EquatorEpoch.OfDate, Aberration.Corrected);
        return Astronomy.Horizon(time, observer, equator.ra, equator.dec, Refraction.None).altitude;
    }

    internal static TwilightPhase ClassifyTwilight(double sunAltitude) => sunAltitude switch
    {
        < -18.0 => TwilightPhase.Night,
        < -12.0 => TwilightPhase.AstronomicalTwilight,
        < -6.0 => TwilightPhase.NauticalTwilight,
        < SunriseAltitude => TwilightPhase.CivilTwilight,
        < GoldenHourTop => TwilightPhase.GoldenHour,
        _ => TwilightPhase.Day,
    };

    private static SunInfo ComputeSun(
        DateTimeOffset time, GeoLocation location, TimeZoneInfo zone, Observer observer, AstroTime now, Equatorial equator)
    {
        Topocentric apparent = Astronomy.Horizon(now, observer, equator.ra, equator.dec, Refraction.Normal);
        Topocentric geometric = Astronomy.Horizon(now, observer, equator.ra, equator.dec, Refraction.None);

        RiseSetResolver.ArcState arc = RiseSetResolver.ResolveArc(Body.Sun, observer, now, geometric.altitude);
        (TimeEvent? rise, TimeEvent? set) = RiseSetResolver.ResolveCalendarDay(Body.Sun, observer, time, zone);

        return new SunInfo
        {
            Body = new BodyInfo
            {
                Altitude = apparent.altitude,
                Azimuth = apparent.azimuth,
                IsUp = arc.IsUp,
                Rise = rise,
                Set = set,
                ArcProgress = arc.Progress,
                BelowProgress = arc.BelowProgress,
            },
            GeometricAltitude = geometric.altitude,
            IsRising = MoonOrientation.HourAngle(now, location.Longitude, equator.ra * 15.0) < 0.0,
        };
    }

    private static MoonInfo ComputeMoon(
        DateTimeOffset time,
        GeoLocation location,
        TimeZoneInfo zone,
        Observer observer,
        AstroTime now,
        Equatorial sunEquator,
        Equatorial moonEquator)
    {
        Topocentric apparent = Astronomy.Horizon(now, observer, moonEquator.ra, moonEquator.dec, Refraction.Normal);
        Topocentric geometric = Astronomy.Horizon(now, observer, moonEquator.ra, moonEquator.dec, Refraction.None);

        RiseSetResolver.ArcState arc = RiseSetResolver.ResolveArc(Body.Moon, observer, now, geometric.altitude);

        double moonRaDegrees = moonEquator.ra * 15.0;
        double positionAngle = MoonOrientation.BrightLimbPositionAngle(
            sunEquator.ra * 15.0, sunEquator.dec, moonRaDegrees, moonEquator.dec);
        double hourAngle = MoonOrientation.HourAngle(now, location.Longitude, moonRaDegrees);
        double parallacticAngle = MoonOrientation.ParallacticAngle(hourAngle, moonEquator.dec, location.Latitude);

        double phaseAngle = MoonOrientation.PhaseAngle(moonEquator.vec, sunEquator.vec);
        double elongation = Astronomy.MoonPhase(now);

        return new MoonInfo
        {
            Body = new BodyInfo
            {
                Altitude = apparent.altitude,
                Azimuth = apparent.azimuth,
                IsUp = arc.IsUp,
                Rise = RiseSetResolver.ToMoonEvent(arc.Rise, time, zone),
                Set = RiseSetResolver.ToMoonEvent(arc.Set, time, zone),
                ArcProgress = arc.Progress,
                BelowProgress = arc.BelowProgress,
            },
            IlluminatedFraction = MoonOrientation.IlluminatedFraction(phaseAngle),
            PhaseAngle = phaseAngle,
            Elongation = elongation,
            PhaseName = MoonOrientation.PhaseName(elongation),
            BrightLimbZenithAngle = MoonOrientation.ZenithAngle(positionAngle, parallacticAngle),
        };
    }
}
