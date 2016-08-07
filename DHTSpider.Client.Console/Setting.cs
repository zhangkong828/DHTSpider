using DHTSpider.Core.Spider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHTSpider.Client.Console
{
    public class Setting
    {
        public static int LocalPort;

        public static bool IsSaveTorrent;

        public static string TorrentSavePath;

        public static bool IsUsingMongoDb;

        public static int InitResolverThreadCount;

        public static int MaxResolverThreadCount;

        public static int WaitSeedsCount;

        public static DhtSpiderSetting DhtSpiderSetting;

        static Setting()
        {
            DhtSpiderSetting = new DhtSpiderSetting();
        }

        public static void InitSetting()
        {
            LocalPort = 6881;

            IsSaveTorrent = true;

            TorrentSavePath = @"torrent\";

            IsUsingMongoDb = false;

            InitResolverThreadCount = 10;

            MaxResolverThreadCount = 50;

            WaitSeedsCount = 100;


            //========DhtSpiderSetting=========

            DhtSpiderSetting.MaxSendQueue = 50;
            DhtSpiderSetting.MaxFindSendPer = 100;
            DhtSpiderSetting.MaxWaitCount = 1000;
            DhtSpiderSetting.MaxCacheCount = 5000;
        }
    }
}
