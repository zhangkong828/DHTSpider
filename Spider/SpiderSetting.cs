using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider
{
    public class SpiderSetting
    {
        public int LocalPort { get; set; }

        public bool IsSaveTorrent { get; set; }

        public string TorrentSavePath { get; set; }

        public int MaxDownLoadThreadCount { get; set; }

        public int MaxSpiderThreadCount { get; set; }

        public SpiderSetting(int localPort = 6881, bool isSaveTorrent = false, string torrentSavePath = "", int maxDownLoadThreadCount = 20, int maxSpiderThreadCount = 2)
        {
            LocalPort = localPort;
            IsSaveTorrent = isSaveTorrent;
            TorrentSavePath = torrentSavePath;
            MaxDownLoadThreadCount = maxDownLoadThreadCount;
            MaxSpiderThreadCount = maxSpiderThreadCount;


            if (IsSaveTorrent)
            {
                if (string.IsNullOrEmpty(TorrentSavePath) || !Directory.Exists(TorrentSavePath))
                {
                    TorrentSavePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "torrent");
                    Directory.CreateDirectory(TorrentSavePath);
                }
            }
        }
    }
}
