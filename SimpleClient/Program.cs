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
                LocalPort = 6881,//使用端口
                IsSaveTorrent = true,//是否保存torrent
                TorrentSavePath = "",//torrent保存路径
                MaxSpiderThreadCount = 1,//爬虫线程数
                MaxDownLoadThreadCount = 20//下载线程数
            })
           .UseDefaultCache() //默认使用内存缓存
           .UseDefaultQueue() //默认使用内存队列
           .Start();


            Console.ReadKey();
        }
    }
}
