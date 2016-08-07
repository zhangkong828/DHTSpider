using System.Collections.Generic;
using Tancoder.Torrent.BEncoding;
using Tancoder.Torrent.Dht;

namespace Tancoder.Torrent
{
    public class GetPeersResult
    {
        public bool HasHash { get; set; }
        public IList<Node> Values { get; set; }
        public IList<Node> Nodes { get; set; }
    }
}