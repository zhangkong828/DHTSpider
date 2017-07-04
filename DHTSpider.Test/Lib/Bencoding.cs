using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BencodeNET.Parsing;
using BencodeNET.Objects;

namespace DHTSpider.Test.Lib
{
    public class Bencoding
    {
        public static BencodeParser parser = new BencodeParser();

        public static void Encode()
        {

        }


        public static IBObject Decode(byte[] bytes)
        {
            return parser.Parse(bytes);
        }

        public static IBObject Decode(string str)
        {
            return parser.ParseString(str);
        }
    }
}
