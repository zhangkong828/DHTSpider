using System;
using System.Net;
using Tancoder.Torrent.Dht.Messages;

namespace DHTSpider.Core.Spider
{
    public class MessageEventArgs : EventArgs
    {
        private IPEndPoint endpoint;
        private DhtMessage message;

        public MessageEventArgs(IPEndPoint endpoint, DhtMessage msg)
        {
            this.endpoint = endpoint;
            this.message = msg;
        }

        public IPEndPoint Endpoint
        {
            get
            {
                return endpoint;
            }

            set
            {
                endpoint = value;
            }
        }

        public DhtMessage Message
        {
            get
            {
                return message;
            }

            set
            {
                message = value;
            }
        }
    }
}
