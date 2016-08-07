using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHTSpider.Core.Spider
{
    public class DhtSpiderSetting
    {
        public int MaxSendQueue { get; set; }
        public int MaxFindSendPer { get; set; }
        public int MaxWaitCount { get; set; }
        public int MaxCacheCount { get; set; }
        public DhtSpiderSetting()
        {
            MaxSendQueue = 150;
            MaxFindSendPer = 200;
            MaxWaitCount = 5000;
            MaxCacheCount = 10000;
        }
    }
}
