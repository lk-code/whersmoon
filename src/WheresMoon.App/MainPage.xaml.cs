using Microsoft.Maui.Controls.Shapes;
using WheresMoon.App.Rendering;
using WheresMoon.App.Resources.Strings;
using WheresMoon.App.Services;
using WheresMoon.App.Settings;
using WheresMoon.Core;
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

	/// <summary>Timer tick after the full minute, so that the clock never shows the old minute.</summary>
	private static readonly TimeSpan TickMargin = TimeSpan.FromMilliseconds(50);

	private readonly SceneDrawable _drawable = new();
	private readonly SceneAnimator _animator;
	private readonly IDispatcherTimer _timer;
	private readonly LocationService _location = new();

	private SafeInsets _insets = SafeInsets.None;
	private SettingsView? _settings;
	private bool _hasAppeared;

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

		_location.Changed += Refresh;

		SemanticProperties.SetDescription(SettingsButton, Strings.Get("SettingsButton"));

#if WINDOWS
		Loaded += (_, _) => Platforms.Windows.WindowChrome.Apply(this, Scene, CloseSettings);
#endif

		// Everything needed for the first frame is at hand, so it already shows the real sky.
		Refresh();
	}

	/// <summary>Recomputes everything for the current moment and schedules the next full minute.</summary>
	public void Refresh()
	{
		// Reading the zone afresh on every tick also picks up time zone changes on all platforms.
		TimeZoneInfo.ClearCachedData();
		TimeZoneInfo zone = TimeZoneInfo.Local;

		DateTimeOffset now = AppClock.Now;
		ClockLabel.Text = SkyTexts.Clock(TimeZoneInfo.ConvertTime(now, zone));

		if (_location.Current is GeoLocation location)
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
				await _location.UpdateAsync();
			};
			window.Activated += (_, _) => Refresh();
			window.Stopped += (_, _) => _timer.Stop();
		}

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
		_drawable.Layout = layout;

		ClockLabel.CharacterSpacing = ClockTracking * layout.ClockFontSize;
		Place(ClockLabel, layout.ClockFontSize, layout.TextTop + layout.ClockFontSize / 2.0);
		Place(SunLabel, layout.RiseSetFontSize, layout.RiseSetLine1CenterY);
		Place(MoonLabel, layout.RiseSetFontSize, layout.RiseSetLine2CenterY);
		Place(PhaseLabel, layout.PhaseFontSize, layout.PhaseLineCenterY);

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

		_settings = new SettingsView(_location);
		_settings.CloseRequested += (_, _) => CloseSettings();

#if IOS
		PresentSheet(_settings);
#else
		SettingsStyle style = SettingsStyle.Current;

		SettingsCard.Content = _settings;
		SettingsCard.SetAppThemeColor(BackgroundColorProperty, style.Surface.Light, style.Surface.Dark);
		SettingsCard.StrokeShape = style.IsBottomSheet
			? new RoundRectangle { CornerRadius = new CornerRadius(style.CornerRadius, style.CornerRadius, 0, 0) }
			: new RoundRectangle { CornerRadius = style.CornerRadius };
		SettingsCard.VerticalOptions = style.IsBottomSheet ? LayoutOptions.End : LayoutOptions.Center;

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
		sheet.Disappearing += (_, _) => _settings = null;

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
	}
#endif

	private void SizeSettingsCard()
	{
		bool isBottomSheet = SettingsStyle.Current.IsBottomSheet && Width < DialogMaximumWidth * 1.5;

		// A fixed width, because a maximum only limits the card on Windows and not its content.
		SettingsCard.WidthRequest = isBottomSheet ? -1.0 : Math.Min(DialogMaximumWidth, Width - 2.0 * DialogMargin);
		SettingsCard.MaximumHeightRequest = Math.Max(0.0, Height * DialogMaximumHeightFraction);
	}

	/// <summary>Keeps screen readers inside the dialog while it is open.</summary>
	private void SetBackgroundAccessible(bool isAccessible)
	{
		// Set one by one, because excluding a whole layout has no effect on Windows.
		foreach (VisualElement element in new VisualElement[] { Scene, ClockLabel, SunLabel, MoonLabel, PhaseLabel, SettingsButton })
		{
			AutomationProperties.SetIsInAccessibleTree(element, isAccessible);
		}
	}
}
