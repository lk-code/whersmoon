using WheresMoon.Core.Scene;

namespace WheresMoon.Core.Tests;

public class SceneTests
{
    private const double ReferenceWidth = 780.0;
    private const double ReferenceHeight = 1688.0;
    private const double ReferenceHorizon = 0.62 * ReferenceHeight;

    [Test]
    public void Sun_passes_the_day_reference_time_on_its_semicircle()
    {
        double t = (10 * 60 + 23 - (7 * 60 + 31)) / (double)(17 * 60 + 2 - (7 * 60 + 31));

        PointD point = ArcGeometry.PointAt(t, ReferenceWidth / 2, ReferenceHorizon, 0.423 * ReferenceWidth, 0.423 * ReferenceWidth);

        point.X.Should().BeInRange(191, 203);
        point.Y.Should().BeInRange(772, 786);
    }

    [Test]
    public void Moon_passes_the_night_reference_time_on_its_semicircle()
    {
        double t = (18 * 60 + 47 - (11 * 60 + 48)) / (double)(20 * 60 + 15 - (11 * 60 + 48));

        PointD point = ArcGeometry.PointAt(t, ReferenceWidth / 2, ReferenceHorizon, 0.346 * ReferenceWidth, 0.346 * ReferenceWidth);

        point.X.Should().BeInRange(615, 628);
        point.Y.Should().BeInRange(899, 914);
    }

    [TestCase(0.0)]
    [TestCase(0.2)]
    [TestCase(0.5)]
    [TestCase(0.75)]
    [TestCase(1.0)]
    public void Every_point_of_an_arc_lies_one_radius_from_its_centre(double t)
    {
        const double Radius = 165.0;

        PointD point = ArcGeometry.PointAt(t, 0.0, 0.0, Radius, Radius);

        Math.Sqrt(point.X * point.X + point.Y * point.Y).Should().BeApproximately(Radius, 0.5e-9);
    }

    [Test]
    public void Drawn_arc_stays_on_the_circle()
    {
        const double LargestRadius = 346.0;

        List<PointD> points = Flatten(ArcGeometry.Outline(0.0, 0.0, LargestRadius, LargestRadius));

        points.Should().AllSatisfy(point => Math.Sqrt(point.X * point.X + point.Y * point.Y).Should().BeApproximately(LargestRadius, 0.1));
    }

    [TestCase(0.0, -1.0, 0.0)]
    [TestCase(0.5, 0.0, 1.0)]
    [TestCase(1.0, 1.0, 0.0)]
    public void Arc_runs_from_left_over_the_apex_to_right(double t, double expectedX, double expectedHeight)
    {
        PointD point = ArcGeometry.PointAt(t, 0, 0, 1, 1);

        point.X.Should().BeApproximately(expectedX, 0.5e-9);
        point.Y.Should().BeApproximately(-expectedHeight, 0.5e-9);
    }

    [TestCase(-30.0, 0)]
    [TestCase(-18.0, 0)]
    [TestCase(-3.5, 3)]
    [TestCase(2.5, 4)]
    [TestCase(10.0, 5)]
    [TestCase(60.0, 5)]
    public void Sky_hits_its_anchors_exactly(double altitude, int anchor)
    {
        SkyColors sky = SkyPalette.Compute(altitude, isRising: true);

        sky.Stops.Select(stop => stop.Color.ToHex()).Should().Equal(DesignTokens.Sky.Anchors[anchor].Stops.Select(stop => stop.Color.ToHex()));
    }

    [Test]
    public void Tokens_keep_the_specified_colours()
    {
        SkyPalette.Compute(-30, true).Stops[0].Color.ToHex().Should().Be("#070D24");
        SkyPalette.Compute(-30, true).Horizon.ToHex().Should().Be("#3C4A72");
        SkyPalette.Compute(30, true).Stops[0].Color.ToHex().Should().Be("#2F6FBF");
        SkyPalette.Compute(30, true).Horizon.ToHex().Should().Be("#DBE9F2");
        SkyPalette.Compute(-3.5, true).Horizon.ToHex().Should().Be("#F2A65A");
        SkyPalette.Compute(2.5, true).Horizon.ToHex().Should().Be("#F6C27A");
    }

