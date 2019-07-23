using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Configuration;
using Plugin.Media;
using Xamarin.Forms;

namespace CaptainPlanet
{
    public partial class MainPage : ContentPage
    {
        public IConfiguration Configuration = null;

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

        private async void takeAPicture(object sender, EventArgs e)
        {
            await CrossMedia.Current.Initialize();
            try
            {
                if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsTakePhotoSupported)
                {
                    await DisplayAlert("No Camera", "No camera available.", "OK");
                    return;
                }
                var file = await CrossMedia.Current.TakePhotoAsync(new Plugin.Media.Abstractions.StoreCameraMediaOptions
                {
                    Directory = "Sample",
                    Name = "xamarin.jpg"
                });
                if (file == null)
                {
                    chosenPicture.IsVisible = false;
                    noPictureText.IsVisible = true;
                    return;
                }
                noPictureText.IsVisible = false;
                chosenPicture.IsVisible = true;
                chosenPicture.Source = ImageSource.FromStream(() => {
                    var stream = file.GetStream();
                    return stream;
                });
                var result = await GetImageDescription(file.GetStream());
                file.Dispose();
                analysisResultText.Text = null;
                analysisResultText.Text = result.Description.Captions.First().Text;    
                foreach (string tag in result.Description.Tags)
                {
                    analysisResultText.Text = analysisResultText.Text + "\n" + tag;
                }
            }
            catch (Exception ex)
            {
                string test = ex.Message;
            }
        }

        private async void pickAPicture(object sender, EventArgs e)
        {
            await CrossMedia.Current.Initialize();
            try
            {
                var file = await CrossMedia.Current.PickPhotoAsync(new Plugin.Media.Abstractions.PickMediaOptions
                {
                    PhotoSize = Plugin.Media.Abstractions.PhotoSize.Medium
                });
                if (file == null)
                {
                    noPictureText.IsVisible = true;
                    chosenPicture.IsVisible = false;
                    return;
                }
                noPictureText.IsVisible = false;
                chosenPicture.IsVisible = true;
                chosenPicture.Source = ImageSource.FromStream(() => {
                    var stream = file.GetStream();
                    return stream;
                });
                var result = await GetImageDescription(file.GetStream());
                analysisResultText.Text = null;
                file.Dispose();
                foreach (string tag in result.Description.Tags)
                {
                    analysisResultText.Text = analysisResultText.Text + "\n" + tag;
                }
            }
            catch (Exception ex)
            {
                string test = ex.Message;
            }
        }

        public async Task<ImageAnalysis> GetImageDescription(Stream imageStream)
        {
            ComputerVisionClient computerVision = new ComputerVisionClient(
                new ApiKeyServiceClientCredentials(App.CognitiveServicesApiKey),
                new System.Net.Http.DelegatingHandler[] { });
            computerVision.Endpoint = "<EndPoint>";

            // Specify the features to return
            return await computerVision.AnalyzeImageInStreamAsync(imageStream, features, null);
        }
    }
}
