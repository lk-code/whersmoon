using Foundation;
using UIKit;

namespace WheresMoon.App;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	/// <summary>The end of the responder chain, so a focused text field gets its keys first.</summary>
	public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)
	{
		bool isHandled = false;

		foreach (UIPress press in presses)
		{
			SceneKey? key = press.Key?.KeyCode switch
			{
				UIKeyboardHidUsage.KeyboardLeftArrow => SceneKey.Left,
				UIKeyboardHidUsage.KeyboardRightArrow => SceneKey.Right,
				UIKeyboardHidUsage.KeyboardHome => SceneKey.Home,
				UIKeyboardHidUsage.KeyboardEscape => SceneKey.Escape,
				_ => null,
			};

			if (key is SceneKey pressed && Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page is MainPage page)
			{
				isHandled |= page.HandleKey(pressed);
			}
		}

		if (!isHandled)
		{
			base.PressesBegan(presses, evt);
		}
	}
}
