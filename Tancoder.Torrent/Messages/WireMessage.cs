using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tancoder.Torrent.Client.Messages;

namespace Tancoder.Torrent.Messages
{
    public abstract class WireMessage : Message
    {
        public int  Length { get; set; }
        public bool Legal { get; protected set; } = false;
        public byte MessageID { get; protected set; } = 0;
        public virtual int OnMessageLength(byte[] pstrlen)
        {
            return Length = ReadInt(pstrlen, 0);
        }

        public abstract bool CheackHead(byte[] buffer, int offset);

        public override void Decode(byte[] buffer, int offset, int length)
        {
            MessageID = ReadByte(buffer, ref offset);
            
            Legal = true;
        }

        public override int Encode(byte[] buffer, int offset)
        {
            int off = offset;
            off += Write(buffer, off, Length);
            off += Write(buffer, off, MessageID);
            return off - offset;
        }
    }
}
