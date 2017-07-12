using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DHTSpider.Cache
{
    public interface ICache<TKey, TValue>
    {
        bool Contains(TKey key);

        TValue Get(TKey key);

        bool Set(TKey key, TValue val);
    }
}
