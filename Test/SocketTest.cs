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
            SActor.SActSocket server = SocketListen(8000, 1024);
            SocketStart(server, (c, ip) => {
                SActor.SActor.Launch<EchoAgent>(c);
            });
            Log("listen 8000");
            //SActor.SActor.Launch<SocketClient>();
        }

    }
}
