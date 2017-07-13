using Spider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var spider = SpiderConfiguration.Create(new SpiderSetting()
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
        }
    }
}
