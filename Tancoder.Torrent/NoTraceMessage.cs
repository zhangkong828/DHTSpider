using System;
using Tancoder.Torrent.BEncoding;

namespace Tancoder.Torrent.Dht.Messages
{
    internal class NoTraceMessage : QueryMessage
    {
        private static ResponseCreator creater = crateByType;

        private static DhtMessage crateByType(BEncodedDictionary dictionary, QueryMessage message)
        {
            var nodes = ((BEncodedDictionary)dictionary["r"]).Keys.Contains("nodes");
            if (nodes)
                return new FindNodeResponse(dictionary, message);
            else
                return null;
        }

        public NoTraceMessage()
            : base(new BEncodedDictionary(), creater)
        {
        }
    }
}