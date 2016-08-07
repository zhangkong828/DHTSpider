using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tancoder.Torrent.BEncoding;

namespace Tancoder.Torrent.Messages.Wire
{
    public class ExtQueryPiece : ExtendMessage
    {
        static readonly BEncodedString MsgTypeKey = "msg_type";
        static readonly BEncodedString PieceKey = "piece";
        static readonly byte MsgType = 0;
        public int PieceID
        {
            get { return (int)((BEncodedNumber)Parameters[PieceKey]).Number; }
            set { Parameters[PieceKey] = new BEncodedNumber(value); }
        }

        public ExtQueryPiece()
        {
            Parameters[MsgTypeKey] = new BEncodedNumber(MsgType);
        }
        public ExtQueryPiece(byte ut_metadata, int piece)
            : this()
        {
            PieceID = piece;
            ExtTypeID = ut_metadata;
        }
    }
}
