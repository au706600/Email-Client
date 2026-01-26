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

namespace Email_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<EmailData> emailEntries = new ObservableCollection<EmailData>();
        private readonly UserCredential _credentials;
        private readonly EmailService _emailservice;
        public ObservableCollection<EmailData> EmailEntries
        {
            get { return emailEntries; }
            set { emailEntries = value; }
        }

        public MainWindow(UserCredential credentials, EmailService emailservice)
        {
            InitializeComponent();

            _emailservice = emailservice;
            _credentials = credentials;
            EmailEntries = new ObservableCollection<EmailData>();
            DataContext = this;

            Loaded += MainWindow_Load;
            /*
            EmailEntries.Add(new EmailData(
                1,
                "James",
                "Testing",
                DateTime.Now
            ));

            EmailEntries.Add(new EmailData(
                2,
                "Thor",
                "Developing",
                DateTime.Now
                ));
            */
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
    }
}
