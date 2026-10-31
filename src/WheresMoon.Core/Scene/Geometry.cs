namespace WheresMoon.Core.Scene;

public readonly record struct PointD(double X, double Y);

/// <summary>Cubic Bézier piece of a path, continuing from the previous end point.</summary>
public readonly record struct CubicSegment(PointD Control1, PointD Control2, PointD End);

/// <summary>A path of cubic Bézier curves.</summary>
public sealed record CubicPath(PointD Start, IReadOnlyList<CubicSegment> Segments);

/// <summary>Half-elliptic arcs above the horizon line.</summary>
public static class ArcGeometry
{
    /// <summary>Point at progress <paramref name="t"/>: 0 is the left end, 0.5 the apex, 1 the right end.</summary>
    public static PointD PointAt(double t, double centerX, double horizonY, double radiusX, double radiusY)
    {
        double theta = Math.PI * (1.0 - t);
        return new PointD(centerX + radiusX * Math.Cos(theta), horizonY - radiusY * Math.Sin(theta));
    }

    /// <summary>Point on the path below the horizon: 0 is the right end, where the body set, 1 the left end, where it rises.</summary>
    public static PointD LowerPointAt(double t, double centerX, double horizonY, double radiusX, double radiusY)
    {
        double theta = Math.PI * t;
        return new PointD(centerX + radiusX * Math.Cos(theta), horizonY + radiusY * Math.Sin(theta));
    }

    /// <summary>The whole arc from its left to its right end.</summary>
    public static CubicPath Outline(double centerX, double horizonY, double radiusX, double radiusY)
    {
        double kx = radiusX * MoonShape.Kappa;
        double ky = radiusY * MoonShape.Kappa;
        double top = horizonY - radiusY;

        return new CubicPath(new PointD(centerX - radiusX, horizonY),
        [
            new(new(centerX - radiusX, horizonY - ky), new(centerX - kx, top), new(centerX, top)),
            new(new(centerX + kx, top), new(centerX + radiusX, horizonY - ky), new(centerX + radiusX, horizonY)),
        ]);
    }
}

/// <summary>The two hill outlines from the design reference.</summary>
public static class HillShapes
{
    /// <summary>
    /// Top edge of a hill in units of the reference: x relative to the width, y relative to the
    /// scene unit and to the horizon line. The second segment of each curve was an SVG "S" command,
    /// so its first control point is the mirror image of the previous one.
    /// </summary>
    private static readonly CubicPath BackOutline = new(
        new PointD(0, 0.021),
        [
            new(new(0.179, -0.031), new(0.359, -0.015), new(0.526, 0.010)),
            new(new(0.693, 0.035), new(0.846, -0.031), new(1.0, 0)),
        ]);

    private static readonly CubicPath FrontOutline = new(
        new PointD(0, 0.108),
        [
            new(new(0.231, 0.056), new(0.436, 0.082), new(0.641, 0.108)),
            new(new(0.846, 0.134), new(0.897, 0.072), new(1.0, 0.092)),
        ]);

    public static CubicPath Back(double width, double unit, double horizonY) => Place(BackOutline, width, unit, horizonY);

    public static CubicPath Front(double width, double unit, double horizonY) => Place(FrontOutline, width, unit, horizonY);

    private static CubicPath Place(CubicPath outline, double width, double unit, double horizonY)
    {
        PointD Map(PointD p) => new(p.X * width, horizonY + p.Y * unit);

        return new CubicPath(
            Map(outline.Start),
            outline.Segments.Select(s => new CubicSegment(Map(s.Control1), Map(s.Control2), Map(s.End))).ToList());
    }
}

/// <summary>Outline of the lit part of the moon.</summary>
public static class MoonShape
{
    /// <summary>Control point distance that makes a cubic Bézier follow a quarter circle.</summary>
    internal const double Kappa = 0.5522847498;

    /// <summary>
    /// Builds the lit area: half of the limb plus the terminator, which is a half ellipse.
    /// </summary>
    /// <param name="center">Moon centre on screen, y pointing down.</param>
    /// <param name="radius">Disc radius.</param>
    /// <param name="illuminatedFraction">Lit fraction of the disc, 0 to 1.</param>
    /// <param name="zenithAngle">Bright limb direction, degrees counter-clockwise from the top of the screen.</param>
    public static CubicPath Lit(PointD center, double radius, double illuminatedFraction, double zenithAngle)
    {
        double angle = zenithAngle * Math.PI / 180.0;
        double brightX = -Math.Sin(angle);
        double brightY = -Math.Cos(angle);

        // Local frame: +x points to the bright limb, +y is perpendicular to it.
        PointD Place(double x, double y) => new(
            center.X + x * brightX - y * brightY,
            center.Y + x * brightY + y * brightX);

        double r = radius;
        double k = r * Kappa;

        // Where the terminator crosses the local x axis: the right edge at new moon, the left at full moon.
        double t = r * (1.0 - 2.0 * Math.Clamp(illuminatedFraction, 0.0, 1.0));
        double tk = t * Kappa;

        return new CubicPath(Place(0, -r),
        [
            new(Place(k, -r), Place(r, -k), Place(r, 0)),
            new(Place(r, k), Place(k, r), Place(0, r)),
            new(Place(tk, r), Place(t, k), Place(t, 0)),
            new(Place(t, -k), Place(tk, -r), Place(0, -r)),
        ]);
    }
}

/// <summary>Few faint stars with fixed, pseudo-random positions.</summary>
public static class StarField
{
    /// <summary>Position relative to width and to the distance from top to horizon.</summary>
    public readonly record struct Star(double X, double Y, double Radius, double Opacity);

    /// <summary>Own generator, because the result must never change between runtimes or platforms.</summary>
    public static IReadOnlyList<Star> Stars { get; } = Create(DesignTokens.Stars.Count, DesignTokens.Stars.Seed);

    /// <summary>Overall star visibility for a sun altitude, 0 to 1.</summary>
    public static double Visibility(double sunAltitude) =>
        SkyPalette.Ramp(sunAltitude, DesignTokens.Stars.StartAltitude, DesignTokens.Stars.FullAltitude);

    private static List<Star> Create(int count, uint seed)
    {
        uint state = seed;
        var stars = new List<Star>(count);

        for (int i = 0; i < count; i++)
        {
            double y = Next(ref state);
            stars.Add(new Star(
                0.03 + 0.94 * Next(ref state),
                DesignTokens.Stars.TopLimit + (DesignTokens.Stars.BottomLimit - DesignTokens.Stars.TopLimit) * y,
                DesignTokens.Stars.MinRadius + (DesignTokens.Stars.MaxRadius - DesignTokens.Stars.MinRadius) * Next(ref state),
                DesignTokens.Stars.MinOpacity + (DesignTokens.Stars.MaxOpacity - DesignTokens.Stars.MinOpacity) * Next(ref state)));
        }

        return stars;
    }

    private static double Next(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return state / (double)uint.MaxValue;
    }
}
