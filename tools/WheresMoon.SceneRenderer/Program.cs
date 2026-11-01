using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Skia;
using SkiaSharp;
using WheresMoon.App.Rendering;
using WheresMoon.Core.Scene;

// Renders the scene without any UI, to compare it with the reference images and to
// look at formats and times of day that are hard to reach on a device.
// Usage: dotnet run --project tools/WheresMoon.SceneRenderer -- <output folder>

const double Heading = 20.0;

string output = args.Length > 0 ? args[0] : "scene-output";
Directory.CreateDirectory(output);

string fonts = Path.Combine(FindRepositoryRoot(), "src", "WheresMoon.App", "Resources", "Fonts");
using SKTypeface light = SKTypeface.FromFile(Path.Combine(fonts, "Manrope-Light.ttf"));
using SKTypeface regular = SKTypeface.FromFile(Path.Combine(fonts, "Manrope-Regular.ttf"));
using SKTypeface medium = SKTypeface.FromFile(Path.Combine(fonts, "Manrope-Medium.ttf"));

const string DayLength = "Tageslänge 9 h 31 min · 3 min kürzer als gestern";
var dayTexts = new Texts("10:23", "Tromsø · Mo., 3. Nov.", ["Sonnenaufgang: 07:31 – Sonnenuntergang: 17:02", "Mondaufgang: 03:10 – Monduntergang: 12:50"], "Abnehmende Sichel · 18 %", DayLength);
var nightTexts = new Texts("18:47", string.Empty, ["Sonnenaufgang: 07:31 – Sonnenuntergang: 17:02", "Mondaufgang: 11:48 – Monduntergang: 20:15"], "Zunehmende Sichel · 18 %", DayLength);
var eventTexts = new Texts("18:47", string.Empty,
    [
        "Vollmond: 24.11., 15:54 · in 23 Tagen",
        "Neumond: 09.11., 08:02 · in 8 Tagen",
        "Supermond: 24.11., 15:54 · in 23 Tagen",
        "Mondfinsternis (Halbschatten): 21.02.27, 00:12",
        "Sonnenfinsternis (ringförmig): 02.08.27, 11:13",
    ],
    string.Empty,
    string.Empty);
var lightTexts = new Texts("18:47", string.Empty,
    [
        "Morgens: Blaue Stunde 06:52 – 07:08",
        "Goldene Stunde 07:08 – 08:15",
        "Abends: Goldene Stunde 15:58 – 17:06",
        "Blaue Stunde 17:06 – 17:22",
    ],
    string.Empty,
    string.Empty);

var day = new SceneState
{
    SunAltitude = 25,
    SunIsRising = true,
    SunProgress = 172.0 / 571.0,
    SunOpacity = 1,
    MoonProgress = 0.70,
    MoonOpacity = 1,
    MoonFraction = 0.18,
    MoonZenithAngle = 80,
    SunAzimuth = 140,
    MoonAzimuth = 250,
};

var moonBelow = day with { MoonOpacity = 0, MoonBelowProgress = 0.55, MoonBelowOpacity = 1 };

var night = new SceneState
{
    SunAltitude = -19,
    SunIsRising = false,
    SunProgress = 0.9,
    SunOpacity = 0,
    SunBelowProgress = 0.12,
    SunBelowOpacity = 1,
    MoonProgress = 419.0 / 507.0,
    MoonOpacity = 1,
    MoonFraction = 0.18,
    MoonZenithAngle = 215,
    SunAzimuth = 285,
    MoonAzimuth = 200,
};

Render("day.png", 390, 844, 2, SafeInsets.None, day, dayTexts);
Render("night.png", 390, 844, 2, SafeInsets.None, night, nightTexts);
Render("detail-day.png", 390, 844, 5, SafeInsets.None, day, dayTexts);
Render("detail-night.png", 390, 844, 5, SafeInsets.None, night, nightTexts);

