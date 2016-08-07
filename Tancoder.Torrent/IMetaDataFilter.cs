using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tancoder.Torrent
{
    public interface IMetaDataFilter
    {
        bool Ignore(InfoHash metadata);
    }
}