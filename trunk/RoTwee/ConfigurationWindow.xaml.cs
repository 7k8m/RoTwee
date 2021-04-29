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

namespace RoTwee
{
    /// <summary>
    /// Interaction logic for ConfigurationWindow.xaml
    /// </summary>
    public partial class ConfigurationWindow : Window
    {
        public ConfigurationWindow()
        {
            InitializeComponent();

            this.Title = Properties.Resources.configurationWindow_msg_configuration;

            _lbl_formatOfTweetForTune.Content = Properties.Resources.configurationWindow_msg_formatOfTweetForTune ;
            _lbl_formatOfTweetForTuneWithHashTag.Content = Properties.Resources.configurationWindow_msg_formatOfTweetForTuneWithHashTag ;

            _btn_Done.Content = Properties.Resources.configurationWindow_msg_done ;
            _btn_Cancel.Content = Properties.Resources.configurationWindow_msg_cancel ;

            _btn_revertToDefault.Content = Properties.Resources.configurationWindow_msg_revertToDefault;

            _tbx_formatOfTweetForTune.Text = App.playingTuneTweetFormat;
            _tbx_formatOfTweetForTuneWithHashTag.Text = App.playingTuneTweetWithHashTagFormat;

        }

        private void _btn_Done_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                CheckFormat(
                    _tbx_formatOfTweetForTune.Text,
                    Properties.Resources.configurationWindow_msg_formatOfTweetForTuneIsWrong
                );

                CheckFormat(
                    _tbx_formatOfTweetForTuneWithHashTag.Text,
                    Properties.Resources.configurationWindow_msg_formatOfTweetForTuneWithHastagIsWrong 
                );

                Properties.Settings.Default.playingTuneTweetFormat = _tbx_formatOfTweetForTune.Text;
                Properties.Settings.Default.playingTuneTweetWithHashTagFormat = _tbx_formatOfTweetForTuneWithHashTag.Text;

                Properties.Settings.Default.Save();

                DialogResult = true;
                this.Close();
            }
            catch (FormatException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private static void CheckFormat(string format, string errorMessage)
        {
            try
            {
                String.Format(format, "artist", "album", "name");
            }
            catch (FormatException ex)
            {
                throw new FormatException(errorMessage, ex);
            }
        }

        private void _btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void _btn_revertToDefault_Click(object sender, RoutedEventArgs e)
        {

            _tbx_formatOfTweetForTune.Text = Properties.Resources .tweetWindow_msg_playingTuneTweetFormat;
            _tbx_formatOfTweetForTuneWithHashTag.Text = Properties.Resources.tweetWindow_msg_playingTuneTweetWithHashTagFormat;
        }
    }
}
