#:project ../src/WheresMoon.Core/WheresMoon.Core.csproj

// Turns the GeoNames dumps into the compact file the app searches, see PlaceIndex for the format.
// Input: cities15000.txt, admin1CodesASCII.txt and alternateNamesV2.txt from https://download.geonames.org/export/dump/
// Usage: dotnet run tools/BuildPlaceData.cs -- <cities15000.txt> <admin1CodesASCII.txt> <alternateNamesV2.txt> <output.tsv.gz>

using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using WheresMoon.Core.Places;

const int Id = 0, Name = 1, Alternatives = 3, Latitude = 4, Longitude = 5, FeatureCode = 7, Country = 8, Admin1 = 10, Admin2 = 11, Admin3 = 12, Admin4 = 13, Population = 14, Elevation = 15, Terrain = 16, Zone = 17;
const string NoTerrainData = "-9999";
const int ShortestName = 3, LongestName = 30, LongestCode = 4;

// Columns of the alternate names file.
const int NamedPlace = 1, Language = 2, Text = 3, IsPreferred = 4, IsShort = 5, IsColloquial = 6, IsHistoric = 7;

// What counts as lying inside a metropolis, in km and inhabitants.
const double MetropolisRadius = 12.0;
const double KilometresPerDegree = 111.2;
const long SmallestMetropolis = 1_000_000, LargestDistrict = 300_000, DistrictsPerMetropolis = 10;

// From this size on a city has a German name people really use.
const long WellKnownCity = 500_000;

string[] townCodes = ["PPL", "PPLA", "PPLA2", "PPLA3", "PPLA4", "PPLA5", "PPLC", "PPLG"];
string[] capitalCodes = ["PPLC", "PPLA", "PPLG"];
string[] cityStates = ["HK", "SG", "MO"];
string[] newYorkCity = ["New York City", "Brooklyn", "Queens", "Manhattan", "The Bronx", "Staten Island"];

// Most German names of towns in these countries date from before 1945 and read as a claim today, without being marked as historic.
string[] sensitiveGermanNames = ["PL", "CZ", "SK", "RU", "LT", "LV", "EE", "RO", "HU", "SI", "HR", "RS", "UA", "BY"];

// Local spellings are better known here than what GeoNames lists as German.
string[] localNamesOnly = ["DE", "AT", "IN", "BD", "TR"];

// What the review of the result turned up: old, disputed or merely odd spellings, and the one name German usage does not share.
string[] poorGermanNames = ["Tighina", "Pekin", "Ĳmuiden", "Port Elizabeth", "Banghazi", "Samarqand", "Kermānschāh", "Lənkəran", "Oase Siwa"];
Dictionary<string, string> germanNameFixes = new() { ["Beijing"] = "Peking" };

// Spain has no column that tells a municipality, and nothing else marks these districts.
string[] spanishDistricts = ["Ciudad Lineal", "Chamartín", "Santutxu", "Natahoyo", "Llefià", "Son Rapinya", "Sant Agustí", "Barrio de Peral"];

// In these countries towns around a metropolis are towns of their own, and the data tells parts of a town by other means.
string[] independentSuburbs = ["DE", "AT", "CH", "FR", "IT", "ES", "US", "NL", "PT", "IE"];

// Where a column of the data names the municipality, every place after the first in it is a part of that one.
Dictionary<string, int> municipalityColumn = new() { ["DE"] = Admin4, ["FR"] = Admin4, ["BE"] = Admin4, ["IT"] = Admin3 };

var districtName = new Regex(@"\(Kreis \d+\)|\bDistrict\b|\bEstate\b|\(balance\)|-ku$|-dong$", RegexOptions.CultureInvariant);

Dictionary<string, (string Name, string Id)> regionRecords = File.ReadLines(args[1])
    .Select(line => line.Split('\t'))
    .ToDictionary(fields => fields[0], fields => (fields[1], fields[3]));

List<string[]> cities = CitiesOnly(File.ReadLines(args[0]).Select(line => line.Split('\t')))
    .OrderByDescending(PopulationOf)
    .ToList();

HashSet<string> wanted = [.. cities.Select(city => city[Id]), .. regionRecords.Values.Select(region => region.Id)];
Dictionary<string, List<string[]>> germanNames = File.ReadLines(args[2])
    .Where(line => line.Contains("\tde\t", StringComparison.Ordinal))
    .Select(line => line.Split('\t'))
    .Where(fields => fields[Language] == "de" && wanted.Contains(fields[NamedPlace]) && !Flag(fields, IsColloquial) && !Flag(fields, IsHistoric))
    .GroupBy(fields => fields[NamedPlace])
    .ToDictionary(group => group.Key, group => group.ToList());

List<string> zones = cities.Select(city => city[Zone]).Distinct().ToList();
List<string> regions = cities.Select(RegionOf).Distinct().ToList();
Dictionary<string, int> zoneIndex = zones.Select((zone, index) => (zone, index)).ToDictionary(item => item.zone, item => item.index);
Dictionary<string, int> regionIndex = regions.Select((region, index) => (region, index)).ToDictionary(item => item.region, item => item.index);

await using FileStream file = File.Create(args[3]);
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
        string.Join('|', UsefulAlternatives(city)),
        GermanCityName(city)));
}

Console.WriteLine($"{cities.Count} places, {zones.Count} zones, {regions.Count} regions, {cities.Count(city => GermanCityName(city).Length > 0)} German names");

