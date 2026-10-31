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

## Reference data in tests

The test project contains rise and set times from the US Naval Observatory and moon positions from NASA/JPL Horizons, both public services. They are used for comparison only and are not part of the app.
