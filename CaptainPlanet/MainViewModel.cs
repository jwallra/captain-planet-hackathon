using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Plugin.Media;
using Plugin.Media.Abstractions;
using SkiaSharp;
using Xamarin.Forms;

namespace CaptainPlanet
{
    public class MainViewModel : BaseViewModel
    {
        // subscriptionKey.
        private string subscriptionKey = AppSettingsManager.Settings["CognitiveServicesApiKey"];

        private static readonly List<VisualFeatureTypes> features = new List<VisualFeatureTypes>()
        {
            VisualFeatureTypes.Categories,
            VisualFeatureTypes.Objects,
            VisualFeatureTypes.Tags
        };

        public MainViewModel()
        {
#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
            TakePhotoCommand = new Command(async () => await TakePhoto());
            PickPhotoCommand = new Command(async () => await PickPhoto());
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
        }

        async Task PredictPhoto(MediaFile photo)
        {
            ComputerVisionClient computerVision = new ComputerVisionClient(
                 new ApiKeyServiceClientCredentials(subscriptionKey),
                 new System.Net.Http.DelegatingHandler[] { });
            computerVision.Endpoint = AppSettingsManager.Settings["CognitiveServicesEndpoint"];

            // Analyse.
            var results = await computerVision.DetectObjectsInStreamAsync(photo.GetStream());
            AllPredictions = results.Objects
                                    .Where(p => p.Confidence > Probability)
                                    .ToList();
        }

        SKBitmap image;
        public SKBitmap Image
        {
            get => image;
            set => Set(ref image, value);
        }

        double probability = .75;
        public double Probability
        {
            get => probability;
            set
            {
                if (Set(ref probability, value))
                {
                    OnPropertyChanged(nameof(ProbabilityText));
                    OnPropertyChanged(nameof(Predictions));
                }
            }
        }

        public string ProbabilityText => $"{Probability:P0}";

        List<DetectedObject> allPredictions = new List<DetectedObject>();
        public List<DetectedObject> AllPredictions
        {
            get => allPredictions;
            set
            {
                Set(ref allPredictions, value);
                OnPropertyChanged(nameof(Predictions));
            }
        }

        public List<DetectedObject> Predictions => AllPredictions.Where(p => p.Confidence > Probability).ToList();

        public ICommand TakePhotoCommand { get; }
        public ICommand PickPhotoCommand { get; }
        public ICommand ShowSettingsCommand { get; }
        public ICommand ShowHelpCommand { get;  }

        Task TakePhoto()
        {
            return GetPhoto(() => CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions { PhotoSize = PhotoSize.Medium }));
        }

        Task PickPhoto()
        {
            return GetPhoto(() => CrossMedia.Current.PickPhotoAsync(new PickMediaOptions { PhotoSize = PhotoSize.Medium }));
        }

        async Task GetPhoto(Func<Task<MediaFile>> getPhotoFunc)
        {
            IsEnabled = false;

            try
            {
                var photo = await getPhotoFunc();

                Image = null;
                AllPredictions = new List<DetectedObject>();

                Image = SKBitmap.Decode(photo.GetStreamWithImageRotatedForExternalStorage());
                await PredictPhoto(photo);

                IsEnabled = true;
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"An error occured: {ex.Message}", "OK");
            }
            finally
            {
                IsEnabled = true;
            }
        }
    }
}
