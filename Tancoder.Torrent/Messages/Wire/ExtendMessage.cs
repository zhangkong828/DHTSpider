using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tancoder.Torrent.BEncoding;

namespace Tancoder.Torrent.Messages.Wire
{
    public abstract class ExtendMessage : WireMessage
    {
        static readonly byte ExtendID = 20;
        public BEncodedDictionary Parameters { get; protected set; }
        public byte ExtTypeID { get; protected set; }
        public ExtendMessage()
        {
            MessageID = ExtendID;
            Parameters = new BEncodedDictionary();
        }
        public override int ByteLength
        {
            get
            {
                return Parameters.LengthInBytes() + sizeof(byte) + sizeof(byte) + sizeof(int);
            }
        }
        public override void Decode(byte[] buffer, int offset, int length)
        {
            base.Decode(buffer, offset, length);

            if (!Legal || MessageID != ExtendID)
            {
                Legal = false;
                return;
            }
            try
            {
                offset += 1;
                ExtTypeID = ReadByte(buffer, ref offset);
                Parameters = BEncodedValue.Decode<BEncodedDictionary>(buffer, offset, length - 2, false);
                Legal = true;
            }
            catch (Exception ex)
            {
                Legal = false;
                Debug.WriteLine(ex);
            }
        }

        public override int Encode(byte[] buffer, int offset)
        {
            int off = offset;
            Length = ByteLength - sizeof(int);
            off += base.Encode(buffer, off);
            off += Write(buffer, off, ExtTypeID);
            off += Parameters.Encode(buffer, off);

            return off - offset;
        }

        public override bool CheackHead(byte[] buffer, int offset)
        {
            return buffer[offset] == ExtendID;
        }
    }
}
