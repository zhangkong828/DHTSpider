using DHTSpider.Test.Listeners;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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


        private void SendMessage(object msg, IPEndPoint localAddress)
        {

        }
    }
}
