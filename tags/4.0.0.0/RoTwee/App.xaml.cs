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
    }
}
