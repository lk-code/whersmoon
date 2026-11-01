using System.Globalization;
using WheresMoon.Core.Places;

namespace WheresMoon.Core.Tests;

public class PlaceSearchTests
{
    private static readonly Lazy<PlaceIndex> Index = new(() => LoadIndex("en"));
    private static readonly Lazy<PlaceIndex> GermanIndex = new(() => LoadIndex("de"));

    private static PlaceIndex LoadIndex(string language)
    {
        string? folder = AppContext.BaseDirectory;
        while (folder is not null && !File.Exists(Path.Combine(folder, "WheresMoon.slnx")))
        {
            folder = Path.GetDirectoryName(folder);
        }

        using FileStream file = File.OpenRead(Path.Combine(folder!, "src", "WheresMoon.App", "Resources", "Raw", "places.tsv.gz"));
        return PlaceIndex.Load(file, language);
    }

    [Theory]
    [InlineData("Tromsø", "tromso")]
    [InlineData("TROMSØ", "tromso")]
    [InlineData("München", "munchen")]
    [InlineData("Saint-Étienne", "saint etienne")]
    [InlineData("Malmö", "malmo")]
    [InlineData("Çanakkale", "canakkale")]
    [InlineData("Łódź", "lodz")]
    [InlineData("Straße", "strasse")]
    [InlineData("Ærøskøbing", "aeroskobing")]
    [InlineData("Sana'a", "sanaa")]
    [InlineData("  St. Gallen  ", "st gallen")]
    [InlineData("", "")]
    public void Folding_removes_what_a_keyboard_may_not_have(string text, string expected)
    {
        Assert.Equal(expected, SearchText.Fold(text));
    }

    [Theory]
    [InlineData("München", "muenchen")]
    [InlineData("Köln", "koeln")]
    [InlineData("Zürich", "zuerich")]
    [InlineData("Tromsø", "tromsoe")]
    public void Umlauts_also_have_their_long_spelling(string text, string expected)
    {
        Assert.Equal(expected, SearchText.ExpandedSpelling(text));
    }

    [Fact]
    public void Names_without_umlauts_have_no_second_spelling()
    {
        Assert.Null(SearchText.ExpandedSpelling("Berlin"));
    }

    [Theory]
    [InlineData("Tromso", "Tromsø", "NO")]
    [InlineData("tromsø", "Tromsø", "NO")]
    [InlineData("Tromsoe", "Tromsø", "NO")]
    [InlineData("Muenchen", "Munich", "DE")]
    [InlineData("München", "Munich", "DE")]
    [InlineData("Köln", "Köln", "DE")]
    [InlineData("Koeln", "Köln", "DE")]
    [InlineData("Cologne", "Köln", "DE")]
    [InlineData("Zurich", "Zürich", "CH")]
    [InlineData("Mailand", "Milan", "IT")]
    [InlineData("Peking", "Beijing", "CN")]
    [InlineData("Sao Paulo", "São Paulo", "BR")]
    [InlineData("reykjavik", "Reykjavík", "IS")]
    public void Search_is_tolerant_of_accents_and_finds_german_names(string query, string name, string country)
    {
        Place first = Index.Value.Search(query, 8)[0];

        Assert.Equal(name, first.Name);
        Assert.Equal(country, first.Country);
    }

    [Theory]
    [InlineData("Spandau", "DE")]
    [InlineData("Kreuzberg", "DE")]
    [InlineData("Altona", "DE")]
    [InlineData("Wattenscheid", "DE")]
    [InlineData("Favoriten", "AT")]
    [InlineData("Stadt Winterthur (Kreis 1)", "CH")]
    [InlineData("Paris 15 Vaugirard", "FR")]
    [InlineData("Marseille 08", "FR")]
    [InlineData("Islington", "GB")]
    [InlineData("Croydon", "GB")]
    [InlineData("Brooklyn", "US")]
    [InlineData("The Bronx", "US")]
    [InlineData("Ciudad Lineal", "ES")]
    [InlineData("Žižkov", "CZ")]
    [InlineData("Ursynów", "PL")]
    [InlineData("Setagaya", "JP")]
    [InlineData("Kowloon", "HK")]
    [InlineData("Bedok New Town", "SG")]
    public void Districts_are_no_places(string name, string country)
    {
        Assert.DoesNotContain(Index.Value.Search(name, 8), place => place.Name == name && place.Country == country);
    }

