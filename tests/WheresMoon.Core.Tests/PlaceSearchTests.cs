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

    [TestCase("Tromsø", "tromso")]
    [TestCase("TROMSØ", "tromso")]
    [TestCase("München", "munchen")]
    [TestCase("Saint-Étienne", "saint etienne")]
    [TestCase("Malmö", "malmo")]
    [TestCase("Çanakkale", "canakkale")]
    [TestCase("Łódź", "lodz")]
    [TestCase("Straße", "strasse")]
    [TestCase("Ærøskøbing", "aeroskobing")]
    [TestCase("Sana'a", "sanaa")]
    [TestCase("  St. Gallen  ", "st gallen")]
    [TestCase("", "")]
    public void Folding_removes_what_a_keyboard_may_not_have(string text, string expected)
    {
        SearchText.Fold(text).Should().Be(expected);
    }

    [TestCase("München", "muenchen")]
    [TestCase("Köln", "koeln")]
    [TestCase("Zürich", "zuerich")]
    [TestCase("Tromsø", "tromsoe")]
    public void Umlauts_also_have_their_long_spelling(string text, string expected)
    {
        SearchText.ExpandedSpelling(text).Should().Be(expected);
    }

    [Test]
    public void Names_without_umlauts_have_no_second_spelling()
    {
        SearchText.ExpandedSpelling("Berlin").Should().BeNull();
    }

    [TestCase("Tromso", "Tromsø", "NO")]
    [TestCase("tromsø", "Tromsø", "NO")]
    [TestCase("Tromsoe", "Tromsø", "NO")]
    [TestCase("Muenchen", "Munich", "DE")]
    [TestCase("München", "Munich", "DE")]
    [TestCase("Köln", "Köln", "DE")]
    [TestCase("Koeln", "Köln", "DE")]
    [TestCase("Cologne", "Köln", "DE")]
    [TestCase("Zurich", "Zürich", "CH")]
    [TestCase("Mailand", "Milan", "IT")]
    [TestCase("Peking", "Beijing", "CN")]
    [TestCase("Sao Paulo", "São Paulo", "BR")]
    [TestCase("reykjavik", "Reykjavík", "IS")]
    public void Search_is_tolerant_of_accents_and_finds_german_names(string query, string name, string country)
    {
        Place first = Index.Value.Search(query, 8)[0];

        first.Name.Should().Be(name);
        first.Country.Should().Be(country);
    }

    [TestCase("Spandau", "DE")]
    [TestCase("Kreuzberg", "DE")]
    [TestCase("Altona", "DE")]
    [TestCase("Wattenscheid", "DE")]
    [TestCase("Favoriten", "AT")]
    [TestCase("Stadt Winterthur (Kreis 1)", "CH")]
    [TestCase("Paris 15 Vaugirard", "FR")]
    [TestCase("Marseille 08", "FR")]
    [TestCase("Islington", "GB")]
    [TestCase("Croydon", "GB")]
    [TestCase("Brooklyn", "US")]
    [TestCase("The Bronx", "US")]
    [TestCase("Ciudad Lineal", "ES")]
    [TestCase("Žižkov", "CZ")]
    [TestCase("Ursynów", "PL")]
    [TestCase("Setagaya", "JP")]
    [TestCase("Kowloon", "HK")]
    [TestCase("Bedok New Town", "SG")]
    public void Districts_are_no_places(string name, string country)
    {
        Index.Value.Search(name, 8).Should().NotContain(place => place.Name == name && place.Country == country);
    }

    [TestCase("Potsdam", "DE")]
    [TestCase("Fürth", "DE")]
    [TestCase("Brühl", "DE")]
    [TestCase("Hürth", "DE")]
    [TestCase("Klosterneuburg", "AT")]
    [TestCase("Winterthur", "CH")]
    [TestCase("Boulogne-Billancourt", "FR")]
    [TestCase("Getafe", "ES")]
    [TestCase("Sesto San Giovanni", "IT")]
    [TestCase("Glendale", "US")]
    [TestCase("Newark", "US")]
    [TestCase("Niterói", "BR")]
    [TestCase("Howrah", "IN")]
    [TestCase("New Delhi", "IN")]
    [TestCase("Hong Kong", "HK")]
    [TestCase("Singapore", "SG")]
    [TestCase("London", "GB")]
    [TestCase("New York City", "US")]
    public void Towns_next_to_a_big_city_stay_places(string name, string country)
    {
        Index.Value.Search(name, 8).Should().Contain(place => place.Name == name && place.Country == country);
    }

    [Test]
    public void A_town_of_the_same_name_as_a_district_stays()
    {
        Index.Value.Search("Manhattan", 8).Single(place => place.Name == "Manhattan").Region.Should().Be("Kansas");
    }

    [TestCase("München", "München", "Bayern")]
    [TestCase("Munich", "München", "Bayern")]
    [TestCase("Nürnberg", "Nürnberg", "Bayern")]
    [TestCase("Köln", "Köln", "Nordrhein-Westfalen")]
    [TestCase("Wien", "Wien", "Wien")]
    [TestCase("Genf", "Genf", "Genf")]
    [TestCase("Luzern", "Luzern", "Luzern")]
    [TestCase("Mailand", "Mailand", "Lombardei")]
    [TestCase("Milan", "Mailand", "Lombardei")]
    [TestCase("Venedig", "Venedig", "Venetien")]
    [TestCase("Den Haag", "Den Haag", "Südholland")]
    [TestCase("Peking", "Peking", "Peking")]
    [TestCase("Los Angeles", "Los Angeles", "Kalifornien")]
    public void German_names_appear_in_german(string query, string name, string region)
    {
        Place first = GermanIndex.Value.Search(query, 8)[0];

        first.Name.Should().Be(name);
        first.Region.Should().Be(region);
    }

    [TestCase("Munich", "Munich", "Bavaria")]
    [TestCase("München", "Munich", "Bavaria")]
    [TestCase("Mailand", "Milan", "Lombardy")]
    public void English_names_stay_english(string query, string name, string region)
    {
        Place first = Index.Value.Search(query, 8)[0];

        first.Name.Should().Be(name);
        first.Region.Should().Be(region);
    }

    [TestCase("Danzig", "Gdańsk")]
    [TestCase("Breslau", "Wrocław")]
    [TestCase("Stettin", "Szczecin")]
    [TestCase("Königsberg", "Kaliningrad")]
    public void Old_german_names_find_a_town_but_are_not_shown(string query, string name)
    {
        GermanIndex.Value.Search(query, 8)[0].Name.Should().Be(name);
    }

    [TestCase("Leesburg")]
    [TestCase("Monroe")]
    [TestCase("Naucalpan")]
    [TestCase("Schwedt")]
    public void Names_carry_no_additions(string query)
    {
        GermanIndex.Value.Search(query, 8).Should().AllSatisfy(place => place.Name.Replace(" / ", string.Empty).Should().NotContain("/"));
        GermanIndex.Value.Search(query, 8).Should().AllSatisfy(place => place.Name.Should().NotContain("Municipio"));
    }

    [TestCase("Frankfurt am Main", "Hessen", "Deutschland", "Frankfurt am Main - Hessen - Deutschland")]
    [TestCase("Luzern", "Luzern", "Schweiz", "Luzern - Schweiz")]
    [TestCase("Berlin", "Berlin", "Deutschland", "Berlin - Deutschland")]
    [TestCase("Monaco", "", "Monaco", "Monaco")]
    [TestCase("Tromsø", "Troms", "Norwegen", "Tromsø - Troms - Norwegen")]
    public void A_full_name_reads_city_region_country(string name, string region, string country, string expected)
    {
        var place = new Place(name, region, "XX", 0.0, 0.0, 0.0, "Etc/UTC");

        place.FullName(name, country).Should().Be(expected);
    }

    [Test]
    public void Typing_narrows_down_as_it_goes()
    {
        Index.Value.Search("ber", 8).Single(place => place.Country == "DE" && place.Name == "Berlin").Name.Should().Be("Berlin");
        Index.Value.Search("frankf", 8)[0].Name.Should().Be("Frankfurt am Main");
        Index.Value.Search("san fr", 8)[0].Name.Should().Be("San Francisco");
    }

    [Test]
    public void A_later_word_of_a_name_matches_too()
    {
        Index.Value.Search("francisco", 8).Should().Contain(place => place.Name == "San Francisco");
    }

    [Test]
    public void Larger_places_come_first_among_equal_matches()
    {
        IReadOnlyList<Place> results = Index.Value.Search("Frankfurt", 2);

        results[0].Name.Should().Be("Frankfurt am Main");
        results[1].Name.Should().Be("Frankfurt (Oder)");
    }

    [Test]
    public void Places_with_the_same_name_can_be_told_apart_by_region()
    {
        IReadOnlyList<Place> results = Index.Value.Search("Springfield", 8).Where(place => place.Country == "US").ToList();

        results.Count.Should().BeGreaterThanOrEqualTo(3);
        results.Select(place => place.Region).Distinct().Count().Should().Be(results.Count);
    }

    [Test]
    public void Middle_of_a_word_does_not_match()
    {
        Index.Value.Search("erlin", 8).Should().NotContain(place => place.Name == "Berlin");
    }

    [Test]
    public void Blank_queries_find_nothing()
    {
        Index.Value.Search("   ", 8).Should().BeEmpty();
        Index.Value.Search("-", 8).Should().BeEmpty();
    }

    [Test]
    public void Unknown_names_find_nothing()
    {
        Index.Value.Search("qqxzqq", 8).Should().BeEmpty();
    }

    [Test]
    public void The_limit_holds()
    {
        Index.Value.Search("san", 3).Count.Should().Be(3);
    }

    [Test]
    public void Places_carry_what_the_sky_needs()
    {
        Place tromso = Index.Value.Search("Tromsø", 1)[0];

        tromso.Zone.Should().Be("Europe/Oslo");
        tromso.Latitude.Should().BeApproximately(69.65, 0.05);
        tromso.Longitude.Should().BeApproximately(18.96, 0.05);
        tromso.Region[..5].Should().Be("Troms");
    }

    [Test]
    public void Every_place_has_a_zone_this_system_knows()
    {
        Place[] samples = new[] { "Tokyo", "Auckland", "Honolulu", "Kathmandu", "Kolkata" }.Select(name => Index.Value.Search(name, 1)[0]).ToArray();

        samples.Should().AllSatisfy(place => TimeZoneInfo.FindSystemTimeZoneById(place.Zone).Id.Should().Be(place.Zone));
    }

    [TestCase(52.5, 13.4, "Europe/Berlin")]
    [TestCase(-33.9, 151.2, "Australia/Sydney")]
    [TestCase(69.65, 18.96, "Europe/Oslo")]
    [TestCase(40.7, -74.0, "America/New_York")]
    public void Coordinates_get_the_zone_of_the_nearest_place(double latitude, double longitude, string zone)
    {
        Index.Value.Nearest(latitude, longitude)!.Zone.Should().Be(zone);
    }

    [Test]
    public void Nearest_place_works_across_the_date_line()
    {
        Place place = Index.Value.Nearest(-17.0, -179.9)!;

        Math.Abs(place.Longitude).Should().BeGreaterThan(170.0);
    }

    [Test]
    public void Stored_places_come_back_unchanged()
    {
        var place = new Place("Tromsø", "Troms og Finnmark", "NO", 69.649, 18.955, 12, "Europe/Oslo");

        Place.FromStoredText(place.ToStoredText()).Should().Be(place);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("not a place")]
    [TestCase("a\tb\tc\tx\ty\tz\tzone")]
    public void Unreadable_stored_text_gives_no_place(string? text)
    {
        Place.FromStoredText(text).Should().BeNull();
    }

    [TestCase("Europe/Oslo", "2026-06-21T12:00:00Z", 14)]
    [TestCase("Europe/Oslo", "2026-12-21T12:00:00Z", 13)]
    [TestCase("Pacific/Auckland", "2026-06-21T12:00:00Z", 0)]
    [TestCase("Asia/Kolkata", "2026-06-21T12:30:00Z", 18)]
    [TestCase("America/New_York", "2026-03-08T12:00:00Z", 8)]
    public void Manual_place_shows_its_local_time(string zoneId, string utc, int expectedHour)
    {
        TimeZoneInfo zone = PlaceZone.Resolve(zoneId, 0.0);

        SkySnapshot sky = SkyCalculator.Compute(DateTimeOffset.Parse(utc, CultureInfo.InvariantCulture), new GeoLocation(48.0, 11.0), zone);

        sky.Time.Hour.Should().Be(expectedHour);
    }

    [Test]
    public void Rise_and_set_of_a_far_place_appear_in_its_own_time()
    {
        TimeZoneInfo tokyo = PlaceZone.Resolve("Asia/Tokyo", 139.7);
        DateTimeOffset time = DateTimeOffset.Parse("2026-06-21T03:00:00Z", CultureInfo.InvariantCulture);

        SkySnapshot sky = SkyCalculator.Compute(time, new GeoLocation(35.68, 139.69), tokyo);

        sky.Sun.Body.Rise!.Value.Time.Offset.Should().Be(TimeSpan.FromHours(9));
        sky.Sun.Body.Rise.Value.Time.Hour.Should().BeInRange(4, 5);
        sky.Sun.Body.Set!.Value.Time.Hour.Should().BeInRange(18, 19);
    }

    [Test]
    public void A_day_boundary_follows_the_place_and_not_utc()
    {
        TimeZoneInfo auckland = PlaceZone.Resolve("Pacific/Auckland", 174.8);
        DateTimeOffset time = DateTimeOffset.Parse("2026-06-21T20:00:00Z", CultureInfo.InvariantCulture);

        SkySnapshot sky = SkyCalculator.Compute(time, new GeoLocation(-36.85, 174.76), auckland);

        sky.Time.Day.Should().Be(22);
        sky.Sun.Body.Rise!.Value.Time.Day.Should().Be(22);
    }

    [Test]
    public void An_unknown_zone_falls_back_to_the_longitude()
    {
        TimeZoneInfo zone = PlaceZone.Resolve("Not/AZone", 139.7);

        zone.BaseUtcOffset.Should().Be(TimeSpan.FromHours(9));
    }
}
