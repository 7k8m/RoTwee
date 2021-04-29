using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.IO;
using System.Net;
using System.Windows.Threading ;

using System.Diagnostics;
using System.Threading;

using RoTwee.Properties;

using OAuthLib;

namespace RoTwee
{
    /// <summary>
    /// Window1.xaml の相互作用ロジック
    /// </summary>
    public partial class TweetsWindow : Window
    {

        private static long lastid = 0;

        private TextBlock[] tweetDispArray;
        private List<String> newTweets = new List<string>();
        private DispatcherTimer timer = new DispatcherTimer();
        

        public TweetsWindow()
        {
            InitializeComponent();

            _tblk_writeMessage.Text = Properties.Resources.tweetWindow_msg_writeMessage;
            _tblk_accessServer.Text = Properties.Resources.tweetWindow_msg_accessServer;

            tweetDispArray = 
                new TextBlock []{
                    textBlock0 ,textBlock1 ,textBlock2 ,textBlock3 ,textBlock4 ,textBlock5 ,textBlock6 ,textBlock7
                };

            Settings.Default.user = "";
            Settings.Default.password = "";
            Settings.Default.Save();

            if (
                Settings .Default .accessTokenValue .Equals ("") || 
                Settings .Default .accessTokenSecret .Equals ("") ||
                ! CheckAccessToken(App.accessToken )
                )
            {
                LoginInfoInputWndow dlg = new LoginInfoInputWndow();
                
                if (!dlg.ShowDialog().Value)
                {
                    Process.GetCurrentProcess().Kill();
                    return;
                }
            }

            try
            {
                UpdateTweets(queryTweet());
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                Process.GetCurrentProcess().Kill();
                return;
            }

            timer.Tick += new EventHandler(reloadByTimer);
            timer.Interval = new TimeSpan(0, 5, 0);//5 minutes
            timer.Start();

        }

        private static bool CheckAccessToken(AccessToken accessToken)
        {
            HttpWebResponse resp = null;
            try
            {
                resp =
                    App.consumer.AccessProtectedResource(
                        accessToken,
                        "http://twitter.com/account/verify_credentials.xml",
                        "GET",
                        "http://twitter.com/",
                        new Parameter[] { }
                        );

                return true;

            }
            catch (WebException ex)
            {
                if (ex.Status != WebExceptionStatus.ProtocolError)
                    throw new WebException(ex.Message, ex);
            }
            finally
            {
                if (resp != null)
                    resp.Close();
            }

            return false;

        }

        private void UpdateTweets(string[] tweets)
        {
            for (int i = 0; i < tweets.Length; i++)
            {
                AssignTextMsg (tweetDispArray[i],tweets[i]);
            }
        }

        private void reloadByTimer(object sender, EventArgs e)
        {
            try
            {
                reload(sender, e);
            }
            catch (WebException ex)
            {
#if DEBUG
                MessageBox.Show(ex.Message);
#endif
            }
        }

        private void reload(object sender, EventArgs e)
        {
            lock (this)
            {
                if (newTweets.Count != 0)
                    return;
                try
                {
                    newTweets = new List<string>(queryTweet());
                }
                catch (WebException ex)
                {
                    if (ex.Status != WebExceptionStatus.ProtocolError)
                        throw new WebException(ex.Message, ex);
                    else
                        newTweets = new List<string>();
                }
                if (newTweets.Count == 0)
                    return;
            }

            rotateMessages.Begin();

        }

        private void DoubleAnimation_CurrentStateInvalidated(object sender, EventArgs e)
        {
            //アニメーションの開始
            if (((Clock)sender).CurrentState == ClockState.Active)
            {
                if (newTweets.Count == 0)
                    return;

                for (int i = 1; i < tweetDispArray.Length ; i++)
                {
                    AssignTextMsg(
                        tweetDispArray[tweetDispArray.Length - i],
                        GetTextMsg(tweetDispArray[tweetDispArray.Length - i - 1])
                    );
                }

                AssignTextMsg(
                    tweetDispArray[0],
                    newTweets[newTweets.Count - 1]
                );

                newTweets.RemoveAt(newTweets.Count - 1);

            }
        }

        private static String[] queryTweet()
        {
            String[] allTweets =
                ParseTweets(getResponse());
            if (allTweets.Length > 8)
                Array.Resize<String>(ref allTweets, 8);
            return allTweets;

        }

