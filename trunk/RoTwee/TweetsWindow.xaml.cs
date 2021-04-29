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
using System.Web;

using System.Diagnostics;
using System.Threading;

using RoTwee.Properties;

using OAuthLib;

using iTunesLib;
using System.Runtime.InteropServices;

namespace RoTwee
{
    /// <summary>
    /// Window1.xaml の相互作用ロジック
    /// </summary>
    public partial class TweetsWindow : Window
    {

        private static long lastid = 0;

        private List<Tweet> newTweets = new List<Tweet>();
        private DispatcherTimer timer = new DispatcherTimer();

        private List<Tweet> presentTweets = new List<Tweet>();

        private static ColorMode colorMode = ColorMode.classic;
        private static long countOfTweet = 0;

        private DispatcherTimer colorTimer = new DispatcherTimer();
        private static long colorTickCount = 0;

        private DispatcherTimer visiblityTimer = new DispatcherTimer();

        private DispatcherTimer removerTimer = new DispatcherTimer();

        private Point? _dragStartPoint;

        private EventHandler newTweetsAnimationCompletedHandler;
        private EventHandler restoreRotateCompletedHandler;

        private double _wheeledAngle = 0.0;

        const int COUNT_OF_TWEETS_IN_ANGLE = 7;
        const double ANGLE_BETWEEN_TWEETS = 90d / (COUNT_OF_TWEETS_IN_ANGLE - 1);
        const double notchAngle = ANGLE_BETWEEN_TWEETS;//synonim for ANGLE_BETWEEN_TWEETS
        const int RATIO_OF_TWEETS_TO_RETAIN = 8;