// GeoNames has no single mark for a part of a city, so each rule catches what the others cannot.
List<string[]> CitiesOnly(IEnumerable<string[]> places)
{
    List<string[]> candidates = places
        .Where(place => townCodes.Contains(place[FeatureCode]) && !districtName.IsMatch(place[Name]) && !IsNamedDistrict(place))
        .OrderByDescending(IsCapital)
        .ThenByDescending(PopulationOf)
        .ToList();

    var municipalities = new HashSet<string>();
    var metropolises = new List<string[]>();
    var kept = new List<string[]>();

    foreach (string[] place in candidates)
    {
        if (MunicipalityOf(place) is string municipality && !municipalities.Add(municipality))
        {
            continue;
        }

        if (metropolises.Any(metropolis => LiesInside(place, metropolis)))
        {
            continue;
        }

        kept.Add(place);

        if (PopulationOf(place) >= SmallestMetropolis)
        {
            metropolises.Add(place);
        }
    }

    return kept;
}

bool IsNamedDistrict(string[] place) =>
    (place[Country] == "US" && place[Name].Contains(" / ", StringComparison.Ordinal))
    || (place[Country] == "ES" && spanishDistricts.Contains(place[Name]));

bool IsCapital(string[] place) => capitalCodes.Contains(place[FeatureCode]);

string? MunicipalityOf(string[] place)
{
    if (cityStates.Contains(place[Country]))
    {
        return place[Country];
    }

    if (place[Country] == "GB" && place[Admin2] == "GLA")
    {
        return "Greater London";
    }

    if (place[Country] == "US" && place[Admin1] == "NY" && newYorkCity.Contains(place[Name]))
    {
        return "New York City";
    }

    return municipalityColumn.TryGetValue(place[Country], out int column) && place[column].Length > 0
        ? $"{place[Country]}.{place[column]}"
        : null;
}

bool LiesInside(string[] place, string[] metropolis)
{
    if (place[Country] != metropolis[Country]
        || independentSuburbs.Contains(place[Country])
        || IsCapital(place)
        || PopulationOf(place) >= LargestDistrict
        || PopulationOf(place) * DistrictsPerMetropolis >= PopulationOf(metropolis))
    {
        return false;
    }

    double north = (Coordinate(place[Latitude]) - Coordinate(metropolis[Latitude])) * KilometresPerDegree;
    double east = (Coordinate(place[Longitude]) - Coordinate(metropolis[Longitude])) * KilometresPerDegree * Math.Cos(Coordinate(place[Latitude]) * Math.PI / 180.0);

    return Math.Sqrt(north * north + east * east) <= MetropolisRadius;
}

// English name, and the German one where it differs.
string RegionOf(string[] city)
{
    if (!regionRecords.TryGetValue($"{city[Country]}.{city[Admin1]}", out (string Name, string Id) region))
    {
        return string.Empty;
    }

    List<string[]> names = germanNames.GetValueOrDefault(region.Id, []).FindAll(name => !name[Text].Contains('('));
    string[]? best = names.Find(name => Flag(name, IsShort)) ?? names.Find(name => Flag(name, IsPreferred)) ?? names.FirstOrDefault();

    return best is null || best[Text] == region.Name ? region.Name : $"{region.Name}|{best[Text]}";
}

string GermanCityName(string[] city)
{
    bool isSensitive = sensitiveGermanNames.Contains(city[Country]) && PopulationOf(city) < WellKnownCity;

    if (germanNameFixes.TryGetValue(city[Name], out string? fixedName))
    {
        return fixedName;
    }

    if (isSensitive || !germanNames.TryGetValue(city[Id], out List<string[]>? names))
    {
        return string.Empty;
    }

    string folded = SearchText.Fold(city[Name]);
    string[]? preferred = names.Find(name => Flag(name, IsPreferred));

    if (preferred is not null)
    {
        return IsPlainCityName(preferred[Text], city) ? preferred[Text] : string.Empty;
    }

    bool isKnownByDefault = names.Exists(name => SearchText.Fold(name[Text]) == folded);

    return isKnownByDefault || !TrustsUnmarkedNames(city)
        ? string.Empty
        : names.Select(name => name[Text]).FirstOrDefault(name => IsPlainCityName(name, city), string.Empty);
}

// German names without a mark are reliable in Europe and for large cities. Germany and Austria need none, their names are German already.
bool TrustsUnmarkedNames(string[] city) =>
    !localNamesOnly.Contains(city[Country]) && (city[Zone].StartsWith("Europe/", StringComparison.Ordinal) || PopulationOf(city) >= WellKnownCity);

// Leaves out additions like "Monroe (Louisiana)", "Stadt Schwedt/Oder" or "Municipio Naucalpan de Juárez", as well as other spellings.
bool IsPlainCityName(string name, string[] city) =>
    name.IndexOfAny(['(', ',', '/']) < 0
    && !poorGermanNames.Contains(name)
    && !name.Contains(city[Name], StringComparison.OrdinalIgnoreCase)
    && SearchText.Fold(name) != SearchText.Fold(city[Name]);

static bool Flag(string[] name, int column) => column < name.Length && name[column] == "1";

long PopulationOf(string[] place) => long.Parse(place[Population], CultureInfo.InvariantCulture);

static double Coordinate(string value) => double.Parse(value, CultureInfo.InvariantCulture);

static string Rounded(string coordinate) => Math.Round(Coordinate(coordinate), 3).ToString(CultureInfo.InvariantCulture);

string ElevationOf(string[] city)
{
    string terrain = city[Terrain] == NoTerrainData ? "0" : city[Terrain];
    return city[Elevation].Length > 0 ? city[Elevation] : terrain;
}

// Only Latin names help people typing on a German or English keyboard. Airport codes only cause false hits.
IEnumerable<string> UsefulAlternatives(string[] city)
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
