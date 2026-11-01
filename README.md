# Where's Moon?

A minimalist app that shows at a glance where the sun and moon currently are, based on your location and including the current moon phase.

Instead of complex 3D sky maps, the app shows two simple arcs above a horizon line:

- **Outer arc (sun):** The sun icon moves from sunrise (left) through its highest point (center) to sunset (right), similar to a sundial.
- **Inner arc (moon):** The moon icon moves the same way between moonrise and moonset.
- **Moon icon:** Shows the current moon phase, from new moon through waxing crescent and full moon to waning crescent.

This makes it easy to see how the sun and moon relate to each other, without any knowledge of astronomy. All calculations run locally on the device, so the app also works offline.

- **Info pages:** Swipe the text below the clock to move between sun and moon with the day length, the next moon phases and eclipses, and the golden and blue hours.
- **Time travel:** Swipe across the scene to move the time, from right to left into the future, a day per screen width, or a month on the events page. A fast swipe flies on and slows down by itself, a slow one stops with the finger. Slow movement catches on the events of the current page. Double tap, the Today button, Home or Escape return to the present. On a computer, drag with the mouse or use the wheel, the trackpad or the left and right arrow keys.
- **Any place:** Follow the device, search 30,000 cities offline, or enter coordinates. Times then appear in the local time of that place.
- **Compass:** Two rings in the ground show the directions to the sun and the moon and turn with the device, corrected to true north with the World Magnetic Model. They shift slightly as the device moves.
- **Stars:** They lie on a band around you and move against every turn of the device. Computers without a compass show north up and still stars.

The app is built with .NET MAUI for iOS, iPadOS, Android, Windows and macOS (through Mac Catalyst). It uses no network, no tracking and no analytics.

## Project structure

| Path | Purpose |
| --- | --- |
| `src/WheresMoon.Core` | All calculations and scene logic, with no MAUI dependency, so native apps can reuse it |
| `src/WheresMoon.App` | The MAUI app: drawing, location, sensors and platform integration |
| `tests/WheresMoon.Core.Tests` | Unit tests against reference data from USNO, JPL Horizons, NASA and NOAA |
| `tools/WheresMoon.SceneRenderer` | Renders the scene to PNG files without any UI, for checking formats and times of day |
| `tools/BuildPlaceData.cs` | Builds the city list from the GeoNames dumps |

## Running the app

Run these commands from the repository root. You need the .NET 10 SDK, version 10.0.403 or newer. Always pick one target framework with `-f`, because building the whole solution on Windows would also try the Apple targets.

### Windows

```sh
dotnet workload install maui
dotnet run --project src/WheresMoon.App/WheresMoon.App.csproj -f net10.0-windows10.0.19041.0
```

### Android

Start an emulator or connect a device first. The command keeps showing the device log until you press Ctrl+C. The app keeps running after that.

```sh
dotnet workload install maui
dotnet run --project src/WheresMoon.App/WheresMoon.App.csproj -f net10.0-android
```

### macOS and iOS

These need a Mac with Xcode. CI builds with Xcode 26.6 and the workload versions pinned in `.github/workload-versions.json`, so install those instead of the latest ones, which can require a newer Xcode than you have:

```sh
sudo dotnet workload install maui --from-rollback-file .github/workload-versions.json
```

macOS, through Mac Catalyst:

```sh
dotnet run --project src/WheresMoon.App/WheresMoon.App.csproj -f net10.0-maccatalyst -p:ValidateXcodeVersion=false
```

iOS, in a simulator. Look up the UDID of a simulator with `xcrun simctl list devices available`, then build for it:

```sh
dotnet build src/WheresMoon.App/WheresMoon.App.csproj -t:Run -f net10.0-ios -r iossimulator-arm64 -p:_DeviceName=:v2:udid=YOUR_UDID -p:ValidateXcodeVersion=false
```

If the app crashes on launch with `Could not register the assembly`, its build output comes from another workload version. Delete `src/WheresMoon.App/bin` and `src/WheresMoon.App/obj` and build again.

A simulator has no compass, so the compass stays still there. The Android emulator does have sensors, which `adb emu sensor set` can move.

## Tests and tools

```sh
dotnet test tests/WheresMoon.Core.Tests/WheresMoon.Core.Tests.csproj
dotnet run --project tools/WheresMoon.SceneRenderer -- scene-output
```

Debug builds accept two environment variables to look at any moment and place: `WHERESMOON_SIM_TIME` (for example `2026-10-30T17:47:00Z`) and `WHERESMOON_SIM_LOCATION` (for example `52.52,13.405`, or `none` for the state without a location).

To update the city list, get `cities15000.txt`, `admin1CodesASCII.txt` and `alternateNamesV2.txt` from https://download.geonames.org/export/dump/ and run:

```sh
dotnet run tools/BuildPlaceData.cs -- cities15000.txt admin1CodesASCII.txt alternateNamesV2.txt src/WheresMoon.App/Resources/Raw/places.tsv.gz
```

To check the licenses of the NuGet packages after a build, run this from the repository root. It fails for a license that is not on its list and for a package missing from `THIRD-PARTY-NOTICES.md`, so a new package needs a line there:

```sh
dotnet run tools/CheckLicenses.cs -- src/WheresMoon.App/obj/project.assets.json
```

## License

MIT, see [LICENSE](LICENSE). Third-party components are listed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).