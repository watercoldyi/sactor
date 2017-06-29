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
                byte[] head = new byte[4];
                if (buf.Peek(head, 0, 4) == 4)
                {
                    int len = BitConverter.ToInt32(head, 0);
                    if (buf.Length() >= 4 + len)
                    {
                        byte[] data = new byte[len];
                        buf.Read(head, 0, 4);
                        buf.Read(data, 0, len);
                        return Encoding.Default.GetString(data);
                    }
                };
                return null;
            }
            );
            Log("connec from " + sock.GetIP());
        }

        void response(string cmd)
        {
            byte[] data = Encoding.Default.GetBytes(cmd);
            //head is 4byte
            var head = BitConverter.GetBytes(data.Length);
            byte[] pg = new byte[head.Length + data.Length];
            head.CopyTo(pg, 0);
            data.CopyTo(pg, 4);
            _rep.Send(pg);
        }

        async void Start()
        {
            while (true)
            {
                var s = await _rep.Recv<string>();
                if (s == null) { break; }
                string[] req = s.Split(' ');
                if (req.Length < 3 || req[0] != "add")
                {
                    response("invalid cmd");
                }
                double a = 0,b = 0;
                if (!double.TryParse(req[1], out a) || !double.TryParse(req[2],out b))
                {
                    response("argument error");
                }
                response((a + b).ToString());
            }
            Exit();
        }
    }
}
