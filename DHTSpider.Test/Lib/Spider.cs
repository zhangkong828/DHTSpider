using DHTSpider.Test.Listeners;
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

        private static KTable ktable = new KTable(4000);

        private UdpListener udp;
        public Spider(IPEndPoint localAddress)
        {
            udp = new UdpListener(localAddress);
        }

        public void Start()
        {
            udp.Start();
            udp.MessageReceived += OnMessageReceived;

            Task.Run(() =>
            {
                while (true)
                {
                    if (true)//Todo
                    {
                        JoinDHTNetwork();
                        MakeNeighbours();
                    }
                    Thread.Sleep(1000);
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

        private void SendMessage(object msg, IPEndPoint localAddress)
        {

        }

        private void OnMessageReceived(byte[] buffer, IPEndPoint endpoint)
        {
            var data = Bencoding.DecodeBDictionary(buffer);
        }


        private void SendFindNodeRequest(IPEndPoint localAddress, byte[] nid = null)
        {
            //Todo

        }
    }
}
