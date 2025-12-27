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

            var config = new ConfigurationBuilder().AddUserSecrets<Login>().Build();

            var clientSecrets = new ClientSecrets
            {
                ClientId = config["Google:ClientId"],
                ClientSecret = config["Google:ClientSecret"]
            };


            var credPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EmailClient", "GoogleOAuth");

            var codeFlow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                //DataStore = new FileDataStore("CredentialCacheFolder", false), 
                DataStore = new FileDataStore(credPath, false),
                Scopes = new[] {"https://mail.google.com/"},
                ClientSecrets = clientSecrets
            });

            var codeReceiver = new LocalServerCodeReceiver();
            var authCode = new AuthorizationCodeInstalledApp(codeFlow, codeReceiver);

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

            if (authorizationOAuth != null)
            {
                var sendWindow = new Window1(credentials);
                sendWindow.Show();
                this.Close();
            }

        }
    }
}
