using WheresMoon.Core;

namespace WheresMoon.App.Services;

internal enum LocationStatus
{
	Found,
	Denied,
	Unavailable,
}

internal readonly record struct LocationLookup(LocationStatus Status, GeoLocation? Location);

/// <summary>Asks the device where it is. Coarse accuracy is enough for sun and moon times.</summary>
internal static partial class DeviceLocation
{
	private static readonly TimeSpan LookupTimeout = TimeSpan.FromSeconds(10);

#if !ANDROID
	public static async Task<LocationLookup> FindAsync()
	{
		try
		{
			PermissionStatus status = await MainThread.InvokeOnMainThreadAsync(Permissions.RequestAsync<Permissions.LocationWhenInUse>);
			if (status != PermissionStatus.Granted)
			{
				return new LocationLookup(LocationStatus.Denied, null);
			}

			Location? location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Low, LookupTimeout))
				?? await Geolocation.Default.GetLastKnownLocationAsync();

			return location is null
				? new LocationLookup(LocationStatus.Unavailable, null)
				: new LocationLookup(LocationStatus.Found, new GeoLocation(location.Latitude, location.Longitude, location.Altitude ?? 0.0));
		}
		catch (PermissionException)
		{
			return new LocationLookup(LocationStatus.Denied, null);
		}
		catch (Exception exception) when (exception is FeatureNotSupportedException or FeatureNotEnabledException or InvalidOperationException)
		{
			return new LocationLookup(LocationStatus.Unavailable, null);
		}
	}
#endif
}