var notch = new SafeInsets(59, 0, 59, 21);
Render("format-phone-landscape.png", 844, 390, 2, notch, day, dayTexts);
Render("format-phone-small.png", 320, 568, 2, new SafeInsets(0, 24, 0, 16), night, nightTexts);
Render("format-tablet-portrait.png", 820, 1180, 1, new SafeInsets(0, 24, 0, 20), day, dayTexts);
Render("format-tablet-landscape.png", 1180, 820, 1, new SafeInsets(0, 24, 0, 20), night, nightTexts);
Render("format-desktop.png", 1920, 1080, 1, SafeInsets.None, day, dayTexts);
Render("format-square.png", 800, 800, 1, SafeInsets.None, moonBelow, dayTexts);
Render("format-tablet-between.png", 800, 1040, 1, SafeInsets.None, night, nightTexts);
Render("format-window-small.png", 360, 480, 2, SafeInsets.None, night, nightTexts);
Render("page-events.png", 320, 568, 2, SafeInsets.None, night, eventTexts);
Render("window-320x400.png", 320, 400, 3, SafeInsets.None, night, eventTexts);
Render("window-360x480.png", 360, 480, 3, SafeInsets.None, night, eventTexts);
Render("window-480x360.png", 480, 360, 3, SafeInsets.None, night, eventTexts);
Render("page-light.png", 390, 844, 2, SafeInsets.None, night, lightTexts);
Render("page-events-en.png", 320, 568, 2, SafeInsets.None, night, eventTexts with
{
    Rows =
    [
        "Full moon: Nov 24, 3:54 PM · in 23 days",
        "New moon: Nov 9, 8:02 AM · in 8 days",
        "Supermoon: Nov 24, 3:54 PM · in 23 days",
        "Lunar eclipse (penumbral): Feb 21, 2027, 12:12 AM",
        "Solar eclipse (annular): Aug 2, 2027, 11:13 AM",
    ],
});

RenderSkySheet("sheet-sky.png", dayTexts);
RenderMoonSheet("sheet-moon.png");

Console.WriteLine($"Wrote images to {Path.GetFullPath(output)}");

void Render(string name, int width, int height, int scale, SafeInsets insets, SceneState state, Texts texts)
{
    using var bitmap = new SKBitmap(width * scale, height * scale);
    using var surface = new SKCanvas(bitmap);
    surface.Scale(scale);
    DrawScene(surface, width, height, insets, state, texts);
    Save(bitmap, name);
}

void DrawScene(SKCanvas target, int width, int height, SafeInsets insets, SceneState state, Texts texts)
{
    SceneLayout layout = SceneLayout.Compute(width, height, insets);
    using var canvas = new SkiaCanvas { Canvas = target };
    new SceneDrawable { State = state, Layout = layout, EastLetter = 'O', Heading = Heading }.Draw(canvas, new RectF(0, 0, width, height));
    DrawTexts(target, layout, texts);
}

void DrawTexts(SKCanvas target, SceneLayout layout, Texts texts)
{
    float centerX = (float)layout.CenterX;
    Line(texts.Clock, light, layout.ClockFontSize, layout.TextTop + layout.ClockFontSize / 2.0, 1f, -0.02f);
    Line(texts.Context, regular, layout.CaptionFontSize, layout.ContextCenterY, (float)DesignTokens.Opacity.PhaseText, 0f);

    bool isList = texts.Phase.Length == 0;
    for (int row = 0; row < texts.Rows.Count; row++)
    {
        Line(texts.Rows[row], medium, isList ? layout.PhaseFontSize : layout.RiseSetFontSize, layout.RowCenterY(row), 1f, 0f);
    }

    Line(texts.Phase, regular, layout.PhaseFontSize, layout.RowCenterY(texts.Rows.Count), (float)DesignTokens.Opacity.PhaseText, 0f);
    Line(texts.Caption, regular, layout.CaptionFontSize, layout.RowCenterY(texts.Rows.Count + 1), (float)DesignTokens.Opacity.CaptionText, 0f);

    void Line(string text, SKTypeface typeface, double size, double centerY, float opacity, float spacing)
    {
        using var font = new SKFont(typeface, (float)size);
        using var paint = new SKPaint { Color = SKColors.White.WithAlpha((byte)(opacity * 255)), IsAntialias = true };
        float width = font.MeasureText(text) + spacing * (float)size * text.Length;
        SKFontMetrics metrics = font.Metrics;
        float baseline = (float)centerY - (metrics.Ascent + metrics.Descent) / 2f;
        target.DrawText(text, centerX - width / 2f, baseline, SKTextAlign.Left, font, paint);
    }
}

