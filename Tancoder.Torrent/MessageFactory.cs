#if !DISABLE_DHT
//
// MessageFactory.cs
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

using Tancoder.Torrent.BEncoding;

namespace Tancoder.Torrent.Dht.Messages
{
    public delegate DhtMessage Creator(BEncodedDictionary dictionary);
    public delegate DhtMessage ResponseCreator(BEncodedDictionary dictionary, QueryMessage message);
    
    public static class MessageFactory
    {
        private static readonly string QueryNameKey = "q";
        private static BEncodedString MessageTypeKey = "y";
        private static BEncodedString TransactionIdKey = "t";

        public static int RegisteredMessages
        {
            get { return messages.Count; }
        }

        static MessageFactory()
        {
            queryDecoders.Add("announce_peer", delegate(BEncodedDictionary d) { return new AnnouncePeer(d); });
            queryDecoders.Add("find_node",     delegate(BEncodedDictionary d) { return new FindNode(d); });
            queryDecoders.Add("get_peers",     delegate(BEncodedDictionary d) { return new GetPeers(d); });
            queryDecoders.Add("ping",          delegate(BEncodedDictionary d) { return new Ping(d); });
        }

        private static Dictionary<BEncodedValue, QueryMessage> messages = new Dictionary<BEncodedValue, QueryMessage>();
        private static Dictionary<BEncodedString, Creator> queryDecoders = new Dictionary<BEncodedString, Creator>();

        public static bool IsRegistered(BEncodedValue transactionId)
        {
            lock (messages)
            {
                return messages.ContainsKey(transactionId);
            }
        }

        public static void RegisterSend(QueryMessage message)
        {
            lock(messages)
            {
                messages.Add(message.TransactionId, message);
            }
        }

        public static bool UnregisterSend(QueryMessage message)
        {
            lock (messages)
            {
                return messages.Remove(message.TransactionId);
            }
        }

        public static DhtMessage DecodeMessage(BEncodedDictionary dictionary)
        {
            DhtMessage message;
            string error;

            if (!TryDecodeMessage(dictionary, out message, out error))
                throw new MessageException(ErrorCode.GenericError, error);

            return message;
        }

        public static bool TryDecodeMessage(BEncodedDictionary dictionary, out DhtMessage message)
        {
            string error;
            return TryDecodeMessage(dictionary, out message, out error);
        }

        public static bool TryDecodeMessage(BEncodedDictionary dictionary, out DhtMessage message, out string error)
        {
            message = null;
            error = null;

            if (dictionary[MessageTypeKey].Equals(QueryMessage.QueryType))
            {
                if (!queryDecoders.ContainsKey((BEncodedString)dictionary[QueryNameKey]))
                {
                    error = ((BEncodedString)dictionary[QueryNameKey]).Text;
                    return false;
                }
                message = queryDecoders[(BEncodedString)dictionary[QueryNameKey]](dictionary);
            }
            else if (dictionary[MessageTypeKey].Equals(ErrorMessage.ErrorType))
            {
                message = new ErrorMessage(dictionary);
            }
            else
            {
                QueryMessage query;
                BEncodedString key = (BEncodedString)dictionary[TransactionIdKey];
                if (messages.TryGetValue(key, out query))
                {
                    messages.Remove(key);
                    try
                    {
                        message = query.ResponseCreator(dictionary, query);
                    }
                    catch
                    {
                        error = "Response dictionary was invalid";
                    }
                }
                else
                {
                    error = "Response had bad transaction ID";
                }
            }

            return error == null && message != null;
        }
        public static bool TryNoTraceDecodeMessage(BEncodedDictionary dictionary, out DhtMessage message, out string error)
        {
            message = null;
            error = null;

            if (dictionary[MessageTypeKey].Equals(QueryMessage.QueryType))
            {
                if (!queryDecoders.ContainsKey((BEncodedString)dictionary[QueryNameKey]))
                {
                    error = ((BEncodedString)dictionary[QueryNameKey]).Text;
                    return false;
                }
                message = queryDecoders[(BEncodedString)dictionary[QueryNameKey]](dictionary);
            }
            else if (dictionary[MessageTypeKey].Equals(ErrorMessage.ErrorType))
            {
                message = new ErrorMessage(dictionary);
            }
            else
            {
                QueryMessage query = new NoTraceMessage();
                BEncodedString key = (BEncodedString)dictionary[TransactionIdKey];
                try
                {
                    message = query.ResponseCreator(dictionary, query);

                    //BEncodedValue q;
                    //if (message == null && dictionary.TryGetValue("q", out q))
                    //{
                    //    error = q.ToString();
                    //}
                    if (message == null && dictionary.ContainsKey("q"))
                    {
                        dictionary[MessageTypeKey] = QueryMessage.QueryType;
                        TryNoTraceDecodeMessage(dictionary, out message, out error);
                    }
                }
                catch(Exception ex)
                {
                    error = ex.ToString();
                }
            }

            return error == null && message != null;
        }
        public static bool TryNoTraceDecodeMessage(BEncodedDictionary dictionary, out DhtMessage message)
        {
            string error;
            return TryNoTraceDecodeMessage(dictionary, out message, out error);
        }
    }
}
#endif