    [Theory]
    [InlineData("Potsdam", "DE")]
    [InlineData("Fürth", "DE")]
    [InlineData("Brühl", "DE")]
    [InlineData("Hürth", "DE")]
    [InlineData("Klosterneuburg", "AT")]
    [InlineData("Winterthur", "CH")]
    [InlineData("Boulogne-Billancourt", "FR")]
    [InlineData("Getafe", "ES")]
    [InlineData("Sesto San Giovanni", "IT")]
    [InlineData("Glendale", "US")]
    [InlineData("Newark", "US")]
    [InlineData("Niterói", "BR")]
    [InlineData("Howrah", "IN")]
    [InlineData("New Delhi", "IN")]
    [InlineData("Hong Kong", "HK")]
    [InlineData("Singapore", "SG")]
    [InlineData("London", "GB")]
    [InlineData("New York City", "US")]
    public void Towns_next_to_a_big_city_stay_places(string name, string country)
    {
        Assert.Contains(Index.Value.Search(name, 8), place => place.Name == name && place.Country == country);
    }

    [Fact]
    public void A_town_of_the_same_name_as_a_district_stays()
    {
        Assert.Equal("Kansas", Index.Value.Search("Manhattan", 8).Single(place => place.Name == "Manhattan").Region);
    }

    [Theory]
    [InlineData("München", "München", "Bayern")]
    [InlineData("Munich", "München", "Bayern")]
    [InlineData("Nürnberg", "Nürnberg", "Bayern")]
    [InlineData("Köln", "Köln", "Nordrhein-Westfalen")]
    [InlineData("Wien", "Wien", "Wien")]
    [InlineData("Genf", "Genf", "Genf")]
    [InlineData("Luzern", "Luzern", "Luzern")]
    [InlineData("Mailand", "Mailand", "Lombardei")]
    [InlineData("Milan", "Mailand", "Lombardei")]
    [InlineData("Venedig", "Venedig", "Venetien")]
    [InlineData("Den Haag", "Den Haag", "Südholland")]
    [InlineData("Peking", "Peking", "Peking")]
    [InlineData("Los Angeles", "Los Angeles", "Kalifornien")]
    public void German_names_appear_in_german(string query, string name, string region)
    {
        Place first = GermanIndex.Value.Search(query, 8)[0];

        Assert.Equal(name, first.Name);
        Assert.Equal(region, first.Region);
    }

    [Theory]
    [InlineData("Munich", "Munich", "Bavaria")]
    [InlineData("München", "Munich", "Bavaria")]
    [InlineData("Mailand", "Milan", "Lombardy")]
    public void English_names_stay_english(string query, string name, string region)
    {
        Place first = Index.Value.Search(query, 8)[0];

        Assert.Equal(name, first.Name);
        Assert.Equal(region, first.Region);
    }

    [Theory]
    [InlineData("Danzig", "Gdańsk")]
    [InlineData("Breslau", "Wrocław")]
    [InlineData("Stettin", "Szczecin")]
    [InlineData("Königsberg", "Kaliningrad")]
    public void Old_german_names_find_a_town_but_are_not_shown(string query, string name)
    {
        Assert.Equal(name, GermanIndex.Value.Search(query, 8)[0].Name);
    }

