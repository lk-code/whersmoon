using System.Diagnostics;
using System.Numerics;
using WheresMoon.Core;
using WheresMoon.Core.Compass;

namespace WheresMoon.App.Services;

/// <summary>
/// Turns sensor readings into the scene's movement. A timer animates at up to 60 frames per second, but
/// only while something moves, so that a phone lying still neither redraws nor keeps the timer running.
/// </summary>
internal sealed class SceneMotion
{
	private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(16);

	/// <summary>Frames without any change before the timer stops.</summary>
	private const int QuietFrames = 30;

	/// <summary>Readings beyond these levels mean the device moves: radians per second, and g away from gravity.</summary>
	private const double TurnWakeLevel = 0.08;
	private const double AccelerationWakeLevel = 0.04;

	private readonly HeadingSource _heading = new();
	private readonly HeadingFilter _filter = new();
	private readonly RingInertia _inertia = new();
	private readonly IDispatcher _dispatcher;
	private readonly IDispatcherTimer _timer;
	private readonly Stopwatch _clock = Stopwatch.StartNew();

	private GeoLocation? _compassLocation;
	private double? _declination;
	private double _magnetic;
	private double _restingMagnetic;
	private volatile bool _hasReading;
	private Vector3 _acceleration;
	private Vector3 _turn;
	private int _screenRotation;
	private bool _isMovingDevice;
	private TimeSpan _lastFrame;
	private int _quiet;
	private bool _isRunning;

	public SceneMotion(IDispatcher dispatcher)
	{
		_dispatcher = dispatcher;
		_timer = dispatcher.CreateTimer();
		_timer.Interval = FrameInterval;
		_timer.Tick += (_, _) => OnFrame();
		_heading.Changed += OnHeading;
	}

	/// <summary>Raised on the main thread when <see cref="Heading"/> changed.</summary>
	public event Action? Moved;

	/// <summary>Direction the user faces in degrees from true north. Keeps counting past 360.</summary>
	public double Heading => _filter.Value;

	/// <summary>Shift of the compass rings, from -1 to 1 of the largest one.</summary>
	public (double X, double Y) RingShift => (_inertia.OffsetX, _inertia.OffsetY);

	public int ScreenRotation
	{
		set
		{
			_screenRotation = value;
			_heading.ScreenRotation = value;
		}
	}

	/// <summary>Where the device is, as that decides what its sensor measures.</summary>
	public GeoLocation? CompassLocation
	{
		set
		{
			if (value != _compassLocation)
			{
				_compassLocation = value;
				_declination = null;
			}
		}
	}

	/// <summary>Sensors only run while the app is in the foreground.</summary>
	public void Start()
	{
		if (!_isRunning)
		{
			_isRunning = true;
			_heading.Start();
			StartMovementSensors();
		}
	}

	public void Stop()
	{
		if (_isRunning)
		{
			_isRunning = false;
			_heading.Stop();
			StopMovementSensors();
			_timer.Stop();
		}
	}

	/// <summary>Without a gyroscope the rings stay put, as an accelerometer alone cannot tell turning from moving.</summary>
	private void StartMovementSensors()
	{
		if (Gyroscope.Default.IsSupported && Accelerometer.Default.IsSupported && !Gyroscope.Default.IsMonitoring)
		{
			Gyroscope.Default.ReadingChanged += OnTurn;
			Accelerometer.Default.ReadingChanged += OnAcceleration;
			Gyroscope.Default.Start(SensorSpeed.Game);
			Accelerometer.Default.Start(SensorSpeed.Game);
			_isMovingDevice = true;
		}
	}

	private void StopMovementSensors()
	{
		if (_isMovingDevice)
		{
			_isMovingDevice = false;
			Gyroscope.Default.Stop();
			Accelerometer.Default.Stop();
			Gyroscope.Default.ReadingChanged -= OnTurn;
			Accelerometer.Default.ReadingChanged -= OnAcceleration;
		}
	}

	private void OnTurn(object? sender, GyroscopeChangedEventArgs e)
	{
		_turn = e.Reading.AngularVelocity;

		if (_turn.Length() > TurnWakeLevel)
		{
			RequestFrames();
		}
	}

	private void OnAcceleration(object? sender, AccelerometerChangedEventArgs e)
	{
		_acceleration = e.Reading.Acceleration;

		if (Math.Abs(_acceleration.Length() - 1.0f) > AccelerationWakeLevel)
		{
			RequestFrames();
		}
	}

	private void RequestFrames()
	{
		if (!_timer.IsRunning)
		{
			_dispatcher.Dispatch(WakeUp);
		}
	}

	/// <summary>Sensors report on their own threads on some systems, and keep reporting while nothing moves.</summary>
	private void OnHeading(double magnetic)
	{
		_magnetic = magnetic;
		_hasReading = true;

		if (Math.Abs(HeadingFilter.Difference(magnetic, _restingMagnetic)) >= HeadingFilter.Deadband)
		{
			RequestFrames();
		}
	}

	private void WakeUp()
	{
		if (_isRunning && !_timer.IsRunning)
		{
			_lastFrame = _clock.Elapsed;
			_quiet = 0;
			_timer.Start();
		}
	}

	private void OnFrame()
	{
		TimeSpan now = _clock.Elapsed;
		double seconds = (now - _lastFrame).TotalSeconds;
		_lastFrame = now;

		bool isFirstReading = _hasReading && _filter.SetTarget(DeviceHeading.ToTrueNorth(_magnetic, Declination()));
		bool hasTurned = _filter.Advance(seconds) || isFirstReading;

		if (AdvanceRings(seconds) || hasTurned)
		{
			_quiet = 0;
			Moved?.Invoke();
		}
		else if (++_quiet >= QuietFrames)
		{
			_restingMagnetic = _magnetic;
			_timer.Stop();
		}
	}

	/// <summary>Only a compass reading gets the magnetic model loaded, which startup should not wait for.</summary>
	private double Declination()
	{
		if (_compassLocation is not GeoLocation location)
		{
			return 0.0;
		}

		_declination ??= WorldMagneticModel.Declination(location, DateTimeOffset.UtcNow);
		return _declination.Value;
	}

	private bool AdvanceRings(double seconds)
	{
		if (!_isMovingDevice)
		{
			return false;
		}

		(double accelerationX, double accelerationY) = RingInertia.ToScreen(_acceleration.X, _acceleration.Y, _screenRotation);
		(double turnX, double turnY) = RingInertia.ToScreen(_turn.X, _turn.Y, _screenRotation);

		_inertia.AccelerationX = accelerationX;
		_inertia.AccelerationY = accelerationY;
		_inertia.TurnX = turnX;
		_inertia.TurnY = turnY;

		return _inertia.Advance(seconds);
	}
}
