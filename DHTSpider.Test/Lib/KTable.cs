using DHTSpider.Test.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHTSpider.Test.Lib
{
    public class KTable
    {
        public KTable(int maxsize)
        {
            NodeId = Utils.RandomID();
            MaxSize = maxsize;
            Nodes = new List<Node>();
        }

        public byte[] NodeId { get; set; }
        public List<Node> Nodes { get; set; }
        public int MaxSize { get; set; }


        public void Push(Node node)
        {
            if (Nodes.Count >= MaxSize)
            {
                return;
            }
            Nodes.Add(node);
        }
    }
}
