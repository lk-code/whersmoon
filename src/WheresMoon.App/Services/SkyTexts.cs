using System.Globalization;
using WheresMoon.App.Controls;
using WheresMoon.App.Resources.Strings;
using WheresMoon.Core;
using WheresMoon.Core.Places;

namespace WheresMoon.App.Services;

/// <summary>Turns a snapshot into the localized lines on the main page.</summary>
internal static class SkyTexts
{
	private const double MediumAltitude = 20.0;
	private const double HighAltitude = 45.0;

	public static string Clock(DateTimeOffset time) => Format(time);

	/// <summary>Line below the clock: a chosen place and the date of a day other than today.</summary>
	public static string Context(Place? place, DateTimeOffset time, DateTimeOffset now)
	{
		string[] parts =
		[
			place is null ? string.Empty : PlaceName(place),
			time.Date == now.Date ? string.Empty : time.ToString(Strings.Get(time.Year == now.Year ? "DateLong" : "DateLongYear"), CultureInfo.CurrentCulture),
		];

		return string.Join(" · ", parts.Where(part => part.Length > 0));
	}

	public static string PlaceLabel(Place place) => place.FullName(PlaceName(place), CountryName(place.Country));

	public static IReadOnlyList<InfoRow> SunAndMoonRows(SkySnapshot sky, DayLengthInfo day) =>
	[
		new(SunLine(sky, day), InfoRowStyle.Primary),
		new(Line(sky.Moon.Body, "Moonrise", "Moonset", "NoMoonrise", "NoMoonset"), InfoRowStyle.Primary),
		new($"{PhaseName(sky.Moon.PhaseName)} · {Percent(sky.Moon.IlluminatedFraction)}", InfoRowStyle.Secondary),
		new(DayLengthLine(day), InfoRowStyle.Caption),
	];

	public static IReadOnlyList<InfoRow> EventRows(IReadOnlyList<SkyEvent> events, DateTimeOffset time, TimeZoneInfo zone)
	{
		DateTimeOffset now = TimeZoneInfo.ConvertTime(time, zone);

		return events.Select(item => new InfoRow(EventLine(item, now, zone), InfoRowStyle.List)).ToList();
	}

	public static IReadOnlyList<InfoRow> LightRows(LightHours hours)
	{
		if (hours.Windows.Count == 0)
		{
			return
			[
				new(Strings.Get("NoLightHours"), InfoRowStyle.List),
				new(Strings.Get(hours.Range == SunRange.AlwaysHigh ? "SunStaysHigh" : "SunStaysLow"), InfoRowStyle.Caption),
			];
		}

		var rows = new List<InfoRow>();

		foreach (IGrouping<DayPart, LightWindow> part in hours.Windows.GroupBy(window => window.Part))
		{
			string prefix = $"{Strings.Get($"Part{part.Key}")}: ";

			foreach (LightWindow window in part)
			{
				rows.Add(new InfoRow($"{prefix}{WindowText(window)}", InfoRowStyle.List));
				prefix = string.Empty;
			}
		}

		if (!hours.Windows.Any(window => window.Kind == LightKind.Blue))
		{
			rows.Add(new InfoRow(Strings.Get("NoBlueHour"), InfoRowStyle.Caption));
		}
		else if (!hours.Windows.Any(window => window.Kind == LightKind.Golden))
		{
			rows.Add(new InfoRow(Strings.Get("NoGoldenHour"), InfoRowStyle.Caption));
		}

		return rows;
	}

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

	/// <summary>A place set by coordinates has no name, so those stand in for it.</summary>
	private static string PlaceName(Place place) => place.Name.Length > 0
		? place.Name
		: string.Create(CultureInfo.CurrentCulture, $"{place.Latitude:0.00}, {place.Longitude:0.00}");

	/// <summary>From the resources, because RegionInfo gives each country's own name on some systems: "Noreg" instead of "Norwegen".</summary>
	private static string CountryName(string code)
	{
		string key = $"Country{code}";
		string name = Strings.Get(key);

		return name == key ? code : name;
	}

	private static string SunLine(SkySnapshot sky, DayLengthInfo day) => day.Kind switch
	{
		DaylightKind.MidnightSun => Strings.Get("MidnightSun"),
		DaylightKind.PolarNight => Strings.Get("PolarNight"),
		_ => Line(sky.Sun.Body, "Sunrise", "Sunset", "NoSunrise", "NoSunset"),
	};

	private static string DayLengthLine(DayLengthInfo day)
	{
		if (day.Kind != DaylightKind.Normal)
		{
			return Strings.Format("DayLengthHours", (int)Math.Round(day.Length.TotalHours));
		}

		int minutes = (int)Math.Round(day.Length.TotalMinutes);
		string length = Strings.Format("DayLength", minutes / 60, minutes % 60);

		if (day.ChangeFromYesterday is not TimeSpan change)
		{
			return length;
		}

		int difference = (int)Math.Round(change.TotalMinutes);
		string comparison = difference switch
		{
			0 => Strings.Get("DaySame"),
			> 0 => Strings.Format("DayLonger", difference),
			_ => Strings.Format("DayShorter", -difference),
		};

		return $"{length} · {comparison}";
	}

	private static string EventLine(SkyEvent item, DateTimeOffset now, TimeZoneInfo zone)
	{
		DateTimeOffset local = TimeZoneInfo.ConvertTime(item.Time, zone);
		string name = Strings.Get($"Event{item.Kind}");
		string date = local.ToString(Strings.Get(local.Year == now.Year ? "DateShort" : "DateWithYear"), CultureInfo.CurrentCulture);

		// Eclipses can be years away, so their line has to make do without the distance.
		return item.Eclipse is EclipseType eclipse
			? $"{name} ({Strings.Get($"Eclipse{eclipse}")}): {date}, {Format(local)}"
			: $"{name}: {date}, {Format(local)} · {DaysAway(local, now)}";
	}

	private static string DaysAway(DateTimeOffset time, DateTimeOffset now) => (time.Date - now.Date).Days switch
	{
		0 => Strings.Get("EventToday"),
		1 => Strings.Get("EventTomorrow"),
		int days => Strings.Format("EventInDays", days),
	};

	private static string WindowText(LightWindow window)
	{
		string name = Strings.Get(window.Kind == LightKind.Golden ? "GoldenHour" : "BlueHour");

		return (window.Start, window.End) switch
		{
			(DateTimeOffset start, DateTimeOffset end) => Strings.Format("LightRange", name, Format(start), Format(end)),
			(null, DateTimeOffset end) => Strings.Format("LightUntil", name, Format(end)),
			(DateTimeOffset start, null) => Strings.Format("LightFrom", name, Format(start)),
			_ => Strings.Format("LightAllDay", name),
		};
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
