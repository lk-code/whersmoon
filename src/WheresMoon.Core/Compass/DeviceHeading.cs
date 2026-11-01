namespace WheresMoon.Core.Compass;

/// <summary>Works out where the user is facing from the device's orientation.</summary>
public static class DeviceHeading
{
    /// <summary>
    /// The direction on the ground the user faces, in degrees from north clockwise.
    /// Lying flat that is where the top of the screen points, held upright where the back of the
    /// device points. Adding both vectors gives a smooth passage between the two, as each one
    /// shrinks towards zero on the ground just when the other takes over.
    /// </summary>
    /// <param name="matrix">Row-major matrix from device to east, north, up, as Android's rotation vector gives it.</param>
    /// <param name="screenRotation">Rotation of the screen against the device's natural orientation: 0, 90, 180 or 270 degrees.</param>
    public static double FromRotationMatrix(ReadOnlySpan<float> matrix, int screenRotation)
    {
        (double upEast, double upNorth) = screenRotation switch
        {
            90 => ((double)matrix[0], (double)matrix[3]),
            180 => (-matrix[1], -matrix[4]),
            270 => (-matrix[0], -matrix[3]),
            _ => (matrix[1], matrix[4]),
        };

        double east = upEast - matrix[2];
        double north = upNorth - matrix[5];

        return Normalize(Math.Atan2(east, north) * 180.0 / Math.PI);
    }

    /// <summary>Turns a magnetic heading into a true one. Declination counts eastwards, so it adds to the heading.</summary>
    public static double ToTrueNorth(double magneticHeading, double declination) => Normalize(magneticHeading + declination);

    public static double Normalize(double degrees)
    {
        double result = degrees % 360.0;
        return result < 0.0 ? result + 360.0 : result;
    }
}
