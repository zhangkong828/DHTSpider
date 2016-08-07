using System;
using Tancoder.Torrent.BEncoding;

namespace Tancoder.Torrent.Dht
{
    public interface ITokenManager
    {
        TimeSpan Timeout { get; set; }

        BEncodedString GenerateToken(Node node);
        bool VerifyToken(Node node, BEncodedString token);
    }
}