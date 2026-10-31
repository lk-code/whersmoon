using System.Globalization;
using WheresMoon.App.Resources.Strings;
using WheresMoon.App.Services;
using WheresMoon.Core;
using WheresMoon.Core.Places;

namespace WheresMoon.App.Settings;

/// <summary>Content of the settings dialog. How it is presented differs per system, see MainPage.</summary>
public partial class SettingsView : ContentView
{
	private const string AstronomyLicenseFile = "licenses/astronomy-engine-mit.txt";
	private const string ManropeLicenseFile = "licenses/manrope-ofl.txt";
	private const string GeoNamesLicenseFile = "licenses/geonames-cc-by-4.0.txt";
	private const string UnicodeLicenseFile = "licenses/unicode-cldr.txt";

	private const int ResultLimit = 8;

	private readonly LocationService _location;
	private int _searchVersion;

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
		ShowRecentPlaces();

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
		SearchEntry.Placeholder = Strings.Get("SearchPlaceholder");
		RecentHeader.Text = Strings.Get("RecentPlaces");
		CoordinatesButton.Text = Strings.Get("EnterCoordinates");
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
		GeoNamesLicenseName.Text = Strings.Get("LicenseGeoNames");
		UnicodeLicenseName.Text = Strings.Get("LicenseUnicode");
		AstronomyLicenseButton.Text = Strings.Get("ShowLicense");
		ManropeLicenseButton.Text = Strings.Get("ShowLicense");
		GeoNamesLicenseButton.Text = Strings.Get("ShowLicense");
		UnicodeLicenseButton.Text = Strings.Get("ShowLicense");

