using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SActor
{
    class InvokeAdapter : SActActor
    {
        public class Waiter
        {
            public object Result { get; set; }
            public bool IsError{ get; set; }
            public string Cmd{ get; set; }
            public object[] P{ get; set; }
            public SActActor Act{ get; set; }
            public Task T{ get; set; }
        }
        protected override void Init(object param)
        {
            SActor.Name(nameof(InvokeAdapter), this);
        }

        public async void Invoke(Waiter wait)
        {
            try
            {
                object[] args = new object[1+ (wait.P  == null ? 0 : wait.P.Length)];
                args[0] = wait.Cmd;
                if(wait.P != null)
                {
                    wait.P.CopyTo(args, 1);
                }
                var r = await Call2<object>(wait.Act,args);
                wait.Result = r;
                wait.IsError = false;
            }
            catch (Exception)
            {
                wait.IsError = true;
            }
            wait.T.Start();
        }
    }
}
