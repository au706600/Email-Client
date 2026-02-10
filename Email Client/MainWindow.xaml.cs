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
using System.ComponentModel;
using System.Printing;

namespace Email_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<EmailData> emailEntries = new ObservableCollection<EmailData>();
        private readonly EmailService _emailservice;
        private EmailData selectedEmail;
        private readonly UserCredential _credentials;
        private ICollectionView emailCollectionView { get;  }
        private string _emailFilterView = string.Empty;
        private bool isPlaceHolder = true;
        public ObservableCollection<EmailData> EmailEntries
        {
            get { return emailEntries; }
            set { emailEntries = value; }
        }

        public string CollectionEmails
        {
            get
            {
                return _emailFilterView;
            }

            set
            {
                _emailFilterView = value;
                OnPropertyChanged(nameof(CollectionEmails));
                emailCollectionView?.Refresh();
            }
        }
        
        public EmailData SelectedEmail
        {
            get { return selectedEmail; }
            set
            {
                selectedEmail = value;
                Select_EmailAsync(this, new RoutedEventArgs());
            }
        }
        private async void Select_EmailAsync(object sender, RoutedEventArgs e)
        {
            await Select_Email(sender, e);
        }

        public MainWindow(EmailService emailservice, UserCredential credentials)
        {
            InitializeComponent();

            _emailservice = emailservice;
            EmailEntries = new ObservableCollection<EmailData>();
            _credentials = credentials;

            emailCollectionView = CollectionViewSource.GetDefaultView(EmailEntries);
            emailCollectionView.Filter = EmailFilter;
            
            DataContext = this;

            searchBox.Text = "Search in mails";
            searchBox.Foreground = Brushes.LightGray;

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
                MessageBox.Show($"Failed to show emails: \n{ex} ");
            }
           
        }

        private void searchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            /*
            if(searchBox.Text == "Search in mails")
            {
                searchBox.Text = "";
                searchBox.Foreground = Brushes.Black;
            }
            */

            if(isPlaceHolder)
            {
                searchBox.Text = "";
                searchBox.Foreground = Brushes.Black;
                isPlaceHolder = false;
            }

            
        }

        private void searchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrWhiteSpace(searchBox.Text))
            {
                searchBox.Text = "Search in mails";
                searchBox.Foreground = Brushes.Gray;
                isPlaceHolder = true;
            }
        }

        private async Task Select_Email(object sender, RoutedEventArgs e)
        {
            try
            {
                if (selectedEmail != null)
                {
                    //this.Cursor = Cursors.Wait;
                    //await _emailservice.getContent(selectedEmail.Uid);
                    //this.Cursor = Cursors.Arrow;
                    var viewEmailContent = new ViewEmailContent(this, selectedEmail, _emailservice, _credentials);
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
            var sendWindow = new Window1(_credentials, this);
            sendWindow.Show();
        }

        

        // Search function (topic or sender)
        private void searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //EmailEntries.Where(s => s.Sender.Contains(searchBox.Text) || s.Topic.Contains(searchBox.Text)).Select(s => s).ToList();
            if (isPlaceHolder)
            {
                CollectionEmails = string.Empty;
            }
            else
            {
                CollectionEmails = searchBox.Text;
            }

        }

        
        private bool EmailFilter(object obj)
        {
            if(obj is EmailData emaildata)
            {
                return emaildata.Sender.Contains(CollectionEmails, StringComparison.InvariantCultureIgnoreCase) || emaildata.Topic.Contains(CollectionEmails, StringComparison.InvariantCultureIgnoreCase);
            }

            return false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        

        // Refresh Logic
        // https://daedtech.com/wpf-and-notifying-property-change/
    }
}
