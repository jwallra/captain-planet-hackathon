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
        public static string[] CompostableWords = {
            "food", "meat", "bacon", "beef", "chicken", "cooked meat", "duck", "ham", "kidneys",
            "lamb", "liver", "minced beef", "paté", "salami", "sausages", "pork", "pork pie", "sausage roll", "turkey", "veal",
            "fruit", "apple", "apricot", "banana", "blackberry", "blackcurrant", "blueberry", "cherry", "coconut", "fig", "gooseberry",
            "grape", "grapefruit", "kiwi fruit", "lemon", "lime", "mango", "melon", "orange", "peach", "pear", "pineapple", "plum",
            "pomegranate", "raspberry", "redcurrant", "rhubarb", "strawberry", "bunch of bananas", "bunch of grapes", "fish", "anchovy",
            "cod", "haddock", "herring", "kipper", "mackerel", "pilchard", "plaice", "salmon", "sardine", "smoked salmon", "sole",
            "trout", "tuna", "vegetable", "artichoke", "asparagus", "aubergine", "avocado", "beansprouts", "beetroot", "broad beans",
            "broccoli", "Brussels sprouts", "cabbage", "carrot", "cauliflower", "celery", "chilli", "chili", "courgette", "cucumber", "French beans",
            "garlic", "ginger", "leek", "lettuce", "mushroom", "onion", "peas", "pepper", "potato", "pumpkin", "radish", "rocket",
            "runner bean", "swede", "sweet potato", "sweetcorn", "tomato", "turnip", "spinach", "spring onion", "squash", "clove of garlic",
            "stick of celery", "baked beans", "corned beef", "kidney beans", "soup", "tinned tomatoes", "chips", "fish fingers",
            "frozen peas", "frozen pizza", "ice cream", "cooking oil", "olive oil", "stock cubes", "tomato puree", "dairy", "butter",
            "cream", "cheese", "blue cheese", "cottage cheese", "goats cheese", "creme fraiche", "eggs", "free range eggs", "margarine",
            "milk", "full-fat milk", "semi-skimmed milk", "skimmed milk", "sour cream", "yoghurt", "yogurt", "bread", "cake", "baguette",
            "bread rolls", "brown bread", "white bread", "garlic bread", "pitta bread", "loaf or loaf of bread", "sliced loaf",
            "danish pastry", "quiche", "sponge cake", "baking powder", "plain flour", "self-raising flour", "cornflour", "sugar",
            "brown sugar", "icing sugar", "pastry", "yeast", "dried apricots", "prunes", "dates", "raisins", "sultanas", "breakfast cereal",
            "cornflakes", "honey", "jam", "marmalade", "muesli", "porridge", "toast", "noodles", "pasta", "pasta sauce", "pizza", "rice",
            "spaghetti", "pepper", "biscuits", "chocolate", "crisps", "hummus", "olives", "peanuts", "sweets", "walnuts", "basil", "chives",
            "coriander", "dill", "parsley", "rosemary", "sage", "thyme", "chilli powder", "cinnamon", "cumin", "curry powder", "nutmeg",
            "paprika", "saffron", "organic", "ready meal", "bag of potatoes", "bar of chocolate", "carton of milk", "box of eggs",
            "tree", "flower", "herb", "bush", "plant", "soil", "dirt", "evergreen", "pine", "palm tree", "dandelion", "hedge", "leaf",
            "grass", "cactus", "orchard apple tree", "animal", "sea urchin", "seaweed"
        };

        // subscriptionKey.
        private string subscriptionKey = AppSettingsManager.Settings["CognitiveServicesApiKey"];

        private static readonly List<VisualFeatureTypes> features = new List<VisualFeatureTypes>()
        {
            VisualFeatureTypes.Categories,
            VisualFeatureTypes.Objects
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
            var objectResults = await computerVision.AnalyzeImageInStreamAsync(photo.GetStream(), features);
            AllPredictions = objectResults.Objects
                .Where(p => p.Confidence > Probability)
                .ToList();
            AllCategories = objectResults.Categories.ToList();
        }

        SKBitmap image;
        public SKBitmap Image
        {
            get => image;
            set => Set(ref image, value);
        }

        double probability = .50;
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
        List<Category> allCategories = new List<Category>();

        public List<DetectedObject> AllPredictions
        {
            get => allPredictions;
            set
            {
                Set(ref allPredictions, value);
                OnPropertyChanged(nameof(Predictions));
            }
        }

        public List<Category> AllCategories
        {
            get => AllCategories;
            set
            {
                Set(ref allCategories, value);
                OnPropertyChanged(nameof(Category));
            }
        }

        public List<DetectedObject> Predictions => AllPredictions.Where(p => p.Confidence > Probability).ToList();
        public List<Category> Categories => AllCategories.ToList();

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
