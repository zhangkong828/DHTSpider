using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Cache
{
    public interface ICache
    {
        bool ContainsKey(string key);

        object Get(string key);

        void Set(string key, object val);

        int Count();
    }
}
