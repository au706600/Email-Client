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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections;
using System.Collections.ObjectModel;
using Google.Apis.Auth.OAuth2;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Asn1.BC;

namespace Email_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<EmailData> emailEntries = new ObservableCollection<EmailData>();
        private readonly EmailService _emailservice;
        private EmailData selectedEmail;
        private readonly UserCredential _credentials;
        public ObservableCollection<EmailData> EmailEntries
        {
            get { return emailEntries; }
            set { emailEntries = value; }
        }
        public EmailData SelectedEmail
        {
            get { return selectedEmail; }
            set
            {
                selectedEmail = value;
                Select_Email(this, new RoutedEventArgs());
            }
        }

        public MainWindow(EmailService emailservice, UserCredential credentials)
        {
            InitializeComponent();

            searchBox.Text = "Search in mails";
            searchBox.Foreground = Brushes.LightGray;

            _emailservice = emailservice;
            EmailEntries = new ObservableCollection<EmailData>();
            _credentials = credentials;

            DataContext = this; 

            Loaded += MainWindow_Load;
        }

        async private void MainWindow_Load(object sender, RoutedEventArgs e)
        {

            try
            {
                var getEmails = await _emailservice.GetEmails(); 
                EmailEntries.Clear();
                foreach (var email in getEmails)
                {
                    EmailEntries.Add(email);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to show emails: {ex.Message} ");
            }
           
        }

        private void searchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if(searchBox.Text == "Search in mails")
            {
                searchBox.Text = "";
                searchBox.Foreground = Brushes.Black;
            }
        }

        private void searchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrWhiteSpace(searchBox.Text))
            {
                searchBox.Text = "Search in mails";
                searchBox.Foreground = Brushes.Gray;
            }
        }

        private async Task Select_Email(object sender, RoutedEventArgs e)
        {
            try
            {
                if (selectedEmail != null)
                {
                    this.Cursor = Cursors.Wait;
                    await _emailservice.getContent(selectedEmail.Uid);
                    this.Cursor = Cursors.Arrow;
                    var viewEmailContent = new ViewEmailContent(this, selectedEmail, _emailservice);
                    viewEmailContent.Show();
                    this.Hide();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to show content: {ex.Message} ");
            }
        }

        private void DrawCircleButton_Click(object sender, RoutedEventArgs e)
        {
            Window1 sendWindow = new Window1(_credentials);
            sendWindow.Show();
            this.Hide();
        }
    }
}