    [Theory]
    [InlineData("Leesburg")]
    [InlineData("Monroe")]
    [InlineData("Naucalpan")]
    [InlineData("Schwedt")]
    public void Names_carry_no_additions(string query)
    {
        Assert.All(GermanIndex.Value.Search(query, 8), place => Assert.DoesNotContain("/", place.Name.Replace(" / ", string.Empty)));
        Assert.All(GermanIndex.Value.Search(query, 8), place => Assert.DoesNotContain("Municipio", place.Name));
    }

    [Theory]
    [InlineData("Frankfurt am Main", "Hessen", "Deutschland", "Frankfurt am Main - Hessen - Deutschland")]
    [InlineData("Luzern", "Luzern", "Schweiz", "Luzern - Schweiz")]
    [InlineData("Berlin", "Berlin", "Deutschland", "Berlin - Deutschland")]
    [InlineData("Monaco", "", "Monaco", "Monaco")]
    [InlineData("Tromsø", "Troms", "Norwegen", "Tromsø - Troms - Norwegen")]
    public void A_full_name_reads_city_region_country(string name, string region, string country, string expected)
    {
        var place = new Place(name, region, "XX", 0.0, 0.0, 0.0, "Etc/UTC");

        Assert.Equal(expected, place.FullName(name, country));
    }

    [Fact]
    public void Typing_narrows_down_as_it_goes()
    {
        Assert.Equal("Berlin", Index.Value.Search("ber", 8).Single(place => place.Country == "DE" && place.Name == "Berlin").Name);
        Assert.Equal("Frankfurt am Main", Index.Value.Search("frankf", 8)[0].Name);
        Assert.Equal("San Francisco", Index.Value.Search("san fr", 8)[0].Name);
    }

    [Fact]
    public void A_later_word_of_a_name_matches_too()
    {
        Assert.Contains(Index.Value.Search("francisco", 8), place => place.Name == "San Francisco");
    }

    [Fact]
    public void Larger_places_come_first_among_equal_matches()
    {
        IReadOnlyList<Place> results = Index.Value.Search("Frankfurt", 2);

        Assert.Equal("Frankfurt am Main", results[0].Name);
        Assert.Equal("Frankfurt (Oder)", results[1].Name);
    }

    [Fact]
    public void Places_with_the_same_name_can_be_told_apart_by_region()
    {
        IReadOnlyList<Place> results = Index.Value.Search("Springfield", 8).Where(place => place.Country == "US").ToList();

        Assert.True(results.Count >= 3);
        Assert.Equal(results.Count, results.Select(place => place.Region).Distinct().Count());
    }

    [Fact]
    public void Middle_of_a_word_does_not_match()
    {
        Assert.DoesNotContain(Index.Value.Search("erlin", 8), place => place.Name == "Berlin");
    }

    [Fact]
    public void Blank_queries_find_nothing()
    {
        Assert.Empty(Index.Value.Search("   ", 8));
        Assert.Empty(Index.Value.Search("-", 8));
    }

    [Fact]
    public void Unknown_names_find_nothing()
    {
        Assert.Empty(Index.Value.Search("qqxzqq", 8));
    }

    [Fact]
    public void The_limit_holds()
    {
        Assert.Equal(3, Index.Value.Search("san", 3).Count);
    }

    [Fact]
    public void Places_carry_what_the_sky_needs()
    {
        Place tromso = Index.Value.Search("Tromsø", 1)[0];

        Assert.Equal("Europe/Oslo", tromso.Zone);
        Assert.Equal(69.65, tromso.Latitude, 1);
        Assert.Equal(18.96, tromso.Longitude, 1);
        Assert.Equal("Troms", tromso.Region[..5]);
    }

    [Fact]
    public void Every_place_has_a_zone_this_system_knows()
    {
        Place[] samples = new[] { "Tokyo", "Auckland", "Honolulu", "Kathmandu", "Kolkata" }.Select(name => Index.Value.Search(name, 1)[0]).ToArray();

        Assert.All(samples, place => Assert.Equal(place.Zone, TimeZoneInfo.FindSystemTimeZoneById(place.Zone).Id));
    }

