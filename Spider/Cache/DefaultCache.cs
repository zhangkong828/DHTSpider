using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Cache
{
    public class DefaultCache : ICache
    {
        public ConcurrentDictionary<string, object> _cache = null;
        public DefaultCache()
        {
            _cache = new ConcurrentDictionary<string, object>();
        }

        public object Get(string key)
        {
            return MemoryCache.Default.Get(key);
            //object val = null;
            //_cache.TryGetValue(key, out val);
            //return val;
        }

        public void Set(string key, object val)
        {
            MemoryCache.Default.Set(key, val, new CacheItemPolicy());
            //_cache.TryAdd(key, val);
        }

        public bool ContainsKey(string key)
        {
            return MemoryCache.Default.Contains(key);
            //return _cache.ContainsKey(key);
        }

        public int Count()
        {
            return MemoryCache.Default.Count();
            //return _cache.Count;
        }
    }
}
