using System.Globalization;
using WheresMoon.App.Resources.Strings;
using WheresMoon.App.Services;
using WheresMoon.Core;

namespace WheresMoon.App.Settings;

/// <summary>Content of the settings dialog. How it is presented differs per system, see MainPage.</summary>
public partial class SettingsView : ContentView
{
	private const string AstronomyLicenseFile = "licenses/astronomy-engine-mit.txt";
	private const string ManropeLicenseFile = "licenses/manrope-ofl.txt";

	private readonly LocationService _location;

	public event EventHandler? CloseRequested;

	/// <summary>Raised when the content grows or shrinks, for example when a license text opens.</summary>
	public event EventHandler? PreferredHeightChanged;

	/// <summary>Height that shows everything without scrolling.</summary>
	public double PreferredHeight => Header.Height + Sections.Height + Scroller.Padding.VerticalThickness;

	internal SettingsView(LocationService location)
	{
		InitializeComponent();

		_location = location;

		ApplyStyle(SettingsStyle.Current);
		ApplyTexts();

		Header.SizeChanged += (_, _) => PreferredHeightChanged?.Invoke(this, EventArgs.Empty);
		Sections.SizeChanged += (_, _) => PreferredHeightChanged?.Invoke(this, EventArgs.Empty);

		AutomaticOption.IsChecked = !location.IsManual;
		ManualOption.IsChecked = location.IsManual;
		ManualFields.IsVisible = location.IsManual;

		if (location.IsManual && location.Current is GeoLocation current)
		{
			LatitudeEntry.Text = current.Latitude.ToString(CultureInfo.CurrentCulture);
			LongitudeEntry.Text = current.Longitude.ToString(CultureInfo.CurrentCulture);
			ElevationEntry.Text = current.Elevation.ToString(CultureInfo.CurrentCulture);
		}

		UpdateStatus();
	}

	/// <summary>Focus target when the dialog opens, for keyboards and screen readers.</summary>
	public void FocusFirstControl() => CloseButton.Focus();

	private void ApplyTexts()
	{
		TitleLabel.Text = Strings.Get("SettingsTitle");
		CloseButton.Text = Strings.Get("Close");
		LocationHeader.Text = Strings.Get("LocationHeader");
		AutomaticOption.Content = Strings.Get("LocationAutomatic");
		ManualOption.Content = Strings.Get("LocationManual");
		LatitudeEntry.Placeholder = Strings.Get("Latitude");
		LongitudeEntry.Placeholder = Strings.Get("Longitude");
		ElevationEntry.Placeholder = Strings.Get("Elevation");
		ErrorLabel.Text = Strings.Get("InvalidCoordinates");
		ApplyButton.Text = Strings.Get("Apply");
		AboutHeader.Text = Strings.Get("AboutHeader");
		AboutLabel.Text = Strings.Get("AboutText");
		VersionLabel.Text = Strings.Format("Version", AppInfo.Current.VersionString);
		LicensesHeader.Text = Strings.Get("LicensesHeader");
		AstronomyLicenseName.Text = Strings.Get("LicenseAstronomy");
		ManropeLicenseName.Text = Strings.Get("LicenseManrope");
		AstronomyLicenseButton.Text = Strings.Get("ShowLicense");
		ManropeLicenseButton.Text = Strings.Get("ShowLicense");

		// Entries have no visible label, so their meaning must reach screen readers another way.
		SemanticProperties.SetDescription(LatitudeEntry, LatitudeEntry.Placeholder);
		SemanticProperties.SetDescription(LongitudeEntry, LongitudeEntry.Placeholder);
		SemanticProperties.SetDescription(ElevationEntry, ElevationEntry.Placeholder);
	}

