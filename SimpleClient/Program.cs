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
                IsSaveTorrent = true,
                TorrentSavePath = "",
                MaxSpiderThreadCount = 2,
                MaxDownLoadThreadCount = 10
            })
           .UseMemoryQueue()
           //.UseRedisQueue()
           //.UseElasticSearchStore()
           .UseMongoDBStore()
           .Start();

            Console.ReadKey();
        }
    }
}
