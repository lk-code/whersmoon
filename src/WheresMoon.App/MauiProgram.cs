namespace WheresMoon.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("Manrope-Light.ttf", "ManropeLight");
				fonts.AddFont("Manrope-Regular.ttf", "ManropeRegular");
				fonts.AddFont("Manrope-Medium.ttf", "ManropeMedium");
			});

		return builder.Build();
	}
}
