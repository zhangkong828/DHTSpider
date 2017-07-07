//
// IDhtEngine.cs
//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2009 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Collections.Generic;
using System.Net;
using Tancoder.Torrent.BEncoding;
using Tancoder.Torrent.Dht;
using Tancoder.Torrent.Dht.Messages;

namespace Tancoder.Torrent
{
    public enum ErrorCode : int
    {
        GenericError = 201,
        ServerError = 202,
        ProtocolError = 203,// malformed packet, invalid arguments, or bad token
        MethodUnknown = 204//Method Unknown
    }
    public interface IDhtEngine : IDisposable
    {
        //event EventHandler StateChanged;

        bool Disposed { get; }
        NodeId LocalId { get; }
        //DhtState State { get; }
        ITokenManager TokenManager { get; }

        void Add(BEncodedList nodes);
        void Add(IEnumerable<Node> enumerable);
        void Add(Node node);

        //void Announce(InfoHash infohash, int port);
        void GetAnnounced(InfoHash infohash, IPEndPoint endpoint);
        void GetPeers(InfoHash infohash);

        GetPeersResult QueryGetPeers(NodeId infohash);
        FindPeersResult QueryFindNode(NodeId target);

        //byte[] SaveNodes();
        void Send(DhtMessage msg, IPEndPoint endpoint);
        
        void Start();
        NodeId GetNeighborId(NodeId target);
        void Stop();
    }
}
