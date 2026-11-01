namespace WheresMoon.Core.Scene;

/// <summary>Colours of sky and ground for a sun altitude.</summary>
public sealed record SkyColors(IReadOnlyList<SkyStop> Stops, HillColors BackHill, HillColors FrontHill)
{
    /// <summary>The colour right at the horizon.</summary>
    public Rgba Horizon => Stops[^1].Color;
}

/// <summary>Interpolates the design tokens continuously over the sun altitude.</summary>
public static class SkyPalette
{
    public static SkyColors Compute(double sunAltitude, bool isRising)
    {
        IReadOnlyList<SkyStop> stops = InterpolateStops(sunAltitude);

        if (!isRising)
        {
            stops = ApplyEveningTint(stops, sunAltitude);
        }

        return ComputeGround(stops, sunAltitude);
    }

    /// <summary>Sun altitude to colour the sky with next, which may only follow the real one so fast.</summary>
    public static double LimitChange(double shown, double target, double seconds)
    {
        double step = DesignTokens.Scrub.FastestSkyChange * seconds;
        return Math.Clamp(target, shown - step, shown + step);
    }

    /// <summary>0 with the sun at <paramref name="zero"/>, 1 at <paramref name="one"/>, smooth in between.</summary>
    public static double Ramp(double value, double zero, double one)
    {
        double t = Math.Clamp((value - zero) / (one - zero), 0.0, 1.0);
        return t * t * (3.0 - 2.0 * t);
    }

    private static List<SkyStop> InterpolateStops(double altitude)
    {
        IReadOnlyList<SkyAnchor> anchors = DesignTokens.Sky.Anchors;

        if (altitude <= anchors[0].SunAltitude)
        {
            return [.. anchors[0].Stops];
        }

        for (int i = 1; i < anchors.Count; i++)
        {
            SkyAnchor upper = anchors[i];
            if (altitude <= upper.SunAltitude)
            {
                SkyAnchor lower = anchors[i - 1];
                double t = (altitude - lower.SunAltitude) / (upper.SunAltitude - lower.SunAltitude);

                return Enumerable.Range(0, lower.Stops.Count)
                    .Select(s => new SkyStop(
                        lower.Stops[s].Position + (upper.Stops[s].Position - lower.Stops[s].Position) * t,
                        Rgba.Mix(lower.Stops[s].Color, upper.Stops[s].Color, t)))
                    .ToList();
            }
        }

        return [.. anchors[^1].Stops];
    }

    private static List<SkyStop> ApplyEveningTint(IReadOnlyList<SkyStop> stops, double altitude)
    {
        double strength = Ramp(altitude, DesignTokens.Sky.EveningFadeLow, DesignTokens.Sky.EveningFullLow)
                        * (1.0 - Ramp(altitude, DesignTokens.Sky.EveningFullHigh, DesignTokens.Sky.EveningFadeHigh));

        return stops
            .Select((stop, i) => stop with
            {
                Color = Rgba.Mix(stop.Color, DesignTokens.Sky.EveningTint, DesignTokens.Sky.EveningAmounts[i] * strength),
            })
            .ToList();
    }

    private static SkyColors ComputeGround(IReadOnlyList<SkyStop> stops, double altitude)
    {
        double day = Ramp(altitude, DesignTokens.Ground.NightAltitude, DesignTokens.Ground.DayAltitude);

        Rgba nightHorizon = DesignTokens.Sky.Anchors[0].Stops[^1].Color;
        Rgba dayHorizon = DesignTokens.Sky.Anchors[^1].Stops[^1].Color;
        Rgba expectedHorizon = Rgba.Mix(nightHorizon, dayHorizon, day);
        Rgba horizon = stops[^1].Color;

        // Only the departure from the plain day-night blend is added, so that at full day
        // and full night the ground hits the reference values exactly.
        Rgba Tinted(Rgba night, Rgba daytime, double share) =>
            Rgba.Mix(night, daytime, day).Shift(expectedHorizon, horizon, share);

        HillColors Blend(HillColors night, HillColors daytime) =>
            new(Tinted(night.Top, daytime.Top, DesignTokens.Ground.SkyShareCrest), Tinted(night.Bottom, daytime.Bottom, DesignTokens.Ground.SkyShareFoot));

        return new SkyColors(
            stops,
            Blend(DesignTokens.Ground.BackNight, DesignTokens.Ground.BackDay),
            Blend(DesignTokens.Ground.FrontNight, DesignTokens.Ground.FrontDay));
    }
}
