using BencodeNET.Objects;
using BencodeNET.Parsing;
using DHTSpider.Test.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DHTSpider.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new BencodeParser();
            BString bstring = parser.ParseString<BString>("12:Hello World!");
            BList blist = parser.ParseString<BList>("l3:foo3:bari42ee");
            BDictionary bdictionary = parser.ParseString<BDictionary>("d3:fooi42e5:Hello6:World!e");

            var a = parser.ParseString<BDictionary>("d1:ad2:id20:abcdefghij01234567896:target20:mnopqrstuvwxyz123456e1:q9:find_node1:t2:aa1:y1:qe");
            var b = parser.ParseString("d1:ad2:id20:abcdefghij01234567896:target20:mnopqrstuvwxyz123456e1:q9:find_node1:t2:aa1:y1:qe");


            var bstr = new BString("Hello World!");
            var c=bstr.EncodeAsString();    // "12:Hello World!"
            var d=bstr.EncodeAsBytes();     // [ 49, 50, 58, 72, ... ]
            var e = Encoding.UTF8.GetBytes(c);

            
            Console.ReadKey();
        }
    }
}
