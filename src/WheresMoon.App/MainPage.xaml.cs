using System.Diagnostics;
using Microsoft.Maui.Controls.Shapes;
using WheresMoon.App.Controls;
using WheresMoon.App.Rendering;
using WheresMoon.App.Resources.Strings;
using WheresMoon.App.Services;
using WheresMoon.App.Settings;
using WheresMoon.Core;
using WheresMoon.Core.Interaction;
using WheresMoon.Core.Scene;

namespace WheresMoon.App;

public partial class MainPage : ContentPage
{
	/// <summary>Label height relative to its font, enough for ascenders and descenders on every platform.</summary>
	private const double LabelHeightFactor = 1.4;

	/// <summary>Slightly negative tracking of the large clock, relative to its font size.</summary>
	private const double ClockTracking = -0.0125;

	private const double DialogMaximumWidth = 480.0;
	private const double DialogMaximumHeightFraction = 0.85;
	private const double DialogMargin = 16.0;

	/// <summary>Touch target around the page dots.</summary>
	private const double PagerButtonWidth = 96.0;
	private const double PagerButtonHeight = 28.0;

	/// <summary>How much of a screen width one press of an arrow key moves the time.</summary>
	private const double KeyStepShare = 1.0 / 96.0;

	/// <summary>A pause this long in seconds ends a run of wheel events.</summary>
	private const double WheelPause = 0.4;

	/// <summary>Shortest time between wheel events, so that a burst does not read as an endless speed.</summary>
	private const double ShortestWheelInterval = 0.008;

	private static readonly Color ClockTint = new(
		(float)DesignTokens.Scrub.ClockTint.R,
		(float)DesignTokens.Scrub.ClockTint.G,
		(float)DesignTokens.Scrub.ClockTint.B);

	/// <summary>Timer tick after the full minute, so that the clock never shows the old minute.</summary>
	private static readonly TimeSpan TickMargin = TimeSpan.FromMilliseconds(50);

	private static readonly TimeSpan GlideFrameInterval = TimeSpan.FromMilliseconds(16);

	private readonly SceneDrawable _drawable = new();
	private readonly SceneAnimator _animator;
	private readonly IDispatcherTimer _timer;
	private readonly IDispatcherTimer _glideTimer;
	private readonly LocationService _location = new();
	private readonly InfoSlider _slider = new();
	private readonly PointerGesture _gesture = new();
	private readonly Stopwatch _gestureClock = Stopwatch.StartNew();
	private readonly TimeScrubber _scrubber = new();
	private readonly TimeGlide _glide = new();
	private readonly SceneMotion _motion;

	private SafeInsets _insets = SafeInsets.None;
	private SceneLayout? _layout;
	private SettingsView? _settings;
	private SkyEventCalendar? _events;
	private SnapPoints? _snapPoints;
	private GeoLocation? _eventsLocation;
	private string? _eventsZone;
	private DateTimeOffset? _selectedTime;
	private TimeSpan _lastWheel;
	private TimeSpan _lastGlideFrame;
	private bool _isSliding;
	private bool _isScrubbing;
	private bool _hasAppeared;

	public MainPage()
	{
		InitializeComponent();

		Scene.Drawable = _drawable;
		_animator = new SceneAnimator(Scene, _drawable);
		_motion = new SceneMotion(Dispatcher);
		_motion.Moved += OnMotion;
		_drawable.EastLetter = Strings.Get("CompassLetterEast")[0];
		TextLayer.Add(_slider);

		_timer = Dispatcher.CreateTimer();
		_timer.IsRepeating = false;

		// A timer that does not repeat stops itself right after this event, which would undo a restart from inside it.
		_timer.Tick += (_, _) => Dispatcher.Dispatch(Refresh);

		_glideTimer = Dispatcher.CreateTimer();
		_glideTimer.Interval = GlideFrameInterval;
		_glideTimer.Tick += (_, _) => OnGlideFrame();

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

		_location.Changed += Refresh;

		Scene.StartInteraction += OnPointerDown;
		Scene.DragInteraction += OnPointerMove;
		Scene.EndInteraction += OnPointerUp;
		Scene.CancelInteraction += (_, _) => OnPointerCancelled();

		_slider.PageChanged += OnPageChanged;
		_slider.PositionChanged += () =>
		{
			_drawable.PagePosition = _slider.Position;
			Scene.Invalidate();
		};

		SemanticProperties.SetDescription(SettingsButton, Strings.Get("SettingsButton"));
		SemanticProperties.SetHint(PagerButton, Strings.Get("PagerHint"));
		NowButton.Text = Strings.Get("NowButton");
		SemanticProperties.SetHint(NowButton, Strings.Get("NowButtonHint"));
		DescribePage();

#if WINDOWS
		Loaded += (_, _) => Platforms.Windows.WindowChrome.Apply(this, Scene, HandleKey, OnWheel);
#elif MACCATALYST
		Loaded += (_, _) => Platforms.MacCatalyst.ScrollInput.Attach(Scene, OnWheel);
#endif

		// Everything needed for the first frame is at hand, so it already shows the real sky.
		Refresh();
	}

