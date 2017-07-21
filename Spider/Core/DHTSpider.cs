using Spider.Core.IoSocket;
using Spider.Core.UdpServer;
using Spider.Log;
using Spider.Queue;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tancoder.Torrent;
using Tancoder.Torrent.BEncoding;
using Tancoder.Torrent.Client;
using Tancoder.Torrent.Dht;
using Tancoder.Torrent.Dht.Listeners;
using Tancoder.Torrent.Dht.Messages;

namespace Spider.Core
{
    public class DHTSpider : IDhtEngine
    {
        public static List<IPEndPoint> BOOTSTRAP_NODES = new List<IPEndPoint>() {
            new IPEndPoint(Dns.GetHostEntry("router.bittorrent.com").AddressList[0], 6881),
            new IPEndPoint(Dns.GetHostEntry("dht.transmissionbt.com").AddressList[0], 6881)
        };

        public DHTSpider(IPEndPoint localAddress, IQueue queue)
        {
            LocalId = NodeId.Create();
            //udp = new DhtListener(localAddress);
            //udpServer = new AsyncUDPServer(localAddress);
            udpSocketListener = new UdpSocketListener(localAddress);
            KTable = new HashSet<Node>();
            TokenManager = new EasyTokenManager();
            Queue = queue;
            MessageQueue = new Queue<KeyValuePair<IPEndPoint, byte[]>>();
        }
        public Queue<KeyValuePair<IPEndPoint, byte[]>> MessageQueue;
        private object locker = new object();
        public IMetaDataFilter Filter { get; set; }
        public IQueue Queue { get; set; }

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
            Logger.Fatal($"Add1  {KTable.Count} {node.Id} {node.Token} {node.EndPoint}");
            lock (locker)
            {
                if (!KTable.Contains(node))
                {
                    lock (locker)
                    {
                        Logger.Fatal($"Add2  {KTable.Count} {node.Id} {node.Token} {node.EndPoint}");
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
            try
            {
                if (Filter == null || (Filter != null && Filter.Ignore(infohash)))
                {
                    NewMetadata?.Invoke(this, new NewMetadataEventArgs(infohash, endpoint));
                }
            }
            catch (Exception)
            {

                throw;
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
            Logger.Warn($"GetPeers");
        }

        public FindPeersResult QueryFindNode(NodeId target)
        {
            var result = new FindPeersResult()
            {
                Found = false,
                //Nodes = KTable.Take(8).ToList(),
                Nodes = KTable.OrderByDescending(n => n.LastSeen).Take(8).ToList(),
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

            //udp.Send(buffer, endpoint);
            // udpServer.Send(buffer, endpoint);
            udpSocketListener.Send(buffer, endpoint);
        }

        public void Start()
        {
            //udp.Start();
            //udp.MessageReceived += OnMessageReceived;

            //udpServer.Start();
            //udpServer.MessageReceived += OnMessageReceived;

            udpSocketListener.Start();
            udpSocketListener.MessageReceived += OnMessageReceived;

            Task.Run(() =>
            {
                while (true)
                {
                    if (Queue.Count() <= 0)
                    {
                        Logger.Info("JoinDHTNetwork MakeNeighbours");
                        JoinDHTNetwork();
                        MakeNeighbours();
                    }
                    Thread.Sleep(3000);
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
            FindNode msg = null;

            var nid = nodeid == null ? LocalId : GetNeighborId(nodeid);
            try
            {
                msg = new FindNode(nid, NodeId.Create());
                Send(msg, address);
            }
            catch
            {
            }
        }

        //private DhtListener udp;
        //private AsyncUDPServer udpServer;
        private UdpSocketListener udpSocketListener;

        private void OnMessageReceived(byte[] buffer, IPEndPoint endpoint)
        {
            //处理消息比较消耗cpu  通过队列来消费，防止cpu过高
            MessageQueue.Enqueue(new KeyValuePair<IPEndPoint, byte[]>(endpoint, buffer));
        }

        private void ProcessMessage()
        {
            while (true)
            {
                if (MessageQueue.Count > 0)
                {
                    lock (locker)
                    {
                        if (MessageQueue.Count > 0)
                        {
                            var msg = MessageQueue.Dequeue();
                            ProcessMessage(msg.Value, msg.Key);
                        }
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
                }
            }
            catch { }
        }


    }
}
