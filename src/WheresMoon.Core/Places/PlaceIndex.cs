using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace WheresMoon.Core.Places;

/// <summary>
/// The list of cities and the search over it. The file is a gzip compressed text with places
/// sorted by population, written by tools/BuildPlaceData.cs. Two table lines come first, one for
/// time zones and one for regions, which the place lines refer to by position:
/// name, country, region, latitude, longitude, elevation, zone, alternative names joined by "|".
/// </summary>
public sealed class PlaceIndex
{
    /// <summary>Separates the names of a place in its search key. Folded text never contains it.</summary>
    private const char KeySeparator = '\n';

    private const char AlternativeSeparator = '|';

    private const int NoMatch = int.MaxValue;

    // From best to worst: the whole name, its start, a later word. Among those, the official name goes
    // first, so that "München" finds Munich, an alternative name, before "Ottobrunn bei München".
    private const int RankWholeName = 0;
    private const int RankNameStart = 2;
    private const int RankNameWord = 4;
    private const int RankOtherName = 1;

    private readonly Place[] _places;
    private readonly string[] _keys;
    private readonly int[] _nameLengths;

    private PlaceIndex(Place[] places, string[] keys, int[] nameLengths)
    {
        _places = places;
        _keys = keys;
        _nameLengths = nameLengths;
    }

    public int Count => _places.Length;

    public static PlaceIndex Load(Stream compressed)
    {
        using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);

        string[] zones = (reader.ReadLine() ?? string.Empty).Split('\t');
        string[] regions = (reader.ReadLine() ?? string.Empty).Split('\t');

        var places = new List<Place>();
        var keys = new List<string>();
        var nameLengths = new List<int>();

        while (reader.ReadLine() is string line)
        {
            string[] fields = line.Split('\t');

            places.Add(new Place(
                fields[0],
                regions[int.Parse(fields[2], CultureInfo.InvariantCulture)],
                fields[1],
                double.Parse(fields[3], CultureInfo.InvariantCulture),
                double.Parse(fields[4], CultureInfo.InvariantCulture),
                double.Parse(fields[5], CultureInfo.InvariantCulture),
                zones[int.Parse(fields[6], CultureInfo.InvariantCulture)]));

            string name = SearchText.Fold(fields[0]);
            nameLengths.Add(name.Length);
            keys.Add(BuildKey(name, fields[0], fields[7]));
        }

        return new PlaceIndex(places.ToArray(), keys.ToArray(), nameLengths.ToArray());
    }

    /// <summary>The best matches: closest to the query first, larger places first among equals.</summary>
    public IReadOnlyList<Place> Search(string query, int limit)
    {
        string folded = SearchText.Fold(query);
        if (folded.Length == 0)
        {
            return [];
        }

        var best = new List<(int Rank, int Position)>(limit + 1);

        for (int i = 0; i < _places.Length; i++)
        {
            int rank = Rank(i, folded);
            if (rank == NoMatch || (best.Count == limit && rank >= best[^1].Rank))
            {
                continue;
            }

            int insertAt = best.FindIndex(entry => entry.Rank > rank);
            best.Insert(insertAt < 0 ? best.Count : insertAt, (rank, i));

            if (best.Count > limit)
            {
                best.RemoveAt(limit);
            }
        }

        return best.Select(entry => _places[entry.Position]).ToList();
    }

    /// <summary>The closest place, which is near enough to tell the time zone of a spot given by coordinates.</summary>
    public Place? Nearest(double latitude, double longitude)
    {
        Place? nearest = null;
        double smallest = double.MaxValue;
        double shrink = Math.Cos(latitude * Math.PI / 180.0);

        foreach (Place place in _places)
        {
            double north = place.Latitude - latitude;
            double east = Math.IEEERemainder(place.Longitude - longitude, 360.0) * shrink;
            double distance = north * north + east * east;

            if (distance < smallest)
            {
                smallest = distance;
                nearest = place;
            }
        }

        return nearest;
    }

    private static string BuildKey(string foldedName, string name, string alternatives)
    {
        var key = new StringBuilder(foldedName);
        var seen = new HashSet<string> { foldedName };

        Append(SearchText.ExpandedSpelling(name));

        foreach (string alternative in alternatives.Split(AlternativeSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            Append(SearchText.Fold(alternative));
            Append(SearchText.ExpandedSpelling(alternative));
        }

        return key.ToString();

        void Append(string? text)
        {
            if (text is { Length: > 0 } && seen.Add(text))
            {
                key.Append(KeySeparator).Append(text);
            }
        }
    }

    private int Rank(int place, string query)
    {
        string key = _keys[place];
        int nameLength = _nameLengths[place];
        int best = NoMatch;

        for (int at = key.IndexOf(query, StringComparison.Ordinal); at >= 0; at = key.IndexOf(query, at + 1, StringComparison.Ordinal))
        {
            bool startsEntry = at == 0 || key[at - 1] == KeySeparator;
            if (!startsEntry && key[at - 1] != ' ')
            {
                continue;
            }

            int end = at + query.Length;
            bool endsEntry = end == key.Length || key[end] == KeySeparator;

            int rank = !startsEntry ? RankNameWord : endsEntry ? RankWholeName : RankNameStart;
            rank += at < nameLength ? 0 : RankOtherName;

            best = Math.Min(best, rank);
        }

        return best;
    }
}