        private static string[] ParseTweets(string xmlDoc)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlDoc);
            XmlNodeList nodeList = doc.SelectNodes("/statuses/status");

            List<String> l = new List<string>();
            foreach (XmlNode node in nodeList ){
                l.Add(
                    node.SelectSingleNode ("text") .InnerText + " by " +
                    node.SelectSingleNode ("user/name").InnerText 
                );

                if (l.Count == 1)
                    lastid = long.Parse(node.SelectSingleNode("id").InnerText);

            }

            return l.ToArray();

        }

        private static string getResponse()
        {
            WebResponse resp = null;
            Stream stream = null;
            TextReader reader = null;
            try
            {
                resp = 
                    App.consumer .AccessProtectedResource (
                        App.accessToken ,
                        "http://twitter.com/statuses/home_timeline.xml",
                        "GET",
                        "http://twitter.com/",
                        ( (lastid != 0) ? 
                            new Parameter[]{ 
                                new Parameter("since_id",lastid.ToString ()) 
                            }:
                            new Parameter[0]
                        )
                    );
                stream = resp.GetResponseStream();
                reader = new StreamReader(stream);

                return reader.ReadToEnd();

            }
            finally
            {
                if (reader != null)
                    reader.Close();
                
                if (stream != null)
                    stream.Close();

                if (resp != null)
                    resp.Close();

            }
        }

        private void DoubleAnimation_Completed(object sender, EventArgs e)
        {
            if (newTweets.Count > 0)
                rotateMessages.Begin();
        }

        private void _btnConfigLogin_Click(object sender, RoutedEventArgs e)
        {

            String origialUserId = Settings.Default.user;

            LoginInfoInputWndow dialog = new LoginInfoInputWndow();
            if ( dialog.ShowDialog().Value && 
                 ! origialUserId .Equals (Settings .Default .user ) )       
            {                
                lastid = 0;
                reload(this, new EventArgs());
            }

        }

        private void _btnAccessServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                reload(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void _btnInputNewMessage_Click(object sender, RoutedEventArgs e)
        {
            NewTweetInputWindow w = new NewTweetInputWindow();
            w.ShowDialog();
            if (w.DialogResult.HasValue && w.DialogResult.Value)
                reload(sender, e);
        }

        private static void AssignTextMsg(TextBlock messageTextBlock,String newMsg)
        {
            messageTextBlock.Inlines.Clear();
            messageTextBlock.Inlines.Add(CreateNewMsgInline(newMsg));
            ((TextBlock)messageTextBlock.ToolTip).Text = newMsg;
            messageTextBlock.Tag = newMsg;
        }

        private static Inline CreateNewMsgInline(String newMsg)
        {
            Inline msgInline = null;
            if (newMsg.ToLower().Contains("http://"))
            {
                msgInline = new Hyperlink(new Run(newMsg));
                msgInline.Foreground = Brushes.GreenYellow;
                ((Hyperlink)msgInline).Click += new RoutedEventHandler(msgHyperLink_Click);
            }
            else
            {
                msgInline = new Run(newMsg );
            }

            return msgInline ;

        }

        private static String GetTextMsg(TextBlock messageTextBlock)
        {
            return (String)messageTextBlock.Tag;
        }

        static void msgHyperLink_Click(object sender, RoutedEventArgs e)
        {
            String msg = ((Run)((Hyperlink)sender).Inlines.FirstInline).Text;
            Uri uri = FindUri(msg);

            if (MessageBox.Show(
                String.Format(
                    Properties.Resources.tweetWindow_msg_confirmNavigation, 
                    uri.ToString() 
                ),
                Properties.Resources.tweetWindow_msg_confirmNavigationCaption,
                MessageBoxButton.OKCancel
                ) == MessageBoxResult.OK
            )
            {
                Process.Start(uri.ToString());
            }

        }

        private static Uri FindUri(string msg)
        {
            //see rfc 3986 2.2&2.3 http://tools.ietf.org/html/rfc3986
            Regex findUri = new Regex(
                "http://" +
                "[a-zA-Z1234567890\\-\\._~" + //unreserved
                ":/\\?#\\[\\]@" + //gen-delims
                "!\\$&'\\(\\)\\*\\+,;=]+"//sub-delims
            );

            return new Uri(findUri.Match(msg).ToString());

        }



    }
}
