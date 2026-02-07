using Google.Apis.Auth.OAuth2;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
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

namespace Email_Client
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ViewEmailContent : Window
    {
        private MainWindow mainwindowCurrent;
        private EmailData _emaildata;
        private EmailService _emailservice;
        private readonly UserCredential _credentials;
        //private MimeMessage _contentEmail;
        public EmailData emailData
        {
            get
            {
                return _emaildata;
            }

            set
            {
                _emaildata = value;
                ViewEmailContent_Load(this, new RoutedEventArgs());
            }
        }
        public ViewEmailContent(MainWindow mainwindow1, EmailData emaildata, EmailService emailService, UserCredential credentials)
        {
            InitializeComponent();
            
            mainwindowCurrent = mainwindow1;
            _emaildata = emaildata;
            _emailservice = emailService;
            _credentials = credentials;
            Loaded += ViewEmailContent_Load;
        }

        private async void ViewEmailContent_Load(object sender, RoutedEventArgs e)
        {
            // Html viewer
            var contentEmail = await _emailservice.getContent(_emaildata.Uid);

            if(!string.IsNullOrEmpty(contentEmail.HtmlBody))
            {
                await HtmlViewer.EnsureCoreWebView2Async();
                //HtmlViewer.NavigateToString(contentEmail.HtmlBody);
                HtmlViewer.NavigateToString($@" <!DOCTYPE html> 
                          <html>
                          <head>
                          <style>
                            body
                            {{ 
                              text-align: center;
                            }}
                          </style>
                          </head>
                          <body>
                          {contentEmail.HtmlBody}
                          </body>
                          </html>
                
                ");
                HtmlViewer.Visibility = Visibility.Visible;
                TextViewer.Visibility = Visibility.Collapsed; 
            }

            // Text viewer
            else
            {
                TextViewer.Text = contentEmail.TextBody;
                TextViewer.Visibility = Visibility.Visible;
                HtmlViewer.Visibility = Visibility.Collapsed;
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            mainwindowCurrent.Show();
            this.Hide();
        }

        private async void Btn_Reply_Click(object sender, RoutedEventArgs e)
        {
            var contentEmail = await _emailservice.getContent(_emaildata.Uid);
            var references = new List<string>();

            foreach(var reference in contentEmail.References)
            {
                references.Add(reference);
            }

            if(!string.IsNullOrEmpty(contentEmail.MessageId))
            {
                references.Add(contentEmail.MessageId);
            }
            
            Window1 replyWindow = new Window1(_credentials, mainwindowCurrent, contentEmail.MessageId, references);
            replyWindow.Show();

            var address = contentEmail.From.Mailboxes.FirstOrDefault();
            if(address != null)
            {
                replyWindow.txtTo.Text = address.Address.ToString();
            }
            replyWindow.txtTopic.Text = "Re: " + contentEmail.Subject;
            //replyWindow. = contentEmail.MessageId;
            replyWindow.txtTopic.Foreground = Brushes.Black;
         
        }

        private async void Btn_Forward_Click(object sender, RoutedEventArgs e)
        {
            var contentEmail = await _emailservice.getContent(_emaildata.Uid);
            
            Window1 forwardWindow = new Window1(_credentials, mainwindowCurrent);
            forwardWindow.Show();

            forwardWindow.txtTopic.Text = "Fwd: " + contentEmail.Subject;
            forwardWindow.txtTopic.Foreground = Brushes.Black;

            if(!string.IsNullOrEmpty(contentEmail.HtmlBody))
            {
                await forwardWindow.ViewHtml.EnsureCoreWebView2Async();
                forwardWindow.ViewHtml.NavigateToString($@" <!DOCTYPE html> 
                          <html>
                          <head>
                          <style>
                            body
                            {{ 
                              text-align: center;
                            }}
                          </style>
                          </head>
                          <body>
                          {contentEmail.HtmlBody}
                          </body>
                          </html>
                
                ");
                forwardWindow.ViewHtml.Visibility = Visibility.Visible;
                forwardWindow.txtText.Visibility = Visibility.Collapsed;

            }
            else
            {
                forwardWindow.txtText.Text = contentEmail.TextBody;
                forwardWindow.txtText.Visibility = Visibility.Visible;
                forwardWindow.ViewHtml.Visibility = Visibility.Collapsed;
            }
            forwardWindow.txtText.Foreground = Brushes.Black;

        }

        
    }
}
