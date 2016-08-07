using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tancoder.Torrent.BEncoding;

namespace Tancoder.Torrent.Messages.Wire
{
    public class ExtData : ExtendMessage
    {
        static readonly BEncodedString MsgTypeKey = "msg_type";
        static readonly BEncodedString PieceKey = "piece";
        static readonly BEncodedNumber MsgType = 1;
        public byte[] Data { get; set; }
        public int PieceID
        {
            get { return (int)((BEncodedNumber)Parameters[PieceKey]).Number; }
            set { Parameters[PieceKey] = new BEncodedNumber(value); }
        }

        public ExtData()
        {
            Parameters[MsgTypeKey] = MsgType;
        }

        public override void Decode(byte[] buffer, int offset, int length)
        {
            int head = 0;
            for (int i = 0; i < length - 1; i++)
            {
                if (buffer[offset + i] == 'e' && buffer[offset + i + 1] == 'e')
                {
                    head = i + 2;
                    break;
                }
            }
            if (head == 0)
                return;
            base.Decode(buffer, offset, head);
            if (!Legal || !Parameters[MsgTypeKey].Equals(MsgType))
            {
                Legal = false;
                return;
            }
            Data = new byte[length - head];
            Array.Copy(buffer, offset + head, Data, 0, Data.Length);
        }
    }
}
