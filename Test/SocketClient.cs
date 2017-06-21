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
                if (buf.Length() > 0)
                {
                    byte[] s = new byte[buf.Length()];
                    buf.Read(s, 0, s.Length);
                    return Encoding.ASCII.GetString(s);
                }
                return null;
            });
            _req.Connect("127.0.0.1", 8000);
            this.Fork(Req);
        }

        async void Req()
        {
            while (true)
            {
                _req.Send(Encoding.ASCII.GetBytes("hello world"));
                var s = await _req.Recv<string>();
                Log("recv:" + s);
            }
        }
    }
}
