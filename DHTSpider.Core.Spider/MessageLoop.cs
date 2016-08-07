using System;
using System.Collections.Generic;
using Tancoder.Torrent.Dht.Messages;
using System.Threading;
using System.Net;
using Tancoder.Torrent.BEncoding;
using Tancoder.Torrent.Dht.Listeners;
using Tancoder.Torrent.Common;
using System.Diagnostics;
using Tancoder.Torrent;
using Tancoder.Torrent.Dht;

namespace DHTSpider.Core.Spider
{
    public class MessageLoop
    {
        private struct SendDetails
        {
            public SendDetails(IPEndPoint destination, DhtMessage message)
            {
                Destination = destination;
                Message = message;
                SentAt = DateTime.MinValue;
            }
            public IPEndPoint Destination;
            public DhtMessage Message;
            public DateTime SentAt;
        }

        public event EventHandler<MessageEventArgs> SentMessage;
        public event EventHandler<MessageEventArgs> ReceivedMessage;
        public event EventHandler<string> OnError;

        IDhtEngine engine;
        DateTime lastSent;
        DhtListener listener;
        private object locker = new object();
        Queue<SendDetails> sendQueue = new Queue<SendDetails>();
        Queue<KeyValuePair<IPEndPoint, DhtMessage>> receiveQueue = new Queue<KeyValuePair<IPEndPoint, DhtMessage>>();
        Thread handleThread;

        private bool CanSend
        {
            get { return sendQueue.Count > 0 && (DateTime.Now - lastSent) > TimeSpan.FromMilliseconds(5); }
        }

        public int GetWaitSendCount()
        {
            lock (locker)
            {
                return sendQueue.Count;
            }
        }
        public int GetWaitReceiveCount()
        {
            lock (locker)
            {
                return receiveQueue.Count;
            }
        }

        public MessageLoop(IDhtEngine engine, DhtListener listener)
        {
            this.engine = engine;
            this.listener = listener;
            listener.MessageReceived += new MessageReceived(OnMessageReceived);
            handleThread = new Thread(new ThreadStart(delegate
            {
                while (true)
                {
                    if (engine.Disposed)
                        return;
                    try
                    {
                        SendMessage();
                        ReceiveMessage();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error in DHT main loop:");
                        Debug.WriteLine(ex);
                    }

                    Thread.Sleep(3);
                }
            }));
        }

        void OnMessageReceived(byte[] buffer, IPEndPoint endpoint)
        {
            lock (locker)
            {
                // I should check the IP address matches as well as the transaction id
                // FIXME: This should throw an exception if the message doesn't exist, we need to handle this
                // and return an error message (if that's what the spec allows)
                try
                {
                    DhtMessage message;
                    string error;
                    if (MessageFactory.TryNoTraceDecodeMessage((BEncodedDictionary)BEncodedValue.Decode(buffer, 0, buffer.Length, false), out message, out error))
                    {
                        if (message is FindNode && receiveQueue.Count > 200)
                        {
                            RaiseOnError("Dump excess msg");
                            return;
                        }
                        receiveQueue.Enqueue(new KeyValuePair<IPEndPoint, DhtMessage>(endpoint, message));
                    }
                    else
                        RaiseOnError(error ?? "Bad Message");
                }
                catch (Exception ex)
                {
                    RaiseOnError(string.Format("OMGZERS! {0}", ex));
                    Debug.WriteLine(ex);
                    //throw new Exception("IP:" + endpoint.Address.ToString() + "bad transaction:" + e.Message);
                }
            }
        }

        private void RaiseMessageSent(IPEndPoint endpoint, DhtMessage query)
        {
            EventHandler<MessageEventArgs> h = SentMessage;
            if (h != null)
                h(this, new MessageEventArgs(endpoint, query));
        }
        private void RaiseOnError(string ex)
        {
            EventHandler<string> h = OnError;
            if (h != null)
                h(this, ex);
        }

        private void SendMessage()
        {
            SendDetails? send = null;
            if (CanSend)
                send = sendQueue.Dequeue();

            if (send != null)
            {
                SendMessage(send.Value.Message, send.Value.Destination);
                SendDetails details = send.Value;
                details.SentAt = DateTime.UtcNow;
            }

        }

        public void Start()
        {
            if (listener.Status != ListenerStatus.Listening)
            {
                listener.Start();
                handleThread.Start();
            }
        }

        public void Stop()
        {
            if (listener.Status != ListenerStatus.NotListening)
            {
                listener.Stop();
                handleThread.Start();
            }
        }

        private void ReceiveMessage()
        {
            if (receiveQueue.Count == 0)
                return;

            KeyValuePair<IPEndPoint, DhtMessage> receive;
            lock (locker)
            {
                receive = receiveQueue.Dequeue();
            }
            DhtMessage m = receive.Value;
            IPEndPoint source = receive.Key;

            if (m == null || source == null)
            {
                return;
            }
            try
            {
                if (m is QueryMessage)
                    m.Handle(engine, new Node(m.Id, source));
                else if (m is ErrorMessage)
                    RaiseOnError(((ErrorMessage)m).ErrorList.ToString());
                RaiseMessageReceived(source, m);
            }
            catch (Exception ex)
            {
                RaiseOnError(string.Format("Handle Error for message: {0}", ex));
                Debug.WriteLine(ex);
            }
        }

        private void RaiseMessageReceived(IPEndPoint endPoint, DhtMessage message)
        {
            EventHandler<MessageEventArgs> h = ReceivedMessage;
            if (h != null)
                h(this, new MessageEventArgs(endPoint, message));
        }

        private void SendMessage(DhtMessage message, IPEndPoint endpoint)
        {
            lastSent = DateTime.Now;
            byte[] buffer = message.Encode();
            listener.Send(buffer, endpoint);
            RaiseMessageSent(endpoint, message);
        }

        public void EnqueueSend(DhtMessage message, IPEndPoint endpoint)
        {
            lock (locker)
            {
                if (message.TransactionId == null)
                {
                    if (message is ResponseMessage)
                        throw new ArgumentException("Message must have a transaction id");
                    //do
                    //{
                    message.TransactionId = TransactionId.NextId();
                    //} while (MessageFactory.IsRegistered(message.TransactionId));
                }

                // We need to be able to cancel a query message if we time out waiting for a response
                //if (message is QueryMessage)
                //    MessageFactory.RegisterSend((QueryMessage)message);

                sendQueue.Enqueue(new SendDetails(endpoint, message));
            }
        }

        public void EnqueueSend(DhtMessage message, Node node)
        {
            EnqueueSend(message, node.EndPoint);
        }
    }
}
