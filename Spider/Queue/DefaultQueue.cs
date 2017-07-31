using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Tancoder.Torrent;

namespace Spider.Queue
{
    public class DefaultQueue : IQueue
    {
        public ConcurrentQueue<KeyValuePair<InfoHash, IPEndPoint>> _queue = null;
        public DefaultQueue()
        {
            _queue = new ConcurrentQueue<KeyValuePair<InfoHash, IPEndPoint>>();
        }

        public KeyValuePair<InfoHash, IPEndPoint> Dequeue()
        {
            var item = new KeyValuePair<InfoHash, IPEndPoint>();
            _queue.TryDequeue(out item);
            return item;
        }

        public void Enqueue(KeyValuePair<InfoHash, IPEndPoint> item)
        {
            _queue.Enqueue(item);
        }

        public int Count()
        {
            return _queue.Count;
        }
    }
}
