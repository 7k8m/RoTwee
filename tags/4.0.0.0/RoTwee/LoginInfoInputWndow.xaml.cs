using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;

using RoTwee.Properties;
using System.Net;
using System.IO;

using OAuthLib;

namespace RoTwee
{
    /// <summary>
    /// LoginInfoInputWndow.xaml の相互作用ロジック
    /// </summary>
    public partial class LoginInfoInputWndow : Window
    {

        private RequestToken _requestToken;
        private Hyperlink _linkToAuthorizeURL = new Hyperlink();

        public LoginInfoInputWndow()
        {
            InitializeComponent();

            this.Title = Properties.Resources.loginInfoInputWindow_msg_caption;
            this._btn_oK.Content = Properties.Resources.loginInfoInputWindow_msg_ok;
            this._btn_cancel.Content = Properties.Resources.loginInfoInputWindow_msg_cancel;
            InitDescription(_tblk_description, _linkToAuthorizeURL);

            _linkToAuthorizeURL .Click +=new RoutedEventHandler(_linkToAuthorizeURL_Click);

            NewRequestToken();

        }

        private static void InitDescription(TextBlock _tblk_description, Hyperlink _linkToAuthorizeURL)
        {
            String source = Properties.Resources.loginInfoInputWindow_msg_description;
            int i = 0;
            while (i < source.Length)
            {
                if (source[i] != '[')
                {
                    StringBuilder sb = new StringBuilder();
                    while ( i < source .Length && source[i] != '[')
                    {
                        sb.Append(source[i]);
                        i++;
                    }
                    
                    _tblk_description.Inlines.Add(new Run(sb.ToString()));

                }else if (source[i] == '[')
                {

                    i++;//for '['

                    StringBuilder sb = new StringBuilder();

                    while (source[i] != ']')
                    {
                        sb.Append(source[i]);
                        i++;
                    }

                    _linkToAuthorizeURL.Inlines.Add(new Run(sb.ToString()));
                    _tblk_description.Inlines.Add(_linkToAuthorizeURL);

                    i++;//for ']'

                }

            }

        }

        private void NewRequestToken()
        {
            _requestToken =
                App.consumer.ObtainUnauthorizedRequestToken(
                    "http://twitter.com/oauth/request_token",
                    "http://twitter.com/"
                );
            _linkToAuthorizeURL.NavigateUri =
                new Uri("http://twitter.com/oauth/authorize?oauth_token=" + _requestToken.TokenValue);

        }


        private void _btn_oK_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                App.accessToken  =
                    App.consumer.RequestAccessToken(
                        _tb_verifier.Text,
                        _requestToken,
                        "http://twitter.com/oauth/access_token",
                        "http://twitter.com/"
                    );

                this.DialogResult = true;
                this.Close();

            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    MessageBox.Show(
                        Properties .Resources .loginInfoInputWindow_msg_failedToAuthenticate 
                    );
                    NewRequestToken();
                }
                else
                {
                    MessageBox.Show(ex.Message);
                }


            }



        }

        private void _btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void _linkToAuthorizeURL_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(_linkToAuthorizeURL.NavigateUri.ToString());
        }

    }
}
