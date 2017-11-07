# DSE2017-building-apps-ArcGISRuntime-dotnet
A simple example app how to use ArcGIS Runtime for .NET to build UWP application. This demo was used in Developer Summit Europe 2017 to demonstrate the basic usage of ArcGIS Runtime SDK for .NET, Toolkit control usage and simplified MVVM.

<img src="GeneralUI.jpg" width="350"/>

## Features
- Opening and loading map from ArcGIS Online (ArcGISPortal, PortalItem, Map)
- Generating an offline map from selected area (OfflineMapTask)
- Opening and loading offline map (MobileMapPackage)
- Showing a callout based on the popup configuration (CalloutDefinition, Popup)
- Using Toolkit controls (Legend, Compass)
- Listing attributes from the selected feature as a list
- Navigation contols (zoom in, zoom out, navigate to initial viewpoint)

## Requirements

- [ArcGIS Runtime for .NET SDK](https://developers.arcgis.com/net/) Version 100.1
- [ArcGIS Runtime Toolkit for UWP](https://github.com/Esri/arcgis-toolkit-dotnet)
  - Note: current build used from https://ci.appveyor.com/nuget/arcgis-toolkit-dotnet.
- [ArcGIS Online user](https://developers.arcgis.com/sign-up)




