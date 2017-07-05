using BencodeNET.Objects;
using DHTSpider.Test.Listeners;
using DHTSpider.Test.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DHTSpider.Test.Lib
{
    public class Spider
    {
        public static List<IPEndPoint> BOOTSTRAP_NODES = new List<IPEndPoint>() {
            new IPEndPoint(Dns.GetHostEntry("router.bittorrent.com").AddressList[0], 6881),
            new IPEndPoint(Dns.GetHostEntry("dht.transmissionbt.com").AddressList[0], 6881)
        };

        public static int TotalCount = 0;

        private static KTable ktable = new KTable(4000);

        private UdpListener udpClinet;
        public Spider(IPEndPoint localAddress)
        {
            udpClinet = new UdpListener(localAddress);
        }

        public void Start()
        {
            udpClinet.Start();
            udpClinet.MessageReceived += OnMessageReceived;

            Task.Run(() =>
            {
                while (true)
                {
                    if (TotalCount < 1000)//Todo
                    {
                        Logger.Default($"TotalCount : {TotalCount}");
                        JoinDHTNetwork();
                        MakeNeighbours();
                    }
                    Thread.Sleep(5000);
                }

            });
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
            foreach (var node in ktable.Nodes)
            {
                SendFindNodeRequest(node.Address, node.NodeId);
            }
            ktable.Nodes.Clear();
        }

        private void SendFindNodeRequest(IPEndPoint address, byte[] nodeid = null)
        {
            try
            {
                var nid = nodeid != null ? Utils.GenNeighborID(nodeid, ktable.NodeId) : ktable.NodeId;
                var msg = new Dictionary<string, object>
                {
                    { "t" , Utils.RandomID().Take(4).ToArray() },
                    { "y" , "q" },
                    { "q" , "find_node" },
                    { "a" , new Dictionary<string,object>
                            {
                                { "id" , nid },
                                { "target" , Utils.RandomID() }
                            }
                    }
                };
                var bytes = BencodeUtility.Encode(msg).ToArray();
                udpClinet.Send(bytes, address);
            }
            catch (Exception ex)
            {
                Logger.Error("SendFindNodeRequest " + ex.Message);
            }
        }


        private void OnMessageReceived(byte[] buffer, IPEndPoint endpoint)
        {
            try
            {
                Logger.Info($"OnMessageReceived : {endpoint.ToString()} {buffer.Length} {Encoding.ASCII.GetString(buffer)}");
                var msg = BencodeUtility.DecodeDictionary(buffer);
                if (msg.ContainsKey("y") && msg["y"].ToString() == "r" && msg.ContainsKey("r"))
                {
                    var r = msg["r"] as Dictionary<string, string>;
                    if (r != null && r.ContainsKey("nodes") && string.IsNullOrEmpty(r["nodes"]))
                    {
                        var nodes = Utils.StringToByte(r["nodes"]);
                        OnFindNodeResponse(nodes);
                    }
                }
                else if (msg.ContainsKey("y") && msg["y"].ToString() == "q" && msg.ContainsKey("q") && msg["q"].ToString() == "get_peers")
                {
                    OnGetPeersRequest(endpoint, msg);
                }
                else if (msg.ContainsKey("y") && msg["y"].ToString() == "q" && msg.ContainsKey("q") && msg["q"].ToString() == "announce_peer")
                {
                    OnAnnouncePeerRequest(endpoint, msg);
                }

            }
            catch (Exception ex)
            {
                Logger.Error("OnMessageReceived " + ex.Message);
            }
        }

        private void OnFindNodeResponse(byte[] bytes)
        {
            try
            {
                var nodes = Node.DecodeNode(bytes);
                foreach (var node in nodes)
                {
                    if (node.Address != udpClinet.Endpoint && Utils.ByteToHexStr(node.NodeId) != Utils.ByteToHexStr(ktable.NodeId) && node.Address.Port < 65536 && node.Address.Port > 0)
                    {
                        ktable.Push(node);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("OnFindNodeResponse " + ex.Message);
            }
        }

        private void OnGetPeersRequest(IPEndPoint address, Dictionary<string, object> msg)
        {
            try
            {
                var a = msg["a"] as Dictionary<string, string>;
                if (a != null)
                {
                    var infohash = Utils.StringToByte(a["info_hash"]);
                    var token = infohash.Take(2).ToArray();
                    var nid = a["id"];
                    var tid = msg["t"];

                    if (tid == null || string.IsNullOrEmpty(tid.ToString()) || infohash.Length != 20 || Utils.StringToByte(nid).Length != 20)
                    {
                        throw new Exception("非法请求！");
                    }

                    var sendmsg = new Dictionary<string, object>
                    {
                        { "t" , Utils.StringToByte(tid.ToString()) },
                        { "y" , "r" },
                        { "r" , new Dictionary<string,object>
                                {
                                    { "id" , Utils.GenNeighborID(infohash,ktable.NodeId) },
                                    { "nodes" , "" },
                                    { "token" , token }
                                }
                        }
                    };
                    var bytes = BencodeUtility.Encode(sendmsg).ToArray();
                    udpClinet.Send(bytes, address);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("OnGetPeersRequest " + ex.Message);
            }
        }

        private void OnAnnouncePeerRequest(IPEndPoint address, Dictionary<string, object> msg)
        {
            try
            {
                var port = 0;
                var a = msg["a"] as Dictionary<string, string>;
                if (a != null)
                {
                    var infohash = a["info_hash"];
                    var token = a["token"];
                    var nid = Utils.StringToByte(a["id"]);
                    var tid = msg["t"];

                    if (tid == null || string.IsNullOrEmpty(tid.ToString()))
                    {
                        throw new Exception("非法请求！");
                    }

                    if (Utils.StringToByte(infohash).Take(2).ToArray().ToString() != token)
                    {
                        return;
                    }

                    var implied_port = 0;
                    if (a.ContainsKey("implied_port") && int.TryParse(a["implied_port"], out implied_port) && implied_port != 0)
                    {
                        port = address.Port;
                    }
                    else if (a.ContainsKey("port") && int.TryParse(a["port"], out implied_port) && implied_port != 0)
                    {
                        port = implied_port;
                    }

                    if (port >= 65536 || port <= 0)
                    {
                        return;
                    }

                    var sendmsg = new Dictionary<string, object>
                    {
                        { "t" , Utils.StringToByte(tid.ToString()) },
                        { "y" , "r" },
                        { "r" , new Dictionary<string,object>
                                {
                                    { "id" , Utils.GenNeighborID(nid,ktable.NodeId) }
                                }
                        }
                    };
                    var bytes = BencodeUtility.Encode(sendmsg).ToArray();
                    udpClinet.Send(bytes, address);

                    TotalCount++;
                    Logger.Success($"InfoHash:{infohash} Address:{address.Address} Port:{port}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("OnAnnouncePeerRequest " + ex.Message);
            }
        }

    }
}
