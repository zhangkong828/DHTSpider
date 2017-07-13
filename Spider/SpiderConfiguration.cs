using Autofac;
using Spider.Core;
using Spider.Log;
using Spider.Queue;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tancoder.Torrent;
using Tancoder.Torrent.BEncoding;
using Tancoder.Torrent.Client;

namespace Spider
{
    public class SpiderConfiguration
    {
        private readonly ContainerBuilder _builder;
        private IContainer _container;

        private static readonly object obj = new object();
        private static SpiderConfiguration _instance = null;

        public SpiderSetting _option { get; set; }
        public IQueue _queue { get; set; }

        private SpiderConfiguration(SpiderSetting option)
        {
            _option = option;
            _builder = new ContainerBuilder();

            if (_option.IsSaveTorrent)
            {
                if (string.IsNullOrEmpty(_option.TorrentSavePath) || !Directory.Exists(_option.TorrentSavePath))
                {
                    _option.TorrentSavePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "torrent");
                    Directory.CreateDirectory(_option.TorrentSavePath);
                }
            }
        }

        public static SpiderConfiguration Create(SpiderSetting option = null)
        {
            if (_instance == null)
            {
                lock (obj)
                {
                    if (_instance == null)
                    {
                        _instance = new SpiderConfiguration(option ?? new SpiderSetting());
                    }
                }
            }
            return _instance;
        }

        public SpiderConfiguration UseMemoryQueue()
        {
            _builder.RegisterType<MemoryQueue>().As<IQueue>().Named<IQueue>("Queue").SingleInstance();
            return _instance;
        }

        public SpiderConfiguration UseRedisQueue()
        {
            _builder.RegisterType<RedisQueue>().As<IQueue>().Named<IQueue>("Queue").SingleInstance();
            return _instance;
        }

        public SpiderConfiguration UseElasticSearchStore()
        {
            return _instance;
        }

        public SpiderConfiguration UseMongoDBStore()
        {
            return _instance;
        }

        public void Stop()
        {

        }

        public SpiderConfiguration Start()
        {
            _container = _builder.Build();
            if (!_container.IsRegisteredWithName<IQueue>("Queue"))
            {
                throw new Exception("没有注册Queue");
            }

            _queue = _container.ResolveNamed<IQueue>("Queue");

            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000 * 60);
                    Logger.ConsoleWrite($"QueueCount:{_queue.Count()}");
                    Logger.Info($"QueueCount:{_queue.Count()}");
                }
            });

            for (var i = 0; i < _option.MaxSpiderThreadCount; i++)
            {
                var port = _option.LocalPort + i;
                Logger.ConsoleWrite($"抓取线程：{i + 1} 端口：{port} 已启动...");
                Task.Run(() =>
                {
                    var spider = new DHTSpider(new IPEndPoint(IPAddress.Any, port));
                    spider.NewMetadata += DHTSpider_NewMetadata;
                    spider.Start();
                });
                Thread.Sleep(1000 * 2);
            }

            for (var i = 0; i < _option.MaxDownLoadThreadCount; i++)
            {
                var id = i + 1;
                Logger.ConsoleWrite($"线程[{id}]已启动下载");
                Task.Run(() =>
                {
                    Download(id);
                });
                Thread.Sleep(1000);
            }


            return _instance;
        }



        private void DHTSpider_NewMetadata(object sender, NewMetadataEventArgs e)
        {
            _queue.Enqueue(new KeyValuePair<InfoHash, IPEndPoint>(e.Metadata, e.Owner));

            Console.ForegroundColor = ConsoleColor.Green;
            Logger.ConsoleWrite($"NewMetadata    Hash:{e.Metadata} Address:{e.Owner.ToString()}");

        }

        private void Download(int threadId)
        {
            while (true)
            {
                try
                {
                    var info = _queue.Dequeue();
                    if (info.Key == null || info.Value == null)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                    var hash = BitConverter.ToString(info.Key.Hash).Replace("-", "");
                    using (WireClient client = new WireClient(info.Value))
                    {
                        var metadata = client.GetMetaData(info.Key);
                        if (metadata != null)
                        {
                            var name = ((BEncodedString)metadata["name"]).Text;
                            if (_option.IsSaveTorrent)
                            {
                                var filepath = $"{_option.TorrentSavePath}\\{hash}.torrent";
                                File.WriteAllBytes(filepath, metadata.Encode());
                            }
                            var list = new List<string>();
                            //foreach (var item in metadata)
                            //{
                            //    list.Add($"[key]={item.Key}[val]={item.Value}");
                            //}
                            //var str = string.Join("&", list);
                            //Logger.Warn($"{hash} {name} {str}");
                            Logger.ConsoleWrite($"线程[{threadId}]下载完成    Hash:{hash}    Name:{name} ");
                        }
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error($"Download {ex.Message}");
                }
            }
        }
    }
}
