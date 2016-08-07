using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tancoder.Torrent;

namespace DHTSpider.Core.Store.MongoDB
{
    public class MongoFilter: IMetaDataFilter
    {
        private SeedCargo cargo;

        public MongoFilter(SeedCargo cargo)
        {
            this.cargo = cargo;
        }

        public SeedCargo Cargo
        {
            get
            {
                return cargo;
            }

            set
            {
                cargo = value;
            }
        }

        public bool Ignore(InfoHash metadata)
        {
            var result = cargo.ExsistHash(metadata.Hash);
            if (result)
                cargo.IncHot(metadata.Hash);
            return result;
        }
    }
}
