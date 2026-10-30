using System.Globalization;
using WheresMoon.Core;

namespace WheresMoon.App.Services;

/// <summary>Remembers the location between launches, so that the app starts with data at once.</summary>
internal static class LocationStore
{
	private const string HasLocationKey = "location.saved";
	private const string LatitudeKey = "location.latitude";
	private const string LongitudeKey = "location.longitude";
	private const string ElevationKey = "location.elevation";
	private const string ManualKey = "location.manual";

	/// <summary>The last known location, or null before the first one.</summary>
	public static GeoLocation? Saved
	{
		get
		{
#if DEBUG
			string? simulated = Environment.GetEnvironmentVariable("WHERESMOON_SIM_LOCATION");
			if (simulated == "none")
			{
				return null;
			}

			if (simulated is not null)
			{
				double[] parts = simulated.Split(',').Select(p => double.Parse(p, CultureInfo.InvariantCulture)).ToArray();
				return new GeoLocation(parts[0], parts[1]);
			}
#endif
			if (!Preferences.Default.Get(HasLocationKey, false))
			{
				return null;
			}

			return new GeoLocation(
				Preferences.Default.Get(LatitudeKey, 0.0),
				Preferences.Default.Get(LongitudeKey, 0.0),
				Preferences.Default.Get(ElevationKey, 0.0));
		}
	}

	/// <summary>True when the user entered the location, which turns off automatic detection.</summary>
	public static bool IsManual
	{
		get => Preferences.Default.Get(ManualKey, false);
		set => Preferences.Default.Set(ManualKey, value);
	}

	public static void Save(GeoLocation location)
	{
		Preferences.Default.Set(LatitudeKey, location.Latitude);
		Preferences.Default.Set(LongitudeKey, location.Longitude);
		Preferences.Default.Set(ElevationKey, location.Elevation);
		Preferences.Default.Set(HasLocationKey, true);
	}
}
