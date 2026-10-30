namespace WheresMoon.Core.Scene;

/// <summary>One colour stop of a vertical sky gradient. Position is relative to the full height on the reference format.</summary>
public readonly record struct SkyStop(double Position, Rgba Color);

/// <summary>Sky gradient that applies at a given sun altitude.</summary>
public sealed record SkyAnchor(double SunAltitude, IReadOnlyList<SkyStop> Stops);

/// <summary>Vertical gradient of one hill.</summary>
public readonly record struct HillColors(Rgba Top, Rgba Bottom);

/// <summary>Central definition of every colour, size and anchor of the scene.</summary>
public static class DesignTokens
{
    // Sizes are multiples of the scene unit U, which equals the width on the reference format.

    public static class Size
    {
        public const double SunArcRadiusX = 0.423;
        public const double SunArcRadiusY = 0.538;
        public const double MoonArcRadiusX = 0.346;
        public const double MoonArcRadiusY = 0.423;

        public const double SunRadius = 0.038;
        public const double SunGlowRadius = 0.123;
        public const double MoonRadius = 0.032;
        public const double MoonGlowRadius = 0.113;

        public const double ClockFont = 0.164;
        public const double RiseSetFont = 0.036;
        public const double PhaseFont = 0.033;
        public const double SmallestFont = 11.0;

        /// <summary>Distances between the text lines' centres, measured on the reference images.</summary>
        public const double ClockToLine1 = 0.0616;
        public const double Line1ToLine2 = 0.0641;
        public const double Line2ToPhase = 0.0783;
        public const double PhaseToBlockEnd = 0.024;

        /// <summary>Half the width of the longest text line.</summary>
        public const double TextHalfWidth = 0.46;

        /// <summary>Smallest gap between text block and the sun disc at the arc's apex.</summary>
        public const double TextToArcGap = 0.02;

        /// <summary>Ground that must stay visible below the horizon line.</summary>
        public const double MinimumGround = 0.16;

        /// <summary>Text block offset below the top safe area, relative to the height.</summary>
        public const double TextTopFraction = 0.09;

        /// <summary>The same offset once the window is too short to afford it.</summary>
        public const double TextTopFractionCompact = 0.03;

        /// <summary>Below this height in device independent units the compact offset applies.</summary>
        public const double CompactHeight = 520.0;

        /// <summary>Width of the reference format, from which fine details such as lines scale up.</summary>
        public const double ReferenceWidth = 390.0;

        /// <summary>Horizon line on the reference format, relative to the height.</summary>
        public const double HorizonFraction = 0.62;

        /// <summary>How much wider than proportional the arcs may become in wide windows.</summary>
        public const double MaximumArcWidening = 1.5;

        public const double ArcStrokeWidth = 1.2;
        public const double MoonArcDash = 2.0;
        public const double MoonArcGap = 5.0;
    }

    public static class Opacity
    {
        public const double SunArcDay = 0.28;
        public const double SunArcNight = 0.16;
        public const double MoonArcDay = 0.18;
        public const double MoonArcNight = 0.22;
        public const double Earthshine = 0.11;
        public const double PhaseText = 0.80;
    }

    public static class Sun
    {
        public static readonly Rgba Disc = Rgba.FromHex("#FFFAF0");
        public static readonly Rgba GlowInner = Rgba.FromHex("#FFF6DC").WithAlpha(0.75);
        public static readonly Rgba GlowMiddle = Rgba.FromHex("#FFE9B0").WithAlpha(0.25);
        public const double GlowMiddlePosition = 0.45;

        /// <summary>Glow tint close to the horizon.</summary>
        public static readonly Rgba GlowWarm = Rgba.FromHex("#FFB56B");

        /// <summary>Sun altitude at which the glow starts warming up.</summary>
        public const double GlowWarmAltitude = 12.0;

        /// <summary>Largest share of the warm tint, reached at the horizon.</summary>
        public const double GlowWarmAmount = 0.65;
    }

    public static class Moon
    {
        public static readonly Rgba Lit = Rgba.FromHex("#F7F5EF");
        public static readonly Rgba Earthshine = Rgba.FromHex("#FFFFFF").WithAlpha(Opacity.Earthshine);
        public static readonly Rgba Glow = Rgba.FromHex("#E8ECFF").WithAlpha(0.35);

