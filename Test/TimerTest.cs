using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class TimerTest:SActor.SActActor
    {

        private void OnTime()
        {
            Log(DateTime.Now.ToString());
            TimeOut(1000, OnTime);
        }
        protected override void Init(object param)
        {
            TimeOut(1000, OnTime);
        }
    }
}
