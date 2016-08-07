using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tancoder.Torrent.BEncoding;

namespace Tancoder.Torrent.Messages.Wire
{
    public class ExtHandShack : ExtendMessage
    {
        static readonly byte ExtHandShackID = 0;
        static readonly string UtMetadataKey = "ut_metadata";
        static readonly string MethodKey = "m";
        static readonly string MetadataSizeKey = "metadata_size";

        public bool SupportUtMetadata
        {
            get
            {
                return ((BEncodedDictionary)Parameters[MethodKey]).Keys.Contains(UtMetadataKey);
            }
            set
            {
                if (value)
                    ((BEncodedDictionary)Parameters[MethodKey])[UtMetadataKey] = new BEncodedNumber(1);
                else
                    ((BEncodedDictionary)Parameters[MethodKey]).Remove(UtMetadataKey);
            }
        }
        public byte UtMetadata
        {
            get
            {
                return (byte)((BEncodedNumber)((BEncodedDictionary)Parameters[MethodKey])[UtMetadataKey]).Number;
            }
            set
            {
                ((BEncodedDictionary)Parameters[MethodKey])[UtMetadataKey] = new BEncodedNumber(value);
            }
        }
        public long MetadataSize
        {
            get
            {
                return ((BEncodedNumber)Parameters[MetadataSizeKey]).Number;
            }
            set
            {
                Parameters[MetadataSizeKey] = new BEncodedNumber(value);
            }
        }

        public bool CanGetMetadate
        {
            get
            {
                return Parameters.Keys.Contains(MetadataSizeKey) && SupportUtMetadata;
            }
        }

        public ExtHandShack()
            : base()
        {
            ExtTypeID = ExtHandShackID;
            Parameters[MethodKey] = new BEncodedDictionary();
        }
    }
}
