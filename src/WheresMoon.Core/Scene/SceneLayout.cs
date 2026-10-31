namespace WheresMoon.Core.Scene;

/// <summary>Distances from the window edges that content must keep clear of.</summary>
public readonly record struct SafeInsets(double Left, double Top, double Right, double Bottom)
{
    public static readonly SafeInsets None = new(0, 0, 0, 0);
}

/// <summary>
/// Sizes and positions for one window size, all in device independent units.
/// The horizon follows the aspect ratio alone. Text, arcs and compass then share what lies above and below it.
/// </summary>
public sealed record SceneLayout
{
    public required double Width { get; init; }
    public required double Height { get; init; }

    /// <summary>Scene unit: equals the width on tall screens, smaller when the sky is short.</summary>
    public required double Unit { get; init; }

    public required double CenterX { get; init; }
    public required double HorizonY { get; init; }

    public required double SunArcRadiusX { get; init; }
    public required double SunArcRadiusY { get; init; }
    public required double MoonArcRadiusX { get; init; }
    public required double MoonArcRadiusY { get; init; }

    /// <summary>Depth of the paths below the horizon, whose lines are not drawn.</summary>
    public required double SunLowerRadiusY { get; init; }
    public required double MoonLowerRadiusY { get; init; }

    public required double SunRadius { get; init; }
    public required double SunGlowRadius { get; init; }
    public required double MoonRadius { get; init; }
    public required double MoonGlowRadius { get; init; }

    public required double TextTop { get; init; }
    public required double TextBottom { get; init; }
    public required double TextHalfWidth { get; init; }
    public required double ClockFontSize { get; init; }
    public required double RiseSetFontSize { get; init; }
    public required double PhaseFontSize { get; init; }
    public required double CaptionFontSize { get; init; }

    /// <summary>Line below the clock for the place name and a date that is not today.</summary>
    public required double ContextCenterY { get; init; }

    public required double FirstRowCenterY { get; init; }
    public required double RowPitch { get; init; }
    public required double DotsCenterY { get; init; }

    /// <summary>Middle of the area below the lower paths, which the compass is centred in.</summary>
    public required double CompassCenterY { get; init; }

    /// <summary>Largest distance from <see cref="CompassCenterY"/> that the compass may reach, whatever it draws.</summary>
    public required double CompassReach { get; init; }

    /// <summary>Factor for line widths and star sizes. Never below 1, so they stay visible on small screens.</summary>
    public required double DetailScale { get; init; }

    /// <summary>Factor for sky stop positions, so that the horizon colour stays at the horizon line.</summary>
    public required double SkyStopScale { get; init; }

    public double CompassRadius => CompassReach / DesignTokens.Compass.Reach;

    public double RowCenterY(int row) => FirstRowCenterY + row * RowPitch;

    /// <summary>How much of a star shows at a point: nothing behind the text, where it would read like a stray dot.</summary>
    public double StarShare(double x, double y)
    {
        if (y < TextTop || y > TextBottom)
        {
            return 1.0;
        }

        double beside = Math.Abs(x - CenterX) - TextHalfWidth;
        return Math.Clamp(beside / (DesignTokens.Stars.TextFade * TextHalfWidth), 0.0, 1.0);
    }

    /// <summary>True for a height where a swipe turns the page instead of moving the time.</summary>
    public bool IsInSlider(double y) => y >= FirstRowCenterY - RowPitch / 2.0 && y <= TextBottom;

    /// <summary>Horizon position relative to the height, without steps as the window changes.</summary>
    public static double HorizonFraction(double width, double height)
    {
        double aspect = height / width;
        double t = Math.Clamp(
            (aspect - DesignTokens.Size.WideAspect) / (DesignTokens.Size.TallAspect - DesignTokens.Size.WideAspect),
            0.0,
            1.0);

        return DesignTokens.Size.HorizonWide + (DesignTokens.Size.HorizonTall - DesignTokens.Size.HorizonWide) * t;
    }

