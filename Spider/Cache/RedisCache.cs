using Spider.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Cache
{
    public class RedisCache : ICache
    {
        //TODO
        public RedisCache()
        {
        }

        public bool ContainsKey(string key)
        {
            throw new NotImplementedException();
        }

        public int Count()
        {
            throw new NotImplementedException();
        }

        public object Get(string key)
        {
            throw new NotImplementedException();
        }

        public void Set(string key, object val)
        {
            throw new NotImplementedException();
        }
    }
}
