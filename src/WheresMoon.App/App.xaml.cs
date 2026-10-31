namespace WheresMoon.App;

public partial class App : Application
{
	/// <summary>Smallest window with all five text rows, the arcs and the compass at a readable size.</summary>
	private const double MinimumWindowWidth = 360;
	private const double MinimumWindowHeight = 480;

	private const double InitialWindowWidth = 520;
	private const double InitialWindowHeight = 900;

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
		window.Width = InitialWindowWidth;
		window.Height = InitialWindowHeight;
#endif

		return window;
	}
}
