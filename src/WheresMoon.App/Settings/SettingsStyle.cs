namespace WheresMoon.App.Settings;

/// <summary>A colour for light and for dark appearance.</summary>
internal readonly record struct ThemedColor(Color Light, Color Dark);

/// <summary>
/// Colours and shapes that make the dialog look at home on each system. Chosen in code,
/// because the OnPlatform markup extension does not work with full trimming.
/// </summary>
internal sealed record SettingsStyle(
	ThemedColor Surface,
	ThemedColor Text,
	ThemedColor SecondaryText,
	ThemedColor Accent,
	ThemedColor OnAccent,
	ThemedColor Error,
	double CornerRadius,
	double TitleSize,
	bool IsBottomSheet)
{
	private static readonly SettingsStyle Fluent = new(
		Themed("#F3F3F3", "#2B2B2B"),
		Themed("#1B1B1B", "#FFFFFF"),
		Themed("#5C5C5C", "#C8C8C8"),
		Themed("#0067C0", "#60CDFF"),
		Themed("#FFFFFF", "#003A5C"),
		Themed("#C42B1C", "#FF99A4"),
		CornerRadius: 8,
		TitleSize: 20,
		IsBottomSheet: false);

	private static readonly SettingsStyle Material = new(
		Themed("#F7F2FA", "#2B2930"),
		Themed("#1D1B20", "#E6E0E9"),
		Themed("#49454F", "#CAC4D0"),
		Themed("#6750A4", "#D0BCFF"),
		Themed("#FFFFFF", "#381E72"),
		Themed("#B3261E", "#F2B8B5"),
		CornerRadius: 28,
		TitleSize: 22,
		IsBottomSheet: true);

	private static readonly SettingsStyle Cupertino = new(
		Themed("#F2F2F7", "#1C1C1E"),
		Themed("#000000", "#FFFFFF"),
		Themed("#6C6C70", "#AEAEB2"),
		Themed("#007AFF", "#0A84FF"),
		Themed("#FFFFFF", "#FFFFFF"),
		Themed("#FF3B30", "#FF453A"),
		CornerRadius: 12,
		TitleSize: 17,
		IsBottomSheet: false);

	public static SettingsStyle Current
	{
		get
		{
			DevicePlatform platform = DeviceInfo.Current.Platform;
			if (platform == DevicePlatform.Android)
			{
				return Material;
			}

			return platform == DevicePlatform.WinUI ? Fluent : Cupertino;
		}
	}

	private static ThemedColor Themed(string light, string dark) => new(Color.FromArgb(light), Color.FromArgb(dark));
}
