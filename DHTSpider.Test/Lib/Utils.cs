using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHTSpider.Test.Lib
{
    public class Utils
    {
        static readonly Random random = new Random();

        public static byte[] RandomID()
        {
            byte[] b = new byte[20];
            lock (random)
                random.NextBytes(b);
            return b;
        }

        public static string GetNodeIdString(byte[] nid)
        {
            return ByteToHexStr(nid);
        }


        public static byte[] GenNeighborID(byte[] target, byte[] nid)
        {
            var nodeid = new byte[20];
            Array.Copy(target, nodeid, 10);
            Array.Copy(nid, 10, nodeid, 10, 10);
            return nodeid;
        }


        public static byte[] StringToByte(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }
        
        public static string ByteToHexStr(byte[] bytes)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    returnStr += bytes[i].ToString("X2");
                }
            }
            return returnStr;
        }
    }
}
