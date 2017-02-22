using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Collections.Concurrent;
namespace SActor
{
    internal class SActSAEAPool
    {
        static ConcurrentQueue<SocketAsyncEventArgs> _pool = new ConcurrentQueue<SocketAsyncEventArgs>();

       public static void Init(uint count)
        {
            if (count == 0)
            {
                count = 64;
            }
            for (uint i = 0; i < count; i++)
            {
                _pool.Enqueue(new SocketAsyncEventArgs());
            }
        }

       public static void Push(SocketAsyncEventArgs obj)
       {
           _pool.Enqueue(obj);
       }

       public static SocketAsyncEventArgs Pop()
       {
           SocketAsyncEventArgs obj;
           if (_pool.TryDequeue(out obj))
           {
               return obj;
           }
           else
           {
               return new SocketAsyncEventArgs();
           }
       }


    }
}
