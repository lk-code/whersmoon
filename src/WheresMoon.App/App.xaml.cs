namespace WheresMoon.App;

public partial class App : Application
{
	private const double MinimumWindowWidth = 320;
	private const double MinimumWindowHeight = 400;

	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new MainPage())
		{
			Title = "Where's Moon?",
			MinimumWidth = MinimumWindowWidth,
			MinimumHeight = MinimumWindowHeight,
		};

#if WINDOWS || MACCATALYST
		// Lets the sky run all the way to the top of the window.
		window.TitleBar = new TitleBar { IsVisible = false };
#endif

		return window;
	}
}