    [TestCase(true)]
    [TestCase(false)]
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
                largest.Should().BeLessThan(0.03, $"jump of {largest:F3} at {altitude:F1} degrees, stop {i}");
            }

            previous = current;
        }
    }

    [Test]
    public void Evening_sky_is_warmer_than_morning_sky()
    {
        Rgba morning = SkyPalette.Compute(-2.0, isRising: true).Horizon;
        Rgba evening = SkyPalette.Compute(-2.0, isRising: false).Horizon;

        (evening.R - evening.B).Should().BeGreaterThan(morning.R - morning.B);
    }

    [Test]
    public void Morning_and_evening_agree_at_noon_and_midnight()
    {
        SkyPalette.Compute(40, false).Horizon.ToHex().Should().Be(SkyPalette.Compute(40, true).Horizon.ToHex());
        SkyPalette.Compute(-30, false).Horizon.ToHex().Should().Be(SkyPalette.Compute(-30, true).Horizon.ToHex());
    }

    [TestCase(30.0, "#6F8FA6", "#3F5C70", "#3A5568", "#1D2F3D")]
    [TestCase(-30.0, "#1C2747", "#121A33", "#0F1630", "#080D1F")]
    public void Ground_matches_the_references_by_day_and_night(double altitude, string backTop, string backBottom, string frontTop, string frontBottom)
    {
        SkyColors sky = SkyPalette.Compute(altitude, true);

        sky.BackHill.Top.ToHex().Should().Be(backTop);
        sky.BackHill.Bottom.ToHex().Should().Be(backBottom);
        sky.FrontHill.Top.ToHex().Should().Be(frontTop);
        sky.FrontHill.Bottom.ToHex().Should().Be(frontBottom);
    }

    [Test]
    public void Ground_catches_a_warm_shimmer_at_dawn()
    {
        Rgba hill = SkyPalette.Compute(-3.5, true).BackHill.Top;
        double day = SkyPalette.Ramp(-3.5, DesignTokens.Ground.NightAltitude, DesignTokens.Ground.DayAltitude);
        Rgba plain = Rgba.Mix(DesignTokens.Ground.BackNight.Top, DesignTokens.Ground.BackDay.Top, day);

        (hill.R > plain.R && hill.B < plain.B).Should().BeTrue();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Front_hill_is_always_darker(bool isRising)
    {
        for (double altitude = -30.0; altitude <= 40.0; altitude += 0.5)
        {
            SkyColors sky = SkyPalette.Compute(altitude, isRising);

            sky.FrontHill.Top.Luminance.Should().BeLessThan(sky.BackHill.Top.Luminance, $"top at {altitude}");
            sky.FrontHill.Bottom.Luminance.Should().BeLessThan(sky.BackHill.Bottom.Luminance, $"bottom at {altitude}");
        }
    }

    [TestCase(0.0)]
    [TestCase(0.18)]
    [TestCase(0.5)]
    [TestCase(0.8)]
    [TestCase(1.0)]
    public void Lit_moon_area_equals_the_illuminated_fraction(double fraction)
    {
        foreach (double angle in new[] { 0.0, 37.0, 90.0, 200.0, 311.0 })
        {
            CubicPath path = MoonShape.Lit(new PointD(5, -3), 10, fraction, angle);

            Area(path).Should().BeInRange((fraction * Math.PI * 100) - 0.5, (fraction * Math.PI * 100) + 0.5);
        }
    }

    [TestCase(0.0, 0.0, -1.0)]
    [TestCase(90.0, -1.0, 0.0)]
    [TestCase(180.0, 0.0, 1.0)]
    [TestCase(270.0, 1.0, 0.0)]
    public void Crescent_sits_on_the_side_of_the_bright_limb(double zenithAngle, double directionX, double directionY)
    {
        (double x, double y) = Centroid(MoonShape.Lit(new PointD(0, 0), 10, 0.2, zenithAngle));

        (x * directionX + y * directionY).Should().BeGreaterThan(5.0);
    }

    [Test]
    public void Gibbous_terminator_bulges_away_from_the_bright_limb()
    {
        CubicPath path = MoonShape.Lit(new PointD(0, 0), 10, 0.75, 90.0);

        // The bright limb points left, so at 75 % the terminator crosses to the right of the centre.
        path.Segments[2].End.X.Should().BeApproximately(5.0, 0.5e-6);
    }

    [Test]
    public void Stars_are_few_faint_and_stable()
    {
        IReadOnlyList<StarField.Star> stars = StarField.Stars;

        stars.Count.Should().Be(108);
        stars.Should().AllSatisfy(star =>
        {
            star.Azimuth.Should().BeInRange(0.0, 360.0);
            star.Y.Should().BeInRange(DesignTokens.Stars.TopLimit, DesignTokens.Stars.BottomLimit);
            star.Radius.Should().BeInRange(0.7, 1.1);
            star.Opacity.Should().BeInRange(0.35, 0.80);
        });

        // Pinned, because a moving star field would be visible to anyone comparing screenshots.
        stars[0].Azimuth.Should().BeApproximately(1.39286, 0.5e-4);
    }

    [Test]
    public void Star_density_is_even_around_the_whole_band()
    {
        for (double start = 0.0; start < 360.0; start += 5.0)
        {
            int count = StarField.Stars.Count(star => Math.Abs(((star.Azimuth - start + 540.0) % 360.0) - 180.0) <= 30.0);

            count.Should().BeInRange(16, 20);
        }
    }

    [TestCase(0.0, 0.0, 200.0)]
    [TestCase(30.0, 0.0, 400.0)]
    [TestCase(-30.0, 0.0, 0.0)]
    [TestCase(90.0, 90.0, 200.0)]
    [TestCase(0.0, 90.0, -400.0)]
    public void Screen_width_shows_sixty_degrees(double azimuth, double heading, double expected)
    {
        StarField.ScreenX(azimuth, heading, 400.0).Should().BeApproximately(expected, 0.5e-6);
    }

    [Test]
    public void Turning_right_by_ninety_degrees_moves_stars_one_and_a_half_widths_left()
    {
        double before = StarField.ScreenX(10.0, 0.0, 400.0);
        double after = StarField.ScreenX(10.0, 90.0, 400.0);

        (after - before).Should().BeApproximately(-1.5 * 400.0, 0.5e-6);
    }

    [Test]
    public void A_full_turn_brings_back_the_same_stars()
    {
        foreach (StarField.Star star in StarField.Stars)
        {
            StarField.ScreenX(star.Azimuth, 37.0 + 360.0, 400.0).Should().BeApproximately(StarField.ScreenX(star.Azimuth, 37.0, 400.0), 0.5e-6);
        }
    }

    [Test]
    public void Heading_keeps_counting_past_a_full_turn_without_harm()
    {
        StarField.ScreenX(100.0, 740.0, 400.0).Should().BeApproximately(StarField.ScreenX(100.0, 20.0, 400.0), 0.5e-6);
        StarField.ScreenX(100.0, -340.0, 400.0).Should().BeApproximately(StarField.ScreenX(100.0, 20.0, 400.0), 0.5e-6);
    }

    [TestCase(0.0, 0.0)]
    [TestCase(-4.0, 0.0)]
    [TestCase(-12.0, 1.0)]
    [TestCase(-30.0, 1.0)]
    public void Stars_fade_with_the_sun(double altitude, double expected)
    {
        StarField.Visibility(altitude).Should().BeApproximately(expected, 0.5e-6);
    }

    [TestCase(1920, 1080, 0.5)] // 16:9 landscape
    [TestCase(1000, 1000, 0.5)] // square
    [TestCase(1000, 1600, 2.0 / 3.0)] // start of the tall range
    [TestCase(900, 1950, 2.0 / 3.0)] // 9:19.5 portrait
    [TestCase(1000, 1300, 0.5 + (2.0 / 3.0 - 0.5) / 2.0)] // halfway between
    public void Horizon_follows_the_aspect_ratio(double width, double height, double expected)
    {
        SceneLayout layout = SceneLayout.Compute(width, height, SafeInsets.None);

        layout.HorizonY.Should().BeApproximately(expected * height, 0.5e-6);
    }

    [Test]
    public void Horizon_does_not_depend_on_the_safe_area()
    {
        SceneLayout plain = SceneLayout.Compute(844, 390, SafeInsets.None);
        SceneLayout notch = SceneLayout.Compute(844, 390, new SafeInsets(59, 0, 59, 21));

        notch.HorizonY.Should().BeApproximately(plain.HorizonY, 0.5e-6);
    }

    [Test]
    public void Horizon_never_jumps_while_a_window_is_resized()
    {
        double previous = SceneLayout.HorizonFraction(1000, 500);

        for (double height = 500; height <= 2200; height += 1)
        {
            double current = SceneLayout.HorizonFraction(1000, height);

            current.Should().BeGreaterThanOrEqualTo(previous, $"horizon moved back at height {height}");
            (current - previous).Should().BeLessThan(0.001, $"jump at height {height}");
            previous = current;
        }
    }

    [TestCase(390, 844)]
    [TestCase(1000, 1300)]
    [TestCase(1920, 1080)]
    public void Sky_gradient_reaches_the_horizon_and_the_ground_the_bottom(double width, double height)
    {
        SceneLayout layout = SceneLayout.Compute(width, height, SafeInsets.None);

        double scaled = DesignTokens.Size.SkyStopHorizon * layout.SkyStopScale * height;
        scaled.Should().BeApproximately(layout.HorizonY, 0.5e-6);
    }

    [Test]
    public void Layout_fills_a_phone_held_upright()
    {
        SceneLayout layout = SceneLayout.Compute(390, 844, SafeInsets.None);

        layout.Unit.Should().BeApproximately(390.0, 0.5e-6);
        layout.HorizonY.Should().BeApproximately(2.0 / 3.0 * 844, 0.5e-6);
        layout.SunArcRadius.Should().BeApproximately(0.423 * 390, 0.5e-6);
        layout.MoonArcRadius.Should().BeApproximately(0.346 * 390, 0.5e-6);
        layout.ClockFontSize.Should().BeApproximately(0.164 * 390, 0.5e-6);
        layout.TextTop.Should().BeApproximately(0.09 * 844, 0.5e-6);
    }

    public static IEnumerable<TestCaseData> Formats()
    {
        var notchPortrait = new SafeInsets(0, 59, 0, 34);
        var notchLandscape = new SafeInsets(59, 0, 59, 21);
        var androidBars = new SafeInsets(0, 32, 0, 24);

        return
        [
            Format(320, 480, SafeInsets.None),
            Format(320, 568, androidBars),
            Format(360, 640, androidBars),
            Format(390, 844, notchPortrait),
            Format(430, 932, notchPortrait),
            Format(844, 390, notchLandscape),
            Format(932, 430, notchLandscape),
            Format(640, 360, androidBars),
            Format(653, 280, androidBars),
            Format(768, 1024, SafeInsets.None),
            Format(820, 1180, new SafeInsets(0, 24, 0, 20)),
            Format(1024, 1366, SafeInsets.None),
            Format(1180, 820, new SafeInsets(0, 24, 0, 20)),
            Format(1366, 1024, SafeInsets.None),
            Format(400, 400, SafeInsets.None),
            Format(1280, 720, SafeInsets.None),
            Format(1920, 1080, SafeInsets.None),
            Format(2560, 1440, SafeInsets.None),
            Format(3840, 2160, SafeInsets.None),
            Format(3840, 600, SafeInsets.None),
            Format(500, 2000, SafeInsets.None),
        ];

        static TestCaseData Format(double width, double height, SafeInsets insets) => new(width, height, insets);
    }

    public static IEnumerable<TestCaseData> FormatsAndTinyWindows() =>
    [
        .. Formats(),
        new TestCaseData(200.0, 150.0, SafeInsets.None),
        new TestCaseData(160.0, 90.0, SafeInsets.None),
        new TestCaseData(120.0, 300.0, SafeInsets.None),
        new TestCaseData(80.0, 80.0, SafeInsets.None),
    ];

    [TestCaseSource(nameof(FormatsAndTinyWindows))]
    public void Arcs_are_exact_semicircles_of_the_design_radii(double width, double height, SafeInsets insets)
    {
        SceneLayout layout = SceneLayout.Compute(width, height, insets);

        PointD sunApex = ArcGeometry.PointAt(0.5, layout.CenterX, layout.HorizonY, layout.SunArcRadius, layout.SunArcRadius);
        PointD moonApex = ArcGeometry.PointAt(0.5, layout.CenterX, layout.HorizonY, layout.MoonArcRadius, layout.MoonArcRadius);

        layout.SunArcRadius.Should().BeApproximately(0.423 * layout.Unit, 0.5e-9);
        layout.MoonArcRadius.Should().BeApproximately(0.346 * layout.Unit, 0.5e-9);
        (layout.HorizonY - sunApex.Y).Should().BeApproximately(layout.SunArcRadius, 0.5e-9);
        (layout.HorizonY - moonApex.Y).Should().BeApproximately(layout.MoonArcRadius, 0.5e-9);
    }

    [TestCaseSource(nameof(FormatsAndTinyWindows))]
    public void Lower_paths_reach_three_tenths_of_the_radius(double width, double height, SafeInsets insets)
    {
        SceneLayout layout = SceneLayout.Compute(width, height, insets);

        PointD sunLowest = ArcGeometry.LowerPointAt(0.5, layout.CenterX, layout.HorizonY, layout.SunArcRadius, layout.SunLowerRadiusY);
        PointD moonLowest = ArcGeometry.LowerPointAt(0.5, layout.CenterX, layout.HorizonY, layout.MoonArcRadius, layout.MoonLowerRadiusY);

        (sunLowest.Y - layout.HorizonY).Should().BeApproximately(0.3 * layout.SunArcRadius, 0.5e-9);
        (moonLowest.Y - layout.HorizonY).Should().BeApproximately(0.3 * layout.MoonArcRadius, 0.5e-9);
    }

    [TestCaseSource(nameof(FormatsAndTinyWindows))]
    public void Arcs_grow_until_the_width_or_the_sky_ends(double width, double height, SafeInsets insets)
    {
        const double Rounding = 0.01;

        SceneLayout layout = SceneLayout.Compute(width, height, insets);

        double arcHeight = layout.SunArcRadius + layout.SunRadius;
        double sideLeft = width / 2.0 - Math.Max(insets.Left, insets.Right) - arcHeight;
        double skyLeft = layout.HorizonY - layout.TextBottom - arcHeight;

        bool fillsWidth = Math.Abs(sideLeft - 0.039 * layout.Unit) < Rounding;
        bool fillsSky = Math.Abs(skyLeft - DesignTokens.Size.TextToArcGap * layout.Unit) < Rounding;

        (fillsWidth || fillsSky).Should().BeTrue($"{sideLeft:F2} left at the sides and {skyLeft:F2} left in the sky");
    }

    [TestCaseSource(nameof(FormatsAndTinyWindows))]
    public void Layout_keeps_everything_on_screen(double width, double height, SafeInsets insets)
    {
        SceneLayout layout = SceneLayout.Compute(width, height, insets);

        double reach = layout.SunArcRadius + layout.SunRadius;
        (layout.CenterX - reach).Should().BeGreaterThanOrEqualTo(insets.Left, "sun leaves on the left");
        (layout.CenterX + reach).Should().BeLessThanOrEqualTo(width - insets.Right, "sun leaves on the right");

        double sunTop = layout.HorizonY - layout.SunArcRadius - layout.SunRadius;
        sunTop.Should().BeGreaterThanOrEqualTo(layout.TextBottom, $"sun at {sunTop:F1} reaches the text ending at {layout.TextBottom:F1}");
        (layout.MoonArcRadius + layout.MoonRadius).Should().BeLessThan(layout.SunArcRadius - layout.SunRadius, "moon on the sun's path");
        layout.TextTop.Should().BeGreaterThanOrEqualTo(insets.Top, "text enters the top safe area");
        (layout.TextHalfWidth * 2.0).Should().BeLessThanOrEqualTo(width - insets.Left - insets.Right + 0.001, "text wider than the safe area");
    }

    [TestCaseSource(nameof(FormatsAndTinyWindows))]
    public void Ground_holds_the_lower_paths_and_below_them_the_compass(double width, double height, SafeInsets insets)
    {
        SceneLayout layout = SceneLayout.Compute(width, height, insets);

        double sunBottom = layout.HorizonY + layout.SunLowerRadiusY + DesignTokens.Size.BelowHalo * layout.SunRadius;
        double moonBottom = layout.HorizonY + layout.MoonLowerRadiusY + DesignTokens.Size.BelowHalo * layout.MoonRadius;
        double compassTop = layout.CompassCenterY - layout.CompassReach;
        double compassBottom = layout.CompassCenterY + layout.CompassReach;

        layout.SunLowerRadiusY.Should().BeGreaterThan(0, "no room for the lower path");
        layout.CompassReach.Should().BeGreaterThan(0, "no room for the compass");
        sunBottom.Should().BeLessThanOrEqualTo(compassTop + 0.001, $"sun's halo ending at {sunBottom:F1} reaches the compass at {compassTop:F1}");
        moonBottom.Should().BeLessThanOrEqualTo(compassTop + 0.001, $"moon's halo ending at {moonBottom:F1} reaches the compass at {compassTop:F1}");
        compassBottom.Should().BeLessThanOrEqualTo(height - insets.Bottom + 0.001, "compass leaves the ground");
    }

    [TestCaseSource(nameof(Formats))]
    public void Text_lines_never_collide(double width, double height, SafeInsets insets)
    {
        SceneLayout layout = SceneLayout.Compute(width, height, insets);

        const double Rounding = 0.001;

        layout.RowPitch.Should().BeGreaterThanOrEqualTo(layout.RiseSetFontSize * 1.15 - Rounding, "rows");
        (layout.FirstRowCenterY - layout.ContextCenterY).Should().BeGreaterThanOrEqualTo((layout.RiseSetFontSize + layout.CaptionFontSize) * 0.575 - Rounding, "context line");
        (layout.ContextCenterY - layout.CaptionFontSize * 0.6).Should().BeGreaterThanOrEqualTo(layout.TextTop + layout.ClockFontSize - Rounding, "clock");
        (layout.DotsCenterY - layout.RowCenterY(DesignTokens.Size.TextRows - 1)).Should().BeGreaterThanOrEqualTo(layout.RiseSetFontSize - Rounding, "dots");
        layout.DotsCenterY.Should().BeLessThanOrEqualTo(layout.TextBottom, "dots below the block");
    }

    [TestCase(200, 150)]
    [TestCase(160, 90)]
    [TestCase(120, 300)]
    [TestCase(80, 80)]
    public void Very_small_windows_shrink_everything_alike(double width, double height)
    {
        SceneLayout layout = SceneLayout.Compute(width, height, SafeInsets.None);

        layout.HorizonY.Should().BeApproximately(SceneLayout.HorizonFraction(width, height) * height, 0.5e-6);
        layout.TextBottom.Should().BeLessThanOrEqualTo(layout.HorizonY, "text runs into the ground");
        (layout.TextHalfWidth * 2.0).Should().BeLessThanOrEqualTo(width + 0.001, "text wider than the window");
        layout.SunArcRadius.Should().BeGreaterThan(0, "no arcs left");
        (layout.CompassCenterY + layout.CompassReach).Should().BeLessThanOrEqualTo(height + 0.001, "compass leaves the window");
        (layout.ClockFontSize > 0 && layout.RiseSetFontSize > 0).Should().BeTrue("no text left");
    }

    [TestCaseSource(nameof(Formats))]
    public void Shifted_rings_stay_inside_the_ground(double width, double height, SafeInsets insets)
    {
        SceneLayout layout = SceneLayout.Compute(width, height, insets);
        double radius = layout.CompassRadius;
        double shift = 2.0 * radius * DesignTokens.Compass.LargestShift;
        double reach = radius * (1.0 + DesignTokens.Compass.SunMarker + DesignTokens.Compass.ArrowLength) + shift;

        (layout.CompassCenterY + reach).Should().BeLessThanOrEqualTo(height - insets.Bottom + 0.001, "below the ground");
        (layout.CompassCenterY - reach).Should().BeGreaterThanOrEqualTo(layout.HorizonY, "above the ground");
        (layout.CenterX + reach).Should().BeLessThanOrEqualTo(width, "off the side");
    }

    [Test]
    public void Text_keeps_its_minimum_size_in_windows_that_have_room()
    {
        SceneLayout.Compute(844, 390, new SafeInsets(59, 0, 59, 21)).RiseSetFontSize.Should().BeApproximately(11.0, 0.5e-6);
        SceneLayout.Compute(320, 480, SafeInsets.None).RiseSetFontSize.Should().BeApproximately(11.0, 0.5e-6);
    }

    [Test]
    public void Fine_details_grow_with_large_scenes_but_never_shrink()
    {
        SceneLayout.Compute(390, 844, SafeInsets.None).DetailScale.Should().BeApproximately(1.0, 0.5e-6);
        SceneLayout.Compute(844, 390, SafeInsets.None).DetailScale.Should().BeApproximately(1.0, 0.5e-6);
        SceneLayout.Compute(780, 1688, SafeInsets.None).DetailScale.Should().BeApproximately(2.0, 0.5e-6);
    }

    [Test]
    public void Stars_vanish_behind_the_text_and_nowhere_else()
    {
        SceneLayout layout = SceneLayout.Compute(900, 900, SafeInsets.None);

        layout.StarShare(layout.CenterX, layout.RowCenterY(2)).Should().Be(0.0);
        layout.StarShare(layout.CenterX, layout.TextBottom + 1).Should().Be(1.0);
        layout.StarShare(10, layout.RowCenterY(2)).Should().Be(1.0);
    }

    [Test]
    public void Stars_fade_gently_at_the_edge_of_the_text()
    {
        SceneLayout layout = SceneLayout.Compute(900, 900, SafeInsets.None);
        double edge = layout.CenterX + layout.TextHalfWidth;
        double fade = DesignTokens.Stars.TextFade * layout.TextHalfWidth;

        layout.StarShare(edge + fade / 2.0, layout.RowCenterY(2)).Should().BeApproximately(0.5, 0.5e-6);
    }

    [TestCase(0.0, 1.0, 0.0)]
    [TestCase(0.5, 0.0, 1.0)]
    [TestCase(1.0, -1.0, 0.0)]
    public void Lower_path_runs_from_right_through_the_lowest_point_to_left(double t, double expectedX, double expectedDepth)
    {
        PointD point = ArcGeometry.LowerPointAt(t, 0, 0, 1, 1);

        point.X.Should().BeApproximately(expectedX, 0.5e-9);
        point.Y.Should().BeApproximately(expectedDepth, 0.5e-9);
    }

    [TestCase(0.0, 0.0, 0.0, -1.0)]
    [TestCase(90.0, 0.0, 1.0, 0.0)]
    [TestCase(180.0, 0.0, 0.0, 1.0)]
    [TestCase(270.0, 0.0, -1.0, 0.0)]
    [TestCase(90.0, 90.0, 0.0, -1.0)]
    [TestCase(0.0, 90.0, -1.0, 0.0)]
    [TestCase(10.0, 370.0, 0.0, -1.0)]
    public void Compass_points_to_the_real_direction(double azimuth, double heading, double expectedX, double expectedY)
    {
        PointD point = CompassGeometry.PointAt(new PointD(0, 0), 1.0, azimuth, heading);

        point.X.Should().BeApproximately(expectedX, 0.5e-9);
        point.Y.Should().BeApproximately(expectedY, 0.5e-9);
    }

    [TestCase(0.0, 0.0)]
    [TestCase(90.0, 0.0)]
    [TestCase(200.0, 35.0)]
    [TestCase(350.0, 280.0)]
    public void An_arrow_head_points_away_from_the_centre_along_its_direction(double azimuth, double heading)
    {
        var center = new PointD(10.0, 20.0);
        const double Radius = 1.0;
        const double Length = 0.2;
        const double HalfWidth = 0.1;

        ArrowHead head = CompassGeometry.HeadAt(center, Radius, Length, HalfWidth, azimuth, heading);
        PointD middle = CompassGeometry.PointAt(center, Radius, azimuth, heading);

        head.Tip.Should().Be(CompassGeometry.PointAt(center, Radius + Length, azimuth, heading));
        ((head.Left.X + head.Right.X) / 2.0).Should().BeApproximately(middle.X, 0.5e-9);
        ((head.Left.Y + head.Right.Y) / 2.0).Should().BeApproximately(middle.Y, 0.5e-9);
        Distance(head.Left, head.Right).Should().BeApproximately(2.0 * HalfWidth, 0.5e-9);
        Distance(head.Tip, head.Right).Should().BeApproximately(Distance(head.Tip, head.Left), 0.5e-9);

        static double Distance(PointD a, PointD b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    }

    [Test]
    public void The_moon_arrow_keeps_clear_of_the_sun_marker()
    {
        double moonTip = DesignTokens.Compass.InnerRing + DesignTokens.Compass.MoonMarker + DesignTokens.Compass.ArrowLength;

        moonTip.Should().BeLessThan(1.0 - DesignTokens.Compass.SunMarker, "the moon's arrow runs into the sun's disc");
    }

    [Test]
    public void Arrows_lie_exactly_midway_between_markers_and_rings()
    {
        DesignTokens.Compass.ArrowOpacity.Should().BeApproximately(0.62, 0.5e-9);
    }

    [Test]
    public void Compass_cross_breaks_off_before_it_reaches_the_letters()
    {
        double nearestArm = DesignTokens.Compass.CrossGap;
        double letterCorner = DesignTokens.Compass.NorthLetterHeight / Math.Sqrt(2.0);

        nearestArm.Should().BeGreaterThan(letterCorner, "the cross turns with the device while letters stay upright, so it can meet any corner");
        (DesignTokens.Compass.LetterRing - nearestArm).Should().BeGreaterThan(DesignTokens.Compass.CrossHub, "no arm left between hub and letter");
    }

    [Test]
    public void The_sky_follows_ordinary_scrubbing_exactly()
    {
        const double Frame = 1.0 / 60.0;
        double shown = 0.0;

        for (int frame = 1; frame <= 120; frame++)
        {
            double target = frame * 1.0;
            shown = SkyPalette.LimitChange(shown, target, Frame);
            shown.Should().Be(target);
        }
    }

    [TestCase(5.0, 30.0)]
    [TestCase(30.0, 10.0)]
    public void Days_racing_past_do_not_flash_the_sky(double daysPerSecond, double largestSwing)
    {
        const double Frame = 1.0 / 60.0;
        const double DailyAltitudeRange = 50.0;

        double shown = 0.0;
        double lowest = double.MaxValue;
        double highest = double.MinValue;

        for (int frame = 1; frame <= 240; frame++)
        {
            double target = DailyAltitudeRange * Math.Sin(2.0 * Math.PI * daysPerSecond * frame * Frame + 0.3);
            shown = SkyPalette.LimitChange(shown, target, Frame);

            if (frame > 60)
            {
                lowest = Math.Min(lowest, shown);
                highest = Math.Max(highest, shown);
            }
        }

        (highest - lowest).Should().BeInRange(0.0, largestSwing);
    }

    [Test]
    public void Fastest_sky_change_stays_below_three_flashes_a_second()
    {
        double fullColourSwing = DesignTokens.Sky.Anchors[^1].SunAltitude - DesignTokens.Sky.Anchors[0].SunAltitude;
        double flashesPerSecond = DesignTokens.Scrub.FastestSkyChange / (2.0 * fullColourSwing);

        flashesPerSecond.Should().BeLessThan(3.0, $"{flashesPerSecond:F1} flashes per second");
    }

    [TestCase('N')]
    [TestCase('E')]
    [TestCase('O')]
    [TestCase('S')]
    [TestCase('W')]
    public void Compass_letters_fit_their_height_around_the_centre(char letter)
    {
        IReadOnlyList<CubicPath> contours = CompassGlyphs.Place(letter, new PointD(100, 50), 10.0);

        List<PointD> points = contours.SelectMany(Flatten).ToList();
        points.Min(p => p.Y).Should().BeInRange(44.0, 46.0);
        points.Max(p => p.Y).Should().BeInRange(54.0, 56.0);
        ((points.Min(p => p.X) + points.Max(p => p.X)) / 2.0).Should().BeApproximately(100.0, 0.5);
    }

    [Test]
    public void Hills_span_the_full_width()
    {
        CubicPath back = HillShapes.Back(1000, 500, 300);
        CubicPath front = HillShapes.Front(1000, 500, 300);

        back.Start.X.Should().Be(0.0);
        back.Segments[^1].End.X.Should().Be(1000.0);
        front.Start.X.Should().Be(0.0);
        front.Segments[^1].End.X.Should().Be(1000.0);
    }

    [Test]
    public void Hill_joins_are_smooth()
    {
        foreach (CubicPath path in new[] { HillShapes.Back(1, 1, 0), HillShapes.Front(1, 1, 0) })
        {
            PointD join = path.Segments[0].End;
            PointD before = path.Segments[0].Control2;
            PointD after = path.Segments[1].Control1;

            (after.X - join.X).Should().BeApproximately(join.X - before.X, 0.5e-6);
            (after.Y - join.Y).Should().BeApproximately(join.Y - before.Y, 0.5e-6);
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
