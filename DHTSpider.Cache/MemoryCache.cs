using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Tancoder.Torrent;

namespace DHTSpider.Cache
{
    public class MemoryCache : ICache<string, KeyValuePair<InfoHash, IPEndPoint>>
    {
        public bool Contains(string key)
        {
            throw new NotImplementedException();
        }

        public KeyValuePair<InfoHash, IPEndPoint> Get(string key)
        {
            throw new NotImplementedException();
        }

        public bool Set(string key, KeyValuePair<InfoHash, IPEndPoint> val)
        {
            throw new NotImplementedException();
        }
    }
}
