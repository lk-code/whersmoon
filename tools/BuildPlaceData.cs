#:project ../src/WheresMoon.Core/WheresMoon.Core.csproj

// Turns the GeoNames dumps into the compact file the app searches, see PlaceIndex for the format.
// Input: cities15000.txt and admin1CodesASCII.txt from https://download.geonames.org/export/dump/
// Usage: dotnet run tools/BuildPlaceData.cs -- <cities15000.txt> <admin1CodesASCII.txt> <output.tsv.gz>

using System.Globalization;
using System.IO.Compression;
using System.Text;
using WheresMoon.Core.Places;

const int Name = 1, Alternatives = 3, Latitude = 4, Longitude = 5, Country = 8, Admin1 = 10, Population = 14, Elevation = 15, Terrain = 16, Zone = 17;
const string NoTerrainData = "-9999";
const int ShortestName = 3, LongestName = 30, LongestCode = 4;

Dictionary<string, string> regionNames = File.ReadLines(args[1])
    .Select(line => line.Split('\t'))
    .ToDictionary(fields => fields[0], fields => fields[1]);

List<string[]> cities = File.ReadLines(args[0])
    .Select(line => line.Split('\t'))
    .OrderByDescending(fields => long.Parse(fields[Population], CultureInfo.InvariantCulture))
    .ToList();

List<string> zones = cities.Select(city => city[Zone]).Distinct().ToList();
List<string> regions = cities.Select(RegionOf).Distinct().ToList();
Dictionary<string, int> zoneIndex = zones.Select((zone, index) => (zone, index)).ToDictionary(item => item.zone, item => item.index);
Dictionary<string, int> regionIndex = regions.Select((region, index) => (region, index)).ToDictionary(item => item.region, item => item.index);

await using FileStream file = File.Create(args[2]);
await using var gzip = new GZipStream(file, CompressionLevel.SmallestSize);
await using var writer = new StreamWriter(gzip, new UTF8Encoding(false)) { NewLine = "\n" };

writer.WriteLine(string.Join('\t', zones));
writer.WriteLine(string.Join('\t', regions));

foreach (string[] city in cities)
{
    writer.WriteLine(string.Join(
        '\t',
        city[Name],
        city[Country],
        regionIndex[RegionOf(city)],
        Rounded(city[Latitude]),
        Rounded(city[Longitude]),
        ElevationOf(city),
        zoneIndex[city[Zone]],
        string.Join('|', UsefulAlternatives(city))));
}

Console.WriteLine($"{cities.Count} places, {zones.Count} zones, {regions.Count} regions");

string RegionOf(string[] city) => regionNames.GetValueOrDefault($"{city[Country]}.{city[Admin1]}", string.Empty);

static string Rounded(string coordinate) =>
    Math.Round(double.Parse(coordinate, CultureInfo.InvariantCulture), 3).ToString(CultureInfo.InvariantCulture);

static string ElevationOf(string[] city)
{
    string terrain = city[Terrain] == NoTerrainData ? "0" : city[Terrain];
    return city[Elevation].Length > 0 ? city[Elevation] : terrain;
}

// Only Latin names help people typing on a German or English keyboard. Airport codes only cause false hits.
static IEnumerable<string> UsefulAlternatives(string[] city)
{
    var seen = new HashSet<string> { SearchText.Fold(city[Name]) };

    foreach (string name in city[Alternatives].Split(',', StringSplitOptions.RemoveEmptyEntries))
    {
        bool isCode = name.Length <= LongestCode && name.All(c => !char.IsLower(c));
        bool isUseful = name.Length is >= ShortestName and <= LongestName && !isCode && !name.Any(char.IsDigit) && name.All(IsLatin);

        if (isUseful && seen.Add(SearchText.Fold(name)))
        {
            yield return name;
        }
    }
}

static bool IsLatin(char c) => !char.IsLetter(c) || c < 'ɐ';
