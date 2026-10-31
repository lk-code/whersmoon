using System.Diagnostics;
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

	private readonly HeadingSource _heading = new();
	private readonly HeadingFilter _filter = new();
	private readonly IDispatcher _dispatcher;
	private readonly IDispatcherTimer _timer;
	private readonly Stopwatch _clock = Stopwatch.StartNew();

	private double _declination;
	private double _magnetic;
	private double _restingMagnetic;
	private volatile bool _hasReading;
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

	public bool HasCompass => _heading.IsSupported;

	/// <summary>Direction the user faces in degrees from true north. Keeps counting past 360.</summary>
	public double Heading => _filter.Value;

	public int ScreenRotation
	{
		set => _heading.ScreenRotation = value;
	}

	/// <summary>Takes the declination for where the device is, as that decides what its sensor measures.</summary>
	public void SetReferenceLocation(GeoLocation location) =>
		_declination = WorldMagneticModel.Declination(location, DateTimeOffset.UtcNow);

	/// <summary>Sensors only run while the app is in the foreground.</summary>
	public void Start()
	{
		if (!_isRunning)
		{
			_isRunning = true;
			_heading.Start();
		}
	}

	public void Stop()
	{
		if (_isRunning)
		{
			_isRunning = false;
			_heading.Stop();
			_timer.Stop();
		}
	}

	/// <summary>Sensors report on their own threads on some systems, and keep reporting while nothing moves.</summary>
	private void OnHeading(double magnetic)
	{
		_magnetic = magnetic;
		_hasReading = true;

		if (Math.Abs(HeadingFilter.Difference(magnetic, _restingMagnetic)) >= HeadingFilter.Deadband)
		{
			_dispatcher.Dispatch(WakeUp);
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

		bool isFirstReading = _hasReading && _filter.SetTarget(DeviceHeading.ToTrueNorth(_magnetic, _declination));

		if (_filter.Advance(seconds) || isFirstReading)
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
}
