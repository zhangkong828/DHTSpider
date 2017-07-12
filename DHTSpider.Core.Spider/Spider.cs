using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Tancoder.Torrent;

namespace DHTSpider.Core.Spider
{
    public class Spider
    {
        private static Spider _instance = null;
        private static readonly object obj = new object();

        public SpiderOption _option { get; set; }

        private Spider(SpiderOption option)
        {
            _option = option;
        }

        public static Spider Create(SpiderOption option = null)
        {
            if (_instance == null)
            {
                lock (obj)
                {
                    if (_instance == null)
                    {
                        _instance = new Spider(option ?? new SpiderOption());
                    }
                }
            }
            return _instance;
        }

        public Spider UseMemoryCache()
        {
            return _instance;
        }

        public Spider UseRedisCache()
        {
            return _instance;
        }

        public Spider UseElasticSearchStore()
        {
            return _instance;
        }

        public Spider UseMongoDBStore()
        {
            return _instance;
        }

        public void Stop()
        {

        }

        public Spider Start()
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
