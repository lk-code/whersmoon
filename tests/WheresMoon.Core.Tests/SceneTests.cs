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

        Assert.Equal(90, stars.Count);
        Assert.All(stars, star =>
        {
            Assert.InRange(star.Azimuth, 0.0, 360.0);
            Assert.InRange(star.Y, DesignTokens.Stars.TopLimit, DesignTokens.Stars.BottomLimit);
            Assert.InRange(star.Radius, 0.7, 1.1);
            Assert.InRange(star.Opacity, 0.35, 0.80);
        });

        // Pinned, because a moving star field would be visible to anyone comparing screenshots.
        Assert.Equal(1.67143, stars[0].Azimuth, 4);
    }

    [Fact]
    public void Star_density_is_even_around_the_whole_band()
    {
        for (double start = 0.0; start < 360.0; start += 5.0)
        {
            int count = StarField.Stars.Count(star => Math.Abs(((star.Azimuth - start + 540.0) % 360.0) - 180.0) <= 30.0);

            Assert.InRange(count, 13, 17);
        }
    }

    [Theory]
    [InlineData(0.0, 0.0, 200.0)]
    [InlineData(30.0, 0.0, 400.0)]
    [InlineData(-30.0, 0.0, 0.0)]
    [InlineData(90.0, 90.0, 200.0)]
    [InlineData(0.0, 90.0, -400.0)]
    public void Screen_width_shows_sixty_degrees(double azimuth, double heading, double expected)
    {
        Assert.Equal(expected, StarField.ScreenX(azimuth, heading, 400.0), 6);
    }

    [Fact]
    public void Turning_right_by_ninety_degrees_moves_stars_one_and_a_half_widths_left()
    {
        double before = StarField.ScreenX(10.0, 0.0, 400.0);
        double after = StarField.ScreenX(10.0, 90.0, 400.0);

        Assert.Equal(-1.5 * 400.0, after - before, 6);
    }

    [Fact]
    public void A_full_turn_brings_back_the_same_stars()
    {
        foreach (StarField.Star star in StarField.Stars)
        {
            Assert.Equal(StarField.ScreenX(star.Azimuth, 37.0, 400.0), StarField.ScreenX(star.Azimuth, 37.0 + 360.0, 400.0), 6);
        }
    }

    [Fact]
    public void Heading_keeps_counting_past_a_full_turn_without_harm()
    {
        Assert.Equal(StarField.ScreenX(100.0, 20.0, 400.0), StarField.ScreenX(100.0, 740.0, 400.0), 6);
        Assert.Equal(StarField.ScreenX(100.0, 20.0, 400.0), StarField.ScreenX(100.0, -340.0, 400.0), 6);
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

    [Theory]
    [InlineData(1920, 1080, 0.5)] // 16:9 landscape
    [InlineData(1000, 1000, 0.5)] // square
    [InlineData(1000, 1600, 2.0 / 3.0)] // start of the tall range
    [InlineData(900, 1950, 2.0 / 3.0)] // 9:19.5 portrait
    [InlineData(1000, 1300, 0.5 + (2.0 / 3.0 - 0.5) / 2.0)] // halfway between
    public void Horizon_follows_the_aspect_ratio(double width, double height, double expected)
    {
        SceneLayout layout = SceneLayout.Compute(width, height, SafeInsets.None);

        Assert.Equal(expected * height, layout.HorizonY, 6);
    }

    [Fact]
    public void Horizon_does_not_depend_on_the_safe_area()
    {
        SceneLayout plain = SceneLayout.Compute(844, 390, SafeInsets.None);
        SceneLayout notch = SceneLayout.Compute(844, 390, new SafeInsets(59, 0, 59, 21));

        Assert.Equal(plain.HorizonY, notch.HorizonY, 6);
    }

    [Fact]
    public void Horizon_never_jumps_while_a_window_is_resized()
    {
        double previous = SceneLayout.HorizonFraction(1000, 500);

        for (double height = 500; height <= 2200; height += 1)
        {
            double current = SceneLayout.HorizonFraction(1000, height);

            Assert.True(current >= previous, $"horizon moved back at height {height}");
            Assert.True(current - previous < 0.001, $"jump at height {height}");
            previous = current;
        }
    }

    [Theory]
    [InlineData(390, 844)]
    [InlineData(1000, 1300)]
    [InlineData(1920, 1080)]
    public void Sky_gradient_reaches_the_horizon_and_the_ground_the_bottom(double width, double height)
    {
        SceneLayout layout = SceneLayout.Compute(width, height, SafeInsets.None);

        double scaled = DesignTokens.Size.SkyStopHorizon * layout.SkyStopScale * height;
        Assert.Equal(layout.HorizonY, scaled, 6);
    }

    [Fact]
    public void Layout_fills_a_phone_held_upright()
    {
        SceneLayout layout = SceneLayout.Compute(390, 844, SafeInsets.None);

        Assert.Equal(390.0, layout.Unit, 6);
        Assert.Equal(2.0 / 3.0 * 844, layout.HorizonY, 6);
        Assert.Equal(0.423 * 390, layout.SunArcRadiusX, 6);
        Assert.Equal(0.164 * 390, layout.ClockFontSize, 6);
        Assert.Equal(0.09 * 844, layout.TextTop, 6);
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
        Assert.True(layout.TextHalfWidth * 2.0 <= width - insets.Left - insets.Right + 0.001, "text wider than the safe area");
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void Ground_holds_the_lower_paths_and_below_them_the_compass(double width, double height, SafeInsets insets)
    {
        SceneLayout layout = SceneLayout.Compute(width, height, insets);

        double pathBottom = layout.HorizonY + layout.SunLowerRadiusY + layout.SunRadius;
        double compassTop = layout.CompassCenterY - layout.CompassReach;
        double compassBottom = layout.CompassCenterY + layout.CompassReach;

        Assert.True(layout.SunLowerRadiusY > 0, "no room for the lower path");
        Assert.True(layout.CompassReach > 0, "no room for the compass");
        Assert.True(pathBottom <= compassTop + 0.001, $"lower path ending at {pathBottom:F1} reaches the compass at {compassTop:F1}");
        Assert.True(compassBottom <= height - insets.Bottom + 0.001, "compass leaves the ground");
        Assert.True(layout.MoonLowerRadiusY + layout.MoonRadius <= layout.SunLowerRadiusY + layout.SunRadius, "moon below the sun's path");
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void Text_lines_never_collide(double width, double height, SafeInsets insets)
    {
        SceneLayout layout = SceneLayout.Compute(width, height, insets);

        Assert.True(layout.RowPitch >= layout.RiseSetFontSize * 1.15, "rows");
        Assert.True(layout.FirstRowCenterY - layout.ContextCenterY >= (layout.RiseSetFontSize + layout.CaptionFontSize) * 0.575, "context line");
        Assert.True(layout.ContextCenterY - layout.CaptionFontSize * 0.6 >= layout.TextTop + layout.ClockFontSize, "clock");
        Assert.True(layout.DotsCenterY - layout.RowCenterY(DesignTokens.Size.TextRows - 1) >= layout.RiseSetFontSize * 0.6 - 0.001, "dots");
        Assert.True(layout.DotsCenterY <= layout.TextBottom, "dots below the block");
    }

    [Theory]
    [InlineData(200, 150)]
    [InlineData(160, 90)]
    [InlineData(120, 300)]
    [InlineData(80, 80)]
    public void Very_small_windows_shrink_everything_alike(double width, double height)
    {
        SceneLayout layout = SceneLayout.Compute(width, height, SafeInsets.None);

        Assert.Equal(SceneLayout.HorizonFraction(width, height) * height, layout.HorizonY, 6);
        Assert.True(layout.TextBottom <= layout.HorizonY, "text runs into the ground");
        Assert.True(layout.TextHalfWidth * 2.0 <= width + 0.001, "text wider than the window");
        Assert.True(layout.SunArcRadiusY > 0 && layout.SunArcRadiusX > 0, "no arcs left");
        Assert.True(layout.CompassCenterY + layout.CompassReach <= height + 0.001, "compass leaves the window");
        Assert.True(layout.ClockFontSize > 0 && layout.RiseSetFontSize > 0, "no text left");
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void Shifted_rings_stay_inside_the_ground(double width, double height, SafeInsets insets)
    {
        SceneLayout layout = SceneLayout.Compute(width, height, insets);
        double radius = layout.CompassRadius;
        double shift = 2.0 * radius * DesignTokens.Compass.LargestShift;
        double reach = radius * (1.0 + DesignTokens.Compass.SunMarker) + shift;

        Assert.True(layout.CompassCenterY + reach <= height - insets.Bottom + 0.001, "below the ground");
        Assert.True(layout.CompassCenterY - reach >= layout.HorizonY, "above the ground");
        Assert.True(layout.CenterX + reach <= width, "off the side");
    }

    [Fact]
    public void Text_keeps_its_minimum_size_in_windows_that_have_room()
    {
        Assert.Equal(11.0, SceneLayout.Compute(844, 390, new SafeInsets(59, 0, 59, 21)).RiseSetFontSize, 6);
        Assert.Equal(11.0, SceneLayout.Compute(320, 480, SafeInsets.None).RiseSetFontSize, 6);
    }

    [Fact]
    public void Fine_details_grow_with_large_scenes_but_never_shrink()
    {
        Assert.Equal(1.0, SceneLayout.Compute(390, 844, SafeInsets.None).DetailScale, 6);
        Assert.Equal(1.0, SceneLayout.Compute(844, 390, SafeInsets.None).DetailScale, 6);
        Assert.Equal(2.0, SceneLayout.Compute(780, 1688, SafeInsets.None).DetailScale, 6);
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void Arcs_use_the_width_unless_the_sky_is_too_narrow_for_it(double width, double height, SafeInsets insets)
    {
        SceneLayout layout = SceneLayout.Compute(width, height, insets);
        double side = Math.Max(insets.Left, insets.Right);
        double margin = DesignTokens.Size.ArcSideMargin * layout.Unit + layout.SunRadius;

        Assert.Equal(width / 2.0 - side - margin, layout.SunArcRadiusX, 6);
    }

    [Fact]
    public void Narrow_windows_do_not_turn_the_arcs_into_a_spike()
    {
        SceneLayout layout = SceneLayout.Compute(500, 2000, SafeInsets.None);

        Assert.True(layout.SunArcRadiusY <= 1.5 * layout.SunArcRadiusX + 0.001);
    }

    [Fact]
    public void Stars_vanish_behind_the_text_and_nowhere_else()
    {
        SceneLayout layout = SceneLayout.Compute(900, 900, SafeInsets.None);

        Assert.Equal(0.0, layout.StarShare(layout.CenterX, layout.RowCenterY(2)));
        Assert.Equal(1.0, layout.StarShare(layout.CenterX, layout.TextBottom + 1));
        Assert.Equal(1.0, layout.StarShare(10, layout.RowCenterY(2)));
    }

    [Fact]
    public void Stars_fade_gently_at_the_edge_of_the_text()
    {
        SceneLayout layout = SceneLayout.Compute(900, 900, SafeInsets.None);
        double edge = layout.CenterX + layout.TextHalfWidth;
        double fade = DesignTokens.Stars.TextFade * layout.TextHalfWidth;

        Assert.Equal(0.5, layout.StarShare(edge + fade / 2.0, layout.RowCenterY(2)), 6);
    }

    [Theory]
    [InlineData(0.0, 1.0, 0.0)]
    [InlineData(0.5, 0.0, 1.0)]
    [InlineData(1.0, -1.0, 0.0)]
    public void Lower_path_runs_from_right_through_the_lowest_point_to_left(double t, double expectedX, double expectedDepth)
    {
        PointD point = ArcGeometry.LowerPointAt(t, 0, 0, 1, 1);

        Assert.Equal(expectedX, point.X, 9);
        Assert.Equal(expectedDepth, point.Y, 9);
    }

    [Theory]
    [InlineData(0.0, 0.0, 0.0, -1.0)]
    [InlineData(90.0, 0.0, 1.0, 0.0)]
    [InlineData(180.0, 0.0, 0.0, 1.0)]
    [InlineData(270.0, 0.0, -1.0, 0.0)]
    [InlineData(90.0, 90.0, 0.0, -1.0)]
    [InlineData(0.0, 90.0, -1.0, 0.0)]
    [InlineData(10.0, 370.0, 0.0, -1.0)]
    public void Compass_points_to_the_real_direction(double azimuth, double heading, double expectedX, double expectedY)
    {
        PointD point = CompassGeometry.PointAt(new PointD(0, 0), 1.0, azimuth, heading);

        Assert.Equal(expectedX, point.X, 9);
        Assert.Equal(expectedY, point.Y, 9);
    }

    [Theory]
    [InlineData('N')]
    [InlineData('E')]
    [InlineData('O')]
    [InlineData('S')]
    [InlineData('W')]
    public void Compass_letters_fit_their_height_around_the_centre(char letter)
    {
        IReadOnlyList<CubicPath> contours = CompassGlyphs.Place(letter, new PointD(100, 50), 10.0);

        List<PointD> points = contours.SelectMany(Flatten).ToList();
        Assert.InRange(points.Min(p => p.Y), 44.0, 46.0);
        Assert.InRange(points.Max(p => p.Y), 54.0, 56.0);
        Assert.Equal(100.0, (points.Min(p => p.X) + points.Max(p => p.X)) / 2.0, 0);
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
