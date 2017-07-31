using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spider.Store
{
    public interface IStore
    {
        bool Insert(object obj);
    }
}
