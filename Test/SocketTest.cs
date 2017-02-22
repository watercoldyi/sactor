using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class SocketTest:SActor.SActActor
    {
        protected override void Init(object param)
        {
            SActor.SActSocket server = SActor.SActSocket.Listen(8000, 1024, this);
            server.Start();
            Log("listen 8000");
        }

        protected override void ProcessSocketMessage(SActor.SActSocketMessage msg)
        {
            switch (msg.Type)
            {
                case SActor.SActSocketMessageType.Accept:
                    {
                        Log("connect from "+msg.AcceptSocket.GetIP());
                        msg.AcceptSocket.Bind(this);
                        msg.AcceptSocket.Start();
                    }
                    break;
                case SActor.SActSocketMessageType.Close:
                    {
                        Log("client close " + msg.Socket.GetIP());
                    }
                    break;
                case SActor.SActSocketMessageType.Data:
                    {
                        msg.Socket.Send(msg.Data,0,msg.Size);
                    }
                    break;
                case SActor.SActSocketMessageType.Error:
                    break;
                case SActor.SActSocketMessageType.Open:
                    break;
            }
        }
    }
}