void RenderSkySheet(string name, Texts texts)
{
    double[] altitudes = [-30, -18, -15, -12, -9, -6, -3.5, -1, 1, 2.5, 5, 8, 10, 20];
    const int Cell = 150, Tall = 324;

    using var bitmap = new SKBitmap(Cell * altitudes.Length, Tall * 2);
    using var target = new SKCanvas(bitmap);

    for (int row = 0; row < 2; row++)
    {
        for (int i = 0; i < altitudes.Length; i++)
        {
            target.Save();
            target.Translate(i * Cell, row * Tall);
            target.ClipRect(new SKRect(0, 0, Cell - 2, Tall - 2));
            target.Scale(Cell / 390f);

            var state = new SceneState
            {
                SunAltitude = altitudes[i],
                SunIsRising = row == 0,
                SunProgress = row == 0 ? 0.08 : 0.92,
                SunOpacity = altitudes[i] > -0.8 ? 1 : 0,
                MoonOpacity = 1,
                MoonProgress = 0.6,
                MoonFraction = 0.4,
                MoonZenithAngle = 250,
            };

            DrawScene(target, 390, 842, SafeInsets.None, state, texts);
            target.Restore();
        }
    }

    Save(bitmap, name);
}

void RenderMoonSheet(string name)
{
    double[] fractions = [0.03, 0.18, 0.35, 0.5, 0.65, 0.82, 0.97, 1.0];
    double[] angles = [0, 45, 90, 135, 180, 225, 270, 315];
    const int Cell = 90;

    using var bitmap = new SKBitmap(Cell * fractions.Length, Cell * angles.Length);
    using var target = new SKCanvas(bitmap);
    target.Clear(new SKColor(0x20, 0x30, 0x60));
    using var canvas = new SkiaCanvas { Canvas = target };

    for (int row = 0; row < angles.Length; row++)
    {
        for (int column = 0; column < fractions.Length; column++)
        {
            var center = new PointD(Cell * (column + 0.5), Cell * (row + 0.5));
            canvas.FillColor = Colors.White.WithAlpha(0.11f);
            canvas.FillCircle((float)center.X, (float)center.Y, 34f);

            CubicPath lit = MoonShape.Lit(center, 34.0, fractions[column], angles[row]);
            var path = new PathF((float)lit.Start.X, (float)lit.Start.Y);
            foreach (CubicSegment s in lit.Segments)
            {
                path.CurveTo((float)s.Control1.X, (float)s.Control1.Y, (float)s.Control2.X, (float)s.Control2.Y, (float)s.End.X, (float)s.End.Y);
            }

            canvas.FillColor = new Color(0.97f, 0.96f, 0.94f);
            canvas.FillPath(path);
        }
    }

    Save(bitmap, name);
}

void Save(SKBitmap bitmap, string name)
{
    using SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
    File.WriteAllBytes(Path.Combine(output, name), data.ToArray());
}

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "WheresMoon.slnx")))
    {
        directory = directory.Parent;
    }

    return directory?.FullName ?? throw new InvalidOperationException("Run this tool from inside the repository.");
}

internal sealed record Texts(string Clock, string Context, IReadOnlyList<string> Rows, string Phase, string Caption);
