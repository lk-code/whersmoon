using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace WheresMoon.App.Platforms.Windows;

/// <summary>Lets the page reach the top of the window, with the caption buttons floating over the sky.</summary>
internal static class WindowChrome
{
	private static readonly string[] TitleBarMarginResources = ["NavigationViewContentMargin", "NavigationViewMinimalContentMargin"];

	/// <summary>Height of the caption area in device independent units. Text must stay below the buttons.</summary>
	public static double CaptionHeight(VisualElement page) =>
		page.Window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window window
			? window.AppWindow.TitleBar.Height / page.Window.DisplayDensity
			: 0.0;

	/// <summary>Distance one notch of a mouse wheel stands for, which reports 120 per notch.</summary>
	private const double WheelNotchDistance = 24.0;
	private const double WheelUnitsPerNotch = 120.0;

	public static void Apply(VisualElement page, VisualElement initialFocus, Func<SceneKey, bool> onKey, Action<double> onWheel)
	{
		if (page.Window?.Handler?.PlatformView is not Microsoft.UI.Xaml.Window window ||
			page.Handler?.PlatformView is not FrameworkElement content)
		{
			return;
		}

		RemoveTitleBarMargin(content);

		var titleBar = window.AppWindow.TitleBar;
		titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
		titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
		titleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
		titleBar.ButtonInactiveForegroundColor = Microsoft.UI.Colors.White;

		content.PreviewKeyDown += (_, e) =>
		{
			SceneKey? key = e.Key switch
			{
				VirtualKey.Left => SceneKey.Left,
				VirtualKey.Right => SceneKey.Right,
				VirtualKey.Home => SceneKey.Home,
				VirtualKey.Escape => SceneKey.Escape,
				_ => null,
			};

			if (key is SceneKey pressed && onKey(pressed))
			{
				e.Handled = true;
			}
		};

		// WinUI focuses the first tab stop on launch, which would draw a focus ring around
		// the settings button. Starting on the scene also lets screen readers begin with it.
		if (initialFocus.Handler?.PlatformView is Control scene)
		{
			scene.IsTabStop = true;
			scene.UseSystemFocusVisuals = false;
			scene.Focus(FocusState.Programmatic);

			scene.PointerWheelChanged += (_, e) =>
			{
				var properties = e.GetCurrentPoint(scene).Properties;
				double distance = properties.MouseWheelDelta / WheelUnitsPerNotch * WheelNotchDistance;

				// Turning the wheel towards the user scrolls down, which counts as later.
				onWheel(properties.IsHorizontalMouseWheel ? distance : -distance);
				e.Handled = true;
			};
		}
	}

	/// <summary>
	/// The navigation view that MAUI hosts every page in reserves room for the title bar through these
	/// resources, even when the bar is hidden. A visual state applies them, so a local margin would not
	/// last, and MAUI sets them again on the window root. Defining them on the view itself wins.
	/// </summary>
	private static void RemoveTitleBarMargin(FrameworkElement content)
	{
		for (DependencyObject? element = content; element is not null; element = VisualTreeHelper.GetParent(element))
		{
			if (element is NavigationView navigationView)
			{
				foreach (string resource in TitleBarMarginResources)
				{
					navigationView.Resources[resource] = new Microsoft.UI.Xaml.Thickness(0);
				}

				// Theme resources are only looked up again when the theme changes.
				ElementTheme theme = navigationView.RequestedTheme;
				navigationView.RequestedTheme = navigationView.ActualTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
				navigationView.RequestedTheme = theme;
				return;
			}
		}
	}
}
