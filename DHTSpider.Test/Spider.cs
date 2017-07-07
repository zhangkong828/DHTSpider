using DHTSpider.Test.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tancoder.Torrent;
using Tancoder.Torrent.BEncoding;
using Tancoder.Torrent.Dht;
using Tancoder.Torrent.Dht.Listeners;
using Tancoder.Torrent.Dht.Messages;

namespace DHTSpider.Test
{
    public class Spider : IDhtEngine
    {
        public static List<IPEndPoint> BOOTSTRAP_NODES = new List<IPEndPoint>() {
            new IPEndPoint(Dns.GetHostEntry("router.bittorrent.com").AddressList[0], 6881),
            new IPEndPoint(Dns.GetHostEntry("dht.transmissionbt.com").AddressList[0], 6881)
        };

        public Spider(IPEndPoint localAddress)
        {

            LocalId = NodeId.Create();
            listener = new DhtListener(localAddress);
            KTable = new HashSet<Node>();
            TokenManager = new EasyTokenManager();
        }
        private object locker = new object();
        public IMetaDataFilter Filter { get; set; }

        public NodeId LocalId { get; set; }

        public ITokenManager TokenManager { get; private set; }

        public HashSet<Node> KTable { get; set; }

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
            lock (locker)
            {
                if (!KTable.Contains(node))
                {
                    lock (KTable)
                    {
                        KTable.Add(node);
                    }
                }
            }
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
                Logger.Success($"InfoHash:{infohash} Address:{endpoint.Address} Port:{endpoint.Port}");
                NewMetadata(this, new NewMetadataEventArgs(infohash, endpoint));
            }
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
            throw new NotImplementedException();
        }

        public FindPeersResult QueryFindNode(NodeId target)
        {
            var result = new FindPeersResult()
            {
                Found = false,
                Nodes = KTable.Take(8).ToList(),
                //Nodes = KTable.OrderByDescending(n => n.LastSeen).Take(8).ToList(),
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


        public void Send(DhtMessage msg, IPEndPoint endpoint)
        {
            var buffer = msg.Encode();
            listener.Send(buffer, endpoint);
        }

        public void Start()
        {
            listener.Start();
            listener.MessageReceived += OnMessageReceived;

            Task.Run(() =>
            {
                while (true)
                {
                    if (true)//Todo
                    {
                        JoinDHTNetwork();
                        MakeNeighbours();
                    }
                    Thread.Sleep(3000);
                }

            });
        }
        public void Stop()
        {
            listener.Stop();
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
            foreach (var node in KTable)
            {
                SendFindNodeRequest(node.EndPoint, node.Id);
            }
            KTable.Clear();
        }

        private void SendFindNodeRequest(IPEndPoint address, NodeId nodeid = null)
        {
            try
            {
                var nid = nodeid == null ? LocalId : GetNeighborId(nodeid);
                FindNode msg = new FindNode(nid, NodeId.Create());
                Send(msg, address);

            }
            catch (Exception ex)
            {
                Logger.Error("SendFindNodeRequest " + ex.Message + ex.StackTrace);
            }
        }

        private DhtListener listener;


        private void OnMessageReceived(byte[] buffer, IPEndPoint endpoint)
        {
            try
            {
                DhtMessage message;
                string error;
                if (MessageFactory.TryNoTraceDecodeMessage((BEncodedDictionary)BEncodedValue.Decode(buffer, 0, buffer.Length, false), out message, out error))
                {
                    Logger.Info($"OnMessageReceived  {message.MessageType}");
                    if (message is QueryMessage)
                    {
                        message.Handle(this, new Node(message.Id, endpoint));
                    }
                }
                else
                {
                    Logger.Error("OnMessageReceived  错误的消息");
                }

            }
            catch (Exception ex)
            {
                Logger.Error("OnMessageReceived " + ex.Message);
            }

        }


    }
}