    public static SceneLayout Compute(double width, double height, SafeInsets insets)
    {
        // The arcs are centred on the window, so the wider inset limits both sides.
        double safeWidth = Math.Max(1.0, width - 2.0 * Math.Max(insets.Left, insets.Right));
        double horizonY = HorizonFraction(width, height) * height;

        double topFraction = height < DesignTokens.Size.CompactHeight
            ? DesignTokens.Size.TextTopFractionCompact
            : DesignTokens.Size.TextTopFraction;
        double textTop = Math.Max(insets.Top + DesignTokens.Size.TextTopFractionCompact * height, topFraction * height);
        double sky = Math.Max(1.0, horizonY - textTop);

        // A very small window keeps its proportions, so the minimum font has to give way as well.
        double smallestFont = DesignTokens.Size.SmallestFont * Math.Min(
            1.0,
            Math.Min(sky / DesignTokens.Size.ComfortableSky, safeWidth / DesignTokens.Size.ComfortableWidth));

        double unit = LargestUnit(safeWidth, sky, smallestFont);
        TextMetrics text = TextMetrics.For(unit, smallestFont);

        double sunRadius = DesignTokens.Size.SunRadius * unit;
        double sunArcX = safeWidth / 2.0 - (DesignTokens.Size.SunRadius + DesignTokens.Size.ArcSideMargin) * unit;
        double sunArcY = Math.Min(
            sky - text.Height - DesignTokens.Size.TextToArcGap * unit - sunRadius,
            sunArcX * DesignTokens.Size.MaximumArcTallness);

        double ground = Math.Max(0.0, height - insets.Bottom - horizonY);
        double lane = Math.Min(DesignTokens.Size.GroundLaneShare * ground, sunRadius + DesignTokens.Size.MaximumLowerArc * unit);
        double sunLower = Math.Max(0.0, lane - sunRadius);
        double compassArea = ground - lane;

        return new SceneLayout
        {
            Width = width,
            Height = height,
            Unit = unit,
            CenterX = width / 2.0,
            HorizonY = horizonY,

            SunArcRadiusX = sunArcX,
            SunArcRadiusY = sunArcY,
            MoonArcRadiusX = sunArcX * DesignTokens.Size.MoonArcWidthShare,
            MoonArcRadiusY = sunArcY * DesignTokens.Size.MoonArcHeightShare,
            SunLowerRadiusY = sunLower,
            MoonLowerRadiusY = sunLower * DesignTokens.Size.MoonLowerShare,

            SunRadius = sunRadius,
            SunGlowRadius = DesignTokens.Size.SunGlowRadius * unit,
            MoonRadius = DesignTokens.Size.MoonRadius * unit,
            MoonGlowRadius = DesignTokens.Size.MoonGlowRadius * unit,

            TextTop = textTop,
            TextBottom = textTop + text.Height,
            TextHalfWidth = DesignTokens.Size.TextHalfWidthEm * text.RowFont,
            ClockFontSize = DesignTokens.Size.ClockFont * unit,
            RiseSetFontSize = text.RowFont,
            PhaseFontSize = text.SecondaryFont,
            CaptionFontSize = text.CaptionFont,
            ContextCenterY = textTop + text.ContextCenter,
            FirstRowCenterY = textTop + text.FirstRowCenter,
            RowPitch = text.RowPitch,
            DotsCenterY = textTop + text.DotsCenter,

            CompassCenterY = horizonY + lane + compassArea / 2.0,
            CompassReach = Math.Min(compassArea / 2.0, Math.Min(safeWidth / 2.0, DesignTokens.Size.CompassLargestReach * unit)),

            DetailScale = Math.Max(1.0, unit / DesignTokens.Size.ReferenceWidth),
            SkyStopScale = horizonY / height / DesignTokens.Size.SkyStopHorizon,
        };
    }

    /// <summary>Largest unit that fits text and the smallest arc into the sky. Minimum fonts make this non-linear.</summary>
    private static double LargestUnit(double safeWidth, double sky, double smallestFont)
    {
        const int Steps = 24;

        if (TotalHeight(safeWidth) <= sky)
        {
            return safeWidth;
        }

        double low = 0.0;
        double high = safeWidth;

        for (int i = 0; i < Steps; i++)
        {
            double middle = (low + high) / 2.0;
            if (TotalHeight(middle) <= sky)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }

        return low;

        double TotalHeight(double unit) =>
            TextMetrics.For(unit, smallestFont).Height
            + (DesignTokens.Size.TextToArcGap + DesignTokens.Size.MinimumArcHeight) * unit;
    }

    /// <summary>Text block geometry relative to its top. Fonts stop shrinking at the minimum, so the gaps must not.</summary>
    private readonly record struct TextMetrics(
        double RowFont,
        double SecondaryFont,
        double CaptionFont,
        double ContextCenter,
        double FirstRowCenter,
        double RowPitch,
        double DotsCenter,
        double Height)
    {
        public static TextMetrics For(double unit, double smallestFont)
        {
            const double HalfLine = 0.6;
            const double FullLine = 1.15;

            // Distance from the last row to the dots, which sit lower than text does and need more room.
            const double DotsLine = 1.05;

            double rowFont = Math.Max(smallestFont, DesignTokens.Size.RiseSetFont * unit);
            double secondaryFont = Math.Max(smallestFont, DesignTokens.Size.PhaseFont * unit);
            double captionFont = Math.Max(smallestFont, DesignTokens.Size.CaptionFont * unit);

            double context = DesignTokens.Size.ClockFont * unit
                           + Math.Max(DesignTokens.Size.ClockToContext * unit, HalfLine * captionFont);
            double firstRow = context + Math.Max(DesignTokens.Size.ContextToRows * unit, HalfLine * (captionFont + rowFont));
            double pitch = Math.Max(DesignTokens.Size.RowPitch * unit, FullLine * rowFont);
            double dots = firstRow + (DesignTokens.Size.TextRows - 1) * pitch
                        + Math.Max(DesignTokens.Size.RowsToDots * unit, DotsLine * rowFont);
            double end = dots + Math.Max(DesignTokens.Size.DotsToBlockEnd * unit, HalfLine * smallestFont);

            return new TextMetrics(rowFont, secondaryFont, captionFont, context, firstRow, pitch, dots, end);
        }
    }
}
