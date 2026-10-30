using WheresMoon.App.Rendering;
using WheresMoon.App.Resources.Strings;
using WheresMoon.App.Services;
using WheresMoon.Core;
using WheresMoon.Core.Scene;

namespace WheresMoon.App;

public partial class MainPage : ContentPage
{
	/// <summary>Label height relative to its font, enough for ascenders and descenders on every platform.</summary>
	private const double LabelHeightFactor = 1.4;

	/// <summary>Slightly negative tracking of the large clock, relative to its font size.</summary>
	private const double ClockTracking = -0.0125;

	/// <summary>Timer tick after the full minute, so that the clock never shows the old minute.</summary>
	private static readonly TimeSpan TickMargin = TimeSpan.FromMilliseconds(50);

	private readonly SceneDrawable _drawable = new();
	private readonly SceneAnimator _animator;
	private readonly IDispatcherTimer _timer;

	private SafeInsets _insets = SafeInsets.None;
	private string _zoneId = TimeZoneInfo.Local.Id;
	private GeoLocation? _location;

	public MainPage()
	{
		InitializeComponent();

		Scene.Drawable = _drawable;
		_animator = new SceneAnimator(Scene, _drawable);

		_timer = Dispatcher.CreateTimer();
		_timer.IsRepeating = false;
		_timer.Tick += (_, _) => Refresh();

		SizeChanged += (_, _) => ApplyLayout();
		SafeAreaProbe.SizeChanged += (_, _) => UpdateInsets();
		SafeAreaProbe.PropertyChanged += (_, e) =>
		{
			// A half turn swaps left and right insets without changing the size.
			if (e.PropertyName is nameof(X) or nameof(Y))
			{
				UpdateInsets();
			}
		};

		SemanticProperties.SetDescription(SettingsButton, Strings.Get("SettingsButton"));

#if WINDOWS
		Loaded += (_, _) => Platforms.Windows.WindowChrome.Apply(this, Scene);
#endif

		_location = LocationStore.Saved;
		Refresh();
	}

	/// <summary>Recomputes everything for the current moment and schedules the next full minute.</summary>
	public void Refresh()
	{
		TimeZoneInfo.ClearCachedData();
		TimeZoneInfo zone = TimeZoneInfo.Local;
		_zoneId = zone.Id;

		DateTimeOffset now = AppClock.Now;
		ClockLabel.Text = SkyTexts.Clock(TimeZoneInfo.ConvertTime(now, zone));

		if (_location is GeoLocation location)
		{
			SkySnapshot sky = SkyCalculator.Compute(now, location, zone);

			SunLabel.Text = SkyTexts.SunLine(sky);
			MoonLabel.Text = SkyTexts.MoonLine(sky);
			PhaseLabel.Text = SkyTexts.PhaseLine(sky);
			SemanticProperties.SetDescription(Scene, SkyTexts.Description(sky));

			_animator.MoveTo(SceneState.From(sky, _animator.Target));
		}
		else
		{
			SunLabel.Text = Strings.Get("LocationMissing");
			MoonLabel.Text = string.Empty;
			PhaseLabel.Text = string.Empty;
		}

		ScheduleNextTick(now);
	}

	private void ScheduleNextTick(DateTimeOffset now)
	{
		TimeSpan untilNextMinute = TimeSpan.FromMinutes(1) - TimeSpan.FromTicks(now.Ticks % TimeSpan.TicksPerMinute);

		_timer.Stop();
		_timer.Interval = untilNextMinute + TickMargin;
		_timer.Start();
	}

	private void UpdateInsets()
	{
		Rect probe = SafeAreaProbe.Frame;
		var insets = new SafeInsets(
			probe.Left,
			probe.Top,
			Math.Max(0.0, SafeArea.Width - probe.Right),
			Math.Max(0.0, SafeArea.Height - probe.Bottom));

		if (insets != _insets)
		{
			_insets = insets;
			ApplyLayout();
		}
	}

	private void ApplyLayout()
	{
		if (Width <= 0 || Height <= 0)
		{
			return;
		}

		SceneLayout layout = SceneLayout.Compute(Width, Height, _insets);
		_drawable.Layout = layout;

		ClockLabel.FontSize = layout.ClockFontSize;
		ClockLabel.CharacterSpacing = ClockTracking * layout.ClockFontSize;
		Place(ClockLabel, layout.ClockFontSize, layout.TextTop + layout.ClockFontSize / 2.0);
		Place(SunLabel, layout.RiseSetFontSize, layout.RiseSetLine1CenterY);
		Place(MoonLabel, layout.RiseSetFontSize, layout.RiseSetLine2CenterY);
		Place(PhaseLabel, layout.PhaseFontSize, layout.PhaseLineCenterY);

		Scene.Invalidate();
	}

	private static void Place(Label label, double fontSize, double centerY)
	{
		double height = fontSize * LabelHeightFactor;

		label.FontSize = fontSize;
		label.HeightRequest = height;
		label.Margin = new Thickness(0, centerY - height / 2.0, 0, 0);
	}

	private void OnSettingsClicked(object? sender, EventArgs e)
	{
	}
}