	/// <summary>Recomputes everything for the moment on display and schedules the next full minute.</summary>
	public void Refresh()
	{
		// Reading the zone afresh on every tick also picks up time zone changes on all platforms.
		TimeZoneInfo.ClearCachedData();

		DateTimeOffset now = AppClock.Now;
		Show(_selectedTime ?? now, isFollowingFinger: false);
		ScheduleNextTick(now);
		_motion.CompassLocation = _location.CompassLocation;
	}

	private void OnMotion()
	{
		_drawable.Heading = _motion.Heading;
		_drawable.RingShift = new PointD(_motion.RingShift.X, _motion.RingShift.Y);
		Scene.Invalidate();
	}

	/// <summary>Reacts to a key that is not meant for a text field. Returns whether it was used.</summary>
	public bool HandleKey(SceneKey key)
	{
		if (_settings is not null)
		{
			if (key == SceneKey.Escape)
			{
				CloseSettings();
				return true;
			}

			return false;
		}

		switch (key)
		{
			case SceneKey.Left:
				StepTime(-1.0);
				return true;
			case SceneKey.Right:
				StepTime(1.0);
				return true;
			default:
				ReturnToNow();
				return true;
		}
	}

	private void Show(DateTimeOffset time, bool isFollowingFinger)
	{
		TimeZoneInfo zone = _location.Zone;
		DateTimeOffset local = TimeZoneInfo.ConvertTime(time, zone);

		ClockLabel.Text = SkyTexts.Clock(local);
		ClockLabel.TextColor = _selectedTime is null ? Colors.White : ClockTint;
		ContextLabel.Text = SkyTexts.Context(_location.Place, local, TimeZoneInfo.ConvertTime(AppClock.Now, zone));

		if (_location.Current is GeoLocation location)
		{
			SkySnapshot sky = SkyCalculator.Compute(time, location, zone);
			SceneState state = SceneState.From(sky, _animator.Target);

			_slider.SetRows(0, SkyTexts.SunAndMoonRows(sky, DayLengthCalculator.Compute(time, location, zone)));
			UpdateOtherPages(time, location, zone);

			if (isFollowingFinger)
			{
				_animator.Show(state);
			}
			else
			{
				SemanticProperties.SetDescription(Scene, SkyTexts.Description(sky));
				_animator.MoveTo(state);
			}
		}
		else
		{
			_slider.SetRows(0, [new InfoRow(Strings.Get("LocationMissing"), InfoRowStyle.Primary)]);
		}
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (_hasAppeared)
		{
			return;
		}

		_hasAppeared = true;

		if (Window is Window window)
		{
			window.Resumed += async (_, _) =>
			{
				Refresh();
				UpdateMotion();
				await _location.UpdateAsync();
			};
			window.Activated += (_, _) => Refresh();
			window.Stopped += (_, _) =>
			{
				StopGlide();
				_timer.Stop();
				_motion.Stop();
			};
		}

		UpdateMotion();
		await _location.UpdateAsync();

		if (_location.Current is null)
		{
			OpenSettings();
		}
	}

	protected override bool OnBackButtonPressed()
	{
		if (_settings is not null)
		{
			CloseSettings();
			return true;
		}

		return base.OnBackButtonPressed();
	}

