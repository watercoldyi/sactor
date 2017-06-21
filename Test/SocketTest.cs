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
            SActor.SActor.Launch<SocketClient>();
        }

        protected override void ProcessSocketMessage(SActor.SActSocketMessage msg)
        {
            switch (msg.Type)
            {
                case SActor.SActSocketMessageType.Accept:
                    {
                        SActor.SActor.Launch<EchoAgent>(msg.AcceptSocket);
                    }
                    break;
                case SActor.SActSocketMessageType.Close:
                case SActor.SActSocketMessageType.Error:
                    break;
            }
        }
    }
}
