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
                await Sleep(1000);
                Log("sleep");
            });
        }
    }
}
