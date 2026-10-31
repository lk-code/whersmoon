# Third-party notices

Where's Moon? includes the following third-party software. The full license texts ship with the app under `src/WheresMoon.App/Resources/Raw/licenses` and can be read in its settings.

## Astronomy Engine

- Source: https://github.com/cosinekitty/astronomy
- Version: commit 865d3da, file `source/csharp/astronomy.cs`
- License: MIT, Copyright (c) 2019-2025 Don Cross
- Location in this repository: `src/WheresMoon.Core/External/astronomy.cs`
- Local changes: `#nullable disable` added, `RegexOptions.Compiled` removed. Both are noted at the top of the file.

## Manrope

- Source: https://github.com/aaronbell/manrope
- Files: `fonts/ttf/manrope-light.ttf`, `manrope-regular.ttf`, `manrope-medium.ttf`
- License: SIL Open Font License 1.1, Copyright 2018 The Manrope Project Authors
- Location in this repository: `src/WheresMoon.App/Resources/Fonts`

## GeoNames

- Source: https://download.geonames.org/export/dump/, files `cities15000.zip` and `admin1CodesASCII.txt`, retrieved 2026-10-31
- License: Creative Commons Attribution 4.0 International
- Location in this repository: `src/WheresMoon.App/Resources/Raw/places.tsv.gz`
- Local changes: reduced by `tools/BuildPlaceData.cs` to name, country, region, coordinates rounded to three decimals, elevation, time zone and Latin alternative names, sorted by population.

## Unicode CLDR

- Source: https://github.com/unicode-org/cldr-json, files `cldr-localenames-full/main/de/territories.json` and the same for `en`, version 49.0.0, retrieved 2026-11-01
- License: Unicode License v3, Copyright (c) Unicode, Inc.
- Location in this repository: the `Country` entries in `src/WheresMoon.App/Resources/Strings`

## World Magnetic Model

- Source: https://www.ncei.noaa.gov/products/world-magnetic-model, file `WMM2025COF.zip`
- Version: WMM2025, valid until the end of 2029
- License: public domain, as a work of NOAA and the British Geological Survey. No license text applies.
- Location in this repository: `src/WheresMoon.Core/Compass/WmmCoefficients.cs`

## Reference data in tests

The test project contains rise and set times from the US Naval Observatory, moon positions from NASA/JPL Horizons, moon phases from Fred Espenak's tables, lunar eclipses from NASA's catalogue, supermoon dates from EarthSky and the official WMM2025 test values from NOAA. They are used for comparison only and are not part of the app.
