using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SActor;
namespace Test
{
    class EchoAgent:SActActor
    {
        SActSocket _socket;
        protected override void Init(object param)
        {
            SActSocket sock = param as SActSocket;
            SocketBind(sock);
            SocketStart(sock);
            Fork(Start);
            _socket = sock;
            Log("connec from " + sock.GetIP());
        }

        async Task<string> GetCmd()
        {
            byte[] head = await SocketRead(_socket, 4);
            if (head == null) { return null; }
            int len = BitConverter.ToInt32(head, 0);
            byte[] data = await SocketRead(_socket, len);
            if (data == null) { return null; }
            var s = Encoding.Default.GetString(data);
            return s;
        }


        void response(string cmd)
        {
            byte[] data = Encoding.Default.GetBytes(cmd);
            //head is 4byte
            var head = BitConverter.GetBytes(data.Length);
            byte[] pg = new byte[head.Length + data.Length];
            head.CopyTo(pg, 0);
            data.CopyTo(pg, 4);
            _socket.Send(pg,0,pg.Length);
        }

        async void Start()
        {
            while (true)
            {
                try
                {
                    var s = await SocketRead(_socket, 1000);
                    if (s == null) { break; }
                    _socket.Send(s, 0, s.Length);
                }
                catch
                {
                    break;
                }
            }
            Exit();
        }
    }
}
