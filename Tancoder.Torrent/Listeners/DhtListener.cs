#if !DISABLE_DHT
using System;
using System.Collections.Generic;
using System.Text;
using Tancoder.Torrent.Client;
using System.Net;
using Tancoder.Torrent.Common;

namespace Tancoder.Torrent.Dht.Listeners
{
    public delegate void MessageReceived(byte[] buffer, IPEndPoint endpoint);

    public class DhtListener : UdpListener
    {
        public event MessageReceived MessageReceived;

        public DhtListener(IPEndPoint endpoint)
            : base(endpoint)
        {

        }

        protected override void OnMessageReceived(byte[] buffer, IPEndPoint endpoint)
        {
            MessageReceived?.Invoke(buffer, endpoint);
        }
    }
}
#endif