#if !DISABLE_DHT
//
// UdpListener.cs
//
// Authors:
//   Alan McGovern <alan.mcgovern@gmail.com>
//
// Copyright (C) 2008 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using Tancoder.Torrent.BEncoding;
using Tancoder.Torrent.Client;
using Tancoder.Torrent.Common;
using System.Diagnostics;

namespace Tancoder.Torrent
{
    public abstract class UdpListener : Listener
    {

        private UdpClient client;

        protected UdpListener(IPEndPoint endpoint)
            : base(endpoint)
        {

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
            //catch (ObjectDisposedException ex)
            //{
            //    // Ignore, we're finished!
            //    throw new Exception($"UdpListener ObjectDisposedException: {ex}");
            //}
            //catch (SocketException ex)
            //{
            //    // If the destination computer closes the connection
            //    // we get error code 10054. We need to keep receiving on
            //    // the socket until we clear all the error states
            //    if (ex.ErrorCode == 10054)
            //    {
            //        while (true)
            //        {
            //            try
            //            {
            //                client.BeginReceive(EndReceive, null);
            //                return;
            //            }
            //            catch (ObjectDisposedException oe)
            //            {
            //                throw new Exception($"UdpListener ObjectDisposedException: {oe}");
            //            }
            //            catch (SocketException e)
            //            {
            //                throw new Exception($"UdpListener SocketException: {e}");
            //                //if (e.ErrorCode != 10054)
            //                //    return;
            //            }
            //        }
            //    }
            //    else if (ex.ErrorCode == 10052)
            //    {

            //    }
            //    else
            //    {
            //        throw new Exception($"UdpListener SocketException: {ex}");
            //    }
            //}
            catch (Exception ex)
            {
                throw new Exception($"UdpListener Exception: {ex}");
            }
        }

        protected abstract void OnMessageReceived(byte[] buffer, IPEndPoint endpoint);

        public virtual void Send(byte[] buffer, IPEndPoint endpoint)
        {
            try
            {
                if (endpoint.Address != IPAddress.Any)
                    client.Send(buffer, buffer.Length, endpoint);
            }
            catch (Exception ex)
            {
                throw new Exception($"UdpListener could not send message: {ex}");
            }
        }

        public override void Start()
        {
            try
            {
                client = new UdpClient(Endpoint);
                {
                    const uint IOC_IN = 0x80000000;
                    int IOC_VENDOR = 0x18000000;
                    int SIO_UDP_CONNRESET = (int)(IOC_IN | IOC_VENDOR | 12);
                    client.Client.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, new byte[4]);
                }
                client.BeginReceive(EndReceive, null);
                RaiseStatusChanged(ListenerStatus.Listening);
            }
            catch (SocketException)
            {
                RaiseStatusChanged(ListenerStatus.PortNotFree);
            }
            catch (ObjectDisposedException)
            {
                // Do Nothing
            }
        }

        public override void Stop()
        {
            try
            {
                client.Close();
            }
            catch
            {
                // FIXME: Not needed
            }
        }
    }
}
#endif