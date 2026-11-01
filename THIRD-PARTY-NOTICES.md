# Third-party notices

Where's Moon? uses the following third-party software and data. The settings of the app show this file, followed by the full license texts from `src/WheresMoon.App/Resources/Raw/licenses`.

Package lines give the name and version, the license, the copyright holder and a link. `tools/CheckLicenses.cs` compares them with the packages a build resolves.

## Astronomy Engine

- Source: https://github.com/cosinekitty/astronomy
- Version: commit 865d3da, file `source/csharp/astronomy.cs`
- License: MIT, Copyright (c) 2019-2025 Don Cross
- Location in this repository: `src/WheresMoon.Core/External/astronomy.cs`
- Local changes: `#nullable disable` added, `RegexOptions.Compiled` removed. Both are noted at the top of the file.

## GeoNames

- Source: https://download.geonames.org/export/dump/, files `cities15000.zip` and `admin1CodesASCII.txt`, retrieved 2026-10-31, and `alternateNamesV2.zip`, retrieved 2026-11-01
- License: Creative Commons Attribution 4.0 International, GeoNames, https://www.geonames.org
- Location in this repository: `src/WheresMoon.App/Resources/Raw/places.tsv.gz`
- Local changes: reduced by `tools/BuildPlaceData.cs` to cities without their districts, with name, German name, country, region with its German name, coordinates rounded to three decimals, elevation, time zone and Latin alternative names, sorted by population.

## Unicode CLDR

- Source: https://github.com/unicode-org/cldr-json, files `cldr-localenames-full/main/de/territories.json` and the same for `en`, version 49.0.0, retrieved 2026-11-01
- License: Unicode License v3, Copyright (c) 2015-2024 Unicode, Inc.
- Location in this repository: the `Country` entries in `src/WheresMoon.App/Resources/Strings`

## World Magnetic Model

- Source: https://www.ncei.noaa.gov/products/world-magnetic-model, file `WMM2025COF.zip`
- Version: WMM2025, valid until the end of 2029
- License: public domain according to NOAA's National Centers for Environmental Information, which state that the information and software may be used freely by the public. No license text applies.
- Authors: NOAA and the British Geological Survey. This is U.S. Government material.
- Location in this repository: `src/WheresMoon.Core/Compass/WmmCoefficients.cs`

## NuGet packages on every system

- Microsoft.Extensions.Configuration.Abstractions 10.0.0 | MIT | Microsoft Corporation | https://dot.net/
- Microsoft.Extensions.Configuration 10.0.0 | MIT | Microsoft Corporation | https://dot.net/
- Microsoft.Extensions.DependencyInjection.Abstractions 10.0.0 | MIT | Microsoft Corporation | https://dot.net/
- Microsoft.Extensions.DependencyInjection 10.0.0 | MIT | Microsoft Corporation | https://dot.net/
- Microsoft.Extensions.Diagnostics.Abstractions 10.0.0 | MIT | Microsoft Corporation | https://dot.net/
- Microsoft.Extensions.FileProviders.Abstractions 10.0.0 | MIT | Microsoft Corporation | https://dot.net/
- Microsoft.Extensions.Hosting.Abstractions 10.0.0 | MIT | Microsoft Corporation | https://dot.net/
- Microsoft.Extensions.Logging.Abstractions 10.0.0 | MIT | Microsoft Corporation | https://dot.net/
- Microsoft.Extensions.Logging 10.0.0 | MIT | Microsoft Corporation | https://dot.net/
- Microsoft.Extensions.Options 10.0.0 | MIT | Microsoft Corporation | https://dot.net/
- Microsoft.Extensions.Primitives 10.0.0 | MIT | Microsoft Corporation | https://dot.net/
- Microsoft.Maui.Controls.Build.Tasks 10.0.0 | MIT | Microsoft Corporation | https://github.com/dotnet/maui
- Microsoft.Maui.Controls.Core 10.0.0 | MIT | Microsoft Corporation | https://github.com/dotnet/maui
- Microsoft.Maui.Controls.Xaml 10.0.0 | MIT | Microsoft Corporation | https://github.com/dotnet/maui
- Microsoft.Maui.Controls 10.0.0 | MIT | Microsoft Corporation | https://github.com/dotnet/maui
- Microsoft.Maui.Core 10.0.0 | MIT | Microsoft Corporation | https://github.com/dotnet/maui
- Microsoft.Maui.Essentials 10.0.0 | MIT | Microsoft Corporation | https://github.com/dotnet/maui
- Microsoft.Maui.Graphics 10.0.0 | MIT | Microsoft Corporation | https://github.com/dotnet/maui
- Microsoft.Maui.Resizetizer 10.0.0 | MIT | Microsoft Corporation | https://github.com/dotnet/maui

