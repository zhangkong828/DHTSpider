using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Tancoder.Torrent;

namespace Spider.Queue
{
    public class RedisQueue : IQueue
    {
        //TODO
        public RedisQueue()
        {
        }
        public KeyValuePair<InfoHash, IPEndPoint> Dequeue()
        {
            throw new NotImplementedException();
        }

        public void Enqueue(KeyValuePair<InfoHash, IPEndPoint> item)
        {
            throw new NotImplementedException();
        }

        public int Count()
        {
            return 0;
        }
    }
}
