using System.Globalization;
using WheresMoon.App.Resources.Strings;
using WheresMoon.Core;

namespace WheresMoon.App.Services;

/// <summary>Turns a snapshot into the localized lines on the main page.</summary>
internal static class SkyTexts
{
	private const double MediumAltitude = 20.0;
	private const double HighAltitude = 45.0;

	public static string Clock(DateTimeOffset time) => Format(time);

	public static string SunLine(SkySnapshot sky) => Line(
		sky.Sun.Body, "Sunrise", "Sunset", "NoSunrise", "NoSunset");

	public static string MoonLine(SkySnapshot sky) => Line(
		sky.Moon.Body, "Moonrise", "Moonset", "NoMoonrise", "NoMoonset");

	public static string PhaseLine(SkySnapshot sky) =>
		$"{PhaseName(sky.Moon.PhaseName)} · {Percent(sky.Moon.IlluminatedFraction)}";

	/// <summary>Description for screen readers, such as "Sun is high in the southeast, Waxing Crescent, 18% lit, sets at 20:15".</summary>
	public static string Description(SkySnapshot sky)
	{
		BodyInfo sun = sky.Sun.Body;
		BodyInfo moon = sky.Moon.Body;

		string sunText = sun.IsUp
			? Strings.Format("SpeakSunUp", AltitudeWord(sun.Altitude), Compass(sun.Azimuth))
			: Strings.Get("SpeakSunDown");

		string phase = PhaseName(sky.Moon.PhaseName);
		string lit = Percent(sky.Moon.IlluminatedFraction);
		string moonText = moon.IsUp
			? Strings.Format("SpeakMoonUp", phase, lit, AltitudeWord(moon.Altitude), Compass(moon.Azimuth))
			: Strings.Format("SpeakMoonDown", phase, lit);

		TimeEvent? next = moon.IsUp ? moon.Set : moon.Rise;
		if (next is null)
		{
			return $"{sunText}. {moonText}.";
		}

		string nextText = Strings.Format(moon.IsUp ? "SpeakMoonSets" : "SpeakMoonRises", Format(next.Value.Time));
		return $"{sunText}. {moonText}, {nextText}.";
	}

	private static string Line(BodyInfo body, string rise, string set, string noRise, string noSet)
	{
		string riseText = Event(body.Rise, rise, noRise);
		string setText = Event(body.Set, set, noSet);
		return $"{riseText} – {setText}";
	}

	private static string Event(TimeEvent? timeEvent, string label, string missing)
	{
		if (timeEvent is null)
		{
			return Strings.Get(missing);
		}

		TimeEvent value = timeEvent.Value;
		string marker = value.DayOffset switch
		{
			0 => string.Empty,
			> 0 => $" (+{value.DayOffset})",
			_ => $" (−{-value.DayOffset})",
		};

		return $"{Strings.Get(label)}: {Format(value.Time)}{marker}";
	}

	private static string Format(DateTimeOffset time) =>
		time.ToString(TimeFormat.Pattern, CultureInfo.CurrentCulture);

	private static string Percent(double fraction) => fraction.ToString("P0", CultureInfo.CurrentCulture);

	private static string PhaseName(MoonPhaseName name) => Strings.Get($"Phase{name}");

	private static string AltitudeWord(double altitude) => altitude switch
	{
		>= HighAltitude => Strings.Get("AltitudeHigh"),
		>= MediumAltitude => Strings.Get("AltitudeMedium"),
		_ => Strings.Get("AltitudeLow"),
	};

	private static string Compass(double azimuth)
	{
		string[] points = ["N", "NE", "E", "SE", "S", "SW", "W", "NW"];
		int index = (int)Math.Round(azimuth / 45.0) % points.Length;
		return Strings.Get($"Compass{points[index]}");
	}
}
