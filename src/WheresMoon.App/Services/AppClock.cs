using System.Globalization;

namespace WheresMoon.App.Services;

/// <summary>
/// Source of the current time. Debug builds can start from another moment through the
/// environment variable WHERESMOON_SIM_TIME, to look at any time of day.
/// </summary>
internal static class AppClock
{
#if DEBUG
	private static readonly TimeSpan Offset = ReadOffset();

	public static DateTimeOffset Now => DateTimeOffset.UtcNow + Offset;

	private static TimeSpan ReadOffset()
	{
		string? value = Environment.GetEnvironmentVariable("WHERESMOON_SIM_TIME");
		return value is null
			? TimeSpan.Zero
			: DateTimeOffset.Parse(value, CultureInfo.InvariantCulture) - DateTimeOffset.UtcNow;
	}
#else
	public static DateTimeOffset Now => DateTimeOffset.UtcNow;
#endif
}
