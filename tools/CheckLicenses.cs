// Fails when a NuGet package has a license this project does not accept or is missing from THIRD-PARTY-NOTICES.md.
// Input: project.assets.json files, which a restore creates. Packages differ per system, so each CI job checks its own.
// Usage, from the repository root: dotnet run tools/CheckLicenses.cs -- <project.assets.json> [<project.assets.json> ...]

using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

const string NoticesFile = "THIRD-PARTY-NOTICES.md";

string[] allowed = ["MIT", "BSD-2-Clause", "BSD-3-Clause", "Apache-2.0", "ISC", "CC0-1.0", "Unicode-3.0"];

// Microsoft's own terms for what MAUI brings along on Windows. They name a file or a link instead of a license id.
string[] microsoftTerms = ["Microsoft.Graphics.Win2D", "Microsoft.Web.WebView2", "Microsoft.Windows.SDK.BuildTools", "Microsoft.WindowsAppSDK"];

var listed = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

foreach (string line in File.ReadLines(NoticesFile))
{
    Match row = Regex.Match(line, @"^- (\S+) (\S+) \| ");

    if (row.Success)
    {
        if (!listed.TryGetValue(row.Groups[1].Value, out HashSet<string>? versions))
        {
            versions = [];
            listed[row.Groups[1].Value] = versions;
        }

        versions.Add(row.Groups[2].Value);
    }
}

var problems = new SortedSet<string>();
var outdated = new SortedSet<string>();
var seen = new HashSet<string>();

foreach (string file in args)
{
    using JsonDocument assets = JsonDocument.Parse(File.ReadAllText(file));
    string[] folders = assets.RootElement.GetProperty("packageFolders").EnumerateObject().Select(folder => folder.Name).ToArray();

    foreach (JsonProperty library in assets.RootElement.GetProperty("libraries").EnumerateObject())
    {
        if (library.Value.GetProperty("type").GetString() != "package" || !seen.Add(library.Name))
        {
            continue;
        }

        string[] parts = library.Name.Split('/');
        string id = parts[0];
        string version = parts[1];

        string? license = LicenseOf(library.Value, folders);

        if (!microsoftTerms.Contains(id, StringComparer.OrdinalIgnoreCase) && !IsAllowed(license))
        {
            problems.Add($"{id} {version}: license \"{license ?? "unknown"}\" needs a decision");
        }

        if (!listed.TryGetValue(id, out HashSet<string>? versions))
        {
            problems.Add($"{id} {version}: missing from {NoticesFile}");
        }
        else if (!versions.Contains(version))
        {
            outdated.Add($"{id}: {NoticesFile} lists {string.Join(", ", versions)}, resolved is {version}");
        }
    }
}

// MAUI's version comes from the installed workload, so a newer one alone should not break a build.
foreach (string message in outdated)
{
    Console.WriteLine($"warning: {message}");
}

foreach (string message in problems)
{
    Console.Error.WriteLine($"error: {message}");
}

Console.WriteLine($"Checked {seen.Count} packages: {problems.Count} problems, {outdated.Count} outdated versions.");
return problems.Count == 0 ? 0 : 1;

string? LicenseOf(JsonElement library, string[] folders)
{
    string relativePath = library.GetProperty("path").GetString()!;
    string nuspecName = library.GetProperty("files").EnumerateArray()
        .Select(entry => entry.GetString()!)
        .First(entry => entry.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));

    string nuspec = folders.Select(folder => Path.Combine(folder, relativePath, nuspecName)).First(File.Exists);
    XElement? license = XDocument.Load(nuspec).Descendants().FirstOrDefault(element => element.Name.LocalName == "license");

    return license?.Attribute("type")?.Value == "expression" ? license.Value.Trim() : null;
}

bool IsAllowed(string? expression) =>
    expression is not null
    && expression.Replace("(", string.Empty).Replace(")", string.Empty).Split(" OR ")
        .Any(choice => choice.Split(" AND ").All(part => allowed.Contains(part.Split(" WITH ")[0].Trim())));
