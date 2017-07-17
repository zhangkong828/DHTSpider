using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Spider.Core.UdpServer
{
    public delegate void MessageReceived(byte[] buffer, IPEndPoint endpoint);

    public class UDPService : IDisposable
    {
        private IPEndPoint endpoint;
        public IPEndPoint Endpoint
        {
            get { return endpoint; }
        }

        private Socket sock;
        public Socket Sock { get { return sock; } }

        /// <summary>
        /// 数据包管理
        /// </summary>
        private BufferManager BuffManagers;

        /// <summary>
        /// Socket异步对象池
        /// </summary>
        private SocketAsyncEventArgsPool SocketAsynPool;
        /// <summary>
        /// 接收包大小
        /// </summary>
        private int MaxBufferSize;
        /// <summary>
        /// 最大用户连接
        /// </summary>
        private int MaxConnectCout;

        private AutoResetEvent[] reset;

        public Thread ReceiveThread { get; private set; }

        public UDPService(IPEndPoint endpoint)
        {
            this.endpoint = endpoint;

            this.MaxBufferSize = 1024;//数据包最大缓冲区
            this.MaxConnectCout = 10000;//最大连接数

            this.reset = new AutoResetEvent[1];
            reset[0] = new AutoResetEvent(false);
        }

        public void Start()
        {
            Run();
        }

        private void Run()
        {
            if (isDisposed == true)
            {
                throw new ObjectDisposedException("UDPService is Disposed");
            }

            sock = new Socket(endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            sock.DontFragment = true;
            sock.EnableBroadcast = true;
            sock.Bind(endpoint);

            BuffManagers = new BufferManager(MaxConnectCout * MaxBufferSize, MaxBufferSize);
            BuffManagers.Inint();

            SocketAsynPool = new SocketAsyncEventArgsPool(MaxConnectCout);

            for (int i = 0; i < MaxConnectCout; i++)
            {
                SocketAsyncEventArgs socketasyn = new SocketAsyncEventArgs();
                socketasyn.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                socketasyn.Completed += new EventHandler<SocketAsyncEventArgs>(Asyn_Completed);
                SocketAsynPool.Push(socketasyn);
            }

            reset[0].Set();
            Receive();
        }

        private void Receive()
        {
            ReceiveThread = new System.Threading.Thread(new System.Threading.ThreadStart(() =>
            {
                while (true)
                {
                    System.Threading.WaitHandle.WaitAll(reset);
                    reset[0].Set();
                    if (SocketAsynPool.Count > 0)
                    {
                        SocketAsyncEventArgs sockasyn = SocketAsynPool.Pop();

                        if (BuffManagers.SetBuffer(sockasyn))
                        {
                            if (!Sock.ReceiveFromAsync(sockasyn))
                            {
                                BeginReceive(sockasyn);
                            }

                        }
                    }
                    else
                    {
                        reset[0].Reset();
                    }
                }
            }));

            ReceiveThread.Start();
        }

        private void Asyn_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.ReceiveFrom:
                case SocketAsyncOperation.Receive:
                    BeginReceive(e);
                    break;

            }

            e.AcceptSocket = null;
            BuffManagers.FreeBuffer(e);
            SocketAsynPool.Push(e);
            reset[0].Set();
        }

        public event MessageReceived MessageReceived;

        private void BeginReceive(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                byte[] data = new byte[e.BytesTransferred];

                Buffer.BlockCopy(e.Buffer, e.Offset, data, 0, data.Length);

                MessageReceived?.Invoke(data, (IPEndPoint)e.RemoteEndPoint);


            }
        }

        public void Send(IPEndPoint ipendpoint, byte[] data)
        {
            sock.BeginSendTo(data, 0, data.Length, SocketFlags.None, ipendpoint, SendCallBack, sock);
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

        #region 资源释放
        /// <summary>
        /// 用来确定是否以释放
        /// </summary>
        private bool isDisposed;


        ~UDPService()
        {
            this.Dispose(false);

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed || disposing)
            {
                try
                {
                    sock.Shutdown(SocketShutdown.Both);
                    sock.Close();

                    for (int i = 0; i < SocketAsynPool.Count; i++)
                    {
                        SocketAsyncEventArgs args = SocketAsynPool.Pop();

                        BuffManagers.FreeBuffer(args);
                    }


                    ReceiveThread.Abort();

                }
                catch
                {
                }

                isDisposed = true;
            }
        }
        #endregion

    }
}
