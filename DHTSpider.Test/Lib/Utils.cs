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

        public static byte[] CreateNodeId()
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


        public static void GenNeighborID()
        {

        }




        /// <summary>
        /// 字节数组转16进制字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
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
