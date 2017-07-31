# DHTSpider
>A Very Simple Spider With DHT Crawler, Written by C#.

* The bittorrent library bases on MonoTorrent.

### 介绍

DHTSpider 是一个由C#编写的 DHT 爬虫, 从全球` DHT ` 网络里"`嗅探`"正在下载的资源, 并把资源`metadata`(种子信息)进行抓取下载.


### 相关

>Cache

+ 使用 `Memory` > UseDefaultCache ( 默认 )
+ 使用 `Redis` > UseRedisCache  ( TODO )

>Queue

+ 使用 `Memory` > UseDefaultQueue （ 默认 ）
+ 使用 `Redis` > UseRedisQueue  ( TODO )

>Store

+ 使用 `MongoDB` > UseMongoDBStore （ TODO ）
+ 使用 `ElasticSearch` > UseElasticSearchStore  ( TODO )

>Log > `NLog`

>Ioc > `Autofac`

### 环境

1. `.net` 版本 `>=4.5.2`

2. 运行的机器需要`独立IP` , 内网机器需要做下`端口映射`


### 使用

```c#

    var spider = SpiderConfiguration.Create() //使用默认配置
    .UseDefaultCache() //默认使用内存缓存
    .UseDefaultQueue() //默认使用内存队列
    .Start();

```

### 配置

```c#
    
    var spider = SpiderConfiguration.Create(new SpiderSetting()
    {
        LocalPort = 6881, //使用端口
        IsSaveTorrent = true, //是否保存torrent
        TorrentSavePath = "", //torrent保存路径
        MaxSpiderThreadCount = 1, //爬虫线程数
        MaxDownLoadThreadCount = 20 //下载线程数
    })
    .UseRedisCache() //使用redis缓存
    .UseRedisQueue() //使用redis队列
    .UseMongoDBStore() //使用mongodb存储
    .Start();
    
```

