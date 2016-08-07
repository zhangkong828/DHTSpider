using DHTSpider.Core.Spider;
using DHTSpider.Core.Store.MongoDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Tancoder.Torrent.BEncoding;
using Tancoder.Torrent.Client;
using Tancoder.Torrent.Dht.Listeners;
using Tancoder.Torrent.Dht.Messages;

namespace DHTSpider.Client.Console
{
    public abstract class BaseMain
    {
        public readonly int initResolverCount = Setting.InitResolverThreadCount;
        public readonly int maxResolverCount = Setting.MaxResolverThreadCount;
        public List<Task> activeTasks = new List<Task>();
        public SeedCargo cargo;
        public DhtSpider spider;
        public BaseMain()
        {
            Init();
            InitDb();
        }

        public event EventHandler<string> OnDownLoadingData;

        public event EventHandler<string> OnDownLoadedData;

        public virtual void Run()
        {
            spider.Start();
            TaskFactory fact = new TaskFactory();
            for (int i = 0; i < initResolverCount; i++)
                activeTasks.Add(fact.StartNew(new Action<object>(MetadataResolver), false));
        }

        protected void Init()
        {
            spider = new DhtSpider(Setting.DhtSpiderSetting, new DhtListener(new IPEndPoint(IPAddress.Any, Setting.LocalPort)));
            spider.NewMetadata += Spider_NewMetadata;

            GetPeers.Hook = delegate (DhtMessage msg)
            {
                var m = msg as GetPeersResponse;
                var nid = spider.GetNeighborId(m.Id);
                m.Parameters["id"] = nid.BencodedString();
                return true;
            };

            var savepath = Path.Combine(Environment.CurrentDirectory, Setting.TorrentSavePath);
            if (Setting.IsSaveTorrent && !Directory.Exists(savepath))
            {
                Directory.CreateDirectory(savepath);
            }
        }

        protected void InitDb()
        {
            if (Setting.IsUsingMongoDb)
            {
                cargo = new SeedCargo();
                spider.Filter = new MongoFilter(cargo);
            }
        }
        protected abstract void MessageLoop_OnError(object sender, Exception e);

        private void MetadataResolver(object id)
        {
            while (true)
            {
                try
                {
                    var info = spider.Pop();
                    if (info.Key == null || info.Value == null)
                    {
                        if ((bool)(id ?? true))
                            return;
                        Thread.Sleep(1000);
                        continue;
                    }
                    var hash = BitConverter.ToString(info.Key.Hash).Replace("-", "");

                    OnDownLoadingData?.Invoke(Task.CurrentId, hash);

                    using (WireClient client = new WireClient(info.Value))
                    {
                        var metadata = client.GetMetaData(info.Key);
                        if (metadata != null)
                        {
                            var name = ((BEncodedString)metadata["name"]).Text;
                            if (Setting.IsSaveTorrent)
                                File.WriteAllBytes(Setting.TorrentSavePath + hash + ".torrent", metadata.Encode());

                            //var i = Seed.FromMetadata(metadata, info.Key.Hash).Info.ToString();
                            System.Console.WriteLine(hash + " : " + name);

                            if (Setting.IsUsingMongoDb)
                            {
                                cargo.Add(metadata, info.Key.Hash);
                            }
                            OnDownLoadedData?.Invoke(Task.CurrentId, hash);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageLoop_OnError(this, ex);
                }
                finally
                {

                }
            }
        }

        private void Spider_NewMetadata(object sender, Tancoder.Torrent.NewMetadataEventArgs e)
        {
            if (activeTasks.Count <= maxResolverCount && spider.GetWaitSeedsCount() > Setting.WaitSeedsCount)
            {
                var task = Task.Run(() =>
                {
                    MetadataResolver(true);
                });
                activeTasks.Add(task);
                task.ContinueWith(n => activeTasks.Remove(task));

            }
        }
    }
}
