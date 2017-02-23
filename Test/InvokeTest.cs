using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{

    class Adder : SActor.SActActor
    {

        protected override void Init(object param)
        {
        }

        public void Add(Response reply,int a, int b)
        {
            reply(true,a+b);
        }

        public void Say()
        {
            Console.WriteLine("Hi my is Adder");
        }
    }
    class InvokeTest:SActor.SActActor
    {

        protected async override void Init(object param)
        {
            try
            {
                SActor.SActActor add = SActor.SActor.Launch<Adder>();
                object r = await Call<object>(add, "Add", 10, 10);
                Log("10+10=" + r);
                Send(add, "Say");
            }
            catch (Exception e)
            {
                Log(e.Message);
            }
        }
    }
}
