using BencodeNET.Objects;
using DHTSpider.Test.Lib;
using DHTSpider.Test.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DHTSpider.Test.Nodes
{
    public class Node
    {
        public Node(byte[] nid, IPEndPoint address)
        {
            _nodeId = nid;
            _address = address;
        }

        public static readonly int MaxFailures = 4;

        /// <summary>
        /// 节点Id
        /// </summary>
        private byte[] _nodeId;
        public byte[] NodeId
        {
            get { return _nodeId; }
        }

        /// <summary>
        /// 节点地址
        /// </summary>
        private IPEndPoint _address;
        public IPEndPoint Address
        {
            get { return _address; }
        }

        /// <summary>
        /// 节点最近一次的活跃时间
        /// </summary>
        private DateTime _lastActiveTime;
        public DateTime LastActiveTime
        {
            get { return _lastActiveTime; }
            set { _lastActiveTime = value; }
        }

        /// <summary>
        /// 失败次数
        /// </summary>
        private int _failedCount;
        public int FailedCount
        {
            get { return _failedCount; }
            set { _failedCount = value; }
        }

        /// <summary>
        /// 节点状态
        /// </summary>
        public NodeState State
        {
            get
            {
                if (_failedCount >= MaxFailures)
                    return NodeState.Bad;
                else if (_lastActiveTime == DateTime.MinValue)
                    return NodeState.Unknown;
                return (DateTime.UtcNow - _lastActiveTime).TotalMinutes < 15 ? NodeState.Good : NodeState.Questionable;
            }
        }
        
        public override string ToString()
        {
            return Utils.GetNodeIdString(_nodeId);
        }


        private void CompactNode(byte[] buffer, int offset)
        {
            Message.Write(buffer, offset, _nodeId);
            CompactPort(buffer, offset + 20);
        }

        public void CompactPort(byte[] buffer, int offset)
        {
            Message.Write(buffer, offset, _address.Address.GetAddressBytes());
            Message.Write(buffer, offset + 4, (ushort)_address.Port);
        }

        /// <summary>
        /// 解码nodes
        /// </summary>
        public static IEnumerable<Node> DecodeNode(byte[] buffer)
        {
            for (int i = 0; (i + 26) <= buffer.Length; i += 26)
                yield return DecodeNode(buffer, i);
        }
        private static Node DecodeNode(byte[] buffer, int offset)
        {
            byte[] id = new byte[20];
            Buffer.BlockCopy(buffer, offset, id, 0, 20);
            IPAddress address = new IPAddress((uint)BitConverter.ToInt32(buffer, offset + 20));
            int port = (int)(ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(buffer, offset + 24));
            return new Node(id, new IPEndPoint(address, port));
        }

        /// <summary>
        /// 编码nodes
        /// </summary>
        /// <param name="nodes"></param>
        public static BString EncodeNode(IList<Node> nodes)
        {
            byte[] buffer = new byte[nodes.Count * 26];
            for (int i = 0; i < nodes.Count; i++)
                nodes[i].CompactNode(buffer, i * 26);

            return new BString(buffer);
        }
    }
}
