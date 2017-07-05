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
            //var nodeid = Utils.RandomID();
            //var nodestr = Utils.GetNodeIdString(nodeid);
            //var nid = Encoding.UTF8.GetString(nodeid);
            //Console.WriteLine(nodeid.ToString());
            //Console.WriteLine(nodestr);
            //Console.WriteLine(nid);



            ////var parser = new BencodeParser();
            ////BString bstring = parser.ParseString<BString>("12:Hello World!");
            ////BList blist = parser.ParseString<BList>("l3:foo3:bari42ee");
            ////BDictionary bdictionary = parser.ParseString<BDictionary>("d3:fooi42e5:Hello6:World!e");

            ////var a = parser.ParseString<BDictionary>("d1:ad2:id20:abcdefghij01234567896:target20:mnopqrstuvwxyz123456e1:q9:find_node1:t2:aa1:y1:qe");
            ////var b = parser.ParseString("d1:ad2:id20:abcdefghij01234567896:target20:mnopqrstuvwxyz123456e1:q9:find_node1:t2:aa1:y1:qe");


            //var bstr1 = new BString(nodeid);
            //Console.WriteLine(bstr1.EncodeAsString());
            //var bstr2 = new BString(nodestr);
            //Console.WriteLine(bstr2.EncodeAsString());
            //var bstr3 = new BString(nid);
            //Console.WriteLine(bstr3.EncodeAsString());
            ////var c=bstr.EncodeAsString();    // "12:Hello World!"
            ////var d=bstr.EncodeAsBytes();     // [ 49, 50, 58, 72, ... ]
            ////var e = Encoding.UTF8.GetBytes(c);

            //Console.WriteLine("------------------------------");


            //var dic = new Dictionary<string, object>
            //    {
            //        { "t" , nodeid.Take(2).ToArray()},
            //        { "y" , "q"},
            //        { "q" , "find_node"},
            //        { "a" , new Dictionary<string,object>
            //                {
            //                    { "id" , nodeid},
            //                    { "target" , nodeid}
            //                }
            //        }
            //    };
            //byte[] bytes = BencodeUtility.Encode(dic).ToArray();

            //string str = Encoding.ASCII.GetString(bytes);
            //Console.WriteLine(str);

            ////Test decode function
            //object obj = BencodeUtility.Decode(bytes);

            ////This is ok in this case
            //dic = BencodeUtility.DecodeDictionary(bytes);

            //var l = dic["t"];

            var spider = new Spider(new IPEndPoint(IPAddress.Any, 6881));
            spider.Start();

            Console.ReadKey();
        }
    }
}
