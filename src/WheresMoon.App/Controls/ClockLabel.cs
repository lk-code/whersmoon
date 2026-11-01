using Microsoft.Maui.Handlers;

namespace WheresMoon.App.Controls;

/// <summary>
/// Label in the light weight of the system font, with digits of equal width so that a running clock stands still.
/// MAUI offers neither, so the native view gets them. A system without the light weight shows its regular one.
/// </summary>
public sealed class ClockLabel : Label
{
#if ANDROID
	private static readonly Android.Graphics.Typeface? LightFont =
		Android.Graphics.Typeface.Create("sans-serif-light", Android.Graphics.TypefaceStyle.Normal);
#endif

	/// <summary>Only a change of the font resets the native one. New text or colours keep it.</summary>
	public static void RegisterFont() =>
		LabelHandler.Mapper.AppendToMapping(nameof(ITextStyle.Font), (handler, label) =>
		{
			if (label is ClockLabel)
			{
				ApplyFont(handler);
			}
		});

	private static void ApplyFont(ILabelHandler handler)
	{
#if IOS || MACCATALYST
		UIKit.UIFont regular = handler.PlatformView.Font;
		handler.PlatformView.Font = UIKit.UIFont.MonospacedDigitSystemFontOfSize(regular.PointSize, UIKit.UIFontWeight.Light) ?? regular;
#elif ANDROID
		handler.PlatformView.Typeface = LightFont;
		handler.PlatformView.FontFeatureSettings = "tnum";
#elif WINDOWS
		handler.PlatformView.FontWeight = Microsoft.UI.Text.FontWeights.Light;
		Microsoft.UI.Xaml.Documents.Typography.SetNumeralAlignment(handler.PlatformView, Microsoft.UI.Xaml.FontNumeralAlignment.Tabular);
#endif
	}
}
