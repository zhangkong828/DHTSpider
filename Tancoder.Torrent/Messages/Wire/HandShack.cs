using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tancoder.Torrent.Dht;
using Tancoder.Torrent.Messages;

namespace Tancoder.Torrent.Messages.Wire
{
    public class HandShack : WireMessage
    {
        static readonly string Protocol = "BitTorrent protocol";
        static readonly byte[] Reserved = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x01 };

        public bool SupportExtend { get; set; }
        public NodeId PeerId { get; set; }
        public InfoHash InfoHash { get; set; }
        public override int ByteLength
        {
            get
            {
                return 1 + Length + Reserved.Length + 20 + 20;
            }
        }

        public override int OnMessageLength(byte[] pstrlen)
        {
            Length = ReadByte(pstrlen, 0);
            return ByteLength - 1;
        }

        public override void Decode(byte[] buffer, int offset, int length)
        {
            if (length != ByteLength - 1)
                return;

            string protocol = ReadString(buffer, ref offset, Length);
            if (protocol != Protocol)
                return;

            byte[] res = ReadBytes(buffer, ref offset, 8);
            SupportExtend = (res[5] & 0x10) > 0;
            InfoHash = new InfoHash(ReadBytes(buffer, ref offset, 20));
            PeerId = new NodeId(ReadBytes(buffer, ref offset, 20));
            Legal = true;
        }

        public override int Encode(byte[] buffer, int offset)
        {
            var off = offset;
            off += Write(buffer, off, (byte)Protocol.Length);
            off += WriteAscii(buffer, off, Protocol);
            off += Write(buffer, off, Reserved);
            off += Write(buffer, off, InfoHash.Hash);
            off += Write(buffer, off, PeerId.Bytes);

            return off - offset;
        }

        public override bool CheackHead(byte[] buffer, int offset)
        {
            if (Length != Protocol.Length)
                return false;
            string protocol = ReadString(buffer, ref offset, Length);
            return protocol == Protocol;
        }

        public HandShack(InfoHash infohash, NodeId id)
        {
            InfoHash = infohash;
            PeerId = id;
            Length = (byte)Protocol.Length;
            Legal = true;
            SupportExtend = true;
        }

        public HandShack(InfoHash infohash)
            : this(infohash, NodeId.Create())
        {
        }

        public HandShack()
        {
        }
    }
}
