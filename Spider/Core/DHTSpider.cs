using Spider.Log;
using Spider.Queue;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Tancoder.Torrent;
using Tancoder.Torrent.BEncoding;
using Tancoder.Torrent.Dht;
using Tancoder.Torrent.Dht.Messages;

namespace Spider.Core
{
    public class DHTSpider : IDhtEngine
    {
        public DHTSpider(IPEndPoint localAddress, IQueue queue)
        {
            LocalAddress = localAddress;
            LocalId = NodeId.Create();
            udpSocketListener = new UdpSocketListener(localAddress);
            KTable = new ConcurrentDictionary<string, Node>();
            TokenManager = new EasyTokenManager();
            Queue = queue;
            MessageQueue = new ConcurrentQueue<KeyValuePair<IPEndPoint, byte[]>>();
        }

        private static List<IPEndPoint> BOOTSTRAP_NODES = new List<IPEndPoint>() {
            new IPEndPoint(Dns.GetHostEntry("router.bittorrent.com").AddressList[0], 6881),
            new IPEndPoint(Dns.GetHostEntry("dht.transmissionbt.com").AddressList[0], 6881)
        };

        private static int MaxNodesSize = 1000;

        public ConcurrentQueue<KeyValuePair<IPEndPoint, byte[]>> MessageQueue;
        private object locker = new object();
        public IMetaDataFilter Filter { get; set; }
        public IQueue Queue { get; set; }

        public NodeId LocalId { get; set; }
        public IPEndPoint LocalAddress { get; set; }
        public ITokenManager TokenManager { get; private set; }

        public ConcurrentDictionary<string, Node> KTable;

        private bool disposed = false;
        public bool Disposed
        {
            get
            {
                return disposed;
            }
        }

        public event NewMetadataEvent NewMetadata;

        public void Add(BEncodedList nodes)
        {
            Add(Node.FromCompactNode(nodes));
        }

        public void Add(IEnumerable<Node> nodes)
        {
            foreach (var node in nodes)
            {
                Add(node);
            }
        }

        public void Add(Node node)
        {
            if (KTable.Count >= MaxNodesSize)
            {
                return;
            }
            if (!KTable.ContainsKey(node.Id.ToString()))
            {
                KTable.TryAdd(node.Id.ToString(), node);
            }
        }

        public Node FindNode(NodeId nid)
        {
            var node = new Node(NodeId.Create(), LocalAddress);
            if (KTable.TryGetValue(nid.ToString(), out node))
            {
                return node;
            }
            return null;
        }

        public void Dispose()
        {
            if (disposed)
                return;
        }

        public void GetAnnounced(InfoHash infohash, IPEndPoint endpoint)
        {
            try
            {
                if (Filter == null || (Filter != null && Filter.Ignore(infohash)))
                {
                    NewMetadata?.Invoke(this, new NewMetadataEventArgs(infohash, endpoint));
                }
            }
            catch { }
        }

        public NodeId GetNeighborId(NodeId target)
        {
            byte[] nid = new byte[target.Bytes.Length];
            Array.Copy(target.Bytes, nid, nid.Length / 2);
            Array.Copy(LocalId.Bytes, nid.Length / 2,
                nid, nid.Length / 2, nid.Length / 2);
            return new NodeId(nid);
        }

        public void GetPeers(InfoHash infohash)
        {
        }

        public FindPeersResult QueryFindNode(NodeId target)
        {
            var result = new FindPeersResult();
            var targetNode = FindNode(target);
            if (targetNode != null)
            {
                result.Nodes.Add(targetNode);
            }
            else
            {
                result.Nodes = GetClosestFromKTable(target);
            }
            return result;
        }