		// Entries have no visible label, so their meaning must reach screen readers another way.
		SemanticProperties.SetDescription(SearchEntry, SearchEntry.Placeholder);
		SemanticProperties.SetDescription(LatitudeEntry, LatitudeEntry.Placeholder);
		SemanticProperties.SetDescription(LongitudeEntry, LongitudeEntry.Placeholder);
		SemanticProperties.SetDescription(ElevationEntry, ElevationEntry.Placeholder);
	}

	private void ApplyStyle(SettingsStyle style)
	{
		TitleLabel.FontSize = style.TitleSize;

		foreach (Label label in new[] { TitleLabel, LocationHeader, AboutHeader, AboutLabel, LicensesHeader, AstronomyLicenseName, ManropeLicenseName, GeoNamesLicenseName, UnicodeLicenseName })
		{
			label.SetAppThemeColor(Label.TextColorProperty, style.Text.Light, style.Text.Dark);
		}

		foreach (Label label in new[] { StatusLabel, RecentHeader, VersionLabel, AstronomyLicenseText, ManropeLicenseText, GeoNamesLicenseText, UnicodeLicenseText })
		{
			label.SetAppThemeColor(Label.TextColorProperty, style.SecondaryText.Light, style.SecondaryText.Dark);
		}

		ErrorLabel.SetAppThemeColor(Label.TextColorProperty, style.Error.Light, style.Error.Dark);

		foreach (RadioButton option in new[] { AutomaticOption, ManualOption })
		{
			option.SetAppThemeColor(RadioButton.TextColorProperty, style.Text.Light, style.Text.Dark);
		}

		foreach (Entry entry in new[] { SearchEntry, LatitudeEntry, LongitudeEntry, ElevationEntry })
		{
			entry.SetAppThemeColor(Entry.TextColorProperty, style.Text.Light, style.Text.Dark);
			entry.SetAppThemeColor(Entry.PlaceholderColorProperty, style.SecondaryText.Light, style.SecondaryText.Dark);
		}

		foreach (Button button in new[] { CloseButton, CoordinatesButton, AstronomyLicenseButton, ManropeLicenseButton, GeoNamesLicenseButton, UnicodeLicenseButton })
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
		else if (_location.Place is Place place)
		{
			text = SkyTexts.PlaceLabel(place);
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

	private async void OnApplyClicked(object? sender, EventArgs e)
	{
		bool isValid = CoordinateParser.TryParse(LatitudeEntry.Text, LongitudeEntry.Text, ElevationEntry.Text, out GeoLocation location);

		ErrorLabel.IsVisible = !isValid;

		if (isValid)
		{
			// A spot without a name still has a local time, which the nearest city tells.
			PlaceIndex? places = await PlaceCatalog.LoadAsync();
			string zone = places?.Nearest(location.Latitude, location.Longitude)?.Zone ?? TimeZoneInfo.Local.Id;

			Choose(new Place(string.Empty, string.Empty, string.Empty, location.Latitude, location.Longitude, location.Elevation, zone));
		}
	}

	private async void OnSearchChanged(object? sender, TextChangedEventArgs e)
	{
		int version = ++_searchVersion;
		string query = e.NewTextValue ?? string.Empty;

		if (SearchText.Fold(query).Length == 0)
		{
			ShowResults(null);
			return;
		}

		PlaceIndex? places = await PlaceCatalog.LoadAsync();

		// The list can take a moment to load, and meanwhile there may be a newer query.
		if (version == _searchVersion)
		{
			ShowResults(places?.Search(query, ResultLimit) ?? []);
		}
	}

	private void ShowResults(IReadOnlyList<Place>? results)
	{
		ResultList.Clear();

		bool isSearching = results is not null;
		RecentHeader.IsVisible = !isSearching && _location.RecentPlaces.Count > 0;
		RecentList.IsVisible = !isSearching;

		if (results?.Count == 0)
		{
			var label = new Label { Text = Strings.Get("NoPlacesFound"), FontSize = 13 };
			label.SetAppThemeColor(Label.TextColorProperty, SettingsStyle.Current.SecondaryText.Light, SettingsStyle.Current.SecondaryText.Dark);
			ResultList.Add(label);
		}

		foreach (Place place in results ?? [])
		{
			ResultList.Add(PlaceButton(place));
		}
	}

	private void ShowRecentPlaces()
	{
		RecentList.Clear();

		foreach (Place place in _location.RecentPlaces)
		{
			RecentList.Add(PlaceButton(place));
		}

		RecentHeader.IsVisible = RecentList.Count > 0;
	}

	private Button PlaceButton(Place place)
	{
		SettingsStyle style = SettingsStyle.Current;
		var button = new Button
		{
			Text = SkyTexts.PlaceLabel(place),
			BackgroundColor = Colors.Transparent,
			BorderWidth = 0,
			Padding = new Thickness(0, 6),
			HorizontalOptions = LayoutOptions.Start,
			LineBreakMode = LineBreakMode.TailTruncation,
		};

		button.SetAppThemeColor(Button.TextColorProperty, style.Text.Light, style.Text.Dark);
		button.Clicked += (_, _) => Choose(place);
		return button;
	}

	private void Choose(Place place)
	{
		ErrorLabel.IsVisible = false;
		_location.SetManual(place);

		SearchEntry.Text = string.Empty;
		SearchEntry.Unfocus();
		ShowResults(null);
		ShowRecentPlaces();
		UpdateStatus();
	}

	private void OnCoordinatesClicked(object? sender, EventArgs e)
	{
		CoordinateFields.IsVisible = !CoordinateFields.IsVisible;
		CoordinatesButton.Text = Strings.Get(CoordinateFields.IsVisible ? "HideCoordinates" : "EnterCoordinates");
	}

	private async void OnAstronomyLicenseClicked(object? sender, EventArgs e) =>
		await ToggleLicenseAsync(AstronomyLicenseText, AstronomyLicenseButton, AstronomyLicenseFile);

	private async void OnManropeLicenseClicked(object? sender, EventArgs e) =>
		await ToggleLicenseAsync(ManropeLicenseText, ManropeLicenseButton, ManropeLicenseFile);

	private async void OnGeoNamesLicenseClicked(object? sender, EventArgs e) =>
		await ToggleLicenseAsync(GeoNamesLicenseText, GeoNamesLicenseButton, GeoNamesLicenseFile);

	private async void OnUnicodeLicenseClicked(object? sender, EventArgs e) =>
		await ToggleLicenseAsync(UnicodeLicenseText, UnicodeLicenseButton, UnicodeLicenseFile);

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
