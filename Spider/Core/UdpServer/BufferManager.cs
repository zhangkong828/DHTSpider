using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Core.UdpServer
{
    internal sealed class BufferManager
    {
        private Byte[] buffer;
        private Int32 bufferSize;
        private Int32 numSize;
        private Int32 currentIndex;
        private Stack<Int32> freeIndexPool;

        public BufferManager(Int32 numsize, Int32 buffersize)
        {
            this.numSize = numsize;
            this.bufferSize = buffersize;

        }

        public void Inint()
        {
            buffer = new byte[numSize];
            freeIndexPool = new Stack<int>(numSize / bufferSize);
        }

        internal void FreeBuffer(SocketAsyncEventArgs args)
        {
            freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);

        }

        internal Boolean SetBuffer(SocketAsyncEventArgs args)
        {
            if (this.freeIndexPool.Count > 0)
            {
                args.SetBuffer(this.buffer, this.freeIndexPool.Pop(), this.bufferSize);
            }
            else
            {
                if ((this.numSize - this.bufferSize) < this.currentIndex)
                {
                    return false;
                }
                args.SetBuffer(this.buffer, this.currentIndex, this.bufferSize);
                this.currentIndex += this.bufferSize;
            }
            return true;
        }
    }
}
