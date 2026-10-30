using WheresMoon.Core.Scene;

namespace WheresMoon.Core.Tests;

public class SceneTests
{
    private const double ReferenceWidth = 780.0;
    private const double ReferenceHeight = 1688.0;
    private const double ReferenceHorizon = 0.62 * ReferenceHeight;

    [Fact]
    public void Sun_lands_where_the_day_reference_image_shows_it()
    {
        double t = (10 * 60 + 23 - (7 * 60 + 31)) / (double)(17 * 60 + 2 - (7 * 60 + 31));

        PointD point = ArcGeometry.PointAt(t, ReferenceWidth / 2, ReferenceHorizon, 0.423 * ReferenceWidth, 0.538 * ReferenceWidth);

        Assert.InRange(point.X, 191, 203);
        Assert.InRange(point.Y, 700, 714);
    }

    [Fact]
    public void Moon_lands_where_the_night_reference_image_shows_it()
    {
        double t = (18 * 60 + 47 - (11 * 60 + 48)) / (double)(20 * 60 + 15 - (11 * 60 + 48));

        PointD point = ArcGeometry.PointAt(t, ReferenceWidth / 2, ReferenceHorizon, 0.346 * ReferenceWidth, 0.423 * ReferenceWidth);

        Assert.InRange(point.X, 615, 628);
        Assert.InRange(point.Y, 869, 885);
    }

    [Theory]
    [InlineData(0.0, -1.0, 0.0)]
    [InlineData(0.5, 0.0, 1.0)]
    [InlineData(1.0, 1.0, 0.0)]
    public void Arc_runs_from_left_over_the_apex_to_right(double t, double expectedX, double expectedHeight)
    {
        PointD point = ArcGeometry.PointAt(t, 0, 0, 1, 1);

        Assert.Equal(expectedX, point.X, 9);
        Assert.Equal(-expectedHeight, point.Y, 9);
    }

    [Theory]
    [InlineData(-30.0, 0)]
    [InlineData(-18.0, 0)]
    [InlineData(-3.5, 3)]
    [InlineData(2.5, 4)]
    [InlineData(10.0, 5)]
    [InlineData(60.0, 5)]
    public void Sky_hits_its_anchors_exactly(double altitude, int anchor)
    {
        SkyColors sky = SkyPalette.Compute(altitude, isRising: true);

        Assert.Equal(
            DesignTokens.Sky.Anchors[anchor].Stops.Select(stop => stop.Color.ToHex()),
            sky.Stops.Select(stop => stop.Color.ToHex()));
    }

