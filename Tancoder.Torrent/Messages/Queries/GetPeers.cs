#if !DISABLE_DHT
//
// GetPeers.cs
//
// Authors:
//   Alan McGovern <alan.mcgovern@gmail.com>
//
// Copyright (C) 2008 Alan McGovern
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
using System.Text;

using Tancoder.Torrent.BEncoding;
using System.Net;

namespace Tancoder.Torrent.Dht.Messages
{
    public class GetPeers : QueryMessage
    {
        private static BEncodedString InfoHashKey = "info_hash";
        private static BEncodedString QueryName = "get_peers";
        private static ResponseCreator responseCreator = delegate(BEncodedDictionary d, QueryMessage m) { return new GetPeersResponse(d, m); };
        
        public static Func<DhtMessage,bool> Hook { get; set; }

        public NodeId InfoHash
        {
            get { return new NodeId((BEncodedString)Parameters[InfoHashKey]); }
        }
        
        public GetPeers(NodeId id, NodeId infohash)
            : base(id, QueryName, responseCreator)
        {
            Parameters.Add(InfoHashKey, infohash.BencodedString());
        }
        
        public GetPeers(BEncodedDictionary d)
            : base(d, responseCreator)
        {
            
        }

        public override void Handle(IDhtEngine engine, Node node)
        {
            base.Handle(engine, node);

            BEncodedString token = engine.TokenManager.GenerateToken(node);
            GetPeersResponse response = new GetPeersResponse(engine.LocalId, TransactionId, token);

            var result = engine.QueryGetPeers(InfoHash);
            if (result.HasHash)
            {
                BEncodedList list = new BEncodedList();
                foreach (Node n in result.Values)
                    list.Add(n.CompactPort());
                response.Values = list;
            }
            else
            {
                // Is this right?
                response.Nodes = Node.CompactNode(result.Nodes);
            }

            if (Hook == null || Hook(response))
                engine.Send(response, node.EndPoint);
        }
    }
}
#endif