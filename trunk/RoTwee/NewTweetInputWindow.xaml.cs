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

using System.Net;
using System.IO;

using System.Diagnostics;
using System.Reflection;

using OAuthLib;

namespace RoTwee
{
    /// <summary>
    /// NewTweetInputWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class NewTweetInputWindow : Window
    {
        public NewTweetInputWindow()
        {
            InitializeComponent();

            this.Title = Properties.Resources.newtweetInputWindow_msg_caption;
            _btn_done.Content = Properties.Resources.newtweetInputWindow_msg_submit;
            _btn_cancel.Content = Properties.Resources.newtweetInputWindow_msg_cancel;

        }

        public NewTweetInputWindow(String text):this()
        {
            _tb_NewTweetInput.Text = text;
        }

        private void _btn_done_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SendNewTweet(_tb_NewTweetInput.Text);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        internal static void SendNewTweet(string newMessage)
        {

            WebResponse resp = null;
            try
            {
                resp =                 
                    App.consumer.AccessProtectedResource(
                        App.accessToken,
                        "http://twitter.com/statuses/update.xml",
                        "POST",
                        "http://twitter.com/",
                        new Parameter []{
                        new Parameter(
                            "status",
                            newMessage
                            )
                        }
                    );
            }
            finally
            {

                if (resp != null)
                    resp.Close();

            }
        }

        private void _btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
