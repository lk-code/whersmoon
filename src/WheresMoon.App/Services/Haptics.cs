namespace WheresMoon.App.Services;

/// <summary>
/// A short tick on phones. MAUI's own haptic feedback needs the vibrate permission on Android,
/// which a view's feedback does not, so the app can stay with the location permission alone.
/// </summary>
internal static class Haptics
{
	public static void Tick(VisualElement element)
	{
#if ANDROID
		(element.Handler?.PlatformView as Android.Views.View)?.PerformHapticFeedback(Android.Views.FeedbackConstants.ClockTick);
#elif IOS
		new UIKit.UISelectionFeedbackGenerator().SelectionChanged();
#endif
	}
}