	/// <summary>The other pages wait until they come into view, so that they cost nothing at startup.</summary>
	private void UpdateOtherPages(DateTimeOffset time, GeoLocation location, TimeZoneInfo zone)
	{
		if (_slider.IsShown(1))
		{
			PrepareEvents(location, zone);
			_slider.SetRows(1, SkyTexts.EventRows(_events!.Upcoming(time), time, zone));
		}

		if (_slider.IsShown(2))
		{
			_slider.SetRows(2, SkyTexts.LightRows(LightHoursCalculator.Compute(time, location, zone)));
		}
	}

	private void PrepareEvents(GeoLocation location, TimeZoneInfo zone)
	{
		if (_eventsLocation != location || _eventsZone != zone.Id)
		{
			_events = new SkyEventCalendar(location);
			_snapPoints = new SnapPoints(location, zone, _events);
			_eventsLocation = location;
			_eventsZone = zone.Id;
		}
	}

	private void OnPageChanged()
	{
		// Every page has its own time scale, which would change the speed in mid flight.
		StopGlide();
		DescribePage();

		if (_location.Current is GeoLocation location)
		{
			UpdateOtherPages(_selectedTime ?? AppClock.Now, location, _location.Zone);
		}
	}

	private void DescribePage()
	{
		string name = Strings.Get(_slider.Page switch
		{
			0 => "PageSunMoon",
			1 => "PageEvents",
			_ => "PageLight",
		});

		SemanticProperties.SetDescription(PagerButton, Strings.Format("PagerDescription", _slider.Page + 1, InfoSlider.PageCount, name));
	}

	private void OnPointerDown(object? sender, TouchEventArgs e)
	{
		StopGlide();

		if (e.Touches.Length > 0)
		{
			_gesture.Begin(new PointD(e.Touches[0].X, e.Touches[0].Y), _gestureClock.Elapsed);
		}
	}

	private void OnPointerMove(object? sender, TouchEventArgs e)
	{
		if (e.Touches.Length == 0)
		{
			return;
		}

		double distance = _gesture.Move(new PointD(e.Touches[0].X, e.Touches[0].Y), _gestureClock.Elapsed);
		if (_gesture.Axis != DragAxis.Horizontal)
		{
			return;
		}

		if (!_isSliding && _layout?.IsInSlider(_gesture.Start.Y) == true)
		{
			_isSliding = true;
			_slider.BeginDrag();
			OnPageChanged();
		}

		if (_isSliding)
		{
			_slider.DragBy(distance);
			return;
		}

		if (!_isScrubbing)
		{
			_isScrubbing = true;
			_scrubber.Begin(_selectedTime ?? AppClock.Now);
		}

		ScrubBy(TimeScrubber.TowardsLater(distance), TimeScrubber.TowardsLater(_gesture.VelocityX));
	}

	private void OnPointerUp(object? sender, TouchEventArgs e)
	{
		TimeSpan now = _gestureClock.Elapsed;
		FinishDrag(_gesture.VelocityAt(now));

		if (_gesture.End(now) == GestureEnd.DoubleTap)
		{
			ReturnToNow();
		}
	}

	private void OnPointerCancelled() => FinishDrag(0.0);

	private void FinishDrag(double velocity)
	{
		if (_isSliding)
		{
			_isSliding = false;
			_slider.EndDrag(velocity);
		}

		if (_isScrubbing)
		{
			_isScrubbing = false;
			StartGlide(TimeScrubber.TowardsLater(velocity));
		}
	}

	/// <summary>A swipe that is still moving as it lets go carries the time on. Any other ends here.</summary>
	private void StartGlide(double velocity)
	{
		if (_layout is SceneLayout layout)
		{
			_glide.Push(velocity, layout.Width);
		}

		if (_glide.IsRunning)
		{
			_lastGlideFrame = _gestureClock.Elapsed;
			_glideTimer.Start();
		}
		else
		{
			Refresh();
		}
	}

	private void OnGlideFrame()
	{
		TimeSpan now = _gestureClock.Elapsed;
		double distance = _glide.Advance((now - _lastGlideFrame).TotalSeconds);
		_lastGlideFrame = now;

		bool hasEnded = !_glide.IsRunning;
		ScrubBy(distance, _glide.Speed);

		// An event catches a glide that has become slow, and there is no finger left to pull it loose.
		if (hasEnded || _scrubber.Caught is not null)
		{
			_glide.Stop();
			_glideTimer.Stop();
			Refresh();
		}
	}

