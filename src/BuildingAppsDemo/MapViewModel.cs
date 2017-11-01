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
        private DelegateCommand _generateMapAreaCommand;
        private DelegateCommand<GeoViewInputEventArgs> _showPopupCommand;
        private string appDataFolder = "";
        private string itemId = "e260b46657a0416788eece91a85363e1";

        public MapViewModel()
        {
            _generateMapAreaCommand = new DelegateCommand(GenerateMapArea);
            _showPopupCommand = new DelegateCommand<GeoViewInputEventArgs>(ShowPopup);
            appDataFolder =
                Path.Combine(
                    Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path,
                    itemId);
            Debug.WriteLine(appDataFolder);
            Initialize();
        }

        public ICommand GenerateMapAreaCommand => _generateMapAreaCommand;
        public ICommand ShowPopupCommand => _showPopupCommand;

        private async void Initialize()
        {
            try
            {
                IsBusyText = "Loading map...";
                IsBusy = true;

                if (Directory.Exists(appDataFolder))
                {
                    var mobileMap = await MobileMapPackage.OpenAsync(appDataFolder);
                    Map = mobileMap.Maps.First();
                    InOnlineMode = false;
                }
                else
                {
                    InOnlineMode = true;
                    var portal = await ArcGISPortal.CreateAsync();
                    var item = await PortalItem.CreateAsync(portal, itemId);
                    var map = new Map(item);
                    Map = map;
                }
                IsBusy = false;
            }
            catch (Exception ex)
            {
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
                    await feature.LoadAsync(); // Load feature to make sure to get all attributes
                    layer.SelectFeature(feature); // Highlight the feature in the layer
                    var popup = result.Popups.First(); // Get popup that is defined in the webmap
                    var calloutDefinition = new CalloutDefinition(popup.Title); // Construct callout

                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets/Forward_96px.png");
                    var buttonImage = new RuntimeImage(new Uri(imagePath));
                    await buttonImage.LoadAsync();
                    calloutDefinition.ButtonImage = buttonImage;
                    calloutDefinition.OnButtonClick = p =>
                    {
                        SelectedFeature = feature;
                    };

                    MapViewService.ShowCalloutForGeoElement(feature, args.Position, calloutDefinition);
                }
                {
                    MapViewService.DismissCallout();
                }
            }
            catch (Exception)
            {

                throw;
            }
                   
        }


        private ArcGISFeature _selectedFeature;
        public ArcGISFeature SelectedFeature
        {
            get { return _selectedFeature; }
            set { SetProperty(ref _selectedFeature, value); }
        }

        private async void GenerateMapArea()
        {
            try
            {
                IsBusy = true;
                IsBusyText = "Generating an offline map...";

                // If temporary data folder doesn't exists, create it
                if (!Directory.Exists(appDataFolder))
                    Directory.CreateDirectory(appDataFolder);

                var areaOfInterest = 
                    MapViewService.GetCurrentViewpoint(ViewpointType.BoundingGeometry)
                        .TargetGeometry as Envelope;

                var task = await OfflineMapTask.CreateAsync(Map);
                var parameters = await task.CreateDefaultGenerateOfflineMapParametersAsync(
                   areaOfInterest);
                parameters.MaxScale = 2250;
                parameters.MinScale = 0;

                var job = task.GenerateOfflineMap(parameters, appDataFolder);
                job.ProgressChanged += ProgressChanged;

                var results = await job.GetResultAsync();
                Map = results.OfflineMap;

                InOnlineMode = false;
            }
            catch (Exception ex)
            {
                throw;
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

        private bool _inOnlineMode = true;
        public bool InOnlineMode
        {
            get { return _inOnlineMode; }
            set { SetProperty(ref _inOnlineMode, value); }
        }
    }
}
