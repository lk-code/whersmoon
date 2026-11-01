namespace WheresMoon.Core.Compass;

/// <summary>
/// Magnetic declination from the World Magnetic Model 2025, valid until the end of 2029. The steps follow
/// the model's technical report: geodetic to spherical position, Schmidt semi-normalized Legendre
/// functions, the field in spherical components, and back to geodetic.
/// </summary>
public static class WorldMagneticModel
{
    private const double SemiMajorAxis = 6378.137;
    private const double Flattening = 1.0 / 298.257223563;
    private const double ReferenceRadius = 6371.2;
    private const double MetresPerKilometre = 1000.0;

    /// <summary>Stays off the poles, where the east component would divide by zero.</summary>
    private const double SmallestSine = 1e-10;

    private const int Size = WmmCoefficients.MaximumDegree + 1;

    /// <summary>Angle in degrees between true and magnetic north, positive when magnetic north lies to the east.</summary>
    public static double Declination(GeoLocation location, DateTimeOffset time) =>
        Declination(location.Latitude, location.Longitude, location.Elevation / MetresPerKilometre, DecimalYear(time));

    internal static double DecimalYear(DateTimeOffset time)
    {
        DateTimeOffset utc = time.ToUniversalTime();
        int days = DateTime.IsLeapYear(utc.Year) ? 366 : 365;

        return utc.Year + (utc.DayOfYear - 1 + utc.TimeOfDay.TotalDays) / days;
    }

    internal static double Declination(double latitude, double longitude, double heightKm, double year)
    {
        double geodetic = latitude * Math.PI / 180.0;
        double lambda = longitude * Math.PI / 180.0;

        double eccentricitySquared = Flattening * (2.0 - Flattening);
        double sinGeodetic = Math.Sin(geodetic);
        double primeVertical = SemiMajorAxis / Math.Sqrt(1.0 - eccentricitySquared * sinGeodetic * sinGeodetic);
        double p = (primeVertical + heightKm) * Math.Cos(geodetic);
        double z = (primeVertical * (1.0 - eccentricitySquared) + heightKm) * sinGeodetic;
        double radius = Math.Sqrt(p * p + z * z);
        double geocentric = Math.Asin(z / radius);

        double colatitude = Math.PI / 2.0 - geocentric;
        double sinTheta = Math.Max(Math.Sin(colatitude), SmallestSine);
        double cosTheta = Math.Cos(colatitude);

        var legendre = new double[Size, Size];
        var derivative = new double[Size, Size];
        FillLegendre(sinTheta, cosTheta, legendre, derivative);

        double yearsSinceEpoch = year - WmmCoefficients.Epoch;
        double radial = 0.0;
        double north = 0.0;
        double east = 0.0;

        foreach (WmmCoefficients.Term term in WmmCoefficients.Terms)
        {
            double g = term.G + yearsSinceEpoch * term.GPerYear;
            double h = term.H + yearsSinceEpoch * term.HPerYear;
            double cosM = Math.Cos(term.Order * lambda);
            double sinM = Math.Sin(term.Order * lambda);
            double scale = Math.Pow(ReferenceRadius / radius, term.Degree + 2);
            double wave = g * cosM + h * sinM;

            radial += (term.Degree + 1) * scale * wave * legendre[term.Degree, term.Order];
            north += scale * wave * derivative[term.Degree, term.Order];
            east += scale * term.Order * (g * sinM - h * cosM) * legendre[term.Degree, term.Order] / sinTheta;
        }

        // Rotating from the spherical frame to the ellipsoid changes north. East stays as it is.
        double tilt = geocentric - geodetic;
        double trueNorth = north * Math.Cos(tilt) + radial * Math.Sin(tilt);

        return Math.Atan2(east, trueNorth) * 180.0 / Math.PI;
    }

    /// <summary>Schmidt semi-normalized Legendre functions and their derivatives by colatitude.</summary>
    private static void FillLegendre(double sinTheta, double cosTheta, double[,] p, double[,] dp)
    {
        var schmidt = new double[Size, Size];
        schmidt[0, 0] = 1.0;
        p[0, 0] = 1.0;

        for (int n = 1; n < Size; n++)
        {
            schmidt[n, 0] = schmidt[n - 1, 0] * (2 * n - 1) / n;

            for (int m = 0; m <= n; m++)
            {
                if (m > 0)
                {
                    schmidt[n, m] = schmidt[n, m - 1] * Math.Sqrt((double)(n - m + 1) * (m == 1 ? 2 : 1) / (n + m));
                }

                if (n == m)
                {
                    p[n, m] = sinTheta * p[n - 1, m - 1];
                    dp[n, m] = sinTheta * dp[n - 1, m - 1] + cosTheta * p[n - 1, m - 1];
                }
                else
                {
                    double k = n == 1 ? 0.0 : (double)((n - 1) * (n - 1) - m * m) / ((2 * n - 1) * (2 * n - 3));
                    double older = n == 1 ? 0.0 : p[n - 2, m];
                    double olderSlope = n == 1 ? 0.0 : dp[n - 2, m];

                    p[n, m] = cosTheta * p[n - 1, m] - k * older;
                    dp[n, m] = cosTheta * dp[n - 1, m] - sinTheta * p[n - 1, m] - k * olderSlope;
                }
            }
        }

        // Applied in a second pass, as the recursion above works on the unnormalized values.
        for (int n = 0; n < Size; n++)
        {
            for (int m = 0; m <= n; m++)
            {
                p[n, m] *= schmidt[n, m];
                dp[n, m] *= schmidt[n, m];
            }
        }
    }
}
