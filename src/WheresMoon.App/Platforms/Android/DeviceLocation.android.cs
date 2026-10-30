using Android;
using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using WheresMoon.Core;
using AndroidLocation = Android.Locations.Location;

namespace WheresMoon.App.Services;

// MAUI's Geolocation insists on the precise location permission, which this app does not want.
// It would also ask again on every call after a coarse grant, so Android talks to LocationManager itself.
internal static partial class DeviceLocation
{
	private static readonly TimeSpan FreshEnough = TimeSpan.FromHours(1);

	public static async Task<LocationLookup> FindAsync()
	{
		try
		{
			PermissionStatus status = await MainThread.InvokeOnMainThreadAsync(Permissions.RequestAsync<CoarseLocationPermission>);
			if (status != PermissionStatus.Granted)
			{
				return new LocationLookup(LocationStatus.Denied, null);
			}

			if (Platform.AppContext.GetSystemService(Context.LocationService) is not LocationManager manager)
			{
				return new LocationLookup(LocationStatus.Unavailable, null);
			}

			IList<string> providers = manager.GetProviders(enabledOnly: true);
			AndroidLocation? known = MostRecent(manager, providers);

			if (known is not null && Age(known) < FreshEnough)
			{
				return Found(known);
			}

			AndroidLocation? current = await RequestCurrentAsync(manager, providers);
			AndroidLocation? best = current ?? known;

			return best is null ? new LocationLookup(LocationStatus.Unavailable, null) : Found(best);
		}
		catch (PermissionException)
		{
			return new LocationLookup(LocationStatus.Denied, null);
		}
		catch (Java.Lang.SecurityException)
		{
			return new LocationLookup(LocationStatus.Denied, null);
		}
	}

	private static LocationLookup Found(AndroidLocation location) => new(
		LocationStatus.Found,
		new GeoLocation(location.Latitude, location.Longitude, location.HasAltitude ? location.Altitude : 0.0));

	private static TimeSpan Age(AndroidLocation location) =>
		DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds(location.Time);

	private static AndroidLocation? MostRecent(LocationManager manager, IList<string> providers) =>
		providers
			.Select(manager.GetLastKnownLocation)
			.OfType<AndroidLocation>()
			.MaxBy(location => location.Time);

	private static async Task<AndroidLocation?> RequestCurrentAsync(LocationManager manager, IList<string> providers)
	{
		string[] preferred = [LocationManager.NetworkProvider, LocationManager.GpsProvider];
		string? provider = preferred.FirstOrDefault(providers.Contains);
		if (provider is null)
		{
			return null;
		}

		using var listener = new SingleFixListener();

		manager.RequestLocationUpdates(provider, 0L, 0f, listener, Looper.MainLooper);
		try
		{
			Task<AndroidLocation?> fix = listener.Fix;
			Task finished = await Task.WhenAny(fix, Task.Delay(LookupTimeout));
			return finished == fix ? await fix : null;
		}
		finally
		{
			manager.RemoveUpdates(listener);
		}
	}

	private sealed class CoarseLocationPermission : Permissions.BasePlatformPermission
	{
		public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
			[(Manifest.Permission.AccessCoarseLocation, true)];
	}

	private sealed class SingleFixListener : Java.Lang.Object, ILocationListener
	{
		private readonly TaskCompletionSource<AndroidLocation?> _fix = new();

		public Task<AndroidLocation?> Fix => _fix.Task;

		public void OnLocationChanged(AndroidLocation location) => _fix.TrySetResult(location);

		public void OnProviderDisabled(string provider) => _fix.TrySetResult(null);

		public void OnProviderEnabled(string provider)
		{
		}

		public void OnStatusChanged(string? provider, [GeneratedEnum] Availability status, Bundle? extras)
		{
		}
	}
}
