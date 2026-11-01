#if !ANDROID && !IOS
namespace WheresMoon.App.Services;

/// <summary>
/// Reports the device's magnetic heading. Android and iOS have their own versions, which also work with the device
/// held upright. Most computers have no compass at all, and those that do are tablets laid flat.
/// </summary>
internal sealed class HeadingSource
{
	public event Action<double>? Changed;

	public bool IsSupported => Microsoft.Maui.Devices.Sensors.Compass.Default.IsSupported;

	/// <summary>Rotation of the screen in degrees, which a tablet compass has already accounted for.</summary>
	public int ScreenRotation { get; set; }

	public void Start()
	{
		if (IsSupported && !Microsoft.Maui.Devices.Sensors.Compass.Default.IsMonitoring)
		{
			Microsoft.Maui.Devices.Sensors.Compass.Default.ReadingChanged += OnReading;
			Microsoft.Maui.Devices.Sensors.Compass.Default.Start(SensorSpeed.Game);
		}
	}

	public void Stop()
	{
		if (IsSupported && Microsoft.Maui.Devices.Sensors.Compass.Default.IsMonitoring)
		{
			Microsoft.Maui.Devices.Sensors.Compass.Default.Stop();
			Microsoft.Maui.Devices.Sensors.Compass.Default.ReadingChanged -= OnReading;
		}
	}

	private void OnReading(object? sender, CompassChangedEventArgs e) => Changed?.Invoke(e.Reading.HeadingMagneticNorth);
}
#endif
