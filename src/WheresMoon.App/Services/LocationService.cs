using WheresMoon.Core;

namespace WheresMoon.App.Services;

/// <summary>Knows the location in use and whether it follows the device or was entered by hand.</summary>
internal sealed class LocationService
{
	/// <summary>Quickly switching between apps should not ask the device every time.</summary>
	private static readonly TimeSpan MinimumLookupInterval = TimeSpan.FromMinutes(15);

	private DateTimeOffset _lastLookup = DateTimeOffset.MinValue;

	public event Action? Changed;

	public GeoLocation? Current { get; private set; } = LocationStore.Saved;

	public bool IsManual => LocationStore.IsManual;

	/// <summary>Outcome of the last lookup, shown in the settings.</summary>
	public LocationStatus Status { get; private set; } = LocationStatus.Found;

	/// <summary>Asks the device for its location, unless the user entered one or it was asked recently.</summary>
	public async Task UpdateAsync()
	{
#if DEBUG
		if (Environment.GetEnvironmentVariable("WHERESMOON_SIM_LOCATION") is not null)
		{
			return;
		}
#endif
		DateTimeOffset now = DateTimeOffset.UtcNow;
		if (IsManual || now - _lastLookup < MinimumLookupInterval)
		{
			return;
		}

		_lastLookup = now;

		LocationLookup lookup = await DeviceLocation.FindAsync();
		Status = lookup.Status;

		if (lookup.Location is GeoLocation location)
		{
			Use(location);
		}
	}

	public void SetManual(GeoLocation location)
	{
		LocationStore.IsManual = true;
		Status = LocationStatus.Found;
		Use(location);
	}

	public Task SetAutomaticAsync()
	{
		LocationStore.IsManual = false;
		_lastLookup = DateTimeOffset.MinValue;
		return UpdateAsync();
	}

	private void Use(GeoLocation location)
	{
		LocationStore.Save(location);
		Current = location;
		Changed?.Invoke();
	}
}
