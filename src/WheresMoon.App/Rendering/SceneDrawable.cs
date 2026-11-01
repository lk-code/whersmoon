using Microsoft.Maui.Graphics;
using WheresMoon.Core.Scene;

namespace WheresMoon.App.Rendering;

/// <summary>Draws sky, stars, arcs, sun, moon and hills. Every size comes from <see cref="SceneLayout"/>.</summary>
public sealed class SceneDrawable : IDrawable
{
	/// <summary>Hills start their colour ramp slightly above the horizon, where the highest crest lies.</summary>
	private const double HillGradientTop = 0.04;

	private const int PageCount = 3;

	public SceneState State { get; set; } = new();

	public SceneLayout? Layout { get; set; }

	/// <summary>Position of the info pages, also between them.</summary>
	public double PagePosition { get; set; }

	/// <summary>Direction the user faces in degrees from true north. Stays 0 without a compass, which shows north up.</summary>
	public double Heading { get; set; }

	/// <summary>Shift of the compass by device movement, from -1 to 1 of the largest shift, y pointing down.</summary>
	public PointD RingShift { get; set; }

	/// <summary>Letter for east on the compass, which is "O" in German.</summary>
	public char EastLetter { get; set; } = 'E';

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		SceneLayout layout = Layout ?? SceneLayout.Compute(dirtyRect.Width, dirtyRect.Height, SafeInsets.None);
		SceneState state = State;
		SkyColors colors = SkyPalette.Compute(state.SunAltitude, state.SunIsRising);

