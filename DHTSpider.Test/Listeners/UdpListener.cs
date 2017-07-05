using DHTSpider.Test.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DHTSpider.Test.Listeners
{
    public delegate void MessageReceived(byte[] buffer, IPEndPoint endpoint);

    public class UdpListener
    {
        public event MessageReceived MessageReceived;

        public UdpListener(IPEndPoint endpoint)
        {
            this._endpoint = endpoint;
        }

        private IPEndPoint _endpoint;
        public IPEndPoint Endpoint
        {
            get { return _endpoint; }
        }

        private UdpClient client;
        public void Start()
        {
            try
            {
                client = new UdpClient(Endpoint);
                client.BeginReceive(EndReceive, null);
            }
            catch (SocketException)
            {

            }
            catch (ObjectDisposedException)
            {

            }
        }

        public void Stop()
        {
            try
            {
                client.Close();
            }
            catch
            {

            }
        }

        private void EndReceive(IAsyncResult result)
        {
            try
            {
                IPEndPoint e = new IPEndPoint(IPAddress.Any, Endpoint.Port);
                byte[] buffer = client.EndReceive(result, ref e);
                OnMessageReceived(buffer, e);
                client.BeginReceive(EndReceive, null);
            }
            catch (ObjectDisposedException)
            {

            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode == 10054)
                {
                    while (true)
                    {
                        try
                        {
                            client.BeginReceive(EndReceive, null);
                            return;
                        }
                        catch (ObjectDisposedException)
                        {
                            return;
                        }
                        catch (SocketException e)
                        {
                            if (e.ErrorCode != 10054)
                                return;
                        }
                    }
                }
            }
        }
        private void OnMessageReceived(byte[] buffer, IPEndPoint endpoint)
        {
            MessageReceived?.Invoke(buffer, endpoint);
        }


        public void Send(byte[] buffer, IPEndPoint endpoint)
        {
            try
            {
                if (endpoint.Address != IPAddress.Any)
                    client.Send(buffer, buffer.Length, endpoint);
                Logger.Info($"{endpoint.ToString()} {buffer.Length} {Encoding.ASCII.GetString(buffer)}");
            }
            catch (Exception ex)
            {
                Logger.Error("UdpListener Send " + ex.Message);
            }
        }
    }
}
