using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHTSpider.Client.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            //初始化参数
            Setting.InitSetting();
            //启动
            var main = new Main();
            main.Run();
        }
    }
}
