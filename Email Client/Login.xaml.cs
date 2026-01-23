using Google.Apis.Auth.OAuth2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Util.Store;
using MailKit.Security;
using MailKit.Net.Imap;
using System.IO;
using Path = System.IO.Path;
using Google.Apis.Util;
using Microsoft.Extensions.Configuration;

namespace Email_Client
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        // https://github.com/jstedfast/MailKit/blob/master/GMailOAuth2.md
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            //const string GmailAccount = "";

            // Builds a specified user secrets configuration source
            var config = new ConfigurationBuilder().AddUserSecrets<Login>().Build();


            // Retrieve ClientId and ClientSecret for OAuth2 authentication. 
            var clientSecrets = new ClientSecrets
            {
                ClientId = config["Google:ClientId"],
                ClientSecret = config["Google:ClientSecret"]
            };

            // Concatenate path string of %APPDATA% with EmailClient and GoogleOAuth, so we get one single path string
            var credPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EmailClient", "GoogleOAuth");

            // Thread-safe OAuth 2.0 authorization code flow that manages and persists end-user credentials through the following:
            // 1) a FileDataStore object that ensures credentials persists in a file
            // 2) a google mail link for registering the application in
            // 3) Configuration file containing the secrets
            var codeFlow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                //DataStore = new FileDataStore("CredentialCacheFolder", false), 
                DataStore = new FileDataStore(credPath, false),
                Scopes = new[] {"https://mail.google.com/"},
                ClientSecrets = clientSecrets
            });

            // The OAuth2 logic is mainly on the following two lines:
            // 1) A LocalServerCodeReceiver instance that is a verication code receiver that runs on a local server on a free port and waits
            // for a call. 

            // 2) When user authenticates, the flow is done through taking a codeFlow parameter on a local server which is the codeReceiver
            var codeReceiver = new LocalServerCodeReceiver();
            var authCode = new AuthorizationCodeInstalledApp(codeFlow, codeReceiver);

            // The data that is returned in authCode variable is then used for authentication which is done asynchronously. 
            var credentials = await authCode.AuthorizeAsync("user", CancellationToken.None);


            if(credentials.Token.IsStale)
            {
                await credentials.RefreshTokenAsync(CancellationToken.None);
            }

            var authorizationOAuth = new SaslMechanismOAuth2(credentials.UserId, credentials.Token.AccessToken);

            Console.WriteLine("UserId" + credentials.UserId);
            Console.WriteLine("AccessToken" + credentials.Token.AccessToken); 
            Console.WriteLine("RefreshToken" + credentials.Token.RefreshToken);
            Console.WriteLine("Token Issued" + credentials.Token.IssuedUtc);
            Console.WriteLine("Token expires in" + credentials.Token.ExpiresInSeconds);


            using (var client = new ImapClient())
            {
                await client.ConnectAsync("imap.gmail.com", 993, SecureSocketOptions.SslOnConnect);
                try
                {
                    await client.AuthenticateAsync(authorizationOAuth);
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
                await client.DisconnectAsync(true);
            }

            // Navigate to sendWindow window
            if (authorizationOAuth != null)
            {
                var sendWindow = new Window1(credentials);
                sendWindow.Show();
                this.Close();
            }

        }
    }
}