	private void StopGlide()
	{
		if (_glide.IsRunning)
		{
			_glide.Stop();
			_glideTimer.Stop();
			Refresh();
		}
	}

	private void OnWheel(double distance)
	{
		if (_settings is not null)
		{
			return;
		}

		StopGlide();

		TimeSpan now = _gestureClock.Elapsed;
		double interval = (now - _lastWheel).TotalSeconds;
		_lastWheel = now;

		if (interval > WheelPause)
		{
			_scrubber.Begin(_selectedTime ?? AppClock.Now);
		}

		ScrubBy(distance, distance / Math.Max(interval, ShortestWheelInterval));
	}

	private void ScrubBy(double distance, double speed)
	{
		if (_location.Current is not GeoLocation location || _layout is not SceneLayout layout)
		{
			return;
		}

		InfoPage page = (InfoPage)_slider.Page;
		PrepareEvents(location, _location.Zone);

		IReadOnlyList<DateTimeOffset> near = _snapPoints!.Around(page, _scrubber.Time, TimeScrubber.ReachOf(page));
		if (_scrubber.Move(distance, speed, layout.Width, TimeScrubber.ScaleOf(page), near))
		{
			Haptics.Tick(Scene);
		}

		SelectTime(_scrubber.Time);
	}

	private void StepTime(double direction)
	{
		if (_location.Current is null)
		{
			return;
		}

		StopGlide();
		_scrubber.Begin(_selectedTime ?? AppClock.Now);
		_scrubber.Step(TimeScrubber.ScaleOf((InfoPage)_slider.Page) * (KeyStepShare * direction));
		SelectTime(_scrubber.Time);
	}

	private void SelectTime(DateTimeOffset time)
	{
		_selectedTime = time;
		NowButton.IsVisible = true;
		Show(time, isFollowingFinger: true);
	}

	private void ReturnToNow()
	{
		StopGlide();

		if (_selectedTime is null)
		{
			return;
		}

		_selectedTime = null;
		NowButton.IsVisible = false;
		Refresh();
	}

	private void OnNowClicked(object? sender, EventArgs e) => ReturnToNow();

	private void OnPagerClicked(object? sender, EventArgs e) => _slider.ShowNext();

	/// <summary>Nothing moves behind the settings, so the sensors can rest there too.</summary>
	private void UpdateMotion()
	{
		if (_settings is null)
		{
			_motion.Start();
		}
		else
		{
			_motion.Stop();
		}
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
			TopInset(probe),
			Math.Max(0.0, SafeArea.Width - probe.Right),
			Math.Max(0.0, SafeArea.Height - probe.Bottom));

		if (insets != _insets)
		{
			_insets = insets;
			ApplyLayout();
		}
	}

	private double TopInset(Rect probe)
	{
#if WINDOWS
		// Windows has no safe areas, but the caption buttons lie over the page.
		return Math.Max(probe.Top, Platforms.Windows.WindowChrome.CaptionHeight(this));
#else
		return probe.Top;
#endif
	}

	private void ApplyLayout()
	{
		if (Width <= 0 || Height <= 0)
		{
			return;
		}

		SceneLayout layout = SceneLayout.Compute(Width, Height, _insets);
		_layout = layout;
		_drawable.Layout = layout;
		_motion.ScreenRotation = DeviceDisplay.Current.MainDisplayInfo.Rotation switch
		{
			DisplayRotation.Rotation90 => 90,
			DisplayRotation.Rotation180 => 180,
			DisplayRotation.Rotation270 => 270,
			_ => 0,
		};

		ClockLabel.CharacterSpacing = ClockTracking * layout.ClockFontSize;
		Place(ClockLabel, layout.ClockFontSize, layout.TextTop + layout.ClockFontSize / 2.0);
		Place(ContextLabel, layout.CaptionFontSize, layout.ContextCenterY);
		_slider.ApplyLayout(layout);

		PagerButton.WidthRequest = PagerButtonWidth;
		PagerButton.HeightRequest = PagerButtonHeight;
		PagerButton.Margin = new Thickness(0, layout.DotsCenterY - PagerButtonHeight / 2.0, 0, 0);

		SizeSettingsCard();
		Scene.Invalidate();
	}

	private static void Place(Label label, double fontSize, double centerY)
	{
		double height = fontSize * LabelHeightFactor;

		label.FontSize = fontSize;
		label.HeightRequest = height;
		label.Margin = new Thickness(0, centerY - height / 2.0, 0, 0);
	}

	private void OnSettingsClicked(object? sender, EventArgs e) => OpenSettings();

	private void OnScrimTapped(object? sender, TappedEventArgs e) => CloseSettings();

	private void OpenSettings()
	{
		if (_settings is not null)
		{
			return;
		}

		StopGlide();
		_settings = new SettingsView(_location);
		_settings.CloseRequested += (_, _) => CloseSettings();
		UpdateMotion();

#if IOS
		PresentSheet(_settings);
#else
		SettingsStyle style = SettingsStyle.Current;

		SettingsCard.Content = _settings;
		SettingsCard.SetAppThemeColor(BackgroundColorProperty, style.Surface.Light, style.Surface.Dark);
		_settings.PreferredHeightChanged += (_, _) => SizeSettingsCard();

		SizeSettingsCard();
		SettingsOverlay.IsVisible = true;
		SetBackgroundAccessible(false);
		_settings.FocusFirstControl();
#endif
	}