	private void ApplyStyle(SettingsStyle style)
	{
		TitleLabel.FontSize = style.TitleSize;

		foreach (Label label in new[] { TitleLabel, LocationHeader, AboutHeader, AboutLabel, LicensesHeader, AstronomyLicenseName, ManropeLicenseName })
		{
			label.SetAppThemeColor(Label.TextColorProperty, style.Text.Light, style.Text.Dark);
		}

		foreach (Label label in new[] { StatusLabel, VersionLabel, AstronomyLicenseText, ManropeLicenseText })
		{
			label.SetAppThemeColor(Label.TextColorProperty, style.SecondaryText.Light, style.SecondaryText.Dark);
		}

		ErrorLabel.SetAppThemeColor(Label.TextColorProperty, style.Error.Light, style.Error.Dark);

		foreach (RadioButton option in new[] { AutomaticOption, ManualOption })
		{
			option.SetAppThemeColor(RadioButton.TextColorProperty, style.Text.Light, style.Text.Dark);
		}

		foreach (Entry entry in new[] { LatitudeEntry, LongitudeEntry, ElevationEntry })
		{
			entry.SetAppThemeColor(Entry.TextColorProperty, style.Text.Light, style.Text.Dark);
			entry.SetAppThemeColor(Entry.PlaceholderColorProperty, style.SecondaryText.Light, style.SecondaryText.Dark);
		}

		foreach (Button button in new[] { CloseButton, AstronomyLicenseButton, ManropeLicenseButton })
		{
			button.SetAppThemeColor(Button.TextColorProperty, style.Accent.Light, style.Accent.Dark);
		}

		ApplyButton.SetAppThemeColor(BackgroundColorProperty, style.Accent.Light, style.Accent.Dark);
		ApplyButton.SetAppThemeColor(Button.TextColorProperty, style.OnAccent.Light, style.OnAccent.Dark);
	}

	private void UpdateStatus()
	{
		string text;

		if (_location.Status == LocationStatus.Denied && !_location.IsManual)
		{
			text = Strings.Get("LocationDenied");
		}
		else if (_location.Current is GeoLocation current)
		{
			text = Strings.Format("LocationCurrent", current.Latitude.ToString("0.00", CultureInfo.CurrentCulture), current.Longitude.ToString("0.00", CultureInfo.CurrentCulture));
		}
		else
		{
			text = Strings.Get("LocationUnavailable");
		}

		StatusLabel.Text = text;
	}

	private void OnCloseClicked(object? sender, EventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);

	private async void OnModeChanged(object? sender, CheckedChangedEventArgs e)
	{
		// Both options report a change, but only the one that became checked matters.
		if (!e.Value || ReferenceEquals(sender, ManualOption) == _location.IsManual)
		{
			ManualFields.IsVisible = ManualOption.IsChecked;
			return;
		}

		ManualFields.IsVisible = ManualOption.IsChecked;

		if (ReferenceEquals(sender, AutomaticOption))
		{
			ErrorLabel.IsVisible = false;
			await _location.SetAutomaticAsync();
			UpdateStatus();
		}
	}

	private void OnApplyClicked(object? sender, EventArgs e)
	{
		bool isValid = CoordinateParser.TryParse(LatitudeEntry.Text, LongitudeEntry.Text, ElevationEntry.Text, out GeoLocation location);

		ErrorLabel.IsVisible = !isValid;

		if (isValid)
		{
			_location.SetManual(location);
			UpdateStatus();
		}
	}

	private async void OnAstronomyLicenseClicked(object? sender, EventArgs e) =>
		await ToggleLicenseAsync(AstronomyLicenseText, AstronomyLicenseButton, AstronomyLicenseFile);

	private async void OnManropeLicenseClicked(object? sender, EventArgs e) =>
		await ToggleLicenseAsync(ManropeLicenseText, ManropeLicenseButton, ManropeLicenseFile);

	private static async Task ToggleLicenseAsync(Label text, Button button, string file)
	{
		if (string.IsNullOrEmpty(text.Text))
		{
			await using Stream stream = await FileSystem.OpenAppPackageFileAsync(file);
			using var reader = new StreamReader(stream);
			text.Text = await reader.ReadToEndAsync();
		}

		text.IsVisible = !text.IsVisible;
		button.Text = Strings.Get(text.IsVisible ? "HideLicense" : "ShowLicense");
	}
}
