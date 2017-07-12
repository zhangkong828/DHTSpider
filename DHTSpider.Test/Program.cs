using DHTSpider.Core.Spider;
using System;
using System.Net;

namespace DHTSpider.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Spider.Create(new SpiderOption()
            {
                LocalPort = 6881,
                IsSaveTorrent = false,
                TorrentSavePath = "",
                MaxSpiderThreadCount = 2,
                MaxDownLoadThreadCount = 30
            })
            .UseMemoryCache()
            //.UseRedisCache()
            .UseElasticSearchStore()
            //.UseMongoDBStore()
            .Start();


            Console.ReadKey();
        }
    }
}
