using WheresMoon.Core;
using WheresMoon.Core.Places;

namespace WheresMoon.App.Services;

/// <summary>Knows the location in use and whether it follows the device or was entered by hand.</summary>
internal sealed class LocationService
{
	/// <summary>Quickly switching between apps should not ask the device every time.</summary>
	private static readonly TimeSpan MinimumLookupInterval = TimeSpan.FromMinutes(15);

	private DateTimeOffset _lastLookup = DateTimeOffset.MinValue;
	private TimeZoneInfo? _placeZone;

	public LocationService()
	{
		if (IsManual && LocationStore.ManualPlace is Place place)
		{
			Place = place;
			_placeZone = PlaceZone.Resolve(place.Zone, place.Longitude);
		}
	}

	public event Action? Changed;

	public GeoLocation? Current { get; private set; } = LocationStore.Saved;

	public bool IsManual => LocationStore.IsManual;

	/// <summary>The chosen place, or null while following the device.</summary>
	public Place? Place { get; private set; }

	/// <summary>Time zone of the location in use. Read it afresh, the device's zone can change at any time.</summary>
	public TimeZoneInfo Zone => _placeZone ?? TimeZoneInfo.Local;

	public IReadOnlyList<Place> RecentPlaces => LocationStore.RecentPlaces;

	/// <summary>Where the compass really is. A chosen place changes the sky, not the device's surroundings.</summary>
	public GeoLocation? CompassLocation => LocationStore.DeviceLocation ?? Current;

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
			LocationStore.DeviceLocation = location;
			Use(location);
		}
	}

	public void SetManual(Place place)
	{
		LocationStore.IsManual = true;
		LocationStore.ManualPlace = place;

		// A place from coordinates has no name, so it would mean nothing in the list.
		if (place.Name.Length > 0)
		{
			LocationStore.AddRecentPlace(place);
		}

		Place = place;
		_placeZone = PlaceZone.Resolve(place.Zone, place.Longitude);
		Status = LocationStatus.Found;
		Use(place.Location);
	}

	public async Task SetAutomaticAsync()
	{
		LocationStore.IsManual = false;
		LocationStore.ManualPlace = null;
		Place = null;
		_placeZone = null;
		_lastLookup = DateTimeOffset.MinValue;

		await UpdateAsync();
		Changed?.Invoke();
	}

	private void Use(GeoLocation location)
	{
		LocationStore.Save(location);
		Current = location;
		Changed?.Invoke();
	}
}