## NuGet packages on Android

These are Microsoft's bindings, under MIT, of Java libraries by The Android Open Source Project, Google, JetBrains and others. Microsoft's notices in each package list the Java libraries under the Apache License 2.0. Glide also uses the Simplified BSD License, Copyright 2014 Google, Inc.

- GoogleGson 2.13.1.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.Android.Glide.Annotations 4.16.0.14 | MIT AND BSD-2-Clause AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.Android.Glide.DiskLruCache 4.16.0.14 | MIT AND BSD-2-Clause AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.Android.Glide.GifDecoder 4.16.0.14 | MIT AND BSD-2-Clause AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.Android.Glide 4.16.0.14 | MIT AND BSD-2-Clause AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Activity.Ktx 1.10.1.3 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Activity 1.10.1.3 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Annotation.Experimental 1.5.1.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Annotation.Jvm 1.9.1.5 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Annotation 1.9.1.5 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.AppCompat.AppCompatResources 1.7.1.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.AppCompat 1.7.1.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Arch.Core.Common 2.2.0.18 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Arch.Core.Runtime 2.2.0.18 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Browser 1.8.0.11 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.CardView 1.0.0.36 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Collection.Jvm 1.5.0.3 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Collection.Ktx 1.5.0.3 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Collection 1.5.0.3 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Concurrent.Futures 1.3.0.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.ConstraintLayout.Core 1.1.1.3 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.ConstraintLayout 2.2.1.3 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.CoordinatorLayout 1.3.0.3 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Core.Core.Ktx 1.16.0.3 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Core.ViewTree 1.0.0.3 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Core 1.16.0.3 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.CursorAdapter 1.0.0.34 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.CustomView.PoolingContainer 1.1.0.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.CustomView 1.2.0.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.DrawerLayout 1.2.0.18 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.DynamicAnimation 1.1.0.3 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Emoji2.ViewsHelper 1.5.0.6 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Emoji2 1.5.0.6 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.ExifInterface 1.4.1.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Fragment.Ktx 1.8.8.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Fragment 1.8.8.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Interpolator 1.0.0.34 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Lifecycle.Common.Jvm 2.9.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Lifecycle.Common 2.9.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Lifecycle.LiveData.Core.Ktx 2.9.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Lifecycle.LiveData.Core 2.9.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Lifecycle.LiveData 2.9.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Lifecycle.Process 2.9.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Lifecycle.Runtime.Android 2.9.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Lifecycle.Runtime.Ktx.Android 2.9.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Lifecycle.Runtime.Ktx 2.9.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Lifecycle.Runtime 2.9.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Lifecycle.ViewModel.Android 2.9.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Lifecycle.ViewModel.Ktx 2.9.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Lifecycle.ViewModel 2.9.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Lifecycle.ViewModelSavedState.Android 2.9.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Lifecycle.ViewModelSavedState 2.9.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Loader 1.1.0.34 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Navigation.Common.Android 2.9.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Navigation.Common 2.9.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Navigation.Fragment 2.9.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Navigation.Runtime.Android 2.9.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Navigation.Runtime 2.9.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Navigation.UI 2.9.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.ProfileInstaller.ProfileInstaller 1.4.1.5 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.RecyclerView 1.4.0.3 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.ResourceInspection.Annotation 1.0.1.22 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.SavedState.SavedState.Android 1.3.1.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.SavedState.SavedState.Ktx 1.3.1.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.SavedState 1.3.1.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Security.SecurityCrypto 1.1.0.4-alpha07 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.SlidingPaneLayout 1.2.0.22 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Startup.StartupRuntime 1.2.0.5 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.SwipeRefreshLayout 1.1.0.29 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Tracing.Tracing.Android 1.3.0.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Tracing.Tracing 1.3.0.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Transition 1.6.0.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.VectorDrawable.Animated 1.2.0.8 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.VectorDrawable 1.2.0.8 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.VersionedParcelable 1.2.1.3 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.ViewPager 1.1.0.4 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.ViewPager2 1.1.0.8 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Window.WindowCore.Jvm 1.4.0.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Window.WindowCore 1.4.0.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.AndroidX.Window 1.4.0.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.Google.Android.Material 1.12.0.5 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.Google.Code.FindBugs.JSR305 3.0.2.21 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.Google.Crypto.Tink.Android 1.18.0.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.Google.ErrorProne.Annotations 2.41.0.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.Google.Guava.ListenableFuture 1.0.0.29 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.Jetbrains.Annotations 26.0.2.3 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.JSpecify 1.0.0.4 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.Kotlin.StdLib 2.2.0.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.KotlinX.Coroutines.Android 1.10.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.KotlinX.Coroutines.Core.Jvm 1.10.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.KotlinX.Coroutines.Core 1.10.2.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.KotlinX.Serialization.Core.Jvm 1.9.0.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries
- Xamarin.KotlinX.Serialization.Core 1.9.0.1 | MIT AND Apache-2.0 | Microsoft Corporation | https://aka.ms/android-libraries

