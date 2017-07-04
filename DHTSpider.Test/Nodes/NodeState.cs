using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHTSpider.Test.Nodes
{
    public enum NodeState
    {
        /// <summary>
        /// 未知
        /// </summary>
        Unknown,
        /// <summary>
        /// 活跃
        /// </summary>
        Good,
        /// <summary>
        /// 可疑不确定
        /// </summary>
        Questionable,
        /// <summary>
        /// 不活跃
        /// </summary>
        Bad
    }
}
