using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SActor;
using SActor.Channel;
namespace Test
{
    class EchoAgent:SActActor
    {
        RepCN _rep;
        protected override void Init(object param)
        {
            SActSocket sock = param as SActSocket;
            Fork(Start);
            _rep = new RepCN(this, param as SActSocket, (RingBuffer buf) =>
            {
                if (buf.Length() > 0)
                {
                    byte[] s = new byte[buf.Length()];
                    buf.Read(s, 0, s.Length);
                    return Encoding.ASCII.GetString(s);
                }
                return null;
            }
            );
            Log("connec from " + sock.GetIP());
        }

        async void Start()
        {
            while (true)
            {
                var s = await _rep.Recv<string>();
                if (s == null) { break; }
                if (!_rep.Send(Encoding.ASCII.GetBytes(s))) { break; }
                
            }
            Exit();
        }
    }
}
