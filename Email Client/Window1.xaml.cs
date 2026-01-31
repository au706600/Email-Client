using MimeKit;
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
        public ViewEmailContent(MainWindow mainwindow1, EmailData emaildata, EmailService emailService)
        {
            InitializeComponent();
            
            Loaded += ViewEmailContent_Load;
            mainwindowCurrent = mainwindow1;
            _emaildata = emaildata;
            _emailservice = emailService;
        }

        async private void ViewEmailContent_Load(object sender, RoutedEventArgs e)
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

        async private void Back_Click(object sender, RoutedEventArgs e)
        {
            mainwindowCurrent.Show();
            this.Hide();
        }
    }
}
