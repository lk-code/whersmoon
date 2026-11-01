using CoreGraphics;
using UIKit;

namespace WheresMoon.App.Platforms.MacCatalyst;

/// <summary>Reports mouse wheel and trackpad scrolling, which reach a view only through a pan recognizer without touches.</summary>
internal static class ScrollInput
{
	public static void Attach(VisualElement element, Action<double> onScroll)
	{
		if (element.Handler?.PlatformView is not UIView view)
		{
			return;
		}

		CGPoint last = default;

		var recognizer = new UIPanGestureRecognizer(pan =>
		{
			if (pan.State == UIGestureRecognizerState.Began)
			{
				last = default;
			}

			CGPoint total = pan.TranslationInView(view);
			double dx = (double)(total.X - last.X);
			double dy = (double)(total.Y - last.Y);
			last = total;

			// Scrolling down or to the right counts as later. Two fingers moving left do that, the same as one on a touch screen.
			onScroll(Math.Abs(dx) >= Math.Abs(dy) ? -dx : -dy);
		})
		{
			AllowedScrollTypesMask = UIScrollTypeMask.All,
			MaximumNumberOfTouches = 0,
		};

		view.AddGestureRecognizer(recognizer);
	}
}