    [Theory]
    [InlineData(52.5, 13.4, "Europe/Berlin")]
    [InlineData(-33.9, 151.2, "Australia/Sydney")]
    [InlineData(69.65, 18.96, "Europe/Oslo")]
    [InlineData(40.7, -74.0, "America/New_York")]
    public void Coordinates_get_the_zone_of_the_nearest_place(double latitude, double longitude, string zone)
    {
        Assert.Equal(zone, Index.Value.Nearest(latitude, longitude)!.Zone);
    }

    [Fact]
    public void Nearest_place_works_across_the_date_line()
    {
        Place place = Index.Value.Nearest(-17.0, -179.9)!;

        Assert.True(Math.Abs(place.Longitude) > 170.0);
    }

    [Fact]
    public void Stored_places_come_back_unchanged()
    {
        var place = new Place("Tromsø", "Troms og Finnmark", "NO", 69.649, 18.955, 12, "Europe/Oslo");

        Assert.Equal(place, Place.FromStoredText(place.ToStoredText()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a place")]
    [InlineData("a\tb\tc\tx\ty\tz\tzone")]
    public void Unreadable_stored_text_gives_no_place(string? text)
    {
        Assert.Null(Place.FromStoredText(text));
    }

    [Theory]
    [InlineData("Europe/Oslo", "2026-06-21T12:00:00Z", 14)]
    [InlineData("Europe/Oslo", "2026-12-21T12:00:00Z", 13)]
    [InlineData("Pacific/Auckland", "2026-06-21T12:00:00Z", 0)]
    [InlineData("Asia/Kolkata", "2026-06-21T12:30:00Z", 18)]
    [InlineData("America/New_York", "2026-03-08T12:00:00Z", 8)]
    public void Manual_place_shows_its_local_time(string zoneId, string utc, int expectedHour)
    {
        TimeZoneInfo zone = PlaceZone.Resolve(zoneId, 0.0);

        SkySnapshot sky = SkyCalculator.Compute(DateTimeOffset.Parse(utc, CultureInfo.InvariantCulture), new GeoLocation(48.0, 11.0), zone);

        Assert.Equal(expectedHour, sky.Time.Hour);
    }

    [Fact]
    public void Rise_and_set_of_a_far_place_appear_in_its_own_time()
    {
        TimeZoneInfo tokyo = PlaceZone.Resolve("Asia/Tokyo", 139.7);
        DateTimeOffset time = DateTimeOffset.Parse("2026-06-21T03:00:00Z", CultureInfo.InvariantCulture);

        SkySnapshot sky = SkyCalculator.Compute(time, new GeoLocation(35.68, 139.69), tokyo);

        Assert.Equal(TimeSpan.FromHours(9), sky.Sun.Body.Rise!.Value.Time.Offset);
        Assert.InRange(sky.Sun.Body.Rise.Value.Time.Hour, 4, 5);
        Assert.InRange(sky.Sun.Body.Set!.Value.Time.Hour, 18, 19);
    }

    [Fact]
    public void A_day_boundary_follows_the_place_and_not_utc()
    {
        TimeZoneInfo auckland = PlaceZone.Resolve("Pacific/Auckland", 174.8);
        DateTimeOffset time = DateTimeOffset.Parse("2026-06-21T20:00:00Z", CultureInfo.InvariantCulture);

        SkySnapshot sky = SkyCalculator.Compute(time, new GeoLocation(-36.85, 174.76), auckland);

        Assert.Equal(22, sky.Time.Day);
        Assert.Equal(22, sky.Sun.Body.Rise!.Value.Time.Day);
    }

    [Fact]
    public void An_unknown_zone_falls_back_to_the_longitude()
    {
        TimeZoneInfo zone = PlaceZone.Resolve("Not/AZone", 139.7);

        Assert.Equal(TimeSpan.FromHours(9), zone.BaseUtcOffset);
    }
}
