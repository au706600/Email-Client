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
        private readonly EmailService _emailservice;
        private EmailData selectedEmail;
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

        public MainWindow(EmailService emailservice)
        {
            InitializeComponent();

            _emailservice = emailservice;
            EmailEntries = new ObservableCollection<EmailData>();

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
    }
}
