using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class SleepTest:SActor.SActActor
    {
        protected  override void Init(object param)
        {
            Fork(async () => {
                Log("sleeping");
                await Sleep(1000);
                Log("sleeped");
            });
        }
    }
}