    [Fact]
    public void Tokens_keep_the_specified_colours()
    {
        Assert.Equal("#070D24", SkyPalette.Compute(-30, true).Stops[0].Color.ToHex());
        Assert.Equal("#3C4A72", SkyPalette.Compute(-30, true).Horizon.ToHex());
        Assert.Equal("#2F6FBF", SkyPalette.Compute(30, true).Stops[0].Color.ToHex());
        Assert.Equal("#DBE9F2", SkyPalette.Compute(30, true).Horizon.ToHex());
        Assert.Equal("#F2A65A", SkyPalette.Compute(-3.5, true).Horizon.ToHex());
        Assert.Equal("#F6C27A", SkyPalette.Compute(2.5, true).Horizon.ToHex());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Sky_never_jumps(bool isRising)
    {
        SkyColors previous = SkyPalette.Compute(-25.0, isRising);

        for (double altitude = -24.9; altitude <= 25.0; altitude += 0.1)
        {
            SkyColors current = SkyPalette.Compute(altitude, isRising);

            for (int i = 0; i < current.Stops.Count; i++)
            {
                Rgba a = previous.Stops[i].Color, b = current.Stops[i].Color;
                double largest = Math.Max(Math.Abs(a.R - b.R), Math.Max(Math.Abs(a.G - b.G), Math.Abs(a.B - b.B)));
                Assert.True(largest < 0.03, $"jump of {largest:F3} at {altitude:F1} degrees, stop {i}");
            }

            previous = current;
        }
    }

    [Fact]
    public void Evening_sky_is_warmer_than_morning_sky()
    {
        Rgba morning = SkyPalette.Compute(-2.0, isRising: true).Horizon;
        Rgba evening = SkyPalette.Compute(-2.0, isRising: false).Horizon;

        Assert.True(evening.R - evening.B > morning.R - morning.B);
    }

    [Fact]
    public void Morning_and_evening_agree_at_noon_and_midnight()
    {
        Assert.Equal(SkyPalette.Compute(40, true).Horizon.ToHex(), SkyPalette.Compute(40, false).Horizon.ToHex());
        Assert.Equal(SkyPalette.Compute(-30, true).Horizon.ToHex(), SkyPalette.Compute(-30, false).Horizon.ToHex());
    }

    [Theory]
    [InlineData(30.0, "#6F8FA6", "#3F5C70", "#3A5568", "#1D2F3D")]
    [InlineData(-30.0, "#1C2747", "#121A33", "#0F1630", "#080D1F")]
    public void Ground_matches_the_references_by_day_and_night(double altitude, string backTop, string backBottom, string frontTop, string frontBottom)
    {
        SkyColors sky = SkyPalette.Compute(altitude, true);

        Assert.Equal(backTop, sky.BackHill.Top.ToHex());
        Assert.Equal(backBottom, sky.BackHill.Bottom.ToHex());
        Assert.Equal(frontTop, sky.FrontHill.Top.ToHex());
        Assert.Equal(frontBottom, sky.FrontHill.Bottom.ToHex());
    }

    [Fact]
    public void Ground_catches_a_warm_shimmer_at_dawn()
    {
        Rgba hill = SkyPalette.Compute(-3.5, true).BackHill.Top;
        double day = SkyPalette.Ramp(-3.5, DesignTokens.Ground.NightAltitude, DesignTokens.Ground.DayAltitude);
        Rgba plain = Rgba.Mix(DesignTokens.Ground.BackNight.Top, DesignTokens.Ground.BackDay.Top, day);

        Assert.True(hill.R > plain.R && hill.B < plain.B);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Front_hill_is_always_darker(bool isRising)
    {
        for (double altitude = -30.0; altitude <= 40.0; altitude += 0.5)
        {
            SkyColors sky = SkyPalette.Compute(altitude, isRising);

            Assert.True(sky.FrontHill.Top.Luminance < sky.BackHill.Top.Luminance, $"top at {altitude}");
            Assert.True(sky.FrontHill.Bottom.Luminance < sky.BackHill.Bottom.Luminance, $"bottom at {altitude}");
        }
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.18)]
    [InlineData(0.5)]
    [InlineData(0.8)]
    [InlineData(1.0)]
    public void Lit_moon_area_equals_the_illuminated_fraction(double fraction)
    {
        foreach (double angle in new[] { 0.0, 37.0, 90.0, 200.0, 311.0 })
        {
            CubicPath path = MoonShape.Lit(new PointD(5, -3), 10, fraction, angle);

            Assert.InRange(Area(path), (fraction * Math.PI * 100) - 0.5, (fraction * Math.PI * 100) + 0.5);
        }
    }

    [Theory]
    [InlineData(0.0, 0.0, -1.0)]
    [InlineData(90.0, -1.0, 0.0)]
    [InlineData(180.0, 0.0, 1.0)]
    [InlineData(270.0, 1.0, 0.0)]
    public void Crescent_sits_on_the_side_of_the_bright_limb(double zenithAngle, double directionX, double directionY)
    {
        (double x, double y) = Centroid(MoonShape.Lit(new PointD(0, 0), 10, 0.2, zenithAngle));

        Assert.True(x * directionX + y * directionY > 5.0);
    }

    [Fact]
    public void Gibbous_terminator_bulges_away_from_the_bright_limb()
    {
        CubicPath path = MoonShape.Lit(new PointD(0, 0), 10, 0.75, 90.0);

        // The bright limb points left, so at 75 % the terminator crosses to the right of the centre.
        Assert.Equal(5.0, path.Segments[2].End.X, 6);
    }

    [Fact]
    public void Stars_are_few_faint_and_stable()
    {
        IReadOnlyList<StarField.Star> stars = StarField.Stars;

        Assert.InRange(stars.Count, 10, 20);
        Assert.All(stars, star =>
        {
            Assert.InRange(star.X, 0.0, 1.0);
            Assert.InRange(star.Y, DesignTokens.Stars.TopLimit, DesignTokens.Stars.BottomLimit);
            Assert.InRange(star.Radius, 0.7, 1.1);
            Assert.InRange(star.Opacity, 0.35, 0.80);
        });

        // Pinned, because a moving star field would be visible to anyone comparing screenshots.
        Assert.Equal(0.42279, stars[0].X, 4);
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(-4.0, 0.0)]
    [InlineData(-12.0, 1.0)]
    [InlineData(-30.0, 1.0)]
    public void Stars_fade_with_the_sun(double altitude, double expected)
    {
        Assert.Equal(expected, StarField.Visibility(altitude), 6);
    }

    [Fact]
    public void Layout_reproduces_the_reference_format()
    {
        SceneLayout layout = SceneLayout.Compute(390, 844, SafeInsets.None);

        Assert.Equal(390.0, layout.Unit, 6);
        Assert.Equal(0.62 * 844, layout.HorizonY, 6);
        Assert.Equal(0.423 * 390, layout.SunArcRadiusX, 6);
        Assert.Equal(0.538 * 390, layout.SunArcRadiusY, 6);
        Assert.Equal(0.164 * 390, layout.ClockFontSize, 6);
        Assert.Equal(0.09 * 844, layout.TextTop, 6);
        Assert.Equal(1.0, layout.SkyStopScale, 6);
    }

    public static TheoryData<double, double, SafeInsets> Formats()
    {
        var notchPortrait = new SafeInsets(0, 59, 0, 34);
        var notchLandscape = new SafeInsets(59, 0, 59, 21);
        var androidBars = new SafeInsets(0, 32, 0, 24);

        return new TheoryData<double, double, SafeInsets>
        {
            { 320, 480, SafeInsets.None },
            { 320, 568, androidBars },
            { 360, 640, androidBars },
            { 390, 844, notchPortrait },
            { 430, 932, notchPortrait },
            { 844, 390, notchLandscape },
            { 932, 430, notchLandscape },
            { 640, 360, androidBars },
            { 653, 280, androidBars },
            { 768, 1024, SafeInsets.None },
            { 820, 1180, new SafeInsets(0, 24, 0, 20) },
            { 1024, 1366, SafeInsets.None },
            { 1180, 820, new SafeInsets(0, 24, 0, 20) },
            { 1366, 1024, SafeInsets.None },
            { 400, 400, SafeInsets.None },
            { 1280, 720, SafeInsets.None },
            { 1920, 1080, SafeInsets.None },
            { 2560, 1440, SafeInsets.None },
            { 3840, 2160, SafeInsets.None },
            { 3840, 600, SafeInsets.None },
            { 500, 2000, SafeInsets.None },
        };
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void Layout_keeps_everything_on_screen(double width, double height, SafeInsets insets)
    {
        SceneLayout layout = SceneLayout.Compute(width, height, insets);

        double reach = layout.SunArcRadiusX + layout.SunRadius;
        Assert.True(layout.CenterX - reach >= insets.Left, "sun leaves on the left");
        Assert.True(layout.CenterX + reach <= width - insets.Right, "sun leaves on the right");

        double sunTop = layout.HorizonY - layout.SunArcRadiusY - layout.SunRadius;
        Assert.True(sunTop >= layout.TextBottom, $"sun at {sunTop:F1} reaches the text ending at {layout.TextBottom:F1}");
        Assert.True(layout.TextTop >= insets.Top, "text enters the top safe area");

        double groundEnd = layout.HorizonY + DesignTokens.Size.MinimumGround * layout.Unit;
        Assert.True(groundEnd <= height - insets.Bottom + 0.001, $"horizon at {layout.HorizonY:F1} leaves too little ground");

        Assert.True(layout.Unit <= width - insets.Left - insets.Right, "text wider than the safe area");
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void Text_lines_never_collide(double width, double height, SafeInsets insets)
    {
        SceneLayout layout = SceneLayout.Compute(width, height, insets);

        Assert.True(layout.RiseSetLine2CenterY - layout.RiseSetLine1CenterY >= layout.RiseSetFontSize * 1.15, "rise and set lines");
        Assert.True(layout.PhaseLineCenterY - layout.RiseSetLine2CenterY >= (layout.RiseSetFontSize + layout.PhaseFontSize) * 0.575, "phase line");
        Assert.True(layout.RiseSetLine1CenterY - layout.RiseSetFontSize * 0.6 >= layout.TextTop + layout.ClockFontSize, "clock");
    }

    [Fact]
    public void Fine_details_grow_with_large_scenes_but_never_shrink()
    {
        Assert.Equal(1.0, SceneLayout.Compute(390, 844, SafeInsets.None).DetailScale, 6);
        Assert.Equal(1.0, SceneLayout.Compute(844, 390, SafeInsets.None).DetailScale, 6);
        Assert.Equal(2.0, SceneLayout.Compute(780, 1688, SafeInsets.None).DetailScale, 6);
    }

    [Fact]
    public void Wide_windows_get_flatter_wider_arcs_within_the_limit()
    {
        SceneLayout layout = SceneLayout.Compute(844, 390, new SafeInsets(59, 0, 59, 21));

        Assert.Equal(0.423 * layout.Unit * 1.5, layout.SunArcRadiusX, 6);
        Assert.Equal(0.538 * layout.Unit, layout.SunArcRadiusY, 6);
    }

    [Fact]
    public void Hills_span_the_full_width()
    {
        CubicPath back = HillShapes.Back(1000, 500, 300);
        CubicPath front = HillShapes.Front(1000, 500, 300);

        Assert.Equal(0.0, back.Start.X);
        Assert.Equal(1000.0, back.Segments[^1].End.X);
        Assert.Equal(0.0, front.Start.X);
        Assert.Equal(1000.0, front.Segments[^1].End.X);
    }

    [Fact]
    public void Hill_joins_are_smooth()
    {
        foreach (CubicPath path in new[] { HillShapes.Back(1, 1, 0), HillShapes.Front(1, 1, 0) })
        {
            PointD join = path.Segments[0].End;
            PointD before = path.Segments[0].Control2;
            PointD after = path.Segments[1].Control1;

            Assert.Equal(join.X - before.X, after.X - join.X, 6);
            Assert.Equal(join.Y - before.Y, after.Y - join.Y, 6);
        }
    }

    private static List<PointD> Flatten(CubicPath path)
    {
        var points = new List<PointD>();
        PointD current = path.Start;

        foreach (CubicSegment segment in path.Segments)
        {
            for (int i = 1; i <= 64; i++)
            {
                double t = i / 64.0, u = 1 - t;
                points.Add(new PointD(
                    u * u * u * current.X + 3 * u * u * t * segment.Control1.X + 3 * u * t * t * segment.Control2.X + t * t * t * segment.End.X,
                    u * u * u * current.Y + 3 * u * u * t * segment.Control1.Y + 3 * u * t * t * segment.Control2.Y + t * t * t * segment.End.Y));
            }

            current = segment.End;
        }

        return points;
    }

    private static double Area(CubicPath path)
    {
        List<PointD> points = Flatten(path);
        double sum = 0;

        for (int i = 0; i < points.Count; i++)
        {
            PointD a = points[i], b = points[(i + 1) % points.Count];
            sum += a.X * b.Y - b.X * a.Y;
        }

        return Math.Abs(sum) / 2.0;
    }

    private static (double X, double Y) Centroid(CubicPath path)
    {
        List<PointD> points = Flatten(path);
        double area = 0, x = 0, y = 0;

        for (int i = 0; i < points.Count; i++)
        {
            PointD a = points[i], b = points[(i + 1) % points.Count];
            double cross = a.X * b.Y - b.X * a.Y;
            area += cross;
            x += (a.X + b.X) * cross;
            y += (a.Y + b.Y) * cross;
        }

        return (x / (3 * area), y / (3 * area));
    }
}
