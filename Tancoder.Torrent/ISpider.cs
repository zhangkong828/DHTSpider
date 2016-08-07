using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Tancoder.Torrent.Dht;

namespace Tancoder.Torrent
{
    public interface ISpider
    {
        event NewMetadataEvent NewMetadata;
        IMetaDataFilter Filter { get; set; }

        KeyValuePair<InfoHash, IPEndPoint> Pop();
    }

    public class NewMetadataEventArgs : EventArgs
    {
        public InfoHash Metadata { get; private set; }
        public IPEndPoint Owner { get; private set; }

        public NewMetadataEventArgs(InfoHash metadata, IPEndPoint endpoint)
        {
            Metadata = metadata;
            Owner = endpoint;
        }
    }

    public delegate void NewMetadataEvent(object sender, NewMetadataEventArgs e);
}