## NuGet packages on iOS and macOS

- Microsoft.DotNet.ILCompiler 10.0.14 | MIT | Microsoft Corporation | https://dot.net/

## NuGet packages on Windows

MAUI brings these along. Their terms come from Microsoft, and the Windows App SDK and WebView2 list further third-party notices in their packages.

- Microsoft.Graphics.Win2D 1.3.2 | Microsoft license terms | Microsoft Corporation | https://github.com/microsoft/Win2D
- Microsoft.IO.RecyclableMemoryStream 3.0.1 | MIT | Microsoft Corporation | https://github.com/Microsoft/Microsoft.IO.RecyclableMemoryStream
- Microsoft.Maui.Graphics.Win2D.WinUI.Desktop 10.0.0 | MIT | Microsoft Corporation | https://github.com/dotnet/maui
- Microsoft.Web.WebView2 1.0.3179.45 | Microsoft license file, BSD style | Microsoft Corporation | https://aka.ms/webview
- Microsoft.Windows.SDK.BuildTools 10.0.22621.756 | Microsoft Software License Terms | Microsoft Corporation | https://aka.ms/WinSDKLicenseURL
- Microsoft.WindowsAppSDK 1.7.250909003 | Microsoft Software License Terms | Microsoft Corporation | https://github.com/microsoft/windowsappsdk

## .NET and platform runtimes

They come with the .NET SDK instead of a package reference. The first link lists the third-party notices of the runtime itself.

- Microsoft.NETCore.App 10.0.14 | MIT | .NET Foundation and contributors | https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT
- Microsoft.Android 36.1.2 | MIT | .NET Foundation and contributors | https://github.com/dotnet/android
- Microsoft.iOS 26.5.10315 | MIT | .NET Foundation and contributors | https://github.com/dotnet/macios
- Microsoft.MacCatalyst 26.5.10315 | MIT | .NET Foundation and contributors | https://github.com/dotnet/macios

## Tools for building and testing

These are not part of the app. The native SkiaSharp packages, which only the scene renderer uses, list third-party notices that include LGPL and MPL components of Skia.

