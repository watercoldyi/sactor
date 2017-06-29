using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SActor;
using SActor.Channel;
namespace Test
{
    class SocketClient:SActActor
    {
        ReqCN _req;
        protected override void Init(object param)
        {
            _req = new ReqCN(this, (RingBuffer buf) => {
                byte[] head = new byte[4];
                if (buf.Peek(head, 0, 4) == 4)
                {
                    int len = BitConverter.ToInt32(head,0);
                    if (buf.Length() >= 4 + len)
                    {
                        byte[] data = new byte[len];
                        buf.Read(head, 0, 4);
                        buf.Read(data, 0, len);
                        return Encoding.Default.GetString(data);
                    }
                }
                return null;
            });
            _req.Connect("127.0.0.1", 8000);
            this.Fork(Req);
        }

        void request(string cmd)
        {
            byte[] data = Encoding.Default.GetBytes(cmd);
            //head is 4byte
            var head = BitConverter.GetBytes(data.Length);
            byte[] pg = new byte[head.Length + data.Length];
            head.CopyTo(pg, 0);
            data.CopyTo(pg, 4);
            _req.Send(pg);
        }

        async void Req()
        {
            while (true)
            {
                request("add 100 100");
                var s = await _req.Recv<string>();
                Log("100+100 = " + s);
            }
        }
    }
}
