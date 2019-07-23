using System;
using System.Collections.Generic;
using System.IO;
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
            //imgBanner.Source = ImageSource.FromResource("XamarinComputerVision.images.banner.png");
            //imgChoosed.Source = ImageSource.FromResource("XamarinComputerVision.images.thumbnail.jpg");
            InitializeComponent();
        }

        private async void btnTake_Clicked(object sender, EventArgs e)
        {
            await CrossMedia.Current.Initialize();
            try
            {
                if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsTakePhotoSupported)
                {
                    await DisplayAlert("No Camera", ":( No camera available.", "OK");
                    return;
                }
                var file = await CrossMedia.Current.TakePhotoAsync(new Plugin.Media.Abstractions.StoreCameraMediaOptions
                {
                    Directory = "Sample",
                    Name = "xamarin.jpg"
                });
                if (file == null) return;
                imgChoosed.Source = ImageSource.FromStream(() => {
                    var stream = file.GetStream();
                    return stream;
                });
                var result = await GetImageDescription(file.GetStream());
                file.Dispose();
                lblResult.Text = null;
                //lblResult.Text = result.Description.Captions.First().Text;    
                foreach (string tag in result.Description.Tags)
                {
                    lblResult.Text = lblResult.Text + "\n" + tag;
                }
            }
            catch (Exception ex)
            {
                string test = ex.Message;
            }
        }

        private async void btnPick_Clicked(object sender, EventArgs e)
        {
            await CrossMedia.Current.Initialize();
            try
            {
                var file = await CrossMedia.Current.PickPhotoAsync(new Plugin.Media.Abstractions.PickMediaOptions
                {
                    PhotoSize = Plugin.Media.Abstractions.PhotoSize.Medium
                });
                if (file == null) return;
                imgChoosed.Source = ImageSource.FromStream(() => {
                    var stream = file.GetStream();
                    return stream;
                });
                var result = await GetImageDescription(file.GetStream());
                lblResult.Text = null;
                file.Dispose();
                foreach (string tag in result.Description.Tags)
                {
                    lblResult.Text = lblResult.Text + "\n" + tag;
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
