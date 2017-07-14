using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Cache
{
    public class MemoryCache : ICache
    {
        public ConcurrentDictionary<string, object> _cache = null;
        public MemoryCache()
        {
            _cache = new ConcurrentDictionary<string, object>();
        }

        public object Get(string key)
        {
            object val = null;
            _cache.TryGetValue(key, out val);
            return val;
        }

        public void Set(string key, object val)
        {
            _cache.TryAdd(key, val);
        }

        public bool ContainsKey(string key)
        {
            return _cache.ContainsKey(key);
        }

        public int Count()
        {
            return _cache.Count;
        }
    }
}
