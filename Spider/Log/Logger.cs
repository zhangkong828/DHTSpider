using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spider.Log
{
    public class Logger
    {
        private static ILogger logger = LogManager.GetCurrentClassLogger();

        public static void Error(string msg)
        {
            logger.Error(msg);
        }

        public static void Info(string msg)
        {
            logger.Info(msg);
        }

        public static void Fatal(string msg)
        {
            logger.Fatal(msg);
        }

        public static void Debug(string msg)
        {
            logger.Debug(msg);
        }

        public static void Trace(string msg)
        {
            logger.Trace(msg);
        }

        public static void Warn(string msg)
        {
            logger.Warn(msg);
        }



        public static void ConsoleWrite(string msg, ConsoleColor consoleColor = ConsoleColor.Green)
        {
            Console.ForegroundColor = consoleColor;
            Console.WriteLine("------------------------------------------------------");
            Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}]{msg}");
        }
    }
}
