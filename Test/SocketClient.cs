using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SActor;
using System.Threading;
namespace Test
{
    class SocketClient:SActActor
    {
        SActSocket _socket;
        int _count;
        protected override void Init(object param)
        {
            this.Fork(Req);
            TimeOut(1000, OnTime);
        }

        void request(string cmd)
        {
            byte[] data = Encoding.Default.GetBytes(cmd);
            //head is 4byte
            var head = BitConverter.GetBytes(data.Length);
            byte[] pg = new byte[head.Length + data.Length];
            head.CopyTo(pg, 0);
            data.CopyTo(pg, 4);
            _socket.Send(pg,0,pg.Length);
        }

        async Task<string> Recv()
        {
            byte[] head = await SocketRead(_socket, 4);
            if (head == null) { return null; }
            int len = BitConverter.ToInt32(head, 0);
            byte[] data = await SocketRead(_socket, len);
            if (data == null) { return null; }
            var s = Encoding.Default.GetString(data);
            return s;
        }

        void OnTime()
        {
            Log("qps:"+_count);
            _count = 0;
            TimeOut(1000, OnTime);
        }
  

        async void Req()
        {
            _socket = await SocketConnect("127.0.0.1", 8000);
            if (_socket == null)
            {
                Log("connect 127.0.0.1:8000 fail");
            }
            Log("connect 127.0.0.1:8000 ok");
            while (true)
            {
                request("add 100 100");
                var s = await Recv();
                //Log("100+100 = " + s);
                _count++;
            }
        }
    }
}