        /// <summary>The glow is absent with the sun at this altitude and full at <see cref="GlowFullAltitude"/>.</summary>
        public const double GlowStartAltitude = 0.0;
        public const double GlowFullAltitude = -6.0;
    }

    public static class Stars
    {
        public const int Count = 15;
        public const uint Seed = 0x4D4F4F4E;
        public const double MinRadius = 0.7;
        public const double MaxRadius = 1.1;
        public const double MinOpacity = 0.35;
        public const double MaxOpacity = 0.80;

        /// <summary>Stars are invisible with the sun at this altitude and full at <see cref="FullAltitude"/>.</summary>
        public const double StartAltitude = -4.0;
        public const double FullAltitude = -12.0;

        /// <summary>Vertical range relative to the distance from the top to the horizon.</summary>
        public const double TopLimit = 0.02;
        public const double BottomLimit = 0.92;
    }

    public static class Sky
    {
        private const double MidPosition = 0.38;
        private const double LowPosition = 0.62;
        private const double HorizonPosition = 0.66;

        /// <summary>How far the third stop lies from the middle towards the horizon colour.</summary>
        private const double LowStopMix = 0.6;

        /// <summary>Anchors ordered by sun altitude. Colours between them are interpolated.</summary>
        public static readonly IReadOnlyList<SkyAnchor> Anchors =
        [
            new(-18.0,
            [
                new(0.00, Rgba.FromHex("#070D24")),
                new(0.40, Rgba.FromHex("#111C42")),
                new(0.60, Rgba.FromHex("#26365F")),
                new(0.66, Rgba.FromHex("#3C4A72")),
            ]),
            Blended(-13.0, "#0A1230", "#1B2750", "#4C5078"),
            Blended(-8.0, "#17224F", "#46508A", "#A5748A"),
            Blended(-3.5, "#2A3F7A", "#7E7BB0", "#F2A65A"),
            Blended(2.5, "#3B6AB0", "#8FB3DA", "#F6C27A"),
            new(10.0,
            [
                new(0.00, Rgba.FromHex("#2F6FBF")),
                new(0.38, Rgba.FromHex("#5B97D6")),
                new(0.62, Rgba.FromHex("#A9CDEA")),
                new(0.66, Rgba.FromHex("#DBE9F2")),
            ]),
        ];

        /// <summary>Tint added to the lower sky in the evening.</summary>
        public static readonly Rgba EveningTint = Rgba.FromHex("#FF6A3D");

        /// <summary>Largest share of the tint per stop, from top to horizon.</summary>
        public static readonly IReadOnlyList<double> EveningAmounts = [0.0, 0.05, 0.10, 0.14];

        /// <summary>Sun altitudes between which the evening tint applies fully, and where it has faded out.</summary>
        public const double EveningFullLow = -6.0;
        public const double EveningFullHigh = 3.0;
        public const double EveningFadeLow = -14.0;
        public const double EveningFadeHigh = 12.0;

        private static SkyAnchor Blended(double altitude, string top, string middle, string horizon)
        {
            Rgba mid = Rgba.FromHex(middle);
            Rgba low = Rgba.FromHex(horizon);

            return new SkyAnchor(altitude,
            [
                new(0.00, Rgba.FromHex(top)),
                new(MidPosition, mid),
                new(LowPosition, Rgba.Mix(mid, low, LowStopMix)),
                new(HorizonPosition, low),
            ]);
        }
    }

    public static class Ground
    {
        public static readonly HillColors BackDay = new(Rgba.FromHex("#6F8FA6"), Rgba.FromHex("#3F5C70"));
        public static readonly HillColors BackNight = new(Rgba.FromHex("#1C2747"), Rgba.FromHex("#121A33"));
        public static readonly HillColors FrontDay = new(Rgba.FromHex("#3A5568"), Rgba.FromHex("#1D2F3D"));
        public static readonly HillColors FrontNight = new(Rgba.FromHex("#0F1630"), Rgba.FromHex("#080D1F"));

        /// <summary>Share of the sky's horizon colour that reaches the ground: full on the crests, less at the foot.</summary>
        public const double SkyShareCrest = 0.25;
        public const double SkyShareFoot = 0.12;

        /// <summary>Sun altitudes between which the ground goes from its night to its day colours.</summary>
        public const double NightAltitude = -10.0;
        public const double DayAltitude = 8.0;
    }
}
