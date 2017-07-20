using Spider.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Core.UdpServer
{
    public delegate void MessageReceived(byte[] buffer, IPEndPoint endpoint);

    public class AsyncUDPServer
    {
        private UdpClient _server;

        private IPEndPoint endpoint;
        public IPEndPoint Endpoint
        {
            get { return endpoint; }
        }

        public bool IsRunning { get; private set; }

        public AsyncUDPServer(IPEndPoint endpoint)
        {
            this.endpoint = endpoint;
            _server = new UdpClient(endpoint);
            const uint IOC_IN = 0x80000000;
            int IOC_VENDOR = 0x18000000;
            int SIO_UDP_CONNRESET = (int)(IOC_IN | IOC_VENDOR | 12);
            _server.Client.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, new byte[4]);
        }

        public void Start()
        {
            if (!IsRunning)
            {
                IsRunning = true;
                _server.EnableBroadcast = true;
                _server.BeginReceive(ReceiveDataAsync, null);
            }
        }

        public void Stop()
        {
            if (IsRunning)
            {
                IsRunning = false;
                _server.Close();

            }
        }


        public event MessageReceived MessageReceived;

        private void ReceiveDataAsync(IAsyncResult ar)
        {
            IPEndPoint remote = null;
            byte[] buffer = null;
            try
            {
                buffer = _server.EndReceive(ar, ref remote);
                MessageReceived?.Invoke(buffer, remote);
                if (IsRunning && _server != null)
                    _server.BeginReceive(ReceiveDataAsync, null);
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.ToString());
                if (IsRunning && _server != null)
                    _server.BeginReceive(ReceiveDataAsync, null);

            }
        }



        /// <summary>  
        /// 发送数据  
        /// </summary>  
        public void Send(byte[] buffer, IPEndPoint endpoint)
        {
            try
            {
                if (endpoint.Address != IPAddress.Any)
                    _server.Send(buffer, buffer.Length, endpoint);
            }
            catch (Exception)
            {
                //TODO 异常处理
            }
        }


    }
}
