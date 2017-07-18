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
            // Determine which type of operation just completed and call the associated handler.
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    this.ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    this.ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
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

                        ////设置发送的数据（原样返回）
                        //Array.Copy(e.Buffer, 0, e.Buffer, e.BytesTransferred, e.BytesTransferred);
                        //e.SetBuffer(e.Offset, e.BytesTransferred);


                        //if (!s.SendAsync(e))        //投递发送请求，这个函数有可能同步发送出去，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件
                        //{
                        //    // 同步发送时处理发送完成事件
                        //    this.ProcessSend(e);
                        //}
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
        /// 发送完成时处理函数
        /// </summary>
        /// <param name="e">与发送完成操作相关联的SocketAsyncEventArg对象</param>
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                Socket s = (Socket)e.UserToken;

                //接收时根据接收的字节数收缩了缓冲区的大小，因此投递接收请求时，恢复缓冲区大小
                e.SetBuffer(0, bufferSize);
                if (!s.ReceiveAsync(e))     //投递接收请求
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

            Logger.Error(outStr);
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
            Logger.Error(outStr);
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
                s.Dispose();
            }
        }

        /// <summary>
        /// accept 操作完成时回调函数
        /// </summary>
        /// <param name="sender">Object who raised the event.</param>
        /// <param name="e">SocketAsyncEventArg associated with the completed accept operation.</param>
        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            this.ProcessAccept(e);
        }

        /// <summary>
        /// 监听Socket接受处理
        /// </summary>
        /// <param name="e">SocketAsyncEventArg associated with the completed accept operation.</param>
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            Socket s = e.AcceptSocket;
            if (s.Connected)
            {
                try
                {
                    SocketAsyncEventArgs ioContext = this.ioContextPool.Pop();
                    if (ioContext != null)
                    {
                        // 从接受的客户端连接中取数据配置ioContext

                        ioContext.UserToken = s;

                        Interlocked.Increment(ref this.numConnectedSockets);
                        string outStr = String.Format("客户 {0} 连入, 共有 {1} 个连接。", s.RemoteEndPoint.ToString(), this.numConnectedSockets);
                        Logger.Error(outStr);

                        if (!s.ReceiveAsync(ioContext))
                        {
                            this.ProcessReceive(ioContext);
                        }
                    }
                    else        //已经达到最大客户连接数量，在这接受连接，发送“连接已经达到最大数”，然后断开连接
                    {
                        //s.Send(Encoding.Default.GetBytes("连接已经达到最大数!"));
                        string outStr = String.Format("连接已满，拒绝 {0} 的连接。", s.RemoteEndPoint);
                        Logger.Error(outStr);
                        s.Close();
                    }
                }
                catch (SocketException ex)
                {
                    Socket token = e.UserToken as Socket;
                    string outStr = String.Format("接收客户 {0} 数据出错, 异常信息： {1} 。", token.RemoteEndPoint, ex.ToString());
                    Logger.Error(outStr);
                }
                catch (Exception ex)
                {
                    Logger.Error("异常：" + ex.ToString());
                }
                // 投递下一个接受请求
                this.StartAccept(e);
            }
        }

        /// <summary>
        /// 从客户端开始接受一个连接操作
        /// </summary>
        /// <param name="acceptEventArg">The context object to use when issuing 
        /// the accept operation on the server's listening socket.</param>
        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            }
            else
            {
                // 重用前进行对象清理
                acceptEventArg.AcceptSocket = null;
            }

            if (!this.listenSocket.AcceptAsync(acceptEventArg))
            {
                this.ProcessAccept(acceptEventArg);
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
            this.listenSocket.ReceiveBufferSize = this.bufferSize;
            this.listenSocket.SendBufferSize = this.bufferSize;

            if (endpoint.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // 配置监听socket为 dual-mode (IPv4 & IPv6) 
                // 27 is equivalent to IPV6_V6ONLY socket option in the winsock snippet below,
                this.listenSocket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
                this.listenSocket.Bind(new IPEndPoint(IPAddress.IPv6Any, endpoint.Port));
            }
            else
            {
                this.listenSocket.Bind(endpoint);
            }


            // 在监听Socket上投递一个接受请求。
            this.StartAccept(null);

            // Blocks the current thread to receive incoming messages.
            mutex.WaitOne();
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        internal void Stop()
        {
            this.listenSocket.Close();
            mutex.ReleaseMutex();
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
                }
            }
            catch
            {

            }
        }
    }
}