#if IOS
	private async void PresentSheet(SettingsView settings)
	{
		var sheet = new ContentPage { Content = settings };
		sheet.SetAppThemeColor(BackgroundColorProperty, SettingsStyle.Current.Surface.Light, SettingsStyle.Current.Surface.Dark);

		// The sheet can also be swiped away, which never reaches CloseSettings.
		sheet.Disappearing += (_, _) =>
		{
			_settings = null;
			UpdateMotion();
		};

		Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page.SetModalPresentationStyle(
			sheet.On<Microsoft.Maui.Controls.PlatformConfiguration.iOS>(),
			Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.UIModalPresentationStyle.PageSheet);

		await Navigation.PushModalAsync(sheet);
	}

	private async void CloseSettings()
	{
		if (Navigation.ModalStack.Count > 0)
		{
			await Navigation.PopModalAsync();
		}
	}
#else
	private void CloseSettings()
	{
		if (_settings is null)
		{
			return;
		}

		_settings = null;
		SettingsOverlay.IsVisible = false;
		SettingsCard.Content = null;
		SetBackgroundAccessible(true);
		SettingsButton.Focus();
		UpdateMotion();
	}
#endif

	private void SizeSettingsCard()
	{
		SettingsStyle style = SettingsStyle.Current;
		bool isBottomSheet = style.IsBottomSheet && Width < DialogMaximumWidth * 1.5;

		// A fixed width, because a maximum only limits the card on Windows and not its content.
		SettingsCard.WidthRequest = isBottomSheet ? -1.0 : Math.Min(DialogMaximumWidth, Width - 2.0 * DialogMargin);
		SettingsCard.VerticalOptions = isBottomSheet ? LayoutOptions.End : LayoutOptions.Center;
		SettingsCard.StrokeShape = isBottomSheet
			? new RoundRectangle { CornerRadius = new CornerRadius(style.CornerRadius, style.CornerRadius, 0, 0) }
			: new RoundRectangle { CornerRadius = style.CornerRadius };

		// A sheet runs to the screen edge behind the gesture bar, so its content keeps clear by itself.
		SettingsHost.SafeAreaEdges = isBottomSheet ? new SafeAreaEdges(SafeAreaRegions.SoftInput) : SafeAreaEdges.All;
		SettingsCard.Padding = new Thickness(0, 0, 0, isBottomSheet ? _insets.Bottom : 0.0);

		double preferred = (_settings?.PreferredHeight ?? 0.0) + SettingsCard.Padding.VerticalThickness;
		double available = Math.Max(0.0, Height * DialogMaximumHeightFraction);
		SettingsCard.HeightRequest = preferred > 0.0 ? Math.Min(preferred, available) : available;
	}

	/// <summary>Keeps screen readers inside the dialog while it is open.</summary>
	private void SetBackgroundAccessible(bool isAccessible)
	{
		// Set one by one, because excluding a whole layout has no effect on Windows.
		foreach (VisualElement element in new VisualElement[] { Scene, ClockLabel, ContextLabel, PagerButton, NowButton, SettingsButton })
		{
			AutomationProperties.SetIsInAccessibleTree(element, isAccessible);
		}

		_slider.SetIsInAccessibleTree(isAccessible);
	}
}
