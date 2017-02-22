using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SActor
{
    internal class SActTimer
    {
        class TimerEx 
        {
            Timer tm;
            SActActor act;
            uint session;

            public TimerEx(int ms,SActActor act,uint session)
            {
                this.act = act;
                this.session = session;
                tm = new Timer(OnTime, null, Timeout.Infinite, Timeout.Infinite);
                tm.Change(ms, Timeout.Infinite);
            }

            void OnTime(object p)
            {
                SActor.Send(null, act, (int)SActMessageType.Timer, session, null);
                tm.Dispose();
            }
        }

        public static void TimeOut(int ms,uint session, SActActor act)
        {
            new TimerEx(ms, act,session);
        }
    }
}
