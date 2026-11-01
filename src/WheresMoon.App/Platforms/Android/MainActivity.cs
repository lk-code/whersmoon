using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace WheresMoon.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);

		// The sky behind the system bars is dark enough at any time of day for light icons.
		if (Window?.DecorView is { } decorView && WindowCompat.GetInsetsController(Window, decorView) is { } controller)
		{
			controller.AppearanceLightStatusBars = false;
			controller.AppearanceLightNavigationBars = false;
		}
	}
}
