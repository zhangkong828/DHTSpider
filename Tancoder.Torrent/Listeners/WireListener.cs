using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tancoder.Torrent.BEncoding;
using Tancoder.Torrent.Messages;
using Tancoder.Torrent.Messages.Wire;

namespace Tancoder.Torrent.Client
{
    public class WireClient : IDisposable
    {
        private TcpClient client;
        private NetworkStream stream;
        public IPEndPoint EndPoint { get; private set; }
        public long MaxMetadataSize { get; set; } = 1024 * 1024;
        static readonly long PieceLength = 16 * 1024;

        public WireClient(IPEndPoint endpoint)
        {
            //client = new TcpClient(new IPEndPoint(IPAddress.Any, 60000))
            client = new TcpClient()
            {
                SendTimeout = 5000,
                ReceiveTimeout = 5000,
            };
            EndPoint = endpoint;
        }
        public WireClient(IPEndPoint endpoint, int localPort)
        {
            client = new TcpClient(new IPEndPoint(IPAddress.Any, localPort))
            {
                SendTimeout = 50000,
                ReceiveTimeout = 50000,
            };
            EndPoint = endpoint;
        }

        public BEncodedDictionary GetMetaData(InfoHash hash)
        {
            WireMessage message;
            ExtHandShack exths;
            long metadataSize;
            int piecesNum;
            byte[] metadata;
            byte ut_metadata;

            try
            {
                //连接
                if (!client.ConnectAsync(EndPoint.Address, EndPoint.Port).Wait(5000))
                {
                    Trace.WriteLine("Connect Timeout", "Socket");
                    return null;
                }
                stream = client.GetStream();

                //发送握手
                message = new HandShack(hash);
                SendMessage(message);

                //接受握手
                message = ReceiveMessage<HandShack>(1);
                if (!message.Legal || !(message as HandShack).SupportExtend)
                {
                    Trace.WriteLine(EndPoint, "HandShack Fail");
                    return null;
                }

                //发送拓展握手
                message = new ExtHandShack() { SupportUtMetadata = true };
                SendMessage(message);

                //接受拓展
                exths = ReceiveMessage<ExtHandShack>();
                if (!exths.Legal || !exths.CanGetMetadate || exths.MetadataSize > MaxMetadataSize || exths.MetadataSize <= 0)
                {
                    Trace.WriteLine(EndPoint, "ExtendHandShack Fail");
                    return null;
                }
                metadataSize = exths.MetadataSize;
                ut_metadata = exths.UtMetadata;
                piecesNum = (int)Math.Ceiling(metadataSize / (decimal)PieceLength);

                //开始接受pieces
                var rtask = ReceivePiecesAsync(metadataSize, piecesNum);
                //开始发送piece请求
                for (int i = 0; i < piecesNum; i++)
                {
                    message = new ExtQueryPiece(ut_metadata, i);
                    SendMessage(message);
                }
                //等待pieces接收完毕
                rtask.Wait();
                metadata = rtask.Result;

                if (metadata == null)
                    return null;

                //检查hash值是否正确
                var sha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider();
                byte[] infohash = sha1.ComputeHash(metadata);
                if (!infohash.SequenceEqual(hash.Hash))
                {
                    Trace.WriteLine(EndPoint, "Hash Wrong");
                    return null;
                }

                return BEncodedDictionary.DecodeTorrent(metadata);
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
            finally
            {
                if (client != null)
                    client.Close();
            }
        }

        public byte[] GetMetaDataBytes(InfoHash hash)
        {
            WireMessage message;
            ExtHandShack exths;
            long metadataSize;
            int piecesNum;
            byte[] metadata;
            byte ut_metadata;

            try
            {
                //连接
                if (!client.ConnectAsync(EndPoint.Address, EndPoint.Port).Wait(5000))
                {
                    Trace.WriteLine("Connect Timeout", "Socket");
                    return null;
                }
                stream = client.GetStream();

                //发送握手
                message = new HandShack(hash);
                SendMessage(message);

                //接受握手
                message = ReceiveMessage<HandShack>(1);
                if (!message.Legal || !(message as HandShack).SupportExtend)
                {
                    Trace.WriteLine(EndPoint, "HandShack Fail");
                    return null;
                }

                //发送拓展握手
                message = new ExtHandShack() { SupportUtMetadata = true };
                SendMessage(message);

                //接受拓展
                exths = ReceiveMessage<ExtHandShack>();
                if (!exths.Legal || !exths.CanGetMetadate || exths.MetadataSize > MaxMetadataSize || exths.MetadataSize <= 0)
                {
                    Trace.WriteLine(EndPoint, "ExtendHandShack Fail");
                    return null;
                }
                metadataSize = exths.MetadataSize;
                ut_metadata = exths.UtMetadata;
                piecesNum = (int)Math.Ceiling(metadataSize / (decimal)PieceLength);

                //开始接受pieces
                var rtask = ReceivePiecesAsync(metadataSize, piecesNum);
                //开始发送piece请求
                for (int i = 0; i < piecesNum; i++)
                {
                    message = new ExtQueryPiece(ut_metadata, i);
                    SendMessage(message);
                }
                //等待pieces接收完毕
                rtask.Wait();
                metadata = rtask.Result;

                if (metadata == null)
                    return null;

                //检查hash值是否正确
                var sha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider();
                byte[] infohash = sha1.ComputeHash(metadata);
                if (!infohash.SequenceEqual(hash.Hash))
                {
                    Trace.WriteLine(EndPoint, "Hash Wrong");
                    return null;
                }

                return metadata;
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
            finally
            {
                if (client != null)
                    client.Close();
            }
        }

        private Task<byte[]> ReceivePiecesAsync(long metadataSize, int piecesNum)
        {
            Trace.Assert(metadataSize > 0);
            return Task.Run(() =>
            {
                try
                {
                    int tryed = 0;
                    byte[] metadata = new byte[metadataSize];
                    BitArray flags = new BitArray(piecesNum);
                    for (int i = 0; i < piecesNum && tryed < 10;)
                    {
                        ExtData data = new ExtData();
                        data = ReceiveMessage<ExtData>();
                        if (data.Legal)
                        {
                            if (data.Data.Length > 0 && !flags[data.PieceID])
                            {
                                var index = data.PieceID;
                                flags.Set(index, true);

                                data.Data.CopyTo(metadata, index * PieceLength);
                                i++;
                            }
                            else
                            {
                                Trace.WriteLine(EndPoint, "Received Empty Data");
                                return null;
                            }
                        }
                        else
                        {
                            Thread.Sleep(100);
                            tryed++;
                        }
                    }
                    for (int i = 0; i < flags.Length; i++)
                    {
                        if (!flags[i])
                        {
                            Trace.WriteLine(EndPoint, "Not Received All pieces");
                            return null;
                        }
                    }
                    return metadata;
                }
                catch (System.IO.IOException)
                {
                    Trace.WriteLine(EndPoint, "Socket Closed");
                    return null;
                }
            });
        }

        public void SendMessage(WireMessage message)
        {
            byte[] buffer = message.Encode();
            stream.Write(buffer, 0, buffer.Length);
        }

        public T ReceiveMessage<T>(int head = 4) where T : WireMessage, new()
        {
            T msg = new T();
            int tmp;
            byte[] buffer;

            while (true)
            {
                //获取数据长度头
                buffer = new byte[head];
                tmp = stream.Read(buffer, 0, head);
                if (tmp == 0)//未获得数据长度头 返回
                {
                    Trace.WriteLine(EndPoint, "No Message to Get");
                    return msg;
                }
                msg.OnMessageLength(buffer);//计算数据包大小
                if (msg.Length == 0)
                {
                    Trace.WriteLine(EndPoint, "Get KeepLive");
                    continue;
                }
                if (msg.Length > 32 * 1024 || msg.Length < 0)//数据包不可能过大
                {
                    Trace.WriteLine(EndPoint, "Invaild Length");
                    return msg;
                }
                buffer = new byte[msg.OnMessageLength(buffer)];

                int rlen = buffer.Length;
                while (rlen > 0)//获取数据主体
                {
                    int size = stream.Read(buffer, buffer.Length - rlen, rlen);
                    if (size == 0)
                        return msg;
                    rlen -= size;
                }

                if (msg.CheackHead(buffer, 0))//数据包符合要求
                    break;
            }

            msg.Decode(buffer, 0, buffer.Length);
            Trace.WriteLineIf(!msg.Legal, new { Length = msg.Length, ID = buffer[0], Endpoint = EndPoint }, "No Required Message");

            return msg;
        }

        public void Dispose()
        {
            client.Close();
        }
    }
}