        public TweetsWindow()
        {
            InitializeComponent();

            newTweetsAnimationCompletedHandler = new EventHandler(NewTweetsAnimation_Completed);
            restoreRotateCompletedHandler = new EventHandler(RestoreRotateCompleted);

            _tblk_writeMessage.Text = Properties.Resources.tweetWindow_msg_writeMessage;
            _tblk_accessServer.Text = Properties.Resources.tweetWindow_msg_accessServer;
            _tblk_postTune.Text = Properties.Resources.tweetWindow_msg_postPlayingTune;
            _tblk_postTuneWithHashTag.Text = Properties.Resources.tweetWindow_msg_postPlayingTuneWithHashTag;
            _tblk_postTuneWithText.Text = Properties.Resources.tweetWindow_msg_postPlayingTuneWithText;
            _tblk_changeColor.Text = Properties.Resources.tweetWindow_msg_changeColor;
            _tblk_recordCounter.Text = Properties.Resources.tweetWindow_msg_postRotateCount;

            Settings.Default.user = "";
            Settings.Default.password = "";
            Settings.Default.Save();

            try
            {
                if (
                    Settings.Default.accessTokenValue.Equals("") ||
                    Settings.Default.accessTokenSecret.Equals("") ||
                    !CheckAccessToken(App.accessToken)
                    )
                {
                    LoginInfoInputWndow dlg = new LoginInfoInputWndow();

                    if (!dlg.ShowDialog().Value)
                    {
                        Process.GetCurrentProcess().Kill();
                        return;
                    }
                }

                InitTweets( queryTweet( COUNT_OF_TWEETS_IN_ANGLE + 1 ) ); 

                timer.Tick += new EventHandler(reloadByTimer);
                timer.Interval = new TimeSpan(0, 5, 0);//5 minutes
                timer.Start();

                colorTimer.Tick += new EventHandler(TickColor);
                colorTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
                colorTimer.Start();

                visiblityTimer.Tick += new EventHandler(UpdateTweetVisible);
                visiblityTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
                visiblityTimer.Start();

                removerTimer.Tick += new EventHandler(RemoveExcessTweets);
                removerTimer.Interval = new TimeSpan(0, 0, 30);
                removerTimer.Start();


            }
            catch (WebException ex)
            {
                MessageBox.Show(Properties.Resources.tweetWindow_msg_startupFailedForNetworkError);
                Process.GetCurrentProcess().Kill();
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Process.GetCurrentProcess().Kill();
                return;
            }
            
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

        private void InitTweets(Tweet[] tweets)
        {
            double degree = -notchAngle;
            for (int i = 0; i < tweets.Length; i++)
            {
                tweets[i].Assign(
                    _grid, 
                    degree, 
                    tweets.Length - i -1,
                    rotateMessages );
                
                tweets[i].SetAnimationInfo(1,i - 1);

                degree += notchAngle ;
                
                presentTweets.Add(tweets[i]);

            }

            countOfTweet += tweets.Length;

            if (tweets.Length > 0)
            {
                Properties.Settings.Default.rotateAngle += ANGLE_BETWEEN_TWEETS;
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

        private void TickColor(object sender, EventArgs e)
        {
            if (colorMode == ColorMode.classic || 
                colorMode == ColorMode .rainbow ||
                colorMode == ColorMode .grayscale )
                return;
            lock (this)
            {
                if (colorMode == ColorMode.acriveRainbow || colorMode == ColorMode.activeGrayscale)
                    colorTickCount++;
                else if (colorMode == ColorMode.reverseActiveRainbow || colorMode == ColorMode.reverseActiveGrayscale)
                    colorTickCount--;

                RecalcColorAllTweets();

            }
        }

        private void UpdateTweetVisible(object sender, EventArgs e)
        {
            foreach (Tweet t in presentTweets)
            {
                t.UpdateVisible();
            }

        }

        private void RemoveExcessTweets(Object sender, EventArgs  e)
        {

            const int tweetRemainCount = RATIO_OF_TWEETS_TO_RETAIN * (COUNT_OF_TWEETS_IN_ANGLE - 1);
            if (presentTweets.Count < tweetRemainCount + 1)
                return;

            lock (this)
            {
                for (int i = tweetRemainCount; i < presentTweets.Count; i++)
                {
                    presentTweets[i].Remove(_grid, rotateMessages);
                }

                presentTweets.RemoveRange(tweetRemainCount, presentTweets.Count - tweetRemainCount);

            }

        }

        private void reload(object sender, EventArgs e)
        {
            reloadCore(false);
            
        }

        //Tweet for rotate count is processd here because it is needed to reload list.
        private void reloadCore(bool tweetRotateCount)
        {
            lock (this)
            {

                if (_dragStartPoint != null)
                    return;

                if (rotateMessages.GetCurrentState() == ClockState.Active)
                    return;

                try
                {
                    if (newTweets.Count == 0)
                        newTweets = new List<Tweet>(queryTweet());

                    if (tweetRotateCount)
                    {

                        NewTweetInputWindow.SendNewTweet(
                            String.Format(
                                Properties.Resources.tweetWindow_msg_rotateCountTweetFormat,
                                (Properties.Settings.Default.rotateAngle + newTweets.Count * ANGLE_BETWEEN_TWEETS) / 360
                            )
                        );

                        newTweets.InsertRange(
                            0,
                            new List<Tweet>(queryTweet())
                        );


                    }

                }
                catch (WebException ex)
                {
                    if (ex.Status != WebExceptionStatus.ProtocolError)
                        throw new WebException(ex.Message, ex);
                    else
                        newTweets = new List<Tweet>();
                }

            }

            _wheeledAngle = 0.0;

            StartRotate(newTweets);

        }

        private void StartRotate(List<Tweet> newTweets)
        {

            for (int i = 0; i < newTweets.Count; i++)
            {
                newTweets[i].Assign(
                    _grid,
                    -1 * notchAngle * ( newTweets .Count - i ),
                    countOfTweet + newTweets .Count - (i + 1),
                    rotateMessages 
                );
            }

            countOfTweet += newTweets.Count;

            presentTweets.InsertRange(0, newTweets);

            for (int i = 0; i < presentTweets.Count; i++)
            {
                presentTweets[i].SetAnimationInfo(newTweets.Count, i - newTweets.Count);
            }

            presentTweets[presentTweets.Count - 1].SetAnimationCompletedHandler(newTweetsAnimationCompletedHandler );

            rotateMessages.Begin();

        }


        private static Tweet[] queryTweet(int? maxLength)
        {
            Tweet[] allTweets =
                ParseTweets(getResponse());

            if (maxLength .HasValue &&
                allTweets.Length > maxLength.Value  )
                Array.Resize<Tweet>(ref allTweets, maxLength.Value  );
            return allTweets;

        }

        private static Tweet[] queryTweet()
        {
            return queryTweet(null);
        }

        private static Tweet[] ParseTweets(string xmlDoc)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlDoc);
            XmlNodeList nodeList = doc.SelectNodes("/statuses/status");

            List<Tweet> l = new List<Tweet>();
            foreach (XmlNode node in nodeList ){
                l.Add(
                    new Tweet(
                        HttpUtility.HtmlDecode (
                            node.SelectSingleNode ("text") .InnerText 
                        ) + " by " +
                        node.SelectSingleNode ("user/name").InnerText   ,
                        node.SelectSingleNode ("user/profile_image_url").InnerText 
                    )
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

        private void NewTweetsAnimation_Completed(object sender, EventArgs e)
        {
            lock (this)
            {

                Properties.Settings.Default.rotateAngle = Properties.Settings.Default.rotateAngle + newTweets.Count * ANGLE_BETWEEN_TWEETS;
                Properties.Settings.Default.Save();

                ((Clock)sender).Completed -= newTweetsAnimationCompletedHandler;
                newTweets.Clear();

            }
        }

        private bool IsNewTweetsAnimation_CompletedFinished(){
            return newTweets.Count == 0;
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

        private static Inline CreateNewMsgInline(String newMsg)
        {
            Inline msgInline = null;
            if (FindUri(newMsg) != null)
            {
                msgInline = new Hyperlink(new Run(newMsg));
                ((Hyperlink)msgInline).Click += new RoutedEventHandler(msgHyperLink_Click);
            }
            else
            {
                msgInline = new Run(newMsg );
            }

            return msgInline ;

        }

        private static Tweet GetTextMsg(TextBlock messageTextBlock)
        {
            return (Tweet)messageTextBlock.Tag;
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
                "https?://" +
                "[a-zA-Z1234567890\\-\\._~" + //unreserved
                ":/\\?#\\[\\]@" + //gen-delims
                "!\\$&'\\(\\)\\*\\+,;=]+",//sub-delims
                RegexOptions.IgnoreCase 
            );

            Match result = findUri.Match(msg);
            if (result.Value.Length > 0)
            {
                Uri uri = null;
                try
                {
                    uri = new Uri(result.Value);
                }
                catch (FormatException)
                {
                    //http://rotwee.codeplex.com/WorkItem/View.aspx?WorkItemId=16220
                }

                return uri;

            }
            else
                return null;

        }

        private void _btnPostTune_Click(object sender, RoutedEventArgs e)
        {
            PostTuneCore(
                sender, 
                e,
                App.playingTuneTweetFormat
            );
        }

        private void _btnPostTuneWithHashtag_Click(object sender, RoutedEventArgs e)
        {
            PostTuneCore(
                sender,
                e,
                App.playingTuneTweetWithHashTagFormat 
            );
        }

        private void PostTuneCore(object sender, RoutedEventArgs e,String format)
        {
            IiTunes itunes = null;
            try
            {
                itunes = new iTunesAppClass();
                if (itunes.CurrentTrack != null && itunes.PlayerState != ITPlayerState.ITPlayerStateStopped)
                {
                    NewTweetInputWindow.SendNewTweet(
                        String.Format(
                        format,
                        itunes.CurrentTrack.Artist,
                        itunes.CurrentTrack.Album,
                        itunes.CurrentTrack.Name
                        )
                    );
                    reload(sender, e);
                }
            }
            catch (WebException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Properties.Resources.tweetWindow_msg_postPlayingTuneError);
            }
            finally
            {
                if (itunes != null)
                    Marshal.ReleaseComObject(itunes);
            }
        }


        private void _btnPostTuneWithText_Click(object sender, RoutedEventArgs e)
        {
            IiTunes itunes = null;
            try
            {
                itunes = new iTunesAppClass();
                if (itunes.CurrentTrack != null && itunes.PlayerState != ITPlayerState.ITPlayerStateStopped)
                {
                    NewTweetInputWindow ntiw =
                        new NewTweetInputWindow(
                            String.Format(
                                App.playingTuneTweetFormat,
                                itunes.CurrentTrack.Artist,
                                itunes.CurrentTrack.Album,
                                itunes.CurrentTrack.Name
                            )
                        );

                    ntiw.ShowDialog();

                    if (ntiw.DialogResult.HasValue && ntiw.DialogResult.Value)
                        reload(sender, e);

                }
                else
                {
                    MessageBox.Show(Properties.Resources.tweetWindow_msg_noTune);
                }
            }
            catch (WebException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Properties.Resources.tweetWindow_msg_postPlayingTuneError);
            }
            finally
            {
                if (itunes != null)
                    Marshal.ReleaseComObject(itunes);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            rotateMessages.Begin();
        }

        private void _btnChangeColor_Click(object sender, RoutedEventArgs e)
        {
            switch (colorMode)
            {
                case ColorMode .classic :
                    colorMode = ColorMode.rainbow;
                    break;

                case ColorMode .rainbow :
                    colorMode = ColorMode.grayscale;
                    break;

                case ColorMode.grayscale :
                    colorMode = ColorMode.acriveRainbow;
                    break;

                case ColorMode .acriveRainbow :
                    colorMode = ColorMode.reverseActiveRainbow;
                    break;

                case ColorMode .reverseActiveRainbow :
                    colorMode = ColorMode.activeGrayscale;
                    break;

                case ColorMode .activeGrayscale :
                    colorMode = ColorMode.reverseActiveGrayscale;
                    break;

                case ColorMode .reverseActiveGrayscale :
                    colorMode = ColorMode.classic;
                    break;

                default :
                    colorMode = ColorMode.classic;
                    break;
            }

            RecalcColorAllTweets();

        }

        private void RecalcColorAllTweets()
        {
            foreach (Tweet tweet in presentTweets)
            {
                tweet.RecalcColor();
            }
        }


        private void _grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                lock (this)
                {                   
                    if (rotateMessages.GetCurrentState() == ClockState.Active)
                        return;

                    _dragStartPoint = e.GetPosition(this);

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        private void _grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {

                if (_dragStartPoint == null)
                    return;

                if (rotateMessages.GetCurrentState() == ClockState.Active)
                    return;

                _dragStartPoint = null;
                _wheeledAngle = 0.0;
                RestoreRotate();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        private void _grid_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {

                if (!_dragStartPoint.HasValue)
                    return;

                if (rotateMessages.GetCurrentState() == ClockState.Active)
                    return;

                Point currentPoint = e.GetPosition(this);
                double deltaAngle = (Math.Atan2(currentPoint.Y, currentPoint.X) - Math.Atan2(_dragStartPoint.Value.Y, _dragStartPoint.Value.X)) * 360 / (2 * Math.PI);
                for (int i = 0; i < presentTweets.Count; i ++ )
                {
                    presentTweets[i].Rotate(deltaAngle, i);
                }
                rotateMessages.Begin();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        private void RestoreRotate()
        {
            lock (this)
            {
                for (int i = 0; i < presentTweets.Count; i ++ )
                {
                    presentTweets[i].RestoreAngle(i);
                }
                if (presentTweets.Count > 0)
                    presentTweets[presentTweets.Count - 1].SetAnimationCompletedHandler(restoreRotateCompletedHandler);
                rotateMessages.Begin();
            }
        }

        private void RestoreRotateCompleted(Object sender, EventArgs ev)
        {
            lock (this)
            {
                ((Clock)sender).Completed -= restoreRotateCompletedHandler;
            }
        }

        class Tweet
        {
            private String _status;
            private String _pictUri;

            private TextBlock _statusTextBlock;
            private DoubleAnimation _animation;
            //7 stands for count of colors in rainbow of which Japanese culture says.
            private long _modulo7;
            

            internal String Status
            {
                get
                {
                    return _status;
                }
            }

            internal Tweet(String status,String pictUri)
            {
                _status = status;
                _pictUri = pictUri;
            }

            internal double Angle
            {
                get
                {
                    return ((RotateTransform)_statusTextBlock.RenderTransform).Angle;
                }
            }

            internal void Assign(Grid grid,double initialDegree,long countOfTweet,Storyboard storyboard)
            {
                
                _modulo7 = countOfTweet % 7 ;

                _statusTextBlock = new TextBlock();
                _statusTextBlock.Inlines.Add(CreateNewMsgInline(_status));
                
                ToolTip tp = new ToolTip();
                DockPanel dp = new DockPanel();
                tp.Content = dp;

                try
                {
                    BitmapImage bmp = new BitmapImage(new Uri(_pictUri));
                    Image img = new Image();
                    
                    img.Width = 32;
                    img.Height = 32;

                    img.Source = bmp;
                    dp.Children.Add (img);
                }
                catch (UriFormatException)
                {
                }

                TextBlock tb = new TextBlock(new Run(_status));
                tb.TextWrapping = TextWrapping.Wrap;
                dp.Children.Add(tb);

                _statusTextBlock.ToolTip = tp;
                _statusTextBlock.Tag = this;

                _statusTextBlock.Margin = new Thickness(45, 0, -1000, 0);
                _statusTextBlock.Height = 20;
                _statusTextBlock.Width = 10000;
                _statusTextBlock.VerticalAlignment = VerticalAlignment.Top;
                RecalcColor();
                _statusTextBlock.FontSize = 10;
                ToolTipService.SetShowDuration(_statusTextBlock, 2147483647);

                //Difference between 45 in left of Margin, and (minus) 40 in X of center is size of font.
                //Difference between 0 in top of Margin, and 10 in Y of center is size of font.
                _statusTextBlock.RenderTransform = new RotateTransform(initialDegree, -40, 10);

                grid.Children.Insert(0,_statusTextBlock );

                _animation = new DoubleAnimation();
                _animation.From = initialDegree;
                _animation.To = initialDegree;
                storyboard.Children.Add(_animation);
                Storyboard.SetTarget(_animation, _statusTextBlock);
                Storyboard.SetTargetProperty(_animation, new PropertyPath("RenderTransform.Angle"));

            }

            internal void Remove(Grid grid, Storyboard board)
            {
                grid.Children.Remove(this._statusTextBlock);
                board.Children.Remove(this._animation);
            }

            internal void SetAnimationInfo(int notch,int index)
            {
                double angle =
                    index * ANGLE_BETWEEN_TWEETS;

                _animation.From = angle;
                _animation.To = angle + notch * notchAngle;
                _animation.Duration = new Duration(new TimeSpan(0, 0, notch));
            }

            internal void SetAnimationCompletedHandler(EventHandler handler)
            {
                if (_animation == null)
                    throw new InvalidOperationException("need to be assigned");

                _animation.Completed += handler;

            }

            internal void RecalcColor()
            {
                _statusTextBlock.Inlines.FirstInline.Foreground = new SolidColorBrush(CalcColor(_modulo7, colorMode));
            }

            internal void UpdateVisible()
            {
                if (this.Angle > 90 + ANGLE_BETWEEN_TWEETS  || this.Angle < 0 - ANGLE_BETWEEN_TWEETS )
                    _statusTextBlock.Visibility = Visibility.Hidden;
                else
                    _statusTextBlock.Visibility = Visibility.Visible ;
            }

            internal void Rotate(double deltaAngle,int index)
            {

                _animation.From = Angle;
                _animation.To = index * ANGLE_BETWEEN_TWEETS  + deltaAngle;
                _animation.Duration = new Duration(new TimeSpan(0, 0, 0));

            }

            internal void RestoreAngle(int index)
            {
                lock (this)
                {
                    _animation.From = Angle;
                    _animation.To = index * ANGLE_BETWEEN_TWEETS ;

                    _animation.Duration = new Duration(new TimeSpan(0, 0, 0, 1));

                }
            }

            private static Color CalcColor(long modulo7, ColorMode colorMode)
            {

                modulo7 = (modulo7 + colorTickCount) % 7;
                if (modulo7 < 0)
                    modulo7 = modulo7 + 7;

                switch (colorMode)
                {
                    case ColorMode.classic:
                        return Colors.GreenYellow;

                    case ColorMode.rainbow:
                    case ColorMode.acriveRainbow :
                    case ColorMode .reverseActiveRainbow :
                        return rainbowColors[modulo7];

                    case ColorMode.grayscale:
                    case ColorMode .activeGrayscale :
                    case ColorMode .reverseActiveGrayscale :
                        byte scale = (byte)(223 * modulo7 / 6 + 32);
                        return Color.FromRgb(scale, scale, scale);

                    default :
                        return Colors.GreenYellow;

                }

            }

        }

        enum ColorMode
        {
            classic,
            rainbow,
            acriveRainbow,
            reverseActiveRainbow,
            grayscale,
            activeGrayscale,
            reverseActiveGrayscale

        }

        static readonly Color[] rainbowColors = 
            new Color[]{
                Colors.Red,
                Colors .Orange ,
                Colors .Yellow ,
                Colors .Green ,
                Colors .Blue ,
                Colors .Indigo ,
                Colors .Purple 
            };

        private void _btnRecordCount_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                recordCount();
            }
            catch (WebException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message );
            }
        }

        private void recordCount()
        {
            reloadCore(true);
        }

        private void _grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {

                lock (this)
                {

                    if (rotateMessages.GetCurrentState() == ClockState.Active)
                        return;

                    _wheeledAngle = _wheeledAngle - e.Delta / Mouse.MouseWheelDeltaForOneLine * ANGLE_BETWEEN_TWEETS * 0.75;
                    for (int i = 0; i < presentTweets.Count; i ++ )
                    {
                        presentTweets[i].Rotate(_wheeledAngle,i);
                    }
                    rotateMessages.Begin();



                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }            
        }

        private void _configurate_Click(object sender, RoutedEventArgs e)
        {
            ConfigurationWindow configWindow = new ConfigurationWindow();
            configWindow.ShowDialog();
        }

    }
}
