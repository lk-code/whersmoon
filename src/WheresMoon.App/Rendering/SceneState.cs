using WheresMoon.Core;

namespace WheresMoon.App.Rendering;

/// <summary>Everything the drawing depends on besides the layout. Numeric, so it can be animated.</summary>
public sealed record SceneState
{
	public double SunAltitude { get; init; } = -30.0;
	public bool SunIsRising { get; init; }
	public double SunProgress { get; init; } = 0.5;
	public double SunOpacity { get; init; }
	public double MoonProgress { get; init; } = 0.5;
	public double MoonOpacity { get; init; }
	public double MoonFraction { get; init; }
	public double MoonZenithAngle { get; init; }

	public static SceneState From(SkySnapshot sky, SceneState? previous) => new()
	{
		SunAltitude = sky.Sun.GeometricAltitude,
		SunIsRising = sky.Sun.IsRising,
		SunProgress = sky.Sun.Body.ArcProgress ?? previous?.SunProgress ?? 0.5,
		SunOpacity = sky.Sun.Body.ArcProgress is null ? 0.0 : 1.0,
		MoonProgress = sky.Moon.Body.ArcProgress ?? previous?.MoonProgress ?? 0.5,
		MoonOpacity = sky.Moon.Body.ArcProgress is null ? 0.0 : 1.0,
		MoonFraction = sky.Moon.IlluminatedFraction,
		MoonZenithAngle = sky.Moon.BrightLimbZenithAngle,
	};

	public static SceneState Lerp(SceneState from, SceneState to, double t) => new()
	{
		SunAltitude = Blend(from.SunAltitude, to.SunAltitude, t),
		SunIsRising = to.SunIsRising,
		SunProgress = BlendProgress(from.SunProgress, from.SunOpacity, to.SunProgress, t),
		SunOpacity = Blend(from.SunOpacity, to.SunOpacity, t),
		MoonProgress = BlendProgress(from.MoonProgress, from.MoonOpacity, to.MoonProgress, t),
		MoonOpacity = Blend(from.MoonOpacity, to.MoonOpacity, t),
		MoonFraction = Blend(from.MoonFraction, to.MoonFraction, t),
		MoonZenithAngle = BlendAngle(from.MoonZenithAngle, to.MoonZenithAngle, t),
	};

	private static double Blend(double from, double to, double t) => from + (to - from) * t;

	/// <summary>A body that reappears, or wraps around near the poles, must not sweep backwards across its arc.</summary>
	private static double BlendProgress(double from, double fromOpacity, double to, double t) =>
		fromOpacity <= 0.0 || Math.Abs(to - from) > 0.5 ? to : Blend(from, to, t);

	private static double BlendAngle(double from, double to, double t)
	{
		double difference = (to - from) % 360.0;
		if (difference > 180.0)
		{
			difference -= 360.0;
		}
		else if (difference < -180.0)
		{
			difference += 360.0;
		}

		return from + difference * t;
	}
}
