# Where's Moon?

A minimalist app that shows at a glance where the sun and moon currently are, based on your location and including the current moon phase.

Instead of complex 3D sky maps, the app shows two simple arcs above a horizon line:

- **Outer arc (sun):** The sun icon moves from sunrise (left) through its highest point (center) to sunset (right), similar to a sundial.
- **Inner arc (moon):** The moon icon moves the same way between moonrise and moonset.
- **Moon icon:** Shows the current moon phase, from new moon through waxing crescent and full moon to waning crescent.

This makes it easy to see how the sun and moon relate to each other, without any knowledge of astronomy. All calculations run locally on the device, so the app also works offline.

The app is built with .NET MAUI for iOS, iPadOS, Android, Windows and macOS (through Mac Catalyst). It uses no network, no tracking and no analytics.

## Project structure

| Path | Purpose |
| --- | --- |
| `src/WheresMoon.Core` | All calculations and scene logic, with no MAUI dependency, so native apps can reuse it |
| `src/WheresMoon.App` | The MAUI app: drawing, location and platform integration |
| `tests/WheresMoon.Core.Tests` | Unit tests against USNO and JPL Horizons reference data |
| `tools/WheresMoon.SceneRenderer` | Renders the scene to PNG files without any UI, for checking formats and times of day |

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

iOS, in the default simulator:

```sh
dotnet run --project src/WheresMoon.App/WheresMoon.App.csproj -f net10.0-ios -p:ValidateXcodeVersion=false
```

To choose a simulator, look up its UDID and build with it:

```sh
xcrun simctl list devices available
dotnet build src/WheresMoon.App/WheresMoon.App.csproj -t:Run -f net10.0-ios -p:_DeviceName=:v2:udid=YOUR_UDID -p:ValidateXcodeVersion=false
```

The Apple targets build in CI, but these run commands have not been tried on a Mac yet. If `dotnet run` does not start the app, use `dotnet build src/WheresMoon.App/WheresMoon.App.csproj -t:Run` with the same options, which is the documented way for MAUI.

## Tests and tools

```sh
dotnet test tests/WheresMoon.Core.Tests/WheresMoon.Core.Tests.csproj
dotnet run --project tools/WheresMoon.SceneRenderer -- scene-output
```

Debug builds accept two environment variables to look at any moment and place: `WHERESMOON_SIM_TIME` (for example `2026-10-30T17:47:00Z`) and `WHERESMOON_SIM_LOCATION` (for example `52.52,13.405`, or `none` for the state without a location).

## License

MIT, see [LICENSE](LICENSE). Third-party components are listed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).