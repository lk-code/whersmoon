using WheresMoon.Core;

namespace WheresMoon.App.Services;

/// <summary>Knows the location in use and whether it follows the device or was entered by hand.</summary>
internal sealed class LocationService
{
	public event Action? Changed;

	public GeoLocation? Current { get; private set; } = LocationStore.Saved;

	public bool IsManual => LocationStore.IsManual;

	/// <summary>Outcome of the last lookup, shown in the settings.</summary>
	public LocationStatus Status { get; private set; } = LocationStatus.Found;

	/// <summary>Asks the device for its location, unless the user entered one.</summary>
	public async Task UpdateAsync()
	{
#if DEBUG
		if (Environment.GetEnvironmentVariable("WHERESMOON_SIM_LOCATION") is not null)
		{
			return;
		}
#endif
		if (IsManual)
		{
			return;
		}

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
		return UpdateAsync();
	}

	private void Use(GeoLocation location)
	{
		LocationStore.Save(location);
		Current = location;
		Changed?.Invoke();
	}
}
