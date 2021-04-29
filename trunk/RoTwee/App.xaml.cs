using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

using RoTwee.Properties ;

using OAuthLib;

namespace RoTwee
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public readonly static Consumer consumer = 
            new Consumer (
                "gMN4nkIU1LiK9Uyu6PlG2w", 
                "ew1oh7nqESvybS0hqI2H1IHHRpOayICLca9mQ"
            );

        public static AccessToken accessToken
        {
            get{
                return new AccessToken (
                    Settings .Default .accessTokenValue ,
                    Settings .Default .accessTokenSecret 
                    );
            }

            set
            {
                Settings.Default.accessTokenValue = value.TokenValue;
                Settings.Default.accessTokenSecret = value.TokenSecret;
                Settings.Default.Save();
            }

        }

        internal static String playingTuneTweetFormat
        {
            get
            {
                if (Settings.Default.playingTuneTweetFormat == null ||
                    Settings.Default.playingTuneTweetFormat.Equals(String.Empty))
                {
                    return RoTwee.Properties.Resources.tweetWindow_msg_playingTuneTweetFormat;
                }
                else
                {
                    return Settings.Default.playingTuneTweetFormat;
                }
            }
        }

        internal static String playingTuneTweetWithHashTagFormat
        {
            get
            {
                if (Settings.Default.playingTuneTweetWithHashTagFormat == null ||
                    Settings.Default.playingTuneTweetWithHashTagFormat.Equals(String.Empty))
                {
                    return RoTwee.Properties.Resources.tweetWindow_msg_playingTuneTweetWithHashTagFormat ;
                }
                else
                {
                    return Settings.Default.playingTuneTweetWithHashTagFormat;
                }
            }
        }


    }
}
