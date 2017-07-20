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

        public SpiderSetting(int localPort = 6881, bool isSaveTorrent = false, string torrentSavePath = "", int maxDownLoadThreadCount = 10, int maxSpiderThreadCount = 1)
        {
            LocalPort = localPort;
            IsSaveTorrent = isSaveTorrent;
            TorrentSavePath = torrentSavePath;
            MaxDownLoadThreadCount = maxDownLoadThreadCount;
            MaxSpiderThreadCount = maxSpiderThreadCount;
        }
        

    }
}
