using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Tancoder.Torrent;

namespace Spider.Queue
{
    public interface IQueue
    {
        void Enqueue(KeyValuePair<InfoHash, IPEndPoint> item);

        KeyValuePair<InfoHash, IPEndPoint> Dequeue();

        int Count();
    }
}
