using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Tasks.Offline;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using Prism.Commands;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace BuildingAppsDemo
{
    public class MapViewModel : BaseViewModel
    {
        private string _appDataFolder = "";
        private string _itemId = "e260b46657a0416788eece91a85363e1";
        private DelegateCommand _generateMapAreaCommand;
        private DelegateCommand<GeoViewInputEventArgs> _showPopupCommand;

        public MapViewModel()
        {
            // Create commands
            _generateMapAreaCommand = new DelegateCommand(GenerateMapArea);
            _showPopupCommand = new DelegateCommand<GeoViewInputEventArgs>(ShowPopup);
            // Set location where the offline map is created
            _appDataFolder =
                Path.Combine(
                    Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path,
                    _itemId);
            Debug.WriteLine(_appDataFolder);
            // Run initialization logic
            Initialize();
        }

        public ICommand GenerateMapAreaCommand => _generateMapAreaCommand;
        public ICommand ShowPopupCommand => _showPopupCommand;

        private ArcGISFeature _selectedFeature;
        public ArcGISFeature SelectedFeature
        {
            get { return _selectedFeature; }
            set { SetProperty(ref _selectedFeature, value); }
        }

        private bool _inOnlineMode = true;
        public bool InOnlineMode
        {
            get { return _inOnlineMode; }
            set { SetProperty(ref _inOnlineMode, value); }
        }

        private async void Initialize()
        {
            try
            {
                IsBusyText = "Loading map...";
                IsBusy = true;

                // If we already have offline map downloaded, open it instead of an online mode
                if (Directory.Exists(_appDataFolder))
                {
                    var mobileMap = await MobileMapPackage.OpenAsync(_appDataFolder);
                    Map = mobileMap.Maps.First();
                    InOnlineMode = false;
                }
                // If we didn't have offline map, load map from ArcGIS Online based on the item id
                else
                {
                    InOnlineMode = true;
                    var portal = await ArcGISPortal.CreateAsync();
                    var item = await PortalItem.CreateAsync(portal, _itemId);
                    var map = new Map(item);
                    Map = map;
                }
                IsBusy = false;
            }
            catch (Exception ex)
            {
                // Production level error handling...
                Debug.WriteLine(ex.ToString());
                throw;
            }
        }

        private async void ShowPopup(GeoViewInputEventArgs args)
        {
            try
            {
                // Clear all selected features
                foreach (var featureLayer in Map.OperationalLayers.OfType<FeatureLayer>())
                    featureLayer.ClearSelection();

                // Identify all features
                var results = await MapViewService.IdentifyLayersAsync(args.Position, 2, false, 1);
                if (results.Any())
                {
                    // Get first result and get feature and layer from it
                    var result = results.First();
                    var feature = result.GeoElements.First() as ArcGISFeature;
                    var layer = result.LayerContent as FeatureLayer;
                    // Load feature to make sure to get all attributes
                    await feature.LoadAsync();
                    // Highlight the feature in the layer
                    layer.SelectFeature(feature);
                    // Get popup that is defined in the webmap. This is a definition that is configured
                    // as part of the WebMap definition which defines what should be shown in the callout
                    var popup = result.Popups.First(); 

                    // Construct callout that defines what is being shown in the callout
                    var calloutDefinition = new CalloutDefinition(popup.Title); 
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets/Forward_96px.png");
                    var buttonImage = new RuntimeImage(new Uri(imagePath));
                    await buttonImage.LoadAsync();
                    calloutDefinition.ButtonImage = buttonImage;
                    // When clicked, set the feature that is clicked to the selected feature
                    calloutDefinition.OnButtonClick = p =>
                    {
                        SelectedFeature = feature;
                    };
                    
                    // After everything is set, show the callout
                    MapViewService.ShowCalloutForGeoElement(feature, args.Position, calloutDefinition);
                }
                else
                {
                    // If no feature was found from the location, hide the existing callout
                    MapViewService.DismissCallout();
                }
            }
            catch (Exception ex)
            {
                // Production level error handling...
                Debug.WriteLine(ex.ToString());
                throw;
            }       
        }      

        private async void GenerateMapArea()
        {
            try
            {
                IsBusy = true;
                IsBusyText = "Generating an offline map...";

                // If temporary data folder doesn't exists, create it
                if (!Directory.Exists(_appDataFolder))
                    Directory.CreateDirectory(_appDataFolder);

                // Get the current viewpoint from the MapView and convert that to the area of interest
                var areaOfInterest = 
                    MapViewService.GetCurrentViewpoint(ViewpointType.BoundingGeometry)
                        .TargetGeometry as Envelope;

                // Create task and set parameters that defines what is taken oflfine
                var task = await OfflineMapTask.CreateAsync(Map);
                var parameters = await task.CreateDefaultGenerateOfflineMapParametersAsync(
                   areaOfInterest);
                parameters.MaxScale = 2250;
                parameters.MinScale = 0;

                // Job is used to generate offline map asyncronously. Make srue that we have hooked
                // to the progress changed events to report changes back to the UI
                var job = task.GenerateOfflineMap(parameters, _appDataFolder);
                job.ProgressChanged += ProgressChanged;

                // Calling GetResultAsync starts the job and waits until the offline map has been created
                var results = await job.GetResultAsync();

                // Change the app from online mode to offline mode
                Map = results.OfflineMap;
                InOnlineMode = false;
            }
            catch (Exception ex)
            {
                // Production level error handling...
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                IsBusy = false;
                IsBusyText = string.Empty;
            }
        }

        private async void ProgressChanged(object sender, EventArgs e)
        {
            try
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                    var generateOfflineMapJob = sender as GenerateOfflineMapJob;
                    ProgressPercentage = generateOfflineMapJob.Progress.ToString() + "%";
                });
            }
            catch (Exception) { }
        }
    }
}
