using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Rest;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace CaptainPlanet
{
    public partial class App : Application
    {
        public static string CognitiveServicesApiKey;

        private static string GetKeyVaultEndpoint() => "https://captain-planet-kv.vault.azure.net";

        public App()
        {
            InitializeComponent();

            //var keyVaultEndpoint = GetKeyVaultEndpoint();
            //if (!string.IsNullOrEmpty(keyVaultEndpoint))
            //{
            //    var azureServiceTokenProvider = new AzureServiceTokenProvider();
            //    string accessToken = azureServiceTokenProvider.GetAccessTokenAsync("https://vault.azure.net").Result;
            //    // var keyVaultClient = new KeyVaultClient((authority, resource, scope) => azureServiceTokenProvider.KeyVaultTokenCallback(authority, resource, scope));
            //    var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
            //    CognitiveServicesApiKey = keyVaultClient.GetSecretAsync(GetKeyVaultEndpoint() + "/secrets/captain-planet-cs-key1").Result.Value;
            //}

            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
