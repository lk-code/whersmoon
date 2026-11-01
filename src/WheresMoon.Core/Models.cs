namespace WheresMoon.Core;

/// <summary>Observer position. Latitude and longitude in degrees, elevation in metres.</summary>
public readonly record struct GeoLocation(double Latitude, double Longitude, double Elevation = 0);

public enum TwilightPhase
{
    Night,
    AstronomicalTwilight,
    NauticalTwilight,
    CivilTwilight,
    GoldenHour,
    Day,
}

public enum MoonPhaseName
{
    NewMoon,
    WaxingCrescent,
    FirstQuarter,
    WaxingGibbous,
    FullMoon,
    WaningGibbous,
    LastQuarter,
    WaningCrescent,
}

public enum DaylightKind
{
    Normal,

    /// <summary>The sun stays up for the whole calendar day.</summary>
    MidnightSun,

    /// <summary>The sun stays down for the whole calendar day.</summary>
    PolarNight,
}

/// <summary>Time the sun is up on one local calendar day.</summary>
/// <param name="ChangeFromYesterday">Difference to the day before, or null if one of the two has no sunrise or sunset.</param>
public sealed record DayLengthInfo(DaylightKind Kind, TimeSpan Length, TimeSpan? ChangeFromYesterday);

public enum LightKind
{
    Golden,
    Blue,
}

public enum DayPart
{
    Morning,

    /// <summary>The window includes solar noon, because the sun stays low all day.</summary>
    Midday,
    Evening,
}

/// <summary>Where the sun stays on a day, for days without any window to show.</summary>
public enum SunRange
{
    Mixed,
    AlwaysHigh,
    AlwaysLow,
}

/// <summary>
/// One golden or blue hour. Start is null if it began before the local day and End is null if it
/// lasts beyond it, which happens at high latitudes.
/// </summary>
public readonly record struct LightWindow(LightKind Kind, DayPart Part, DateTimeOffset? Start, DateTimeOffset? End);

/// <summary>Golden and blue hours of one local calendar day.</summary>
/// <param name="Boundaries">Every start and end of a window, in order.</param>
public sealed record LightHours(IReadOnlyList<LightWindow> Windows, SunRange Range, IReadOnlyList<DateTimeOffset> Boundaries);

public enum SkyEventKind
{
    FullMoon,
    NewMoon,
    Supermoon,
    LunarEclipse,
    SolarEclipse,
}

public enum EclipseType
{
    Penumbral,
    Partial,
    Annular,
    Total,
}

/// <summary>An upcoming event. <see cref="Eclipse"/> is only set for eclipses.</summary>
public readonly record struct SkyEvent(SkyEventKind Kind, DateTimeOffset Time, EclipseType? Eclipse = null);

/// <summary>
/// A rise or set for display. <see cref="Time"/> is in the requested time zone,
/// <see cref="DayOffset"/> counts local calendar days from today (+1 = tomorrow).
/// </summary>
public readonly record struct TimeEvent(DateTimeOffset Time, int DayOffset);

/// <summary>Position and rise/set state shared by sun and moon.</summary>
public sealed record BodyInfo
{
    /// <summary>Apparent altitude above the horizon in degrees, including refraction.</summary>
    public required double Altitude { get; init; }

    /// <summary>Azimuth in degrees, clockwise from north.</summary>
    public required double Azimuth { get; init; }

    /// <summary>True between rise and set, using the same criterion as the rise/set search.</summary>
    public required bool IsUp { get; init; }

    /// <summary>Rise to display, or null when there is none to show today.</summary>
    public required TimeEvent? Rise { get; init; }

    /// <summary>Set to display, or null when there is none to show today.</summary>
    public required TimeEvent? Set { get; init; }

    /// <summary>
    /// Position on the schematic arc: 0 is the left end, 0.5 the apex, 1 the right end.
    /// Null while the body is below the horizon.
    /// </summary>
    public required double? ArcProgress { get; init; }

    /// <summary>
    /// Position on the path below the horizon: 0 is the right end, where the body set, 1 the left
    /// end, where it rises. Null while the body is up.
    /// </summary>
    public required double? BelowProgress { get; init; }
}

public sealed record SunInfo
{
    public required BodyInfo Body { get; init; }

    /// <summary>Altitude without refraction, which is what twilight limits and sky colours are defined on.</summary>
    public required double GeometricAltitude { get; init; }

    /// <summary>True before solar noon, false after it.</summary>
    public required bool IsRising { get; init; }
}

public sealed record MoonInfo
{
    public required BodyInfo Body { get; init; }

    /// <summary>Illuminated fraction of the disc as seen from the observer, 0 to 1.</summary>
    public required double IlluminatedFraction { get; init; }

    /// <summary>Sun-Moon-observer angle in degrees: 0 is full, 180 is new.</summary>
    public required double PhaseAngle { get; init; }

    /// <summary>Geocentric ecliptic longitude difference to the sun, 0 to 360 degrees.</summary>
    public required double Elongation { get; init; }

    public required MoonPhaseName PhaseName { get; init; }

    /// <summary>
    /// Direction of the bright limb's midpoint in degrees, counter-clockwise from the zenith
    /// as seen by the observer. 90 points left on a screen whose top is the zenith.
    /// </summary>
    public required double BrightLimbZenithAngle { get; init; }
}

/// <summary>Everything the calculation knows about the sky at one moment and place.</summary>
public sealed record SkySnapshot
{
    public required DateTimeOffset Time { get; init; }
    public required GeoLocation Location { get; init; }
    public required SunInfo Sun { get; init; }
    public required MoonInfo Moon { get; init; }
    public required TwilightPhase Twilight { get; init; }
}
