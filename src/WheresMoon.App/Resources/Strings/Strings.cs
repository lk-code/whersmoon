using System.Globalization;
using System.Resources;

namespace WheresMoon.App.Resources.Strings;

/// <summary>Typed access to the texts in AppResources.resx. The SDK build does not generate this class.</summary>
internal static class Strings
{
	private static readonly ResourceManager Manager =
		new("WheresMoon.App.Resources.Strings.AppResources", typeof(Strings).Assembly);

	public static string Get(string key) => Manager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

	public static string Format(string key, params object[] values) =>
		string.Format(CultureInfo.CurrentCulture, Get(key), values);
}
