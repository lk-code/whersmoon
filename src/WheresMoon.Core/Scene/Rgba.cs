using System.Globalization;

namespace WheresMoon.Core.Scene;

/// <summary>sRGB colour with channels from 0 to 1. Independent of any UI framework.</summary>
public readonly record struct Rgba(double R, double G, double B, double A = 1.0)
{
    public static Rgba FromHex(string hex)
    {
        ReadOnlySpan<char> digits = hex.AsSpan().TrimStart('#');
        return new Rgba(Channel(digits[..2]), Channel(digits[2..4]), Channel(digits[4..6]));

        static double Channel(ReadOnlySpan<char> value) =>
            byte.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
    }

    public Rgba WithAlpha(double alpha) => this with { A = alpha };

    public string ToHex() => $"#{Byte(R):X2}{Byte(G):X2}{Byte(B):X2}";

    /// <summary>Relative brightness, used to compare colours.</summary>
    public double Luminance => 0.2126 * ToLinear(R) + 0.7152 * ToLinear(G) + 0.0722 * ToLinear(B);

    /// <summary>Mixes in OKLab, where gradients between hues stay clean instead of turning grey.</summary>
    public static Rgba Mix(Rgba a, Rgba b, double amount)
    {
        double t = Math.Clamp(amount, 0.0, 1.0);
        (double l1, double a1, double b1) = ToOklab(a);
        (double l2, double a2, double b2) = ToOklab(b);

        return FromOklab(
            l1 + (l2 - l1) * t,
            a1 + (a2 - a1) * t,
            b1 + (b2 - b1) * t,
            a.A + (b.A - a.A) * t);
    }

    /// <summary>Adds the difference <paramref name="to"/> minus <paramref name="from"/>, scaled by <paramref name="amount"/>.</summary>
    public Rgba Shift(Rgba from, Rgba to, double amount) => new(
        Math.Clamp(R + (to.R - from.R) * amount, 0.0, 1.0),
        Math.Clamp(G + (to.G - from.G) * amount, 0.0, 1.0),
        Math.Clamp(B + (to.B - from.B) * amount, 0.0, 1.0),
        A);

    private static int Byte(double channel) => (int)Math.Round(Math.Clamp(channel, 0.0, 1.0) * 255.0);

    private static double ToLinear(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

    private static double FromLinear(double c) => c <= 0.0031308 ? c * 12.92 : 1.055 * Math.Pow(c, 1.0 / 2.4) - 0.055;

    private static (double L, double A, double B) ToOklab(Rgba c)
    {
        double r = ToLinear(c.R), g = ToLinear(c.G), b = ToLinear(c.B);

        double l = Math.Cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b);
        double m = Math.Cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b);
        double s = Math.Cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b);

        return (0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s,
                1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s,
                0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s);
    }

    private static Rgba FromOklab(double lightness, double a, double b, double alpha)
    {
        double l = lightness + 0.3963377774 * a + 0.2158037573 * b;
        double m = lightness - 0.1055613458 * a - 0.0638541728 * b;
        double s = lightness - 0.0894841775 * a - 1.2914855480 * b;

        l = l * l * l;
        m = m * m * m;
        s = s * s * s;

        return new Rgba(
            Math.Clamp(FromLinear(Math.Max(0.0, +4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s)), 0.0, 1.0),
            Math.Clamp(FromLinear(Math.Max(0.0, -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s)), 0.0, 1.0),
            Math.Clamp(FromLinear(Math.Max(0.0, -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s)), 0.0, 1.0),
            alpha);
    }
}
