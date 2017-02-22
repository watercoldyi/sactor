using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{

    class Adder : SActor.SActActor
    {

        protected override void Init(object param)
        {
            
        }

        public void Add(Action<object> reply,int a, int b)
        {
            reply(a + b);
        }
    }
    class InvokeTest:SActor.SActActor
    {

        protected override void Init(object param)
        {
            SActor.SActActor add = SActor.SActor.Launch<Adder>();
            Call(add, (ok, r) => {
                Log("10+10="+r);
            }, "Add", 10, 10);
        }
    }
}
