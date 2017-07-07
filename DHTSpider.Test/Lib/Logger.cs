using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHTSpider.Test.Lib
{
    public class Logger
    {
        private static ILogger logger = LogManager.GetCurrentClassLogger();

        public static void Error(string msg)
        {
            logger.Error(msg);
        }

        public static void Success(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Write(msg);
        }

        public static void Info(string msg)
        {
            logger.Info(msg);
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
