using System.Collections.Generic;
using Tancoder.Torrent.Dht;

namespace Tancoder.Torrent
{
    public class FindPeersResult
    {
        public bool Found { get; set; }
        public IList<Node> Nodes { get; set; }
    }
}