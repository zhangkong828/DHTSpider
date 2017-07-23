using Spider.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Core
{
    public delegate void MessageReceived(byte[] buffer, IPEndPoint endpoint);

    public class UdpSocketListener
    {
        private IPEndPoint endpoint;
        public IPEndPoint Endpoint
        {
            get { return endpoint; }
        }

        private Socket m_ListenSocket;

        private SocketAsyncEventArgs m_ReceiveSAE;

        public UdpSocketListener(IPEndPoint endpoint)
        {
            this.endpoint = endpoint;
        }

        public void Start()
        {
            try
            {
                m_ListenSocket = new Socket(this.endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                m_ListenSocket.Ttl = 255;
                m_ListenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                m_ListenSocket.Bind(this.endpoint);

                //Mono 不支持
                //if (Platform.SupportSocketIOControlByCodeEnum)
                {
                    uint IOC_IN = 0x80000000;
                    uint IOC_VENDOR = 0x18000000;
                    uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;

                    byte[] optionInValue = { Convert.ToByte(false) };
                    byte[] optionOutValue = new byte[4];
                    m_ListenSocket.IOControl((int)SIO_UDP_CONNRESET, optionInValue, optionOutValue);
                }

                var eventArgs = new SocketAsyncEventArgs();
                m_ReceiveSAE = eventArgs;

                eventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(eventArgs_Completed);
                eventArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

                int receiveBufferSize = 2048;
                var buffer = new byte[receiveBufferSize];
                eventArgs.SetBuffer(buffer, 0, buffer.Length);

                m_ListenSocket.ReceiveFromAsync(eventArgs);
            }
            catch (Exception ex)
            {
                OnError(ex);

            }
        }
        public event MessageReceived MessageReceived;
        private void eventArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                var errorCode = (int)e.SocketError;

                //The listen socket was closed
                if (errorCode == 995 || errorCode == 10004 || errorCode == 10038)
                    //return;
                    Logger.Fatal($"The listen socket was closed  errorCode:{errorCode}");
                Logger.Fatal($"errorCode:{errorCode}");
            }

            if (e.LastOperation == SocketAsyncOperation.ReceiveFrom)
            {
                try
                {
                    //获取接收到的数据
                    byte[] ByteArray = new byte[e.BytesTransferred];
                    Array.Copy(e.Buffer, 0, ByteArray, 0, ByteArray.Length);
                    //测试
                    Logger.Debug($"buffer:{ByteArray.Length}   remote:{(IPEndPoint)e.RemoteEndPoint}");
                    MessageReceived?.Invoke(ByteArray, (IPEndPoint)e.RemoteEndPoint);
                }
                catch (Exception exc)
                {
                    OnError(exc);
                }

                try
                {
                    m_ListenSocket.ReceiveFromAsync(e);
                }
                catch (Exception exc)
                {
                    OnError(exc);
                }
            }
        }


        private void OnError(Exception ex)
        {
            Logger.Fatal(ex.Message + ex.StackTrace);
            //throw ex;
        }

        public void Stop()
        {
            if (m_ListenSocket == null)
                return;

            lock (this)
            {
                if (m_ListenSocket == null)
                    return;

                m_ReceiveSAE.Completed -= new EventHandler<SocketAsyncEventArgs>(eventArgs_Completed);
                m_ReceiveSAE.Dispose();
                m_ReceiveSAE = null;

                //if (!Platform.IsMono)
                {
                    try
                    {
                        m_ListenSocket.Shutdown(SocketShutdown.Both);
                    }
                    catch { }
                }

                try
                {
                    m_ListenSocket.Close();
                }
                catch { }
                finally
                {
                    m_ListenSocket = null;
                }
            }

        }


        public void Send(byte[] buffer, IPEndPoint endpoint)
        {
            try
            {
                if (endpoint.Address != IPAddress.Any)
                {
                    var len = m_ListenSocket.SendTo(buffer, endpoint);

                    //测试
                    Logger.Warn($"Send :{len}  {buffer.Length} {endpoint}");
                }
                else
                {
                    Logger.Fatal($"Send Not Work {endpoint.ToString()}");
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

    }
}
