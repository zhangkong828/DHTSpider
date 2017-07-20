using Spider.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Spider.Core.IoSocket
{

    public delegate void MessageReceived(byte[] buffer, IPEndPoint endpoint);
    /// <summary>
    /// 基于SocketAsyncEventArgs 实现 IOCP 服务器
    /// </summary>
    internal sealed class IoServer
    {
        /// <summary>
        /// 监听Socket，用于接受客户端的连接请求
        /// </summary>
        private Socket listenSocket;

        /// <summary>
        /// 用于服务器执行的互斥同步对象
        /// </summary>
        private static Mutex mutex = new Mutex();

        /// <summary>
        /// 用于每个I/O Socket操作的缓冲区大小
        /// </summary>
        private Int32 bufferSize;

        /// <summary>
        /// 服务器上连接的客户端总数
        /// </summary>
        private Int32 numConnectedSockets;

        /// <summary>
        /// 服务器能接受的最大连接数量
        /// </summary>
        private Int32 numConnections;

        /// <summary>
        /// 完成端口上进行投递所用的IoContext对象池
        /// </summary>
        private IoContextPool ioContextPool;

        private IPEndPoint endpoint;

        /// <summary>
        /// 构造函数，建立一个未初始化的服务器实例
        /// </summary>
        /// <param name="numConnections">服务器的最大连接数据</param>
        /// <param name="bufferSize"></param>
        internal IoServer(IPEndPoint endpoint)
        {
            this.endpoint = endpoint;
            this.numConnectedSockets = 0;
            this.numConnections = 10000;
            this.bufferSize = 1024;

            this.ioContextPool = new IoContextPool(numConnections);

            // 为IoContextPool预分配SocketAsyncEventArgs对象
            for (Int32 i = 0; i < this.numConnections; i++)
            {
                SocketAsyncEventArgs ioContext = new SocketAsyncEventArgs();
                ioContext.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
                ioContext.SetBuffer(new Byte[this.bufferSize], 0, this.bufferSize);

                // 将预分配的对象加入SocketAsyncEventArgs对象池中
                this.ioContextPool.Add(ioContext);
            }
        }

        /// <summary>
        /// 当Socket上的发送或接收请求被完成时，调用此函数
        /// </summary>
        /// <param name="sender">激发事件的对象</param>
        /// <param name="e">与发送或接收完成操作相关联的SocketAsyncEventArg对象</param>
        private void OnIOCompleted(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.ReceiveFrom:
                case SocketAsyncOperation.Receive:
                    this.ProcessReceive(e);
                    break;
            }
            ioContextPool.Add(e);
        }

        public event MessageReceived MessageReceived;

        /// <summary>
        ///接收完成时处理函数
        /// </summary>
        /// <param name="e">与接收完成操作相关联的SocketAsyncEventArg对象</param>
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            // 检查远程主机是否关闭连接
            if (e.BytesTransferred > 0)
            {
                if (e.SocketError == SocketError.Success)
                {
                    Socket s = (Socket)e.UserToken;
                    //判断所有需接收的数据是否已经完成
                    if (s.Available == 0)
                    {

                        //获取接收到的数据
                        byte[] ByteArray = new byte[e.BytesTransferred];
                        Array.Copy(e.Buffer, 0, ByteArray, 0, ByteArray.Length);

                        MessageReceived?.Invoke(ByteArray, (IPEndPoint)e.RemoteEndPoint);

                    }
                    else if (!s.ReceiveAsync(e))    //为接收下一段数据，投递接收请求，这个函数有可能同步完成，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件
                    {
                        // 同步接收时处理接收完成事件
                        this.ProcessReceive(e);
                    }
                }
                else
                {
                    this.ProcessError(e);
                }
            }
            else
            {
                this.CloseClientSocket(e);
            }
        }



        /// <summary>
        /// 处理socket错误
        /// </summary>
        /// <param name="e"></param>
        private void ProcessError(SocketAsyncEventArgs e)
        {
            Socket s = e.UserToken as Socket;
            IPEndPoint localEp = s.LocalEndPoint as IPEndPoint;

            this.CloseClientSocket(s, e);

            string outStr = String.Format("套接字错误 {0}, IP {1}, 操作 {2}。", (Int32)e.SocketError, localEp, e.LastOperation);

            Logger.Fatal(outStr);
        }

        /// <summary>
        /// 关闭socket连接
        /// </summary>
        /// <param name="e">SocketAsyncEventArg associated with the completed send/receive operation.</param>
        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            Socket s = e.UserToken as Socket;
            this.CloseClientSocket(s, e);
        }

        private void CloseClientSocket(Socket s, SocketAsyncEventArgs e)
        {
            Interlocked.Decrement(ref this.numConnectedSockets);

            // SocketAsyncEventArg 对象被释放，压入可重用队列。
            this.ioContextPool.Push(e);
            string outStr = String.Format("客户 {0} 断开, 共有 {1} 个连接。", s.RemoteEndPoint.ToString(), this.numConnectedSockets);
            Logger.Fatal(outStr);
            try
            {
                s.Shutdown(SocketShutdown.Send);
            }
            catch (Exception)
            {
                // Throw if client has closed, so it is not necessary to catch.
            }
            finally
            {
                s.Close();
            }
        }

        /// <summary>
        /// 启动服务，开始监听
        /// </summary>
        /// <param name="port">Port where the server will listen for connection requests.</param>
        internal void Start()
        {
            // 创建监听socket
            this.listenSocket = new Socket(endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            //this.listenSocket.ReceiveBufferSize = this.bufferSize;
            //this.listenSocket.SendBufferSize = this.bufferSize;

            listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            listenSocket.DontFragment = true;
            listenSocket.EnableBroadcast = true;
            listenSocket.Bind(endpoint);

            // Blocks the current thread to receive incoming messages.
            //mutex.WaitOne();

            Receive();
        }

        private void Receive()
        {
            var ReceiveThread = new Thread(new ThreadStart(() =>
            {
                while (true)
                {
                    if (ioContextPool.Count > 0)
                    {
                        SocketAsyncEventArgs ioContext = this.ioContextPool.Pop();
                        if (!listenSocket.ReceiveFromAsync(ioContext))
                        {
                            ProcessReceive(ioContext);
                        }
                    }
                }
            }));

            ReceiveThread.Start();
        }


        /// <summary>
        /// 停止服务
        /// </summary>
        internal void Stop()
        {
            this.listenSocket.Close();
            //mutex.ReleaseMutex();
        }


        internal void Send(IPEndPoint ipendpoint, byte[] data)
        {
            listenSocket.BeginSendTo(data, 0, data.Length, SocketFlags.None, ipendpoint, SendCallBack, listenSocket);
        }

        private void SendCallBack(IAsyncResult result)
        {
            try
            {
                Socket sock = result.AsyncState as Socket;

                if (sock != null)
                {
                    sock.EndSend(result);
                    Logger.Warn("SendCallBack");
                }
            }
            catch
            {

            }
        }
    }
}
