using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHTSpider.Test.Lib
{
    public class BencodeUtility
    {
        private BencodeUtility()
        {
        }

        public static object Decode(string str, Encoding encoding = null)
        {
            return Decode(encoding == null ? Encoding.ASCII.GetBytes(str) : encoding.GetBytes(str));
        }

        public static object Decode(byte[] data)
        {
            Int32 offset = 0;
            return DecodeObject(data, ref offset);
        }

        public static Dictionary<string, object> DecodeDictionary(byte[] data)
        {
            Int32 offset = 1;
            return DecodeDictionary(data, ref offset);
        }

        public static Int64 DecodeInt(byte[] data)
        {
            Int32 offset = 1;
            return DecodeInt(data, ref offset);
        }

        public static byte[] DecodeString(byte[] data)
        {
            Int32 offset = 0;
            return DecodeString(data, ref offset);
        }

        public static List<object> DecodeList(byte[] data)
        {
            Int32 offset = 1;
            return DecodeList(data, ref offset);
        }

        private static object DecodeObject(byte[] data, ref Int32 offset)
        {
            byte currentByte = data[offset];
            switch (currentByte)
            {
                //This is a dictionary
                case (byte)'d':
                    {
                        AddOffset(ref offset, 1, data.Length);
                        return DecodeDictionary(data, ref offset);
                    }
                //This is a string
                case (byte)'0':
                case (byte)'1':
                case (byte)'2':
                case (byte)'3':
                case (byte)'4':
                case (byte)'5':
                case (byte)'6':
                case (byte)'7':
                case (byte)'8':
                case (byte)'9':
                    {
                        return DecodeString(data, ref offset);
                    }
                //This is a number
                case (byte)'i':
                    {
                        AddOffset(ref offset, 1, data.Length); //From the byte next to 'i'
                        return DecodeInt(data, ref offset);
                    }
                //This is a list
                case (byte)'l':
                    {
                        AddOffset(ref offset, 1, data.Length); //From the byte next to 'l'
                        return DecodeList(data, ref offset);
                    }
                default:
                    throw new Exception(string.Format("Unvalid data character '{0}', offset {1}", (char)currentByte, offset));
            }
        }

        private static Dictionary<string, object> DecodeDictionary(byte[] data, ref Int32 offset)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            while (data[offset] != 'e')
            {
                dic.Add
                    (
                        Encoding.ASCII.GetString(DecodeString(data, ref offset)),
                        DecodeObject(data, ref offset)
                    );
            }
            AddOffset(ref offset, 1, data.Length);
            return dic;
        }

        private static byte[] DecodeString(byte[] data, ref Int32 offset)
        {
            //Calculate the length of bytes that string occupies
            String lengthString = string.Empty;
            while (data[offset] != ':')
            {
                if ('0' <= data[offset] && data[offset] <= '9')
                {
                    lengthString += (char)data[offset];
                }
                else
                    throw new Exception(string.Format("Decode string error, invalid character '{0}', offset {1}", (char)data[offset], offset));
                AddOffset(ref offset, 1, data.Length);
            }
            AddOffset(ref offset, 1, data.Length);
            Int32 strLength = Convert.ToInt32(lengthString);
            if (strLength == 0) return null;
            if (offset + strLength > data.Length)
                throw new Exception(string.Format("Decode string error, invalid string length '{0}', offset {1}", strLength, offset));

            //Copy from the byte next to ':'
            byte[] strBytes = new byte[strLength];
            Buffer.BlockCopy(data, offset, strBytes, 0, strLength);

            AddOffset(ref offset, strLength, data.Length);
            return strBytes;
        }

        private static Int64 DecodeInt(byte[] data, ref Int32 offset)
        {
            String intString = string.Empty;
            while (data[offset] != 'e')
            {
                intString += (char)data[offset];
                AddOffset(ref offset, 1, data.Length);
            }
            Int64 result;
            if (!Int64.TryParse(intString, out result))
            {
                throw new Exception(string.Format("Decode integer error, invalid integer string \"{0}\", offset {1}", intString, offset));
            }

            AddOffset(ref offset, 1, data.Length);
            return result;
        }

        private static List<object> DecodeList(byte[] data, ref Int32 offset)
        {
            List<object> result = new List<object>();
            while (data[offset] != 'e')
            {
                result.Add(DecodeObject(data, ref offset));
            }

            AddOffset(ref offset, 1, data.Length);
            return result;
        }

        private static void AddOffset(ref Int32 offset, int length, int max)
        {
            offset += length;
            if (offset > max)
                throw new Exception("Add offset error, index must less than data length");
        }

        public static IEnumerable<Byte> Encode(object obj)
        {
            Dictionary<string, object> dic = obj as Dictionary<string, object>;
            if (dic != null)
            {
                yield return (byte)'d';
                foreach (KeyValuePair<string, object> pair in dic)
                {
                    byte[] bytes = GetAscii(pair.Key.Length + ":" + pair.Key);
                    foreach (Byte b in bytes.Concat(Encode(pair.Value)))
                    {
                        yield return b;
                    }
                }
                yield return (byte)'e';
                yield break;
            }

            Int32? int32 = obj as Int32?;
            if (int32 != null)
            {
                byte[] bytes = GetAscii("i" + int32.Value + "e");
                foreach (Byte b in bytes)
                {
                    yield return b;
                }
                yield break;
            }

            Int64? int64 = obj as Int64?;
            if (int64 != null)
            {
                byte[] bytes = GetAscii("i" + int64.Value + "e");
                foreach (Byte b in bytes)
                {
                    yield return b;
                }
                yield break;
            }

            string str = obj as string;
            if (str != null)
            {
                byte[] bytes = GetAscii(str.Length + ":" + str);
                foreach (Byte b in bytes)
                {
                    yield return b;
                }
                yield break;
            }

            byte[] byteArr = obj as byte[];
            if (byteArr != null)
            {
                byte[] bytes = GetAscii(byteArr.Length + ":");
                foreach (Byte b in bytes.Concat(byteArr))
                {
                    yield return b;
                }
                yield break;
            }

            List<object> list = obj as List<object>;
            if (list != null)
            {
                yield return (byte)'l';
                foreach (object item in list)
                {
                    foreach (Byte b in Encode(item))
                    {
                        yield return b;
                    }
                }
                yield return (byte)'e';
            }
        }

        private static byte[] GetAscii(string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }
    }
}
