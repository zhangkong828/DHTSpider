using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tancoder.Torrent.BEncoding;
using Tancoder.Torrent.Dht;

namespace DHTSpider.Test
{
    public class EasyTokenManager : ITokenManager
    {
        public TimeSpan Timeout { get; set; }
        private readonly int tokenLength = 10;

        public BEncodedString GenerateToken(Node node)
        {
            BEncodedString token = getToken(node);
            return token;
        }

        private BEncodedString getToken(Node node)
        {
            byte[] buffer = new byte[tokenLength];
            Array.Copy(node.Id.Bytes, buffer, tokenLength);
            var token = new BEncodedString(buffer);
            return token;
        }

        public bool VerifyToken(Node node, BEncodedString token)
        {
            return getToken(node).Equals(token);
        }
    }
}
