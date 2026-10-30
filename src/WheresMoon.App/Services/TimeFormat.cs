using System.Globalization;

namespace WheresMoon.App.Services;

/// <summary>
/// The short time pattern the system uses. Android and iOS have a 12/24 hour switch that can
/// differ from the locale, and .NET does not see it there.
/// </summary>
internal static class TimeFormat
{
	public static string Pattern
	{
		get
		{
#if ANDROID
			return Android.Text.Format.DateFormat.Is24HourFormat(Platform.AppContext) ? "HH:mm" : "h:mm tt";
#elif IOS || MACCATALYST
			string? template = Foundation.NSDateFormatter.GetDateFormatFromTemplate("j", 0, Foundation.NSLocale.CurrentLocale);
			return template?.Contains('a') == true ? "h:mm tt" : "HH:mm";
#else
			return CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
#endif
		}
	}
}