- AwesomeAssertions 9.6.0 | Apache-2.0 | Dennis Doomen 2010-2025, AwesomeAssertions 2025- | https://github.com/AwesomeAssertions/AwesomeAssertions
- Castle.Core 5.1.1 | Apache-2.0 | 2004-2022 Castle Project | http://www.castleproject.org/
- coverlet.collector 6.0.4 | MIT | Toni Solarin-Sodara | https://github.com/coverlet-coverage/coverlet
- Microsoft.ApplicationInsights 2.23.0 | MIT | Microsoft Corporation | https://go.microsoft.com/fwlink/?LinkId=392727
- Microsoft.CodeCoverage 17.14.1 | MIT | Microsoft Corporation | https://github.com/microsoft/vstest
- Microsoft.Extensions.DependencyModel 8.0.2 | MIT | Microsoft Corporation | https://dot.net/
- Microsoft.Maui.Graphics.Skia 10.0.110 | MIT | Microsoft Corporation | https://github.com/dotnet/maui
- Microsoft.Maui.Graphics 10.0.110 | MIT | Microsoft Corporation | https://github.com/dotnet/maui
- Microsoft.NET.ILLink.Tasks 10.0.14 | MIT | Microsoft Corporation | https://dot.net/
- Microsoft.NET.Test.Sdk 17.14.1 | MIT | Microsoft Corporation | https://github.com/microsoft/vstest
- Microsoft.Testing.Extensions.Telemetry 2.4.1 | MIT | Microsoft Corporation | https://github.com/microsoft/testfx
- Microsoft.Testing.Extensions.TrxReport.Abstractions 2.4.1 | MIT | Microsoft Corporation | https://github.com/microsoft/testfx
- Microsoft.Testing.Extensions.VSTestBridge 2.4.1 | MIT | Microsoft Corporation | https://github.com/microsoft/testfx
- Microsoft.Testing.Platform.MSBuild 2.4.1 | MIT | Microsoft Corporation | https://github.com/microsoft/testfx
- Microsoft.Testing.Platform 2.4.1 | MIT | Microsoft Corporation | https://github.com/microsoft/testfx
- Microsoft.TestPlatform.AdapterUtilities 18.10.1 | MIT | Microsoft Corporation | https://github.com/microsoft/vstest
- Microsoft.TestPlatform.ObjectModel 18.9.0 | MIT | Microsoft Corporation | https://github.com/microsoft/vstest
- Microsoft.TestPlatform.TestHost 17.14.1 | MIT | Microsoft Corporation | https://github.com/microsoft/vstest
- Newtonsoft.Json 13.0.3 | MIT | James Newton-King 2008 | https://www.newtonsoft.com/json
- NSubstitute 6.2.0 | BSD-3-Clause | Anthony Egerton and David Tchepak | https://nsubstitute.github.io/
- NUnit 5.0.0 | MIT | 2009-2019 Charlie Poole, 2014-2025 Rob Prouse, 2026 Terje Sandstrom and contributors | https://nunit.org/
- NUnit3TestAdapter 6.3.1 | MIT | 2011-2021 Charlie Poole, 2014-2026 Terje Sandstrom | https://docs.nunit.org/articles/vs-test-adapter/Index.html
- runtime.linux-arm64.Microsoft.DotNet.ILCompiler 10.0.14 | MIT | Microsoft Corporation | https://dot.net/
- runtime.linux-x64.Microsoft.DotNet.ILCompiler 10.0.14 | MIT | Microsoft Corporation | https://dot.net/
- runtime.osx-arm64.Microsoft.DotNet.ILCompiler 10.0.14 | MIT | Microsoft Corporation | https://dot.net/
- SkiaSharp.NativeAssets.macOS 4.150.1 | MIT | Microsoft Corporation | https://go.microsoft.com/fwlink/?linkid=868515
- SkiaSharp.NativeAssets.Win32 4.150.1 | MIT | Microsoft Corporation | https://go.microsoft.com/fwlink/?linkid=868515
- SkiaSharp 4.150.1 | MIT | Microsoft Corporation | https://go.microsoft.com/fwlink/?linkid=868515
- System.Diagnostics.EventLog 6.0.0 | MIT | Microsoft Corporation | https://dot.net/

## Reference data in tests

The test project contains rise and set times from the US Naval Observatory, moon positions from NASA/JPL Horizons, moon phases from Fred Espenak's tables, lunar eclipses from NASA's catalogue, supermoon dates from EarthSky and the official WMM2025 test values from NOAA. They are used for comparison only and are not part of the app.
