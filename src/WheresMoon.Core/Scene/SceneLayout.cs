namespace WheresMoon.Core.Scene;

/// <summary>Distances from the window edges that content must keep clear of.</summary>
public readonly record struct SafeInsets(double Left, double Top, double Right, double Bottom)
{
    public static readonly SafeInsets None = new(0, 0, 0, 0);
}

/// <summary>
/// Sizes and positions for one window size, all in device independent units.
/// Everything is derived from the scene unit <see cref="Unit"/>.
/// </summary>
public sealed record SceneLayout
{
    public required double Width { get; init; }
    public required double Height { get; init; }

    /// <summary>The reference's W: equals the width on tall screens, smaller when the height is short.</summary>
    public required double Unit { get; init; }

    public required double CenterX { get; init; }
    public required double HorizonY { get; init; }

    public required double SunArcRadiusX { get; init; }
    public required double SunArcRadiusY { get; init; }
    public required double MoonArcRadiusX { get; init; }
    public required double MoonArcRadiusY { get; init; }

    public required double SunRadius { get; init; }
    public required double SunGlowRadius { get; init; }
    public required double MoonRadius { get; init; }
    public required double MoonGlowRadius { get; init; }

    public required double TextTop { get; init; }
    public required double TextBottom { get; init; }
    public required double ClockFontSize { get; init; }
    public required double RiseSetFontSize { get; init; }
    public required double PhaseFontSize { get; init; }
    public required double RiseSetLine1CenterY { get; init; }
    public required double RiseSetLine2CenterY { get; init; }
    public required double PhaseLineCenterY { get; init; }

    /// <summary>Factor for line widths and star sizes. Never below 1, so they stay visible on small screens.</summary>
    public required double DetailScale { get; init; }

    /// <summary>Factor for sky stop positions, so that the horizon colour stays at the horizon line.</summary>
    public required double SkyStopScale { get; init; }

    /// <summary>True for a point inside the area the text lines can occupy.</summary>
    public bool IsInTextBlock(double x, double y) =>
        y >= TextTop && y <= TextBottom && Math.Abs(x - CenterX) <= DesignTokens.Size.TextHalfWidth * Unit;

    public static SceneLayout Compute(double width, double height, SafeInsets insets)
    {
        // The arcs are centred on the window, so the wider inset limits both sides.
        double safeWidth = Math.Max(1.0, width - 2.0 * Math.Max(insets.Left, insets.Right));

        double topFraction = height < DesignTokens.Size.CompactHeight
            ? DesignTokens.Size.TextTopFractionCompact
            : DesignTokens.Size.TextTopFraction;
        double textTop = insets.Top + topFraction * height;

        double available = height - insets.Bottom - textTop;
        double unit = LargestUnit(safeWidth, available);
        TextMetrics text = TextMetrics.For(unit);

        double horizonY = Math.Max(
            DesignTokens.Size.HorizonFraction * height,
            textTop + text.Height + (DesignTokens.Size.TextToArcGap + SunTopAboveHorizon) * unit);

        // Height limited windows leave free sky at the sides, which the arcs may take up by getting flatter.
        double widening = Math.Clamp(safeWidth / unit, 1.0, DesignTokens.Size.MaximumArcWidening);

        return new SceneLayout
        {
            Width = width,
            Height = height,
            Unit = unit,
            CenterX = width / 2.0,
            HorizonY = horizonY,

            SunArcRadiusX = DesignTokens.Size.SunArcRadiusX * unit * widening,
            SunArcRadiusY = DesignTokens.Size.SunArcRadiusY * unit,
            MoonArcRadiusX = DesignTokens.Size.MoonArcRadiusX * unit * widening,
            MoonArcRadiusY = DesignTokens.Size.MoonArcRadiusY * unit,

            SunRadius = DesignTokens.Size.SunRadius * unit,
            SunGlowRadius = DesignTokens.Size.SunGlowRadius * unit,
            MoonRadius = DesignTokens.Size.MoonRadius * unit,
            MoonGlowRadius = DesignTokens.Size.MoonGlowRadius * unit,

            TextTop = textTop,
            TextBottom = textTop + text.Height,
            ClockFontSize = DesignTokens.Size.ClockFont * unit,
            RiseSetFontSize = text.RiseSetFont,
            PhaseFontSize = text.PhaseFont,
            RiseSetLine1CenterY = textTop + text.Line1Center,
            RiseSetLine2CenterY = textTop + text.Line2Center,
            PhaseLineCenterY = textTop + text.PhaseCenter,

            DetailScale = Math.Max(1.0, unit / DesignTokens.Size.ReferenceWidth),
            SkyStopScale = horizonY / height / DesignTokens.Size.HorizonFraction,
        };
    }

    private static double SunTopAboveHorizon => DesignTokens.Size.SunArcRadiusY + DesignTokens.Size.SunRadius;

    /// <summary>Largest unit that fits text, arcs and ground into the height. Minimum fonts make this non-linear.</summary>
    private static double LargestUnit(double safeWidth, double available)
    {
        const int Steps = 24;

        if (TotalHeight(safeWidth) <= available)
        {
            return safeWidth;
        }

        double low = 1.0;
        double high = safeWidth;

        for (int i = 0; i < Steps; i++)
        {
            double middle = (low + high) / 2.0;
            if (TotalHeight(middle) <= available)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }

        return low;

        static double TotalHeight(double unit) =>
            TextMetrics.For(unit).Height
            + (DesignTokens.Size.TextToArcGap + SunTopAboveHorizon + DesignTokens.Size.MinimumGround) * unit;
    }

    /// <summary>Text block geometry relative to its top. Fonts stop shrinking at the minimum, so the gaps must not.</summary>
    private readonly record struct TextMetrics(
        double RiseSetFont, double PhaseFont, double Line1Center, double Line2Center, double PhaseCenter, double Height)
    {
        public static TextMetrics For(double unit)
        {
            const double HalfLine = 0.6;
            const double FullLine = 1.15;

            double riseSetFont = Math.Max(DesignTokens.Size.SmallestFont, DesignTokens.Size.RiseSetFont * unit);
            double phaseFont = Math.Max(DesignTokens.Size.SmallestFont, DesignTokens.Size.PhaseFont * unit);

            double line1 = DesignTokens.Size.ClockFont * unit
                         + Math.Max(DesignTokens.Size.ClockToLine1 * unit, HalfLine * riseSetFont);
            double line2 = line1 + Math.Max(DesignTokens.Size.Line1ToLine2 * unit, FullLine * riseSetFont);
            double phase = line2 + Math.Max(DesignTokens.Size.Line2ToPhase * unit, HalfLine * (riseSetFont + phaseFont));
            double end = phase + Math.Max(DesignTokens.Size.PhaseToBlockEnd * unit, HalfLine * phaseFont);

            return new TextMetrics(riseSetFont, phaseFont, line1, line2, phase, end);
        }
    }
}