		DrawSky(canvas, layout, colors);
		DrawStars(canvas, layout, state);
		DrawArcs(canvas, layout, state);
		DrawSun(canvas, layout, state);
		DrawMoon(canvas, layout, state);
		DrawHill(canvas, layout, HillShapes.Back(layout.Width, layout.Unit, layout.HorizonY), colors.BackHill);
		DrawHill(canvas, layout, HillShapes.Front(layout.Width, layout.Unit, layout.HorizonY), colors.FrontHill);
		DrawSunBelow(canvas, layout, state);
		DrawMoonBelow(canvas, layout, state);
		DrawCompass(canvas, layout, state);
		DrawPageDots(canvas, layout);
	}

	private static void DrawSky(ICanvas canvas, SceneLayout layout, SkyColors colors)
	{
		var bounds = new RectF(0, 0, (float)layout.Width, (float)layout.Height);
		var stops = colors.Stops
			.Select(stop => new PaintGradientStop((float)Math.Min(stop.Position * layout.SkyStopScale, 1.0), ToColor(stop.Color)))
			.ToArray();

		FillWithPaint(canvas, new LinearGradientPaint(stops, new Point(0, 0), new Point(0, 1)), bounds, () => canvas.FillRectangle(bounds));
	}

	private void DrawStars(ICanvas canvas, SceneLayout layout, SceneState state)
	{
		double visibility = StarField.Visibility(state.SunAltitude);
		if (visibility <= 0.0)
		{
			return;
		}

		foreach (StarField.Star star in StarField.Stars)
		{
			double x = StarField.ScreenX(star.Azimuth, Heading, layout.Width);
			double y = star.Y * layout.HorizonY;
			double share = layout.StarShare(x, y);

			// Stars move behind the text, so they fade out there instead of vanishing at its edge.
			if (share > 0.0 && x >= 0.0 && x <= layout.Width)
			{
				canvas.FillColor = Colors.White.WithAlpha((float)(star.Opacity * visibility * share));
				canvas.FillCircle((float)x, (float)y, (float)(star.Radius * layout.DetailScale));
			}
		}
	}

	private static void DrawArcs(ICanvas canvas, SceneLayout layout, SceneState state)
	{
		double day = SkyPalette.Ramp(state.SunAltitude, -6.0, 6.0);

		canvas.SaveState();
		canvas.StrokeSize = (float)(DesignTokens.Size.ArcStrokeWidth * layout.DetailScale);
		canvas.StrokeLineCap = LineCap.Round;

		canvas.StrokeColor = Colors.White.WithAlpha((float)Blend(DesignTokens.Opacity.SunArcNight, DesignTokens.Opacity.SunArcDay, day));
		canvas.DrawPath(ToPath(ArcGeometry.Outline(layout.CenterX, layout.HorizonY, layout.SunArcRadius, layout.SunArcRadius)));

		// All platforms multiply dash lengths by the stroke width, so the pattern is given in stroke widths.
		canvas.StrokeDashPattern =
		[
			(float)(DesignTokens.Size.MoonArcDash / DesignTokens.Size.ArcStrokeWidth),
			(float)(DesignTokens.Size.MoonArcGap / DesignTokens.Size.ArcStrokeWidth),
		];
		canvas.StrokeColor = Colors.White.WithAlpha((float)Blend(DesignTokens.Opacity.MoonArcNight, DesignTokens.Opacity.MoonArcDay, day));
		canvas.DrawPath(ToPath(ArcGeometry.Outline(layout.CenterX, layout.HorizonY, layout.MoonArcRadius, layout.MoonArcRadius)));

		canvas.RestoreState();
	}

	private static void DrawSun(ICanvas canvas, SceneLayout layout, SceneState state)
	{
		if (state.SunOpacity <= 0.0)
		{
			return;
		}

		PointD center = ArcGeometry.PointAt(state.SunProgress, layout.CenterX, layout.HorizonY, layout.SunArcRadius, layout.SunArcRadius);

		double warmth = DesignTokens.Sun.GlowWarmAmount
			* (1.0 - SkyPalette.Ramp(state.SunAltitude, 0.0, DesignTokens.Sun.GlowWarmAltitude));
		Rgba inner = Warmed(DesignTokens.Sun.GlowInner, warmth);
		Rgba middle = Warmed(DesignTokens.Sun.GlowMiddle, warmth);

		DrawGlow(canvas, center, layout.SunGlowRadius, state.SunOpacity,
		[
			new PaintGradientStop(0f, ToColor(inner)),
			new PaintGradientStop((float)DesignTokens.Sun.GlowMiddlePosition, ToColor(middle)),
			new PaintGradientStop(1f, ToColor(middle.WithAlpha(0.0))),
		]);

		canvas.FillColor = ToColor(DesignTokens.Sun.Disc, state.SunOpacity);
		canvas.FillCircle((float)center.X, (float)center.Y, (float)layout.SunRadius);
	}

	private static void DrawMoon(ICanvas canvas, SceneLayout layout, SceneState state)
	{
		if (state.MoonOpacity <= 0.0)
		{
			return;
		}

		PointD center = ArcGeometry.PointAt(state.MoonProgress, layout.CenterX, layout.HorizonY, layout.MoonArcRadius, layout.MoonArcRadius);

		double glow = SkyPalette.Ramp(state.SunAltitude, DesignTokens.Moon.GlowStartAltitude, DesignTokens.Moon.GlowFullAltitude);
		if (glow > 0.0)
		{
			Rgba color = DesignTokens.Moon.Glow;
			DrawGlow(canvas, center, layout.MoonGlowRadius, state.MoonOpacity * glow,
			[
				new PaintGradientStop(0f, ToColor(color)),
				new PaintGradientStop(1f, ToColor(color.WithAlpha(0.0))),
			]);
		}

		DrawMoonDisc(canvas, center, layout.MoonRadius, state, state.MoonOpacity);
	}

	private static void DrawMoonDisc(ICanvas canvas, PointD center, double radius, SceneState state, double opacity)
	{
		canvas.FillColor = ToColor(DesignTokens.Moon.Earthshine, opacity);
		canvas.FillCircle((float)center.X, (float)center.Y, (float)radius);

		canvas.FillColor = ToColor(DesignTokens.Moon.Lit, opacity);
		canvas.FillPath(ToPath(MoonShape.Lit(center, radius, state.MoonFraction, state.MoonZenithAngle)));
	}

	private static void DrawSunBelow(ICanvas canvas, SceneLayout layout, SceneState state)
	{
		if (state.SunBelowOpacity <= 0.0)
		{
			return;
		}

		PointD center = ArcGeometry.LowerPointAt(state.SunBelowProgress, layout.CenterX, layout.HorizonY, layout.SunArcRadius, layout.SunLowerRadiusY);

		DrawHalo(canvas, center, DesignTokens.Size.BelowHalo * layout.SunRadius, state.SunBelowOpacity);
		canvas.FillColor = ToColor(DesignTokens.Sun.Disc, DesignTokens.Opacity.BelowDisc * state.SunBelowOpacity);
		canvas.FillCircle((float)center.X, (float)center.Y, (float)layout.SunRadius);
	}

	private static void DrawMoonBelow(ICanvas canvas, SceneLayout layout, SceneState state)
	{
		if (state.MoonBelowOpacity <= 0.0)
		{
			return;
		}

		PointD center = ArcGeometry.LowerPointAt(state.MoonBelowProgress, layout.CenterX, layout.HorizonY, layout.MoonArcRadius, layout.MoonLowerRadiusY);

		DrawHalo(canvas, center, DesignTokens.Size.BelowHalo * layout.MoonRadius, state.MoonBelowOpacity);
		DrawMoonDisc(canvas, center, layout.MoonRadius, state, DesignTokens.Opacity.BelowDisc * state.MoonBelowOpacity);
	}

	private static void DrawHalo(ICanvas canvas, PointD center, double radius, double opacity)
	{
		canvas.FillColor = Colors.White.WithAlpha((float)(DesignTokens.Opacity.BelowHalo * opacity));
		canvas.FillCircle((float)center.X, (float)center.Y, (float)radius);
	}

	private static void DrawGlow(ICanvas canvas, PointD center, double radius, double opacity, PaintGradientStop[] stops)
	{
		var bounds = new RectF((float)(center.X - radius), (float)(center.Y - radius), (float)(2.0 * radius), (float)(2.0 * radius));

		foreach (PaintGradientStop stop in stops)
		{
			stop.Color = stop.Color.WithAlpha((float)(stop.Color.Alpha * opacity));
		}

		var paint = new RadialGradientPaint { GradientStops = stops, Center = new Point(0.5, 0.5), Radius = 0.5 };

		FillWithPaint(canvas, paint, bounds, () => canvas.FillCircle((float)center.X, (float)center.Y, (float)radius));
	}

	private void DrawCompass(ICanvas canvas, SceneLayout layout, SceneState state)
	{
		double radius = layout.CompassRadius;
		double largestShift = 2.0 * radius * DesignTokens.Compass.LargestShift;
		var center = new PointD(layout.CenterX + RingShift.X * largestShift, layout.CompassCenterY + RingShift.Y * largestShift);

		canvas.SaveState();
		canvas.StrokeSize = (float)(DesignTokens.Size.ArcStrokeWidth * layout.DetailScale);
		canvas.StrokeLineCap = LineCap.Round;
		canvas.StrokeColor = Colors.White.WithAlpha((float)DesignTokens.Compass.RingOpacity);
		canvas.DrawCircle((float)center.X, (float)center.Y, (float)radius);
		canvas.DrawCircle((float)center.X, (float)center.Y, (float)(radius * DesignTokens.Compass.InnerRing));
		DrawTicks(canvas, center, radius);
		DrawCross(canvas, center, radius);
		canvas.RestoreState();

		double letterRing = radius * DesignTokens.Compass.LetterRing;
		DrawLetter(canvas, 'N', CompassGeometry.PointAt(center, letterRing, 0.0, Heading), radius * DesignTokens.Compass.NorthLetterHeight, DesignTokens.Compass.NorthOpacity);
		DrawLetter(canvas, EastLetter, CompassGeometry.PointAt(center, letterRing, 90.0, Heading), radius * DesignTokens.Compass.LetterHeight, DesignTokens.Compass.LetterOpacity);
		DrawLetter(canvas, 'S', CompassGeometry.PointAt(center, letterRing, 180.0, Heading), radius * DesignTokens.Compass.LetterHeight, DesignTokens.Compass.LetterOpacity);
		DrawLetter(canvas, 'W', CompassGeometry.PointAt(center, letterRing, 270.0, Heading), radius * DesignTokens.Compass.LetterHeight, DesignTokens.Compass.LetterOpacity);

		double sunMarker = radius * DesignTokens.Compass.SunMarker;
		double moonRing = radius * DesignTokens.Compass.InnerRing;
		double moonMarker = radius * DesignTokens.Compass.MoonMarker;

		DrawArrow(canvas, layout, center, radius, sunMarker, state.SunAzimuth, ToColor(DesignTokens.Sun.Disc, DesignTokens.Compass.ArrowOpacity));
		PointD sun = CompassGeometry.PointAt(center, radius, state.SunAzimuth, Heading);
		canvas.FillColor = ToColor(DesignTokens.Sun.Disc);
		canvas.FillCircle((float)sun.X, (float)sun.Y, (float)sunMarker);

		DrawArrow(canvas, layout, center, moonRing, moonMarker, state.MoonAzimuth, ToColor(DesignTokens.Moon.Lit, DesignTokens.Compass.ArrowOpacity));
		DrawMoonMarker(canvas, CompassGeometry.PointAt(center, moonRing, state.MoonAzimuth, Heading), moonMarker, state);
	}

	/// <summary>The moon in its phase, but solid, so that neither rings nor cross show through its dark side.</summary>
	private static void DrawMoonMarker(ICanvas canvas, PointD center, double radius, SceneState state)
	{
		canvas.FillColor = ToColor(DesignTokens.Compass.MoonDarkSide);
		canvas.FillCircle((float)center.X, (float)center.Y, (float)radius);

		canvas.FillColor = ToColor(DesignTokens.Moon.Lit);
		canvas.FillPath(ToPath(MoonShape.Lit(center, radius, state.MoonFraction, state.MoonZenithAngle)));
	}

	private void DrawTicks(ICanvas canvas, PointD center, double radius)
	{
		int count = (int)(360.0 / DesignTokens.Compass.TickSpacing);

		canvas.StrokeColor = Colors.White.WithAlpha((float)DesignTokens.Compass.TickOpacity);

		for (int tick = 0; tick < count; tick++)
		{
			double length = tick % DesignTokens.Compass.LongTickEvery == 0 ? DesignTokens.Compass.LongTickLength : DesignTokens.Compass.TickLength;
			DrawSpoke(canvas, center, radius * (1.0 - length), radius, tick * DesignTokens.Compass.TickSpacing);
		}
	}

	/// <summary>The cross runs from the middle through each letter to the outer ring, and leaves out the letter itself.</summary>
	private void DrawCross(ICanvas canvas, PointD center, double radius)
	{
		double hub = radius * DesignTokens.Compass.CrossHub;
		double beforeLetter = radius * (DesignTokens.Compass.LetterRing - DesignTokens.Compass.CrossGap);
		double behindLetter = radius * (DesignTokens.Compass.LetterRing + DesignTokens.Compass.CrossGap);

		for (double azimuth = 0.0; azimuth < 360.0; azimuth += 90.0)
		{
			double opacity = azimuth == 0.0 ? DesignTokens.Compass.NorthCrossOpacity : DesignTokens.Compass.CrossOpacity;

			canvas.StrokeColor = Colors.White.WithAlpha((float)opacity);
			DrawSpoke(canvas, center, hub, beforeLetter, azimuth);
			DrawSpoke(canvas, center, behindLetter, radius, azimuth);
		}

		canvas.FillColor = Colors.White.WithAlpha((float)DesignTokens.Compass.NorthCrossOpacity);
		canvas.FillCircle((float)center.X, (float)center.Y, (float)hub);
	}

	private void DrawSpoke(ICanvas canvas, PointD center, double from, double to, double azimuth)
	{
		PointD start = CompassGeometry.PointAt(center, from, azimuth, Heading);
		PointD end = CompassGeometry.PointAt(center, to, azimuth, Heading);

		canvas.DrawLine((float)start.X, (float)start.Y, (float)end.X, (float)end.Y);
	}

	/// <summary>A small arrow from the middle of a marker outwards. The solid disc on top leaves only the part beyond it.</summary>
	private void DrawArrow(ICanvas canvas, SceneLayout layout, PointD center, double ring, double marker, double azimuth, Color color)
	{
		double reference = layout.CompassRadius;
		double tip = ring + marker + reference * DesignTokens.Compass.ArrowLength;

		PointD start = CompassGeometry.PointAt(center, ring, azimuth, Heading);
		ArrowHead head = CompassGeometry.HeadAt(
			center,
			tip - reference * DesignTokens.Compass.ArrowHeadLength,
			reference * DesignTokens.Compass.ArrowHeadLength,
			reference * DesignTokens.Compass.ArrowHeadHalfWidth,
			azimuth,
			Heading);

		// One path, because lines drawn one by one would add up to a brighter spot where they meet.
		var path = new PathF((float)start.X, (float)start.Y);
		path.LineTo((float)head.Tip.X, (float)head.Tip.Y);
		path.MoveTo((float)head.Left.X, (float)head.Left.Y);
		path.LineTo((float)head.Tip.X, (float)head.Tip.Y);
		path.LineTo((float)head.Right.X, (float)head.Right.Y);

		canvas.SaveState();
		canvas.StrokeSize = (float)(DesignTokens.Compass.ArrowStroke * DesignTokens.Size.ArcStrokeWidth * layout.DetailScale);
		canvas.StrokeLineCap = LineCap.Round;
		canvas.StrokeLineJoin = LineJoin.Round;
		canvas.StrokeColor = color;
		canvas.DrawPath(path);
		canvas.RestoreState();
	}

	private static void DrawLetter(ICanvas canvas, char letter, PointD center, double height, double opacity)
	{
		var path = new PathF();

		foreach (CubicPath contour in CompassGlyphs.Place(letter, center, height))
		{
			path.MoveTo((float)contour.Start.X, (float)contour.Start.Y);
			AddSegments(path, contour);
			path.Close();
		}

		canvas.FillColor = Colors.White.WithAlpha((float)opacity);
		canvas.FillPath(path, WindingMode.EvenOdd);
	}

	private void DrawPageDots(ICanvas canvas, SceneLayout layout)
	{
		double spacing = DesignTokens.Size.DotSpacing * layout.Unit;
		double radius = Math.Max(1.0, DesignTokens.Size.DotRadius * layout.Unit);
		double firstX = layout.CenterX - spacing * (PageCount - 1) / 2.0;

		for (int page = 0; page < PageCount; page++)
		{
			double closeness = Math.Max(0.0, 1.0 - Math.Abs(page - PagePosition));

			canvas.FillColor = Colors.White.WithAlpha((float)Blend(DesignTokens.Opacity.DotIdle, DesignTokens.Opacity.DotActive, closeness));
			canvas.FillCircle((float)(firstX + page * spacing), (float)layout.DotsCenterY, (float)radius);
		}
	}

	private static void DrawHill(ICanvas canvas, SceneLayout layout, CubicPath outline, HillColors colors)
	{
		PathF path = ToPath(outline);
		path.LineTo((float)layout.Width, (float)layout.Height);
		path.LineTo(0, (float)layout.Height);
		path.Close();

		float top = (float)(layout.HorizonY - HillGradientTop * layout.Unit);
		var bounds = new RectF(0, top, (float)layout.Width, (float)layout.Height - top);
		var paint = new LinearGradientPaint(
			[new PaintGradientStop(0f, ToColor(colors.Top)), new PaintGradientStop(1f, ToColor(colors.Bottom))],
			new Point(0, 0),
			new Point(0, 1));

		FillWithPaint(canvas, paint, bounds, () => canvas.FillPath(path));
	}

	/// <summary>Android keeps a gradient on the canvas until the state is restored, even after a plain colour is set.</summary>
	private static void FillWithPaint(ICanvas canvas, Paint paint, RectF bounds, Action fill)
	{
		canvas.SaveState();
		canvas.SetFillPaint(paint, bounds);
		fill();
		canvas.RestoreState();
	}

	private static PathF ToPath(CubicPath source)
	{
		var path = new PathF((float)source.Start.X, (float)source.Start.Y);
		AddSegments(path, source);
		return path;
	}

	private static void AddSegments(PathF path, CubicPath source)
	{
		foreach (CubicSegment segment in source.Segments)
		{
			path.CurveTo(
				(float)segment.Control1.X, (float)segment.Control1.Y,
				(float)segment.Control2.X, (float)segment.Control2.Y,
				(float)segment.End.X, (float)segment.End.Y);
		}
	}

	private static Rgba Warmed(Rgba color, double amount) =>
		Rgba.Mix(color, DesignTokens.Sun.GlowWarm.WithAlpha(color.A), amount);

	private static double Blend(double from, double to, double t) => from + (to - from) * t;

	private static Color ToColor(Rgba color, double opacity = 1.0) =>
		new((float)color.R, (float)color.G, (float)color.B, (float)(color.A * opacity));
}
