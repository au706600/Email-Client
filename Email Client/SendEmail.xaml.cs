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
using MimeKit;
using MailKit.Net.Smtp;
using Path = System.IO.Path;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Util.Store;
using Google.Apis.Auth.OAuth2;
using System.Threading;
using Google.Apis.Util;
using MailKit.Security;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;

namespace Email_Client
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        //private Login? loginWindow;
        private readonly UserCredential _credentials;
        public Window1(UserCredential credentials)
        {
            InitializeComponent();
            _credentials = credentials;
        }

        private void Window1_loaded(object sender, RoutedEventArgs e)
        {
            txtTopic.Text = "Write topic here";
            txtTopic.Foreground = Brushes.LightGray;
            txtText.Text = "Write text here";
            txtText.Foreground = Brushes.LightGray;
        }

        private void txtTopic_GotFocus(object sender, RoutedEventArgs e)
        {
            if(txtTopic.Text == "Write topic here")
            {
                txtTopic.Text = "";
                txtTopic.Foreground = Brushes.Black;
            }
        }

        private void txtTopic_LostFocus(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrWhiteSpace(txtTopic.Text))
            {
                txtTopic.Text = "Write topic here";
                txtTopic.Foreground = Brushes.LightGray;
            }
        }

        private void txtText_GotFocus(object sender, RoutedEventArgs e)
        {
            if(txtText.Text == "Write text here")
            {
                txtText.Text = "";
                txtText.Foreground = Brushes.Black;
            }
        }

        private void txtText_LostFocus(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrWhiteSpace(txtText.Text))
            {
                txtText.Text = "Write text here";
                txtText.Foreground = Brushes.LightGray;
            }
        }

        // https://stackoverflow.com/questions/62612997/how-to-get-email-after-using-google-oauth2-in-c
        async private void Button_Click(object sender, RoutedEventArgs e)
        {

            var service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = _credentials,
                ApplicationName = "Email Client"
            });

            var user = await service.Users.GetProfile("me").ExecuteAsync();

            string emailUser = user.EmailAddress;

            if(_credentials.Token.IsStale)
            {
                await _credentials.RefreshTokenAsync(CancellationToken.None);
            }

            var authorizationOAuth = new SaslMechanismOAuthBearer(emailUser, _credentials.Token.AccessToken);

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(emailUser));
            message.To.Add(MailboxAddress.Parse(txtTo.Text));
            message.Subject = txtTopic.Text;
            message.Body = new TextPart("plain")
            {
                Text = txtText.Text
            };

            /*
             |Protocol|Standard Port|SSL Port|
                |:------:|:-----------:|:------:|
                | SMTP   | 25 or 587   | 465    |
                | POP3   | 110         | 995    |
                | IMAP   | 143         | 993    |

            */

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                //client.AuthenticationMechanisms.Remove("XOAUTH2");
                await client.AuthenticateAsync(authorizationOAuth);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }

            MessageBox.Show("Email Sent Successfully :)");
        }
    }
}
