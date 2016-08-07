using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Tancoder.Torrent;
using Tancoder.Torrent.BEncoding;
using Tancoder.Torrent.Dht;
using Tancoder.Torrent.Dht.Listeners;
using Tancoder.Torrent.Dht.Messages;

namespace DHTSpider.Core.Spider
{
    public class DhtSpider : ISpider, IDhtEngine
    {
        private bool disposed = false;
        private Queue<KeyValuePair<InfoHash, IPEndPoint>> seeds = new Queue<KeyValuePair<InfoHash, IPEndPoint>>();

        public DhtSpider(DhtSpiderSetting setting, DhtListener listener)
        {
            if (listener == null)
                throw new ArgumentNullException("setting");
            if (listener == null)
                throw new ArgumentNullException("listener");

            MaxSendQueue = setting.MaxCacheCount;
            MaxFindSendPer = setting.MaxFindSendPer;
            MaxWaitCount = setting.MaxWaitCount;
            MaxCacheCount = setting.MaxCacheCount;

            TokenManager = new EasyTokenManager();
            LocalId = NodeId.Create();
            MessageLoop = new MessageLoop(this, listener);
            MessageLoop.ReceivedMessage += MessageLoop_ReceivedMessage;
        }

        public event NewMetadataEvent NewMetadata;
        public event EventHandler StateChanged;

        public bool Disposed
        {
            get
            {
                return disposed;
            }
        }
        public IMetaDataFilter Filter { get; set; }
        public NodeId LocalId { get; private set; }
        public int MaxSendQueue { get; set; }
        public int MaxFindSendPer { get; set; }
        public int MaxWaitCount { get; set; }
        public MessageLoop MessageLoop { get; private set; }
        public Queue<Node> NextNodes { get; private set; } = new Queue<Node>();
        public DhtState State { get; private set; }
        public ITokenManager TokenManager { get; private set; }
        public HashSet<Node> VistedNodes { get; private set; } = new HashSet<Node>();
        public int MaxCacheCount { get; set; }

        public void Add(Node node)
        {
            lock (NextNodes)
            {
                if (!VistedNodes.Contains(node) && NextNodes.Count < MaxWaitCount)
                {
                    NextNodes.Enqueue(node);
                    lock (VistedNodes)
                    {
                        VistedNodes.Add(node);
                    }
                }
            }
        }

        public void Add(IEnumerable<Node> nodes)
        {
            foreach (var node in nodes)
            {
                Add(node);
            }
        }

        public void Add(BEncodedList nodes)
        {
            Add(Node.FromCompactNode(nodes));
        }

        public void Announce(InfoHash infohash, int port)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (disposed)
                return;
        }

        public void GetAnnounced(InfoHash infohash, IPEndPoint endpoint)
        {
            if (Filter == null || (Filter != null && !Filter.Ignore(infohash)))
            {
                lock (seeds)
                {
                    seeds.Enqueue(new KeyValuePair<InfoHash, IPEndPoint>(infohash, endpoint));
                }
                RaiseNewMetadata(infohash, endpoint);
            }
        }

        public void GetPeers(InfoHash infohash)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, object> GetReport()
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            result["Seeds Waited"] = seeds.Count;
            result["Cache Nodes"] = VistedNodes.Count;
            result["Sends Waited"] = NextNodes.Count;

            return result;
        }

        public int GetWaitSeedsCount()
        {
            return seeds.Count;
        }

        public KeyValuePair<InfoHash, IPEndPoint> Pop()
        {
            lock (seeds)
            {
                if (seeds.Count > 0)
                    return seeds.Dequeue();
                else
                    return new KeyValuePair<InfoHash, IPEndPoint>();
            }
        }

        public FindPeersResult QueryFindNode(NodeId target)
        {
            var result = new FindPeersResult()
            {
                Found = false,
                Nodes = VistedNodes.Take(8).ToList(),
                //Nodes = VistedNodes.OrderByDescending(n => n.LastSeen).Take(8).ToList(),
            };
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

        public byte[] SaveNodes()
        {
            throw new NotImplementedException();
        }

        public void Send(DhtMessage msg, IPEndPoint endpoint)
        {
            if (msg is FindNodeResponse && MessageLoop.GetWaitSendCount() > MaxSendQueue)
                return;
            MessageLoop.EnqueueSend(msg, endpoint);
        }

        public void SendFindNodes()
        {
            var waitsend = MessageLoop.GetWaitSendCount();
            lock (NextNodes)
            {
                for (int i = 0; i < NextNodes.Count && waitsend < MaxFindSendPer; i++)
                {
                    var next = NextNodes.Dequeue();
                    SendFindNode(next);
                    waitsend++;
                }
            }
        }

        public void Start()
        {
            var bootstrap = new Node[]
            {
                new Node
                (
                    NodeId.Create(),
                    new IPEndPoint(Dns.GetHostEntry("router.bittorrent.com").AddressList[0], 6881)
                ),
                new Node
                (
                    NodeId.Create(),
                    new IPEndPoint(Dns.GetHostEntry("dht.transmissionbt.com").AddressList[0], 6881)
                )
            };
            Start(bootstrap);
        }

        public void Start(Node[] initialNodes)
        {
            MessageLoop.Start();
            foreach (var item in initialNodes)
            {
                SendFindNode(item);
            }
            RaiseStateChanged(DhtState.Ready);
        }

        public void Stop()
        {
            MessageLoop.Stop();
        }

        private void MessageLoop_ReceivedMessage(object sender, MessageEventArgs e)
        {
            if (e.Message is FindNodeResponse)
            {
                var r = e.Message as FindNodeResponse;
                Add(Node.FromCompactNode(r.Nodes));
            }
            if (VistedNodes.Count > MaxCacheCount)
            {
                lock (VistedNodes)
                {
                    VistedNodes.Clear();
                }
            }
        }
        private void RaiseNewMetadata(InfoHash infohash, IPEndPoint endpoint)
        {
            if (NewMetadata != null)
                NewMetadata(this, new NewMetadataEventArgs(infohash, endpoint));
        }

        private void RaiseStateChanged(DhtState newState)
        {
            State = newState;

            if (StateChanged != null)
                StateChanged(this, EventArgs.Empty);
        }

        private void SendFindNode(Node node)
        {
            FindNode msg = new FindNode(GetNeighborId(node.Id), NodeId.Create());
            //FindNode msg = new FindNode(LocalId, NodeId.Create());
            Send(msg, node.EndPoint);
        }

        public NodeId GetNeighborId(NodeId target)
        {
            byte[] nid = new byte[target.Bytes.Length];
            Array.Copy(target.Bytes, nid, nid.Length / 2);
            Array.Copy(LocalId.Bytes, nid.Length / 2,
                nid, nid.Length / 2, nid.Length / 2);
            return new NodeId(nid);
        }
    }
}
