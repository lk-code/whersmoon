using CoreLocation;

namespace WheresMoon.App.Services;

/// <summary>Reports the magnetic heading, which iOS already keeps right with the device held upright. It needs no location permission.</summary>
internal sealed class HeadingSource
{
	private const double SmallestChange = 0.5;

	private CLLocationManager? _manager;
	private int _screenRotation;

	public event Action<double>? Changed;

	public bool IsSupported => CLLocationManager.HeadingAvailable;

	/// <summary>Rotation of the screen in degrees. iOS can correct for it, but it has to be told.</summary>
	public int ScreenRotation
	{
		get => _screenRotation;
		set
		{
			_screenRotation = value;

			if (_manager is not null)
			{
				_manager.HeadingOrientation = OrientationFor(value);
			}
		}
	}

	public void Start()
	{
		if (_manager is null && IsSupported)
		{
			_manager = new CLLocationManager
			{
				HeadingFilter = SmallestChange,
				HeadingOrientation = OrientationFor(_screenRotation),
			};
			_manager.UpdatedHeading += OnHeading;
			_manager.StartUpdatingHeading();
		}
	}

	public void Stop()
	{
		if (_manager is not null)
		{
			_manager.StopUpdatingHeading();
			_manager.UpdatedHeading -= OnHeading;
			_manager = null;
		}
	}

	/// <summary>MAUI counts the turn of the interface, and a device turned left shows its interface turned right.</summary>
	private static CLDeviceOrientation OrientationFor(int screenRotation) => screenRotation switch
	{
		90 => CLDeviceOrientation.LandscapeLeft,
		180 => CLDeviceOrientation.PortraitUpsideDown,
		270 => CLDeviceOrientation.LandscapeRight,
		_ => CLDeviceOrientation.Portrait,
	};

	private void OnHeading(object? sender, CLHeadingUpdatedEventArgs e) => Changed?.Invoke(e.NewHeading.MagneticHeading);
}
