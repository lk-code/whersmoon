using Android.Content;
using Android.Hardware;
using Android.Runtime;
using WheresMoon.Core.Compass;

namespace WheresMoon.App.Services;

/// <summary>
/// Reports the magnetic heading from the rotation vector, which unlike MAUI's compass stays right with the
/// device held upright. Phones without a gyroscope only have the variant based on the magnetometer.
/// </summary>
internal sealed class HeadingSource : Java.Lang.Object, ISensorEventListener
{
	private const int QuaternionSize = 4;

	private readonly SensorManager? _sensors = Platform.AppContext.GetSystemService(Context.SensorService) as SensorManager;
	private readonly Sensor? _sensor;
	private readonly float[] _rotation = new float[QuaternionSize];
	private readonly float[] _matrix = new float[9];

	public HeadingSource()
	{
		_sensor = _sensors?.GetDefaultSensor(SensorType.RotationVector)
			?? _sensors?.GetDefaultSensor(SensorType.GeomagneticRotationVector);
	}

	public event Action<double>? Changed;

	public bool IsSupported => _sensor is not null;

	/// <summary>Rotation of the screen in degrees.</summary>
	public int ScreenRotation { get; set; }

	public void Start() => _sensors?.RegisterListener(this, _sensor, SensorDelay.Game);

	public void Stop() => _sensors?.UnregisterListener(this);

	public void OnSensorChanged(SensorEvent? e)
	{
		if (e?.Values is { Count: >= QuaternionSize } values)
		{
			// Some phones send a fifth value, which the conversion below refuses.
			for (int i = 0; i < QuaternionSize; i++)
			{
				_rotation[i] = values[i];
			}

			SensorManager.GetRotationMatrixFromVector(_matrix, _rotation);
			Changed?.Invoke(DeviceHeading.FromRotationMatrix(_matrix, ScreenRotation));
		}
	}

	public void OnAccuracyChanged(Sensor? sensor, [GeneratedEnum] SensorStatus accuracy)
	{
	}
}
