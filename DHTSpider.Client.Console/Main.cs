using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHTSpider.Core.Spider;

namespace DHTSpider.Client.Console
{
    public class Main : BaseMain
    {
        protected override void MessageLoop_OnError(object sender, Exception e)
        {
            System.Console.WriteLine(e.Message);
        }

        public override void Run()
        {
            base.Run();
            OnDownLoadingData += MainConsole_OnDownLoadingData;
            OnDownLoadedData += MainConsole_OnDownLoadedData;
        }
        
        private void MainConsole_OnDownLoadingData(object sender, string e)
        {
            System.Console.WriteLine(e);
        }
        
        private void MainConsole_OnDownLoadedData(object sender, string e)
        {
            System.Console.WriteLine(e);
        }
    }
}
