using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Xamarin.Forms;

namespace CaptainPlanet
{
    public partial class MainPage : ContentPage
    {
        // subscriptionKey.
        private string subscriptionKey = AppSettingsManager.Settings["CognitiveServicesApiKey"];

        private static readonly List<VisualFeatureTypes> features = new List<VisualFeatureTypes>()
        {
            VisualFeatureTypes.Categories,
            VisualFeatureTypes.Objects,
            VisualFeatureTypes.Tags
        };

        public MainPage()
        {
            InitializeComponent();
        }

        private async void TakeAPicture(object sender, EventArgs e)
        {
            await CrossMedia.Current.Initialize();
            if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsTakePhotoSupported)
            {
                await DisplayAlert("No Camera", "No camera available.", "OK");
                HidePicture();
                return;
            }
            var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
            {
                Directory = "Sample",
                Name = "xamarin.jpg"
            });
            await AnalyseFile(file);
        }

        private async void PickAPicture(object sender, EventArgs e)
        {
            await CrossMedia.Current.Initialize();

            var file = await CrossMedia.Current.PickPhotoAsync(new PickMediaOptions
            {
                PhotoSize = PhotoSize.Medium
            });
            await AnalyseFile(file);
        }

        private async Task AnalyseFile(MediaFile file)
        {
            const string analysisFailedMessage = "Picture analysis failed.";
            if (file == null) { 
                HidePicture();
                return;
            }

            try
            {
                chosenPicture.Source = ImageSource.FromStream(() =>
                {
                    var stream = file.GetStream();
                    return stream;
                });
                var result = await GetImageDescription(file.GetStream());
                ShowPicture();
                IdentifyObjects(result.Objects);
                file.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{analysisFailedMessage}: {ex.Message}");
            }
        }

        private async Task<ImageAnalysis> GetImageDescription(Stream imageStream)
        {
            ComputerVisionClient computerVision = new ComputerVisionClient(
                new ApiKeyServiceClientCredentials(subscriptionKey),
                new System.Net.Http.DelegatingHandler[] { });
            computerVision.Endpoint = AppSettingsManager.Settings["CognitiveServicesEndpoint"];

            // Analyse.
            return await computerVision.AnalyzeImageInStreamAsync(imageStream, features, null);
        }

        private void IdentifyObjects(IList<DetectedObject> detectedObjects)
        {
            foreach (DetectedObject o in detectedObjects)
            {
                // Scale rectangle to rendered image
                var xScaleFactor = chosenPicture.Width / o.Rectangle.W;
                var yScaleFactor = chosenPicture.Height / o.Rectangle.H;
                var rectangle = new Rectangle(
                    chosenPicture.X + (o.Rectangle.W * xScaleFactor),
                    chosenPicture.Y + (o.Rectangle.H * yScaleFactor),
                    chosenPicture.Width * yScaleFactor,
                    chosenPicture.Height * xScaleFactor);

                // Draw this somehow
            }
        }

        private void ShowPicture(){
            noPictureText.IsVisible = false;
            chosenPicture.IsVisible = true;
        }

        private void HidePicture(){
            chosenPicture.IsVisible = false;
            noPictureText.IsVisible = true;
        }
    }
}
