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
using Microsoft.Win32;
using System.IO;

namespace Email_Client
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        //private Login? loginWindow;
        
        // Pass credentials verification data as parameter to sending window
        private readonly UserCredential _credentials;
        private MainWindow mainwindow1;
        private readonly string? _InReplyTo;
        private readonly List<String>? _References;
        private List<String> _files = new List<String>();
        
        public Window1(UserCredential credentials, MainWindow mainwindow, string? InReplyTo = null, List<String>? References  = null)
        {
            InitializeComponent();
            _credentials = credentials;
            _InReplyTo = InReplyTo;
            _References = References;

            // Set placeholder text
            txtTopic.Text = "Write topic here";
            txtTopic.Foreground = Brushes.LightGray;
            txtText.Text = "Write text here";
            txtText.Foreground = Brushes.LightGray;
            mainwindow1 = mainwindow;
        }

        private void Window1_loaded(object sender, RoutedEventArgs e)
        {
            
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

    
        async private void Button_Click(object sender, RoutedEventArgs e)
        {
            // service initialization with credentials which uses the credentials object that was passed to the sending window constructor
            var service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = _credentials,
                ApplicationName = "Email Client"
            });

            // Accesstoken retrieval for user email
            var user = await service.Users.GetProfile("me").ExecuteAsync();

            // User email address
            string emailUser = user.EmailAddress;

            
            if(_credentials.Token.IsStale)
            {
                await _credentials.RefreshTokenAsync(CancellationToken.None);
            }

            // OAuth2 authentication mechanism for MailKit and for sending email
            var authorizationOAuth = new SaslMechanismOAuthBearer(emailUser, _credentials.Token.AccessToken);

            // Create email message instance for email operation, in this case sending email through SMTP
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(emailUser));

            if (string.IsNullOrEmpty(txtTo.Text))
            {
                MessageBox.Show("Email recipient is required.");
                return;
            }

            // Check that email is in valid format which is done through the IsValidEmail method (Implementation is shown below)
            if (!IsValidEmail(txtTo.Text))
            {
                return;
            }

            // Read recipient email address from user
            message.To.Add(new MailboxAddress("",txtTo.Text.ToString()));

            // If email topic is empty, set default topic as "(Intet Emne)"
            if (String.IsNullOrEmpty(txtTopic.Text) || txtTopic.Text == "Write topic here")
            {
                txtTopic.Text = "(Intet Emne)";
            }

            // Read email subject from user 
            message.Subject = txtTopic.Text;

            // If email body is empty, set default body as empty string, meaning no content
            if (String.IsNullOrEmpty(txtText.Text) || txtText.Text == "Write text here")
            {
                txtText.Text = "";
            }

            // When replying, the messages we reply to should continue on same thread
            if(!String.IsNullOrEmpty(_InReplyTo))
            {
                message.InReplyTo = _InReplyTo;
            }

            
             if (_References != null && !string.IsNullOrEmpty(message.MessageId))
                {
                    _References.Add(message.MessageId);
                }
            

            // Attachments
            var builder = new BodyBuilder();

            // Read email body from user
            builder.TextBody = txtText.Text;

            // Check if we have any attachments
            if (listFiles.Items.Count != 0)
            {
                foreach(var files in _files)
                {
                    builder.Attachments.Add(files);
                }
            }

            message.Body = builder.ToMessageBody();

            /*
             |Protocol|Standard Port|SSL Port|
                |:------:|:-----------:|:------:|
                | SMTP   | 25 or 587   | 465    |
                | POP3   | 110         | 995    |
                | IMAP   | 143         | 993    |

            */

            // SmtpClient allows us for sending email through SMTP protocol so in this case, we create an instance of it, whereby
            // we connect to Gmail's SMTP server, authenticate through OAuth2 and send the email message object asynchronously
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

        // This method checks that the email address entered by user is in valid format. The valid format is defined in RFC which is implicitly 
        // internally implemented inside the TryParse method
        private static bool IsValidEmail(string email)
        {
            if (!MailboxAddress.TryParse(email, out var address))
            {
                MessageBox.Show("Invalid email format. Please enter a valid email address.");
                return false;
            }

            if (!address.Address.Contains('@'))
            {
                MessageBox.Show("Invalid email format. Please enter a valid email address.");
                return false;
            }

            return true;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            mainwindow1.Show();
            this.Hide();
        }

        private void Upload_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog uploadFile = new OpenFileDialog();
            uploadFile.Filter = "Text files (*.pdf)|*.pdf";
            uploadFile.Multiselect = true;
            if (uploadFile.ShowDialog() == true)
            {
                foreach(var files in uploadFile.FileNames)
                {
                    _files.Add(files);
                    listFiles.Items.Add(Path.GetFileName(files));
                }
                listFiles.Visibility = Visibility.Visible;
            }
        }
    }
}
