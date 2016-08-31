# DHTSpider
>A Very Simple Spider With DHT Crawler, Written by C#.

* The bittorrent library bases on DhtWalker,MonoTorrent.




## 介绍

DHTSpider 是一个由C#编写的 DHT 爬虫, 从全球` DHT ` 网络里"`嗅探`"正在下载的资源, 并把资源`metadata`(种子信息)进行抓取下载.


## 相关

使用 `MongoDB` 进行数据存储.

使用 `ElasticSearch` 进行全文检索(待定).


## 环境

1. `.net` 版本 `>=4.5.2`

2. 运行的机器需要`独立IP` , 能够访问外网 ( `翻墙` )


## 参数

```
Setting.InitSetting();//初始化相关参数
```

```
    
    LocalPort = 6881;

    IsSaveTorrent = true;

    TorrentSavePath = @"torrent\";

    IsUsingMongoDb = false;

    InitResolverThreadCount = 10;

    MaxResolverThreadCount = 50;

    WaitSeedsCount = 100;


    //DhtSpiderSetting

    DhtSpiderSetting.MaxSendQueue = 50;
    DhtSpiderSetting.MaxFindSendPer = 100;
    DhtSpiderSetting.MaxWaitCount = 1000;
    DhtSpiderSetting.MaxCacheCount = 5000;
    
```

## 感谢
项目开发中，`bittorrent`相关类库参考了 `DhtWalker` 和 `MonoTorrent`, 在此感谢作者 →_→
