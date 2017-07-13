using Spider.Core;
using Spider.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Tancoder.Torrent;

namespace Spider
{
    public class SpiderConfiguration
    {
        private static SpiderConfiguration _instance = null;
        private static readonly object obj = new object();

        public SpiderSetting _option { get; set; }

        private SpiderConfiguration(SpiderSetting option)
        {
            _option = option;
        }

        public static SpiderConfiguration Create(SpiderSetting option = null)
        {
            if (_instance == null)
            {
                lock (obj)
                {
                    if (_instance == null)
                    {
                        _instance = new SpiderConfiguration(option ?? new SpiderSetting());
                    }
                }
            }
            return _instance;
        }

        public SpiderConfiguration UseMemoryCache()
        {
            return _instance;
        }

        public SpiderConfiguration UseRedisCache()
        {
            return _instance;
        }

        public SpiderConfiguration UseElasticSearchStore()
        {
            return _instance;
        }

        public SpiderConfiguration UseMongoDBStore()
        {
            return _instance;
        }

        public void Stop()
        {

        }

        public SpiderConfiguration Start()
        {
            for (var i = 0; i < _option.MaxSpiderThreadCount; i++)
            {
                Task.Run(() =>
                {
                    var spider = new DHTSpider(new IPEndPoint(IPAddress.Any, _option.LocalPort + i));
                    spider.NewMetadata += DHTSpider_NewMetadata;
                    spider.Start();
                });
            }

            return _instance;
        }

        private void DHTSpider_NewMetadata(object sender, NewMetadataEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Logger.ConsoleWrite($"NewMetadata    Hash:{e.Metadata} Address:{e.Owner.ToString()}");
        }
    }
}