        public GetPeersResult QueryGetPeers(NodeId infohash)
        {
            var result = new GetPeersResult()
            {
                HasHash = false,
                Nodes = new List<Node>(),
            };
            return result;
        }
        public List<Node> GetClosestFromKTable(NodeId target)
        {
            SortedList<NodeId, Node> sortedNodes = new SortedList<NodeId, Node>(8);
            foreach (Node n in KTable.Values)
            {
                NodeId distance = n.Id.Xor(target);
                if (sortedNodes.Count == 8)
                {
                    if (distance > sortedNodes.Keys[sortedNodes.Count - 1])//maxdistance
                        continue;
                    //remove last (with the maximum distance)
                    sortedNodes.RemoveAt(sortedNodes.Count - 1);
                }
                sortedNodes.Add(distance, n);
            }
            return new List<Node>(sortedNodes.Values);
        }

        public void Send(DhtMessage msg, IPEndPoint endpoint)
        {
            if (msg.TransactionId == null)
            {
                if (msg is ResponseMessage)
                {
                    //throw new ArgumentException("Message must have a transaction id");
                }
                msg.TransactionId = TransactionId.NextId();
            }
            var buffer = msg.Encode();

            udpSocketListener.Send(buffer, endpoint);
        }

        public void Start()
        {
            udpSocketListener.Start();
            udpSocketListener.MessageReceived += OnMessageReceived;

            Task.Run(() =>
            {
                while (true)
                {
                    if (Queue.Count() <= 0)
                    {
                        JoinDHTNetwork();
                       // MakeNeighbours();
                    }
                    Thread.Sleep(1000);
                }

            });

            for (int i = 0; i < 10; i++)
            {
                Task.Run(() =>
                {
                    ProcessMessage();
                });
                Thread.Sleep(100);
            }

        }
        public void Stop()
        {
            udpSocketListener.Stop();
        }

        private void JoinDHTNetwork()
        {
            foreach (var item in BOOTSTRAP_NODES)
            {
                SendFindNodeRequest(item);
            }
        }
        private void MakeNeighbours()
        {
            foreach (var item in KTable)
            {
                SendFindNodeRequest(item.Value.EndPoint, item.Value.Id);
            }
            KTable.Clear();
        }

        private void SendFindNodeRequest(IPEndPoint address, NodeId nodeid = null)
        {
            var nid = nodeid == null ? LocalId : GetNeighborId(nodeid);
            try
            {
                var msg = new FindNode(nid, NodeId.Create());
                Send(msg, address);
            }
            catch (Exception ex)
            {
                Logger.Trace($"SendFindNodeRequest nid:{nid} {address} {ex.ToString()}");
            }
        }

        private UdpSocketListener udpSocketListener;

        private void OnMessageReceived(byte[] buffer, IPEndPoint endpoint)
        {
            try
            {
                MessageQueue.Enqueue(new KeyValuePair<IPEndPoint, byte[]>(endpoint, buffer));

            }
            catch (Exception ex)
            {
                Logger.Fatal($"OnMessageReceived :{ex.ToString()}");
            }
        }

        private void ProcessMessage()
        {
            while (true)
            {
                if (MessageQueue.Count > 0)
                {
                    var msg = new KeyValuePair<IPEndPoint, byte[]>();
                    if (MessageQueue.TryDequeue(out msg))
                    {
                        ProcessMessage(msg.Value, msg.Key);
                    }
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }
        private void ProcessMessage(byte[] buffer, IPEndPoint endpoint)
        {
            try
            {
                DhtMessage message;
                string error;
                if (MessageFactory.TryNoTraceDecodeMessage((BEncodedDictionary)BEncodedValue.Decode(buffer, 0, buffer.Length, false), out message, out error))
                {
                    if (message is QueryMessage)
                    {
                        message.Handle(this, new Node(message.Id, endpoint));
                    }

                    //var node = new Node(message.Id, endpoint);
                    //if (!KTable.TryGetValue(message.Id.ToString(), out node))
                    //{
                    //    Add(new Node(message.Id, endpoint));
                    //}
                    //node.Seen();
                    //message.Handle(this, node);
                }
            }
            catch { }
        }


    }
}
