using CosineKitty;

namespace WheresMoon.Core;

/// <summary>
/// How the moon appears to an observer: lit fraction and the direction of its bright limb.
/// Astronomy Engine has no such function, so the formulas follow Meeus, Astronomical Algorithms, ch. 14 and 48.
/// </summary>
public static class MoonOrientation
{
    /// <summary>
    /// Position angle of the bright limb's midpoint in degrees, measured from celestial north
    /// through east. Inputs are right ascension and declination in degrees.
    /// </summary>
    public static double BrightLimbPositionAngle(double sunRa, double sunDec, double moonRa, double moonDec)
    {
        double deltaRa = Radians(sunRa - moonRa);
        double sunDecRad = Radians(sunDec);
        double moonDecRad = Radians(moonDec);

        double y = Math.Cos(sunDecRad) * Math.Sin(deltaRa);
        double x = Math.Sin(sunDecRad) * Math.Cos(moonDecRad)
                 - Math.Cos(sunDecRad) * Math.Sin(moonDecRad) * Math.Cos(deltaRa);

        return Normalize(Degrees(Math.Atan2(y, x)));
    }

    /// <summary>
    /// Parallactic angle in degrees: how far the zenith lies from celestial north, seen from the body.
    /// Written without tan(latitude) so it stays defined at the poles.
    /// </summary>
    public static double ParallacticAngle(double hourAngleDegrees, double declination, double latitude)
    {
        double h = Radians(hourAngleDegrees);
        double dec = Radians(declination);
        double lat = Radians(latitude);

        double y = Math.Sin(h) * Math.Cos(lat);
        double x = Math.Sin(lat) * Math.Cos(dec) - Math.Cos(lat) * Math.Sin(dec) * Math.Cos(h);

        return Degrees(Math.Atan2(y, x));
    }

    /// <summary>Bright limb direction relative to the zenith, counter-clockwise, 0 to 360 degrees.</summary>
    public static double ZenithAngle(double positionAngle, double parallacticAngle) =>
        Normalize(positionAngle - parallacticAngle);

    /// <summary>Hour angle in degrees, -180 to 180, negative before transit.</summary>
    internal static double HourAngle(AstroTime time, double longitude, double rightAscensionDegrees)
    {
        double hours = Astronomy.SiderealTime(time) + longitude / 15.0 - rightAscensionDegrees / 15.0;
        double degrees = Normalize(hours * 15.0);
        return degrees > 180.0 ? degrees - 360.0 : degrees;
    }

    /// <summary>Phase angle in degrees from observer-centred vectors of moon and sun.</summary>
    internal static double PhaseAngle(AstroVector moon, AstroVector sun)
    {
        // Angle at the moon between the directions to the observer and to the sun.
        double ox = -moon.x, oy = -moon.y, oz = -moon.z;
        double sx = sun.x - moon.x, sy = sun.y - moon.y, sz = sun.z - moon.z;

        double dot = ox * sx + oy * sy + oz * sz;
        double crossX = oy * sz - oz * sy;
        double crossY = oz * sx - ox * sz;
        double crossZ = ox * sy - oy * sx;
        double cross = Math.Sqrt(crossX * crossX + crossY * crossY + crossZ * crossZ);

        return Degrees(Math.Atan2(cross, dot));
    }

    internal static double IlluminatedFraction(double phaseAngleDegrees) =>
        (1.0 + Math.Cos(Radians(phaseAngleDegrees))) / 2.0;

    internal static MoonPhaseName PhaseName(double elongation)
    {
        const double principalHalfWidth = 6.0;
        double e = Normalize(elongation);

        if (e < principalHalfWidth || e >= 360.0 - principalHalfWidth) return MoonPhaseName.NewMoon;
        if (e < 90.0 - principalHalfWidth) return MoonPhaseName.WaxingCrescent;
        if (e < 90.0 + principalHalfWidth) return MoonPhaseName.FirstQuarter;
        if (e < 180.0 - principalHalfWidth) return MoonPhaseName.WaxingGibbous;
        if (e < 180.0 + principalHalfWidth) return MoonPhaseName.FullMoon;
        if (e < 270.0 - principalHalfWidth) return MoonPhaseName.WaningGibbous;
        if (e < 270.0 + principalHalfWidth) return MoonPhaseName.LastQuarter;
        return MoonPhaseName.WaningCrescent;
    }

    private static double Normalize(double degrees)
    {
        double result = degrees % 360.0;
        return result < 0.0 ? result + 360.0 : result;
    }

    private static double Radians(double degrees) => degrees * Math.PI / 180.0;

    private static double Degrees(double radians) => radians * 180.0 / Math.PI;
}
