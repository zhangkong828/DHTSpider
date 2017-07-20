using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Core.IoSocket
{
    /// <summary>
    /// 与每个客户Socket相关联，进行Send和Receive投递时所需要的参数
    /// </summary>
    internal sealed class IoContextPool
    {
        List<SocketAsyncEventArgs> pool;        //为每一个Socket客户端分配一个SocketAsyncEventArgs，用一个List管理，在程序启动时建立。
        Int32 capacity;                         //pool对象池的容量
        Int32 boundary;                         //已分配和未分配对象的边界，大的是已经分配的，小的是未分配的

        internal IoContextPool(Int32 capacity)
        {
            this.pool = new List<SocketAsyncEventArgs>(capacity);
            this.boundary = 0;
            this.capacity = capacity;
        }

        /// <summary>
        /// 往pool对象池中增加新建立的对象，因为这个程序在启动时会建立好所有对象，
        /// 故这个方法只在初始化时会被调用,因此，没有加锁。
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        internal bool Add(SocketAsyncEventArgs arg)
        {
            if (arg != null && pool.Count < capacity)
            {
                pool.Add(arg);
                boundary++;
                return true;
            }
            else
                return false;
        }
        internal Int32 Count
        {
            get { return this.pool.Count; }
        }
        /// <summary>
        /// 取出集合中指定对象，内部使用
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        //internal SocketAsyncEventArgs Get(int index)
        //{
        //    if (index >= 0 && index < capacity)
        //        return pool[index];
        //    else
        //        return null;
        //}

        /// <summary>
        /// 从对象池中取出一个对象，交给一个socket来进行投递请求操作
        /// </summary>
        /// <returns></returns>
        internal SocketAsyncEventArgs Pop()
        {
            lock (this.pool)
            {
                if (boundary > 0)
                {
                    --boundary;
                    return pool[boundary];
                }
                else
                    return null;
            }
        }

        /// <summary>
        /// 一个socket客户断开，与其相关的IoContext被释放，重新投入Pool中，备用。
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        internal bool Push(SocketAsyncEventArgs arg)
        {
            if (arg != null)
            {
                lock (this.pool)
                {
                    int index = this.pool.IndexOf(arg, boundary);  //找出被断开的客户,此处一定能查到，因此index不可能为-1，必定要大于0。
                    if (index == boundary)         //正好是边界元素
                        boundary++;
                    else
                    {
                        this.pool[index] = this.pool[boundary];     //将断开客户移到边界上，边界右移
                        this.pool[boundary++] = arg;
                    }
                }
                return true;
            }
            else
                return false;
        }
    }
}
