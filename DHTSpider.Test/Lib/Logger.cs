using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHTSpider.Test.Lib
{
    public class Logger
    {
        public static void Error(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Write(msg);
        }

        public static void Success(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Write(msg);
        }

        public static void Info(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Write(msg);
        }

        public static void Default(string msg)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Write(msg);
        }

        private static void Write(string msg)
        {
            Console.WriteLine("------------------------------------------------------");
            Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}]{msg}");
        }
    }
}
