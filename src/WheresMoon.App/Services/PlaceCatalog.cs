using WheresMoon.Core.Places;

namespace WheresMoon.App.Services;

/// <summary>The city list, loaded when a search first asks for it and never at startup.</summary>
internal static class PlaceCatalog
{
	private const string File = "places.tsv.gz";

	private static Task<PlaceIndex?>? _loading;

	/// <summary>Returns the list, or null if it cannot be read, which leaves the search without results.</summary>
	public static Task<PlaceIndex?> LoadAsync() => _loading ??= Task.Run(ReadAsync);

	private static async Task<PlaceIndex?> ReadAsync()
	{
		try
		{
			await using Stream stream = await FileSystem.OpenAppPackageFileAsync(File);
			return PlaceIndex.Load(stream);
		}
		catch (Exception exception) when (exception is IOException or InvalidDataException or FormatException or IndexOutOfRangeException)
		{
			return null;
		}
	}
